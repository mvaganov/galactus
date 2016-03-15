using UnityEngine;
using System.Collections;

public class ThirdPersonHelper : MonoBehaviour {

	public Transform firstPersonTransform;
	public float distance = 10;
	public Transform cameraTransform;


	void Update () {
		Vector3 delta = cameraTransform.forward.normalized * distance * firstPersonTransform.lossyScale.z;
		transform.position = firstPersonTransform.position - delta;
	}
}
