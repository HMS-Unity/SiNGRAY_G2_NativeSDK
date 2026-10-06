using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using TMPro;
using UnityEngine;

namespace Singray.Foundation.SampleScenes
{
    /// <summary>
    /// Presents MRTK's standard in-world keyboard and mirrors its text in the sample panel.
    /// </summary>
    public sealed class MrtkKeyboardSample : MonoBehaviour, ISdkSampleLifecycle
    {
        [SerializeField] private NonNativeKeyboard keyboard;
        [SerializeField] private TMP_Text outputText;
        [SerializeField, Range(0.25f, 1f)] private float keyboardScale = 0.5f;

        private Coroutine openRoutine;
        private bool subscribed;
        private readonly List<Material> runtimeTextMaterials = new List<Material>();

        private void OnEnable()
        {
            openRoutine = StartCoroutine(OpenNextFrame());
        }

        private IEnumerator OpenNextFrame()
        {
            // NonNativeKeyboard disables itself from Awake, so present it one frame later.
            yield return null;
            openRoutine = null;
            OpenKeyboard();
        }

        public void OpenKeyboard()
        {
            if (keyboard == null)
            {
                Debug.LogError("SDK Samples: MRTK NonNativeKeyboard is not configured.", this);
                return;
            }

            Subscribe();
            string currentText = outputText == null || outputText.text == "Tap the button or a key to start typing"
                ? string.Empty
                : outputText.text;
            // Keep MRTK's normal presentation path so its key panels are fully initialized,
            // but prevent TMP from opening Android's soft keyboard. On the G2 firmware the
            // INPUT_METHOD_SHOW broadcast can crash XvMainActivity when its accessibility
            // service has not been created yet.
            keyboard.InputField.shouldHideSoftKeyboard = true;
            keyboard.InputField.shouldHideMobileInput = true;
            keyboard.InputField.interactable = true;
            keyboard.PresentKeyboard(currentText, NonNativeKeyboard.LayoutType.Alpha);
            NormalizeKeyboardText();

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 keyboardPosition = mainCamera.transform.position
                    + mainCamera.transform.forward * 1.15f
                    - mainCamera.transform.up * 0.20f;
                keyboard.RepositionKeyboard(keyboardPosition);
                keyboard.transform.localScale *= keyboardScale;
            }
        }

        private void NormalizeKeyboardText()
        {
            TMP_Text[] labels = keyboard.GetComponentsInChildren<TMP_Text>(true);
            Shader fallbackShader = Shader.Find("TextMeshPro/Distance Field");

            for (int i = 0; i < labels.Length; i++)
            {
                TMP_Text label = labels[i];
                label.color = Color.white;

                Material source = label.fontSharedMaterial;
                if (source == null || source.shader == null || source.shader.isSupported || fallbackShader == null)
                {
                    continue;
                }

                Material fallbackMaterial = new Material(source)
                {
                    shader = fallbackShader,
                    name = source.name + " Android Fallback"
                };
                if (fallbackMaterial.HasProperty("_FaceColor"))
                {
                    fallbackMaterial.SetColor("_FaceColor", Color.white);
                }

                label.fontSharedMaterial = fallbackMaterial;
                runtimeTextMaterials.Add(fallbackMaterial);
            }
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            keyboard.OnTextUpdated += HandleTextUpdated;
            keyboard.OnTextSubmitted += HandleTextSubmitted;
            keyboard.OnClosed += HandleKeyboardClosed;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || keyboard == null)
            {
                return;
            }

            keyboard.OnTextUpdated -= HandleTextUpdated;
            keyboard.OnTextSubmitted -= HandleTextSubmitted;
            keyboard.OnClosed -= HandleKeyboardClosed;
            subscribed = false;
        }

        private void HandleTextUpdated(string value)
        {
            if (outputText != null)
            {
                outputText.text = value;
            }
        }

        private void HandleTextSubmitted(object sender, EventArgs eventArgs)
        {
            if (outputText != null && keyboard != null)
            {
                outputText.text = keyboard.InputField.text;
            }
        }

        private void HandleKeyboardClosed(object sender, EventArgs eventArgs)
        {
            Unsubscribe();
        }

        public void ShutdownSample()
        {
            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
                openRoutine = null;
            }

            Unsubscribe();
            if (keyboard != null && keyboard.gameObject.activeSelf)
            {
                keyboard.Close();
            }
        }

        private void OnDisable()
        {
            ShutdownSample();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < runtimeTextMaterials.Count; i++)
            {
                if (runtimeTextMaterials[i] != null)
                {
                    Destroy(runtimeTextMaterials[i]);
                }
            }

            runtimeTextMaterials.Clear();
        }
    }
}
