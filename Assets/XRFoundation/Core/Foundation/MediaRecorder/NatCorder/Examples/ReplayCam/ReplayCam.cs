/* 
*   NatCorder
*   Copyright (c) 2020 Yusuf Olokoba
*/

namespace NatSuite.Examples {

    using UnityEngine;
    using UnityEngine.Rendering;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using Recorders;
    using Recorders.Clocks;
    using Recorders.Inputs;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine.UI;
    using UnityEngine.Events;
    using Singray.Foundation;

    public class ReplayCam : SingletonMonoBehaviour<ReplayCam> {


        [HideInInspector]
        internal int videoWidth = 1920;
        internal int videoHeight = 1080;
        public bool recordMicrophone;

        // Whether to record an audio track at all. Turning this off skips the AudioInput hook
        // (which copies every audio buffer on the audio thread and calls into the native encoder)
        // and the muxer's audio interleaving, so it's worth disabling when the clip doesn't need
        // sound. Defaults to on to preserve the existing behaviour.
        public bool recordAudio = true;

        private bool isRecording;
        public bool IsRecording {
        get { return isRecording; }
        }


        private IMediaRecorder recorder;
        private AudioInput audioInput;
        private AudioSource microphoneSource;
        private int countTime = 0;

        // Frame capture: reads the MR compositor's already-rendered CameraRenderTexture
        // instead of forcing a second scene render (see CameraInput.OnFrame, which this
        // replaces for the recording feature specifically).
        private RenderTexture convertedTexture;
        private Texture2D readbackBuffer;
        private Coroutine captureRoutine;
        private RealtimeClock recordClock;
        private int recordFrameRate;

        // --- Producer / consumer decoupling (encoder commits never block the main thread) ---
        // The main thread (producer) only keeps the newest rendered frame in a shared buffer,
        // while a dedicated background thread (consumer) commits it to the encoder as soon as it
        // arrives. This keeps the native CommitFrame call (GCHandle pin + JNI/P-Invoke) off the
        // main thread's frame budget. Committed rate follows whatever the app/producer actually
        // supplies — no frame duplication/frame-hold, so it can drop below the target frame rate
        // if the app can't keep up, but every committed frame is a genuinely new one with a real
        // timestamp (correct A/V pacing is preserved either way).
        //
        // Triple buffer: producer fills writeBuffer, hands it off via sharedBuffer under a lock,
        // consumer commits from readBuffer. The lock only guards a reference swap (O(1)), never a
        // copy or a native commit, so neither thread blocks the other for any real time.
        private byte[] writeBuffer;
        private byte[] sharedBuffer;
        private byte[] readBuffer;
        private readonly object bufferLock = new object();
        private volatile bool hasNewSharedFrame;

        private Thread consumerThread;
        private volatile bool consumerRunning;
        private AutoResetEvent frameAvailableEvent; // signaled by the producer on every new frame
        private long producerIntervalNs;  // sync ReadPixels fallback cap only (see below)
        private long lastProduceTicks;

        // Async path back-pressure: instead of guessing a fixed production interval, only start a
        // new Blit+AsyncGPUReadback once fewer than MaxReadbackInFlight requests are outstanding.
        // This tracks whatever rate the GPU readback pipeline can actually sustain — capturing
        // effectively every render frame when the app is slow (closing the gap between "app fps"
        // and "fresh fps" as much as physically possible) without piling up unbounded in-flight
        // requests when the app is fast. Allowing more than 1 in flight is an experiment: if a
        // single readback's round trip is dominated by queueing behind other GPU work rather than
        // by the copy itself, letting the GPU pipeline 2 requests back-to-back should raise "fresh"
        // fps even though per-request latency stays the same. Safe to reuse convertedTexture as the
        // source across overlapping requests — Unity inserts the readback's GPU-side copy at the
        // point Request() was called, so a later Blit into the same texture cannot corrupt an
        // earlier still-in-flight request's data. The synchronous ReadPixels fallback (no async
        // readback support) has no "in flight" concept, so it keeps the old fixed-interval throttle
        // to avoid stalling on every single render frame.
        private const int MaxReadbackInFlight = 3;
        private int readbackInFlightCount;

