using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubetestCameraController : MonoBehaviour {
    float yaw, pitch;
    public float sensitivity = 10;
    public Transform target;
    public float distanceFromTarget = 2;
    public Vector2 pitchMinMax = new Vector2(-40, 85);
    public float rotationSmoothTime = .125f;
    Vector3 rotationSmoothVelocity, currentRotation;
    public bool lockCursor = true;

    public void Start()
    {
        if(lockCursor) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void LateUpdate() {
        yaw += Input.GetAxis("Mouse X") * sensitivity;
        pitch += Input.GetAxis("Mouse Y") * sensitivity;
        pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);
        Vector3 targetRotation = new Vector3(-pitch, yaw);
        currentRotation = Vector3.SmoothDamp(currentRotation, targetRotation, ref rotationSmoothVelocity, rotationSmoothTime);
        transform.eulerAngles = currentRotation;

        transform.position = target.position - transform.forward * distanceFromTarget;

    }
}
