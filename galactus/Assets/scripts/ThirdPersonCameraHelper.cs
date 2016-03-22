using UnityEngine;
using System.Collections;

public class ThirdPersonCameraHelper : MonoBehaviour {

	public Transform firstPersonTransform;
	public float distance = 10;
	public Transform cameraTransform;


	void Update () {
        if (firstPersonTransform)
        {
            var d = Input.GetAxis("Mouse ScrollWheel");
            if (d > 0f) { distance -= 0.125f; if (distance < 0) distance = 0; }
            else if (d < 0f) { distance += 0.125f; }
            Vector3 delta = cameraTransform.forward.normalized * distance * firstPersonTransform.lossyScale.z;
            transform.position = firstPersonTransform.position - delta;
        }
	}
}