        // Diagnostics: measure how long each AsyncGPUReadback actually takes end-to-end (Request ->
        // completion callback), since the in-flight cap above means that latency (divided by how
        // many can overlap) is what caps "fresh" fps independent of app render fps. Request
        // timestamps are queued in issuance order and matched to completions in that same order —
        // valid because overlapping requests against the same GPU queue complete in submission
        // order. Both the write (in CaptureFrames) and read (in OnReadbackComplete) happen on the
        // main thread (AsyncGPUReadback callbacks fire from the Unity player loop), so no locking
        // needed.
        private readonly Queue<long> pendingReadbackRequestTicks = new Queue<long>();
        private long readbackLatencySumNs;
        private long readbackLatencyMaxNs;
        private int readbackLatencySampleCount;

        // Diagnostics: once per real second, log the app's true render fps, fresh (app-produced)
        // fps, and committed (output) fps — so a gap between them shows up in the device log
        // instead of only being visible by eye. Consumer-side counters are updated with
        // Interlocked since they cross the thread boundary.
        private long statsWindowStartTicks;
        private int renderFramesInWindow;   // main thread: actual app render tick count (true app fps)
        private int freshFramesInWindow;    // producer thread
        private int committedInWindow;      // consumer thread
        private float minAppFpsInWindow;    // main thread
        private static readonly long StatsWindowTicks = 1_000_000_000L;

        // Gate that holds back capture until the RGB camera delivers its first real frame, so the
        // recording doesn't open on the placeholder background. Bounded: if the camera never comes
        // up we record anyway rather than emit a zero-frame file.
        private long firstFrameGateStartTicks;
        private bool firstFrameGateTimedOut;
        private static readonly long FirstFrameGateTicks = 5_000_000_000L;

        // Actual capture resolution used for this recording session (videoWidth/videoHeight,
        // capped to maxRecordingDimension's long edge). Kept separate from videoWidth/videoHeight
        // so callers that set those fields still see the uncapped camera resolution.
        // 1920 = 1080p cap, 1280 = 720p cap; set this (e.g. from another script, before calling
        // StartRecording) to pick recording quality vs. per-frame capture cost.
        public int maxRecordingDimension = 1920;
        private int recordWidth;
        private int recordHeight;

        // Cached readback callback, reused every captured frame instead of allocating a new
        // closure per AsyncGPUReadback.Request call.
        private Action<AsyncGPUReadbackRequest> onReadbackComplete;

        private XvMRVideoCaptureManager mrVideoCaptureManager;
        private XvMRVideoCaptureManager MrVideoCaptureManager
        {
            get
            {
                if (mrVideoCaptureManager == null)
                {
                    mrVideoCaptureManager = FindObjectOfType<XvMRVideoCaptureManager>();
                }
                return mrVideoCaptureManager;
            }
        }

        public AudioListener audioListener;
        [HideInInspector]
        internal Camera cam;
      
        //public Text infoTxt;
        //public VideoManagerControl videoManageControl;

        private IEnumerator Start () {
            audioListener = FindObjectOfType<AudioListener>();

            // Start microphone
            microphoneSource = gameObject.AddComponent<AudioSource>();
            microphoneSource.mute =
            microphoneSource.loop = true;
            microphoneSource.bypassEffects =
            microphoneSource.bypassListenerEffects = false;
            // The WaitUntil below only terminates once the microphone is actually capturing, so it
            // has to stay inside this branch — with no Microphone.Start, GetPosition stays at 0 and
            // the coroutine would hang here forever, never reaching Play().
            if (recordMicrophone)
            {
                microphoneSource.clip = Microphone.Start(null, true, 10, AudioSettings.outputSampleRate);
                yield return new WaitUntil(() => Microphone.GetPosition(null) > 0);
            }
            microphoneSource.Play();
        }

