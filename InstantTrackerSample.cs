/*==============================================================================
Copyright 2017 Maxst, Inc. All Rights Reserved.
==============================================================================*/

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine.UI;

using maxstAR;

public class InstantTrackerSample : MonoBehaviour
{
	[SerializeField]
	private Text startBtnText = null;

	private Vector3 touchToWorldPosition = Vector3.zero;
	private Vector3 touchSumPosition = Vector3.zero;
    private Matrix4x4 cvtMat = Matrix4x4.identity;

	private bool startTrackerDone = false;
	private bool cameraStartDone = false;
	private bool findSurfaceDone = false;
    private bool readyConvertingMatrixDone = false;

	private InstantTrackableBehaviour instantTrackable = null;

	void Start()
	{
		instantTrackable = FindObjectOfType<InstantTrackableBehaviour>();
		if (instantTrackable == null)
		{
			return;
		}

		instantTrackable.OnTrackFail();
	}

	void Update()
	{
		if (Input.GetKey(KeyCode.Escape))
		{
			SceneStackManager.Instance.LoadPrevious();
		}

		if (instantTrackable == null)
		{
			return;
		}

		StartCamera();

		if (!startTrackerDone)
		{
			TrackerManager.GetInstance().StartTracker(TrackerManager.TRACKER_TYPE_INSTANT);
			SensorDevice.GetInstance().Start();
			startTrackerDone = true;
		}

		TrackingState state = TrackerManager.GetInstance().UpdateTrackingState();
		TrackingResult trackingResult = state.GetTrackingResult();
		if (trackingResult.GetCount() == 0)
		{
			instantTrackable.OnTrackFail();
			return;
		}		

		if (Input.touchCount > 0)
		{
			UpdateTouchDelta(Input.GetTouch(0).position);
		}

		Trackable trackable = trackingResult.GetTrackable(0);
        if(!readyConvertingMatrixDone)
        {
            Quaternion rotation = Quaternion.Euler(-90, 0, 0);
            Matrix4x4 M = Matrix4x4.TRS(Camera.main.transform.position, rotation, new Vector3(1, 1, 1));
            Matrix4x4 Minv = M.inverse;
            Matrix4x4 RT_t = trackable.GetPose();
            RT_t = Minv * RT_t;
            float dist = 1.0f / Math.Abs(RT_t[6]);
            if (dist < Math.Sqrt(5.0))
            {
                cvtMat = Matrix4x4.identity;
            }
            else
            {
                float[] R0_t = { RT_t[0], RT_t[1], RT_t[2], RT_t[4], RT_t[5], RT_t[6], RT_t[8], RT_t[9], RT_t[10] };
                float[] Tc = { RT_t[12], RT_t[13], RT_t[14] };
                float[] T0 = { 0, 0, -dist };
                float[] Tt = { T0[0] - Tc[0], T0[1] - Tc[1], T0[2] - Tc[2] };
                float[] T1 = {R0_t[0]*Tt[0] + R0_t[1]*Tt[1] + R0_t[2]*Tt[2],
                            R0_t[3]*Tt[0] + R0_t[4]*Tt[1] + R0_t[5]*Tt[2],
                            R0_t[6]*Tt[0] + R0_t[7]*Tt[1] + R0_t[8]*Tt[2]};

                float[] RTn = { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, T1[0], T1[1], T1[2], 1 };
                cvtMat = new Matrix4x4();
                for (int i = 0; i < 16; i++)
                {
                    cvtMat[i] = RTn[i];
                }
                //cvtMat = cvtMat.transpose;
            }
 
            readyConvertingMatrixDone = true;
        }

        Matrix4x4 current = trackable.GetPose();
        current = current * cvtMat;
        Matrix4x4 poseMatrix = current * Matrix4x4.Translate(touchSumPosition);
		instantTrackable.OnTrackSuccess(trackable.GetId(), trackable.GetName(), poseMatrix);
	}

	private void UpdateTouchDelta(Vector2 touchPosition)
	{
		switch (Input.GetTouch(0).phase)
		{
			case TouchPhase.Began:
				touchToWorldPosition = TrackerManager.GetInstance().GetWorldPositionFromScreenCoordinate(touchPosition);
				break;

			case TouchPhase.Moved:
				Vector3 currentWorldPosition = TrackerManager.GetInstance().GetWorldPositionFromScreenCoordinate(touchPosition);
				touchSumPosition += (currentWorldPosition - touchToWorldPosition);
				touchToWorldPosition = currentWorldPosition;
				break;
		}
	}

	void OnApplicationPause(bool pause)
	{
		if (pause)
		{
			SensorDevice.GetInstance().Stop();
			TrackerManager.GetInstance().StopTracker();
			startTrackerDone = false;
			StopCamera();
		}
	}

	void OnDestroy()
	{
		SensorDevice.GetInstance().Stop();
		TrackerManager.GetInstance().StopTracker();
		TrackerManager.GetInstance().DestroyTracker();
		StopCamera();
	}

	void StartCamera()
	{
		if (!cameraStartDone)
		{
			Debug.Log("Unity StartCamera");
			ResultCode result = CameraDevice.GetInstance().Start();
			if (result == ResultCode.Success)
			{
				cameraStartDone = true;
				//CameraDevice.GetInstance().SetAutoWhiteBalanceLock(true);   // For ODG-R7 preventing camera flickering
			}
		}
	}

	void StopCamera()
	{
		if (cameraStartDone)
		{
			Debug.Log("Unity StopCamera");
			CameraDevice.GetInstance().Stop();
			cameraStartDone = false;
		}
	}

	public void OnClickStart()
	{
		if (!findSurfaceDone)
		{
			TrackerManager.GetInstance().FindSurface();
			if (startBtnText != null)
			{
				startBtnText.text = "Stop Tracking";
			}
			findSurfaceDone = true;
			touchSumPosition = Vector3.zero;
            readyConvertingMatrixDone = false;
		}
		else
		{
			TrackerManager.GetInstance().QuitFindingSurface();
			if (startBtnText != null)
			{
				startBtnText.text = "Start Tracking";
			}
			findSurfaceDone = false;
		}
	}
}
