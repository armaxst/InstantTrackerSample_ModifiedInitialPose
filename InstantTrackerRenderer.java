/*
 * Copyright 2017 Maxst, Inc. All Rights Reserved.
 */

package com.maxst.ar.sample.instantTracker;

import android.app.Activity;
import android.graphics.Bitmap;
import android.opengl.GLES20;
import android.opengl.GLSurfaceView.Renderer;
import android.renderscript.Matrix3f;
import android.renderscript.Matrix4f;
import android.util.Log;

import com.maxst.ar.CameraDevice;
import com.maxst.ar.MaxstAR;
import com.maxst.ar.MaxstARUtil;
import com.maxst.ar.Trackable;
import com.maxst.ar.TrackerManager;
import com.maxst.ar.TrackingResult;
import com.maxst.ar.TrackingState;
import com.maxst.ar.sample.arobject.TexturedCube;
import com.maxst.ar.sample.util.BackgroundRenderHelper;

import javax.microedition.khronos.egl.EGLConfig;
import javax.microedition.khronos.opengles.GL10;


class InstantTrackerRenderer implements Renderer {

	public static final String TAG = InstantTrackerRenderer.class.getSimpleName();

	private int surfaceWidth;
	private int surfaceHeight;
	private BackgroundRenderHelper backgroundRenderHelper;

	private TexturedCube texturedCube;
	private float posX;
	private float posY;
	private Activity activity;

	private boolean initializedForFirstPose = false;
	private Matrix4f cvtMat = new Matrix4f();

	InstantTrackerRenderer(Activity activity) {
		this.activity = activity;
	}

	@Override
	public void onDrawFrame(GL10 unused) {
		GLES20.glClear(GLES20.GL_COLOR_BUFFER_BIT | GLES20.GL_DEPTH_BUFFER_BIT);
		GLES20.glViewport(0, 0, surfaceWidth, surfaceHeight);

		TrackingState state = TrackerManager.getInstance().updateTrackingState();
		TrackingResult trackingResult = state.getTrackingResult();

		backgroundRenderHelper.drawBackground();

		if (trackingResult.getCount() == 0) {
			return;
		}

		float [] projectionMatrix = CameraDevice.getInstance().getProjectionMatrix();

		Trackable trackable = trackingResult.getTrackable(0);
		if(initializedForFirstPose)
		{
			float[] RT_t = trackable.getPoseMatrix();
			float[] Tc = {RT_t[12], RT_t[13], RT_t[14]};
			if(Tc[0] == 0.0f && Tc[1] == 0.0f && Tc[2] < Math.sqrt(5.0)) {
				cvtMat.loadIdentity();
			}
			else {
				float[] R0_t = {RT_t[0], RT_t[1], RT_t[2], RT_t[4], RT_t[5], RT_t[6], RT_t[8], RT_t[9], RT_t[10]};
				float dist = 1.0f / Math.abs(RT_t[10]);
				float[] T0 = {0,0,dist};
				float[] Tt = {T0[0]-Tc[0], T0[1]-Tc[1], T0[2]-Tc[2]};
				float[] T1 = {R0_t[0]*Tt[0] + R0_t[1]*Tt[1] + R0_t[2]*Tt[2],
						R0_t[3]*Tt[0] + R0_t[4]*Tt[1] + R0_t[5]*Tt[2],
						R0_t[6]*Tt[0] + R0_t[7]*Tt[1] + R0_t[8]*Tt[2]};

				float[] RTn = {1,0,0,T1[0], 0,1,0,T1[1], 0,0,1,T1[2], 0,0,0,1};
				cvtMat = new Matrix4f(RTn);
				cvtMat.transpose();
			}

			initializedForFirstPose = false;
		}

		GLES20.glEnable(GLES20.GL_DEPTH_TEST);

		Matrix4f cur = new Matrix4f(trackable.getPoseMatrix());
		Matrix4f trans = new Matrix4f(cvtMat.getArray());
		cur.multiply(trans);
		//texturedCube.setTransform(trackable.getPoseMatrix());
		texturedCube.setTransform(cur.getArray());
		texturedCube.setTranslate(posX, posY, -0.05f);
		texturedCube.setProjectionMatrix(projectionMatrix);
		texturedCube.draw();
	}

	@Override
	public void onSurfaceChanged(GL10 unused, int width, int height) {

		surfaceWidth = width;
		surfaceHeight = height;

		texturedCube.setScale(0.3f, 0.3f, 0.1f);

		MaxstAR.onSurfaceChanged(width, height);
	}

	@Override
	public void onSurfaceCreated(GL10 unused, EGLConfig config) {
		GLES20.glClearColor(0.0f, 0.0f, 0.0f, 1.0f);

		backgroundRenderHelper = new BackgroundRenderHelper();
		backgroundRenderHelper.init();

		texturedCube = new TexturedCube();
		Bitmap bitmap = MaxstARUtil.getBitmapFromAsset("MaxstAR_Cube.png", activity.getAssets());
		texturedCube.setTextureBitmap(bitmap);

		MaxstAR.onSurfaceCreated();
	}

	void setTranslate(float x, float y) {
		posX += x;
		posY += y;
	}

	void resetPosition() {
		posX = 0;
		posY = 0;
		initializedForFirstPose = true;
	}
}
