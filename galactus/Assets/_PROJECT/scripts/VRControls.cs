using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRControls : MonoBehaviour {
    public Camera head, leftEye, rightEye;
    public Transform leftHand, rightHand;
	public void TakeUserInterfaceControl(GameObject oldCamera) {
		Camera thisCam = oldCamera.GetComponent<Camera>();
		if(thisCam) thisCam.enabled = false;
		AudioListener al = oldCamera.GetComponent<AudioListener>();
		if(al) al.enabled = false;
		//GUILayer guiL = oldCamera.GetComponent<GUILayer>();
		//if(guiL) guiL.enabled = false;

		//head.gameObject.AddComponent<GUILayer>();
	}
}