        private void OnDestroy () {
            // Stop the encoder feed thread if we're torn down mid-recording, so it can't outlive
            // this object and keep calling into a disposed recorder.
            consumerRunning = false;
            frameAvailableEvent?.Set();
            if (consumerThread != null)
            {
                consumerThread.Join(200);
                consumerThread = null;
            }
            frameAvailableEvent?.Dispose();
            frameAvailableEvent = null;
            // Stop microphone
            if (recordMicrophone)
            {
                microphoneSource.Stop();
                Microphone.End(null);
            }
        }

       
        public void StartRecording()
        {
            if (isRecording) {
                return;
            }

            isRecording = true;

            // Start recording
            var frameRate = 30;
            recordFrameRate = frameRate;
            //var sampleRate = recordMicrophone ? AudioSettings.outputSampleRate : 0;
            //var channelCount = recordMicrophone ? (int)AudioSettings.speakerMode : 0;
            // MP4Recorder takes 0/0 to mean "no audio track", which skips audio muxing entirely.
            var sampleRate = recordAudio ? AudioSettings.outputSampleRate : 0;
            var channelCount = recordAudio ? (int)AudioSettings.speakerMode : 0;
            recordClock = new RealtimeClock();

            // Cap the capture resolution instead of recording at the raw camera resolution
            // (which can be 1920x1080+); this directly shrinks the per-frame Blit/readback/copy
            // cost. videoWidth/videoHeight themselves are left untouched for other readers.
            recordWidth = videoWidth;
            recordHeight = videoHeight;
            if (Mathf.Max(recordWidth, recordHeight) > maxRecordingDimension)
            {
                float scale = maxRecordingDimension / (float)Mathf.Max(recordWidth, recordHeight);
                recordWidth = Mathf.Max(2, Mathf.RoundToInt(recordWidth * scale) & ~1);
                recordHeight = Mathf.Max(2, Mathf.RoundToInt(recordHeight * scale) & ~1);
            }

            recorder = new MP4Recorder(recordWidth, recordHeight, frameRate, sampleRate, channelCount);

            // Sync ReadPixels fallback only: caps production to ~1.5x the target rate so it
            // doesn't stall on every single render frame (see the throttle in CaptureFrames).
            producerIntervalNs = (1_000_000_000L / frameRate) * 2 / 3;
            lastProduceTicks = -1;
            readbackInFlightCount = 0;
            pendingReadbackRequestTicks.Clear();
            statsWindowStartTicks = -1;
            firstFrameGateStartTicks = -1;
            firstFrameGateTimedOut = false;
            renderFramesInWindow = 0;
            freshFramesInWindow = 0;
            committedInWindow = 0;
            minAppFpsInWindow = float.MaxValue;
            readbackLatencySumNs = 0;
            readbackLatencyMaxNs = 0;
            readbackLatencySampleCount = 0;
            hasNewSharedFrame = false;
            frameAvailableEvent = new AutoResetEvent(false);

            // Make sure the MR compositor is actually producing CameraRenderTexture frames;
            // capture reads that texture directly instead of forcing its own scene render.
            if (MrVideoCaptureManager != null && !MrVideoCaptureManager.IsOn)
            {
                MrVideoCaptureManager.StartCapture();
            }

            convertedTexture = new RenderTexture(recordWidth, recordHeight, 0, RenderTextureFormat.ARGB32);
            int bufferSize = recordWidth * recordHeight * 4;
            writeBuffer = new byte[bufferSize];
            sharedBuffer = new byte[bufferSize];
            readBuffer = new byte[bufferSize];
            // Cache the readback callback once per session instead of allocating a new closure
            // on every AsyncGPUReadback.Request call.
            onReadbackComplete = OnReadbackComplete;
            captureRoutine = StartCoroutine(CaptureFrames());

            // Start the encoder feed thread. It must be running before audio starts so both
            // streams begin near the same clock origin.
            consumerRunning = true;
            consumerThread = new Thread(ConsumerLoop) { IsBackground = true, Name = "ReplayCamEncoder" };
            consumerThread.Start();

            //audioInput = recordMicrophone ? new AudioInput(recorder, clock, microphoneSource, true) : null;
            audioInput = recordAudio ? new AudioInput(recorder, recordClock, audioListener) : null;
            // Unmute microphone
            microphoneSource.mute = audioInput == null;



            MyDebugTool.Log("StartRecording");

        }

