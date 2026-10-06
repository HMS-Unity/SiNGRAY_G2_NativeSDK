package org.xv.xvsdk.ext.ble;

import android.content.Context;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.os.Handler;
import android.os.Looper;

public class AndroidSensorPhone {
    private static final String TAG = "AndroidSensorPhone";

    private static AndroidSensorPhone sInstance;

    public static AndroidSensorPhone getInstance(Context context) {
        if (sInstance == null) {
            sInstance = new AndroidSensorPhone(context);
        }
        return sInstance;
    }

    private final Context mContext;
    private final Handler mHandler;
    private float[] mRotationVector;
    private float[] mInitRotation;
    private float[] mQuaternion;
    private ISensorPhoneListener mOrientationListener;

    private AndroidSensorPhone(Context context) {
        mContext = context;
        mHandler = new Handler(Looper.getMainLooper());
        mQuaternion = new float[4];
    }

    public boolean start(ISensorPhoneListener listener) {
        mOrientationListener = listener;
        SensorManager sm = (SensorManager) mContext.getSystemService(Context.SENSOR_SERVICE);
        Sensor rotationSensor = sm.getDefaultSensor(Sensor.TYPE_GAME_ROTATION_VECTOR);
        /*
        Sensor rotationSensor = sm.getDefaultSensor(Sensor.TYPE_ROTATION_VECTOR);
		if (rotationSensor == null) {
			rotationSensor = sm.getDefaultSensor(Sensor.TYPE_GAME_ROTATION_VECTOR);
		}
        */
        boolean ret = sm.registerListener(mSensorListener, rotationSensor, SensorManager.SENSOR_DELAY_GAME);
        return ret;
    }

    public boolean reset() {
        mInitRotation = null;
        return true;
    }

    SensorEventListener mSensorListener = new SensorEventListener() {
        private int mAccuracy = SensorManager.SENSOR_STATUS_UNRELIABLE;

        public void onSensorChanged(SensorEvent sensorEvent) {
            if (mAccuracy == SensorManager.SENSOR_STATUS_UNRELIABLE) {
                return;
            }

            switch (sensorEvent.sensor.getType()) {
                case Sensor.TYPE_GAME_ROTATION_VECTOR:
                //case Sensor.TYPE_ROTATION_VECTOR:
                    mRotationVector = sensorEvent.values.clone();
                    break;
            }
            updateOrientation();
        }

        public void onAccuracyChanged(Sensor sensor, int accuracy) {
            mAccuracy = accuracy;
        }
    };

    private void updateOrientation() {
        if (mRotationVector == null) {
            return;
        }

        SensorManager.getQuaternionFromVector(mQuaternion, mRotationVector);
        /*
        float[] rotationMatrix = new float[9];
        SensorManager.getRotationMatrixFromVector(rotationMatrix, mRotationVector);
        float[] rotation = toPitchYawRoll(rotationMatrix);

        final float EPSILON = 1e-5f;

        if (Math.abs(rotation[0]) <= EPSILON
                && Math.abs(rotation[1]) <= EPSILON
                && Math.abs(rotation[2]) <= EPSILON) {

            return;
        }

        if (mInitRotation == null) {
            mInitRotation = new float[3];
            mInitRotation[0] = rotation[0];
            mInitRotation[1] = rotation[1];
            mInitRotation[2] = rotation[2];
        }
        Utils.log(TAG, "update init pitch:" + mInitRotation[0] + " yaw:" + mInitRotation[1] + " roll:" + mInitRotation[2]);
        final float[] data = new float[]{rotation[0] - mInitRotation[0], rotation[1] - mInitRotation[1], rotation[2] - mInitRotation[2]};
        Utils.log(TAG, "update rotation pitch:" + data[0] + " yaw:" + data[1] + " roll:" + data[2]);
        */
        mHandler.post(new Runnable() {
            @Override
            public void run() {
                if (mOrientationListener != null) {
                    //mOrientationListener.onCallback(rotation);
                    mOrientationListener.onCallback(mQuaternion[0], mQuaternion[1], mQuaternion[2], mQuaternion[3]);
                }
            }
        });
    }

    private float[] toPitchYawRoll(float[] rVector) {
        float[] rotation = new float[3];
        SensorManager.getOrientation(rVector, rotation);

        float[] result = new float[rotation.length];
        for (int i = 0; i < rotation.length; i++) {
            result[i] = (float) Math.toDegrees(rotation[i]);
        }
        return result;
    }

}
