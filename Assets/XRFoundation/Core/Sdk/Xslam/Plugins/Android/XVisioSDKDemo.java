package com.xvisio.unity;

import android.Manifest;
import android.content.Context;
import android.content.pm.PackageManager;
import android.hardware.usb.UsbDevice;
import android.os.Bundle;
import android.util.Log;
import android.view.KeyEvent;

import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerActivity;

public class XVisioSDKDemo extends UnityPlayerActivity {

    private static final String TAG = "xvsdk-demo";

    private static final int PERMISSIONS_REQUEST_CAMERA = 0;
    private boolean mPermissionsGranted = false;

    private Context mAppContext;
    private Context mActivityContext;
    //static private XCamera mCamera;

    static public double posX = 0;
    static public double posY = 0;
    static public double posZ = 0;
    static public double posRoll = 0;
    static public double posPitch = 0;
    static public double posYaw = 0;

    static public int m_fd = -1;

    static private Object mutex = new Object();

    //qiyan
    public static XVisioSDKDemo instance = null;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        //qiyan
        instance = this;

        mAppContext = getApplicationContext();
        mActivityContext = this;

    }


    @Override
    public void onRequestPermissionsResult(int requestCode, String permissions[], int[] grantResults) {
        if (checkSelfPermission(Manifest.permission.CAMERA) != PackageManager.PERMISSION_GRANTED) {
            requestPermissions(new String[]{Manifest.permission.CAMERA}, PERMISSIONS_REQUEST_CAMERA);
            return;
        }
        mPermissionsGranted = true;

    }

    @Override
    protected void onResume() {
        super.onResume();
        if (mPermissionsGranted) {

        } else {
            Log.e(TAG, "missing permissions");
        }
    }

    @Override
    protected void onPause() {
        super.onPause();
        /*if(mCamera != null){
            mCamera.pause();
		}*/
    }


    public static boolean isXVisio(UsbDevice usbDevice) {
        if (usbDevice.getVendorId() == 0x040e)
            return true;
        return false;
    }


    public static int getFd() {
        return m_fd;
    }

    //qiyan

    public void resolution3840() {
        execShellAction("wm size 1080x3840");
    }

    public void resolution2400() {
        execShellAction("wm size 1080x2400");
    }

    public static void execShellAction(String cmd) {
        try {
            Process process = Runtime.getRuntime().exec(cmd);
            process.getErrorStream().close();
            process.getOutputStream().close();
            process.getInputStream().close();
            process.waitFor();
            process.destroy();
        } catch (Exception e) {
            Log.d("MainActivity", "exec command fail", e);
        }
    }


    public int Add(int x, int y) {
        return x + y;
    }

    public boolean onKeyDown(int keyCode, KeyEvent event) {

        switch (keyCode) {
// Increase volume
            case KeyEvent.KEYCODE_VOLUME_UP:

                UnityPlayer.UnitySendMessage("VolumeUp", "VolumeUp", "Up");
                return true;

// Decrease volume
            case KeyEvent.KEYCODE_VOLUME_DOWN:

                UnityPlayer.UnitySendMessage("VolumeDown", "VolumeDown", "Down");
                return true;

        }

        return false;
    }

    public boolean onKeyUp(int keyCode, KeyEvent event) {

        switch (keyCode) {
// Increase volume
            case KeyEvent.KEYCODE_VOLUME_UP:

                UnityPlayer.UnitySendMessage("VolumeUp", "VolumeUp_up", "up");
                return true;

        }

        return false;
    }

}