        // Producer (main thread): keep the newest rendered frame available for the consumer.
        // Does NOT commit to the encoder itself — that is the consumer thread's job. Also
        // collects/logs the once-per-second diagnostics on the main thread.
        private IEnumerator CaptureFrames()
        {
            var endOfFrame = new WaitForEndOfFrame();
            if (!SystemInfo.supportsAsyncGPUReadback)
                readbackBuffer = new Texture2D(recordWidth, recordHeight, TextureFormat.RGBA32, false, false);
            while (true)
            {
                yield return endOfFrame;

                var now = recordClock.timestamp;

                // --- diagnostics (main thread): sample app fps every render frame, flush once/sec ---
                if (statsWindowStartTicks < 0)
                    statsWindowStartTicks = now;
                // Every pass through this loop corresponds to exactly one WaitForEndOfFrame, i.e.
                // one real app render — counting these directly gives the app's true render fps
                // for the window, independent of the capture/production throttle below.
                renderFramesInWindow++;
                var instantAppFps = Time.unscaledDeltaTime > 0f ? 1f / Time.unscaledDeltaTime : 0f;
                if (instantAppFps < minAppFpsInWindow)
                    minAppFpsInWindow = instantAppFps;

                if (now - statsWindowStartTicks >= StatsWindowTicks)
                {
                    int rendered = renderFramesInWindow;
                    int committed = Interlocked.Exchange(ref committedInWindow, 0);
                    int fresh = Interlocked.Exchange(ref freshFramesInWindow, 0);
                    float minFps = minAppFpsInWindow;
                    long latencySum = readbackLatencySumNs;
                    long latencyMax = readbackLatencyMaxNs;
                    int latencyCount = readbackLatencySampleCount;
                    if (committed < recordFrameRate || minFps < recordFrameRate)
                    {
                        string latencyInfo = latencyCount > 0
                            ? $" Readback latency avg={(latencySum / latencyCount) / 1_000_000.0:F1}ms max={latencyMax / 1_000_000.0:F1}ms(n={latencyCount})"
                            : "";
                        MyDebugTool.Log($"[ReplayCam] Recording frame-rate diagnostics: application={rendered}fps submitted={committed}fps(target {recordFrameRate}fps) " +
                            $"new frames={fresh}fps minimum application frame rate={minFps:F1}fps{latencyInfo}");
                    }
                    statsWindowStartTicks = now;
                    renderFramesInWindow = 0;
                    minAppFpsInWindow = float.MaxValue;
                    readbackLatencySumNs = 0;
                    readbackLatencyMaxNs = 0;
                    readbackLatencySampleCount = 0;
                }

                // Throttle production: async path uses back-pressure (skip while a previous
                // readback is still in flight, so we capture as fast as the readback pipeline can
                // actually sustain — no faster, no slower). The synchronous ReadPixels fallback
                // has no "in flight" concept, so it keeps a fixed-interval cap to avoid stalling
                // on every single render frame.
                if (SystemInfo.supportsAsyncGPUReadback)
                {
                    if (readbackInFlightCount >= MaxReadbackInFlight)
                        continue;
                }
                else if (lastProduceTicks >= 0 && now - lastProduceTicks < producerIntervalNs)
                {
                    continue;
                }
                if (MrVideoCaptureManager == null || MrVideoCaptureManager.CameraRenderTexture == null)
                    continue;
                // CameraRenderTexture is allocated up front, but the compositor only fills it once
                // the device delivers its first RGB frame — which lags StartCapture() by a variable
                // amount. Blitting before then records the placeholder background (white) with the
                // VR content already composited over it, so skip those frames entirely rather than
                // delaying the start of the recording. If the camera never comes up, give up
                // waiting and record anyway — a white-headed clip beats a zero-frame file.
                if (!MrVideoCaptureManager.HasFrame)
                {
                    if (firstFrameGateStartTicks < 0)
                        firstFrameGateStartTicks = now;
                    if (now - firstFrameGateStartTicks < FirstFrameGateTicks)
                        continue;
                    if (!firstFrameGateTimedOut)
                    {
                        firstFrameGateTimedOut = true;
                        MyDebugTool.LogError("[ReplayCam] Timed out waiting for the first RGB frame; recording will continue (initial frames may be white)");
                    }
                }
                lastProduceTicks = now;

                // Format-convert (source is RGB565) and resize to the recorder's frame size in
                // one Blit, instead of re-rendering the scene like CameraInput.OnFrame does.
                Graphics.Blit(MrVideoCaptureManager.CameraRenderTexture, convertedTexture);

                if (SystemInfo.supportsAsyncGPUReadback)
                {
                    readbackInFlightCount++;
                    pendingReadbackRequestTicks.Enqueue(now);
                    AsyncGPUReadback.Request(convertedTexture, 0, onReadbackComplete);
                }
                else
                {
                    var prevActive = RenderTexture.active;
                    RenderTexture.active = convertedTexture;
                    readbackBuffer.ReadPixels(new Rect(0, 0, recordWidth, recordHeight), 0, 0, false);
                    RenderTexture.active = prevActive;
                    if (writeBuffer != null)
                    {
                        readbackBuffer.GetRawTextureData<byte>().CopyTo(writeBuffer);
                        PublishWriteBuffer();
                    }
                }
            }
        }

