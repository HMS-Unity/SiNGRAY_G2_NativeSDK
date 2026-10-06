package com.xv;


import android.app.Application;

import com.dongao.dlna.DLNALibrary;
import com.dongao.dlna.upnp.UpnpServiceBiz;

import com.iflytek.cloud.SpeechConstant;
import com.iflytek.cloud.SpeechUtility;

public class XvApp extends Application {

    private static final String App_ID = "60408e1a";

    @Override
    public void onCreate() {
		initAlitalk();
        super.onCreate();
		DLNALibrary.getInstance().init(this);
    }


	@Override
    public void onTerminate() {
        UpnpServiceBiz.newInstance().closeUpnpService(this);
        super.onTerminate();
    }
	
	private void initAlitalk() {
		// Initialize at app startup so SpeechUtility is available when an Activity is restored from an old intent after the background process was killed
        // This API returns null outside the main process; add SpeechConstant.FORCE_LOGIN + "=true" to use speech in another process
        // Separate parameters with commas
        // Set your registered application ID

        // The app ID must match the downloaded SDK, otherwise error 10407 occurs

        StringBuffer param = new StringBuffer();
        param.append("appid=" + App_ID);
        param.append(",");
        // Enable v5+
        param.append(SpeechConstant.ENGINE_MODE + "=" + SpeechConstant.MODE_MSC);
        SpeechUtility.createUtility(XvApp.this, param.toString());
	}

}
