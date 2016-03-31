using UnityEngine;
using System.Collections;

public class ThirdPersonCamera : MonoBehaviour {
	public float distance = 10;
	public Transform cameraTransform;

	private Vector2 move;
	public float xSensitivity = 5, ySensitivity = 5;
	public bool invertY = false;

	public Transform followedEntity;

	void LateUpdate () {
		if (followedEntity)
        {
            var d = Input.GetAxis("Mouse ScrollWheel");
            if (d > 0f) { distance -= 0.125f; if (distance < 0) distance = 0; }
            else if (d < 0f) { distance += 0.125f; }
			Vector3 delta = cameraTransform.forward.normalized * distance * followedEntity.lossyScale.z;
			transform.position = followedEntity.position - delta;
        }
		move = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
		if(move.x != 0) {
			transform.Rotate(0, move.x * xSensitivity, 0);
		}
		if(move.y != 0) {
			if(invertY) { move.y *= -1; }
			transform.Rotate(-move.y * ySensitivity, 0, 0);
		}
	}
}