        private void OnReadbackComplete(AsyncGPUReadbackRequest request)
        {
            // Decrement unconditionally (including on error) so a single failed readback can't
            // permanently occupy an in-flight slot.
            if (readbackInFlightCount > 0)
                readbackInFlightCount--;
            // Latency sample: how long this request actually took end-to-end, regardless of
            // whether it errored (an erroring request still occupied an in-flight slot). Matched
            // to its issuance timestamp by queue order (see field comment for why that's valid).
            long requestTicks = pendingReadbackRequestTicks.Count > 0
                ? pendingReadbackRequestTicks.Dequeue()
                : recordClock.timestamp;
            long latency = recordClock.timestamp - requestTicks;
            readbackLatencySumNs += latency;
            if (latency > readbackLatencyMaxNs)
                readbackLatencyMaxNs = latency;
            readbackLatencySampleCount++;
            if (writeBuffer == null || request.hasError)
                return;
            request.GetData<byte>().CopyTo(writeBuffer);
            PublishWriteBuffer();
        }

        // Hand the freshly filled writeBuffer over to the consumer by swapping it with sharedBuffer
        // under the lock (a pointer swap only), then wake the consumer thread immediately instead
        // of making it poll on a timer.
        private void PublishWriteBuffer()
        {
            lock (bufferLock)
            {
                var tmp = sharedBuffer;
                sharedBuffer = writeBuffer;
                writeBuffer = tmp;
                hasNewSharedFrame = true;
            }
            frameAvailableEvent.Set();
            Interlocked.Increment(ref freshFramesInWindow);
        }

        // Consumer (background thread): commits each new frame to the encoder as soon as the
        // producer publishes it, off the main thread. No frame duplication — if the app can't
        // supply a new frame in time, this thread simply has nothing to commit until it does.
        private void ConsumerLoop()
        {
#if PLATFORM_ANDROID && !UNITY_EDITOR
            // The native encoder reaches MediaCodec through JNI; a non-main thread must attach to
            // the JVM first or the very first CommitFrame will crash (same as AudioInput does on
            // the audio thread). Must be paired with DetachCurrentThread on exit.
            AndroidJNI.AttachCurrentThread();
#endif
            try
            {
                while (consumerRunning)
                {
                    // Sleep until the producer signals a new frame; the timeout is just so this
                    // thread re-checks consumerRunning periodically and StopRecording's Join()
                    // doesn't have to wait indefinitely if a signal is ever missed.
                    frameAvailableEvent.WaitOne(100);
                    if (!consumerRunning)
                        break;

                    bool gotFrame;
                    lock (bufferLock)
                    {
                        gotFrame = hasNewSharedFrame;
                        if (gotFrame)
                        {
                            var tmp = readBuffer;
                            readBuffer = sharedBuffer;
                            sharedBuffer = tmp;
                            hasNewSharedFrame = false;
                        }
                    }
                    if (gotFrame && readBuffer != null)
                    {
                        recorder.CommitFrame(readBuffer, recordClock.timestamp);
                        Interlocked.Increment(ref committedInWindow);
                    }
                }
            }
            catch (Exception e)
            {
                MyDebugTool.Log("[ReplayCam] Recording thread exception: " + e.Message);
            }
            finally
            {
#if PLATFORM_ANDROID && !UNITY_EDITOR
                AndroidJNI.DetachCurrentThread();
#endif
            }
        }

        public async void StopRecording (UnityAction<string> callback) {

            if (!isRecording)
            {
                return;
            }
            isRecording = false;
           

            // Mute microphone
            microphoneSource.mute = true;

            // Tear down every resource even if an earlier step throws, so a single failure (e.g.
            // audioInput.Dispose()) can't leak the capture coroutine, the encoder feed thread, or
            // the capture buffers/textures across repeated start/stop cycles.
            try
            {
                audioInput?.Dispose();
            }
            finally
            {
                audioInput = null;
                if (captureRoutine != null)
                {
                    StopCoroutine(captureRoutine);
                    captureRoutine = null;
                }
                // Stop the consumer thread and wait for it to finish committing BEFORE
                // FinishWriting, so no CommitFrame races with the encoder being finalized.
                consumerRunning = false;
                frameAvailableEvent?.Set();
                if (consumerThread != null)
                {
                    consumerThread.Join(500);
                    consumerThread = null;
                }
                frameAvailableEvent?.Dispose();
                frameAvailableEvent = null;
                if (convertedTexture != null)
                {
                    Destroy(convertedTexture);
                    convertedTexture = null;
                }
                if (readbackBuffer != null)
                {
                    Destroy(readbackBuffer);
                    readbackBuffer = null;
                }
                writeBuffer = null;
                sharedBuffer = null;
                readBuffer = null;
            }

            var path = await recorder.FinishWriting();
            // Playback recording
            MyDebugTool.Log($"Saved recording to: {path}");
           // var prefix = Application.platform == RuntimePlatform.IPhonePlayer ? "file://" : "";
            //infoTxt.text = path;
            // On mobile, play the recorded video after recording ends
            //Handheld.PlayFullScreenMovie($"{prefix}{path}");


#if PLATFORM_ANDROID && !UNITY_EDITOR
            string dstDir = "/storage/emulated/0/DCIM/ScreenRecorder";
            string path2 = dstDir + "/" + getDate() + ".mp4";
            await Task.Run(() =>
            {
                if (!Directory.Exists(dstDir))
                {
                    Directory.CreateDirectory(dstDir);
                }
                File.Copy(path, path2);
            });

            callback?.Invoke(path2);
#endif

#if UNITY_EDITOR
            string dirPath = Path.GetFileName(path);

            string rootPath = Application.dataPath.Replace("Assets/", "");
            string path_configData = rootPath + "/Screenshots/" + dirPath;

            await Task.Run(() =>
            {
                if (!Directory.Exists(rootPath + "/Screenshots"))
                {
                    Directory.CreateDirectory(rootPath + "/Screenshots");
                }
                File.Copy(path, path_configData);
            });

            Debug.LogError(path_configData);
            callback?.Invoke(path_configData);

#endif
        }

        public void RemoveRecording()
        {
           // if (File.Exists(m_resultPath))
            {

               // File.Delete(m_resultPath);
            }
        }

        private static string getDate()
        {
            string str = DateTime.Now.Year.ToString();
            if (DateTime.Now.Month.ToString().Length == 1)
            {
                str += "0" + DateTime.Now.Month;
            }
            else
            {
                str += DateTime.Now.Month;
            }
            if (DateTime.Now.Day.ToString().Length == 1)
            {
                str += "0" + DateTime.Now.Day;
            }
            else
            {
                str += DateTime.Now.Day;
            }
            if (DateTime.Now.Hour.ToString().Length == 1)
            {
                str += "0" + DateTime.Now.Hour;
            }
            else
            {
                str += DateTime.Now.Hour;
            }
            if (DateTime.Now.Minute.ToString().Length == 1)
            {
                str += "0" + DateTime.Now.Minute;
            }
            else
            {
                str += DateTime.Now.Minute;
            }
            if (DateTime.Now.Second.ToString().Length == 1)
            {
                str += "0" + DateTime.Now.Second;
            }
            else
            {
                str += DateTime.Now.Second;
            }
            return str;
        }
    }
}