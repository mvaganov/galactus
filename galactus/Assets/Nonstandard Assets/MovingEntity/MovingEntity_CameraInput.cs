using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>A custom Unity3D character controller, useful for player characters
/// Latest version at: https://pastebin.com/pC0Ddjsi
/// The complementary MovingEntity script is at: https://pastebin.com/xFUD4tk2 </summary>
/// <description>MIT License - TL;DR - This code is free, don't bother me about it!</description>
public class MovingEntity_CameraInput : MonoBehaviour {
    [Tooltip("if left null, will search for a MobileEntity script on this object.")]
    public MovingEntityBase controlling;
    public BaseInputController inputController;

	public MovingEntityBase GetControlled() { return controlling; }

	public virtual void Initialize(){
		MovingEntityBase toControl = controlling;
		controlling = null;
		if (toControl == null) { toControl = GetComponent<MovingEntityBase> (); }
		Control(toControl);
	}

    void Start() {
		Initialize ();
    }
    public virtual void Control(MovingEntityBase me) {
        // un-control previous
        if (controlling != null) {
            controlling.transform.tag = "Untagged";
            inputController.Release (controlling);
        }
        // take control of current
        controlling = me;
        if(controlling.transform.tag == "Untagged" || controlling.transform.tag.Length == 0) { 
            controlling.transform.tag = "Player";
        }
        if (controlling is MovingEntity) {
            if (!(inputController is GroundedInputController)) {
                if (inputController != null) {
                    inputController = new GroundedInputController (inputController);
                } else {
                    inputController = new GroundedInputController ();
                }
            }
        } else {
            if (inputController == null) inputController = new BaseInputController ();
        }
        inputController.Start(controlling);
    }
    public virtual void Update() {
        controlling.MoveDirection = inputController.Update(controlling);
    }
    // let the controlled entity call LateUpdate after any positional adjustments
    void LateUpdate() {
        inputController.LateUpdate(controlling);
    }
}

[System.Serializable]
public class BaseInputController {
    /// <summary>the transform controlling where the camera should go. Might be different than myCamera if a VR headset is plugged in.</summary>
    [HideInInspector]
    public Transform camHandle;
    /// <summary>how the 3D camera should move with the player.</summary>
    [Tooltip("how the 3D camera should move with the player\n"+
        "* Fixed Camera: other code should control the camera\n"+
        "* Lock To Player: follow player with current offset\n"+
        "* Rotate 3rd Person: 3rd person, scrollwheel zoom\n"+
        "* Lock-and-Rotate-with-RMB: like the Unity editor Scene view")]
    public ControlStyle mouseLookMode = ControlStyle.freeRotate;
    public enum ControlStyle { staticCamera, noRotate, freeRotate, rotateWithRMB }
    /// <summary>how far away the camera should be from the player</summary>
    protected Vector3 cameraOffset;
    public float horizontalSensitivity = 5, verticalSensitivity = 5;
    [System.Serializable]
    public class ControlAxisNames {
        public string forward = "Vertical", right = "Horizontal",
        turnHorizontal = "Mouse X", turnVertical = "Mouse Y", 
        jump = "Jump", zoomInAndOut = "Mouse ScrollWheel";
    }
    [Tooltip("Axis used for controls\n(see: Edit->Project Settings->Input")]
    public ControlAxisNames controls = new ControlAxisNames();
    [Tooltip("if true, automatically slow velocity to zero if there is no user-input")]
    public bool AutoSlowdown = true;
    [Tooltip("If true, a raycast is sent to make sure the camera doesn't clip through solid objects.")]
    public bool cameraWontClip = true;
    [Tooltip("how far the camera should be from the PlayerControl transform")]
    public float cameraDistance = 5;
    [Tooltip("how far the eye-focus (where the camera rests) should be above the PlayerControl transform")]
    public float eyeHeight = 0.125f;
    [HideInInspector]
    public float currentPitch { get; protected set; }
    /// <summary>keeps the camera from looking too far (only effective with gravity)</summary>
    [Tooltip("keeps the camera from looking too far (only effective with gravity)")]
    public float maxCameraPitch = 90, minCameraPitch = -90;
    [Tooltip("Camera for the PlayerControl to use. Will automagically find one if not set.")]
    public Camera myCamera;
    /// <summary>'up' direction for the player, used to orient the camera when there is gravity</summary>
    [HideInInspector]
    public Vector3 cameraUp = Vector3.up;
    /// <summary>the vertical tilt of the camera</summary>
    public Vector3 CameraCenter(Transform t) { return (eyeHeight == 0)? t.position : t.position + t.up * eyeHeight; }

    public void Copy(BaseInputController c) {
        myCamera = c.myCamera;
        camHandle = c.camHandle;
        mouseLookMode = c.mouseLookMode;
        cameraDistance = c.cameraDistance;
        horizontalSensitivity = c.horizontalSensitivity;
        verticalSensitivity = c.verticalSensitivity;
        cameraWontClip = c.cameraWontClip;
        eyeHeight = c.eyeHeight;
    }
    public virtual void UpdateCameraAngles(float dx, float dy) {
        // simplistic gravity-less rotation
        camHandle.Rotate(Vector3.up, dx);
        camHandle.Rotate(Vector3.right, -dy);
    }
    protected void EnsureCamera(MovingEntityBase p) {
        if (!myCamera) { // make sure there is a camera to control!
            myCamera = Camera.main;
            if (myCamera == null) {
                myCamera = (new GameObject ("<main camera>")).AddComponent<Camera> ();
                myCamera.tag = "MainCamera";
            }
        } else {
            cameraOffset = camHandle.position - p.transform.position;
            cameraDistance = cameraOffset.magnitude;
        }
        if(UnityEngine.XR.XRDevice.isPresent) {
            camHandle = (new GameObject("<camera handle>")).transform;
            myCamera.transform.position = Vector3.zero;
            myCamera.transform.SetParent(camHandle);
        } else {
            camHandle = myCamera.transform;
        }
        UpdateCamera (p, true);
    }
    public virtual void Start(MovingEntityBase p) {
        EnsureCamera (p);
        p.AutomaticallyTurnToFaceLookDirection = false;
    }
    public virtual void Release(MovingEntityBase me) { }
    public bool ChangeCameraDistanceBasedOnScrollWheel() {
        float scroll = Input.GetAxis(controls.zoomInAndOut);
        if(scroll != 0) {
            cameraDistance -= scroll * 10;
            cameraDistance = Mathf.Max(0, cameraDistance);
            if(cameraDistance > 0 && cameraOffset != Vector3.zero) {
                cameraOffset = cameraOffset.normalized * cameraDistance;
            }
            return true;
        }
        return false;
    }
    public virtual void LateUpdate(MovingEntityBase me) {
        bool mustUpdateCamera = ChangeCameraDistanceBasedOnScrollWheel();
        UpdateCamera (me, mustUpdateCamera);
    }
    public virtual void UpdateCamera(MovingEntityBase me, bool mustUpdate) {
        if(mouseLookMode == ControlStyle.staticCamera && !mustUpdate) { return; }
        bool updatingWithMouseInput = (mouseLookMode == ControlStyle.freeRotate) || 
            (mouseLookMode == ControlStyle.rotateWithRMB && Input.GetMouseButton(1));
        // camera rotation
        if (updatingWithMouseInput) {
            // get the rotations that the user input is indicating
            UpdateCameraAngles (Input.GetAxis (controls.turnHorizontal) * horizontalSensitivity, Input.GetAxis (controls.turnVertical) * verticalSensitivity);
        } else if (mustUpdate) {
            UpdateCameraAngles (0, 0);
        }
        me.LookDirection = camHandle.forward;
        Vector3 eyeFocus = CameraCenter(me.transform);
        // move the camera to be looking at the player's eyes/head, ideally with no geometry in the way
        RaycastHit rh;
        float calculatedDistForCamera = cameraDistance;
        if(cameraWontClip && Physics.SphereCast(eyeFocus, myCamera.nearClipPlane, -camHandle.forward, out rh, cameraDistance)) {
            calculatedDistForCamera = rh.distance;
        }
        if(calculatedDistForCamera != 0) { cameraOffset = -myCamera.transform.forward * calculatedDistForCamera; }
        Vector3 nextLocation = eyeFocus + ((cameraDistance > 0) ? cameraOffset : Vector3.zero);
        camHandle.position = nextLocation;
    }
    public virtual Vector3 Update(MovingEntityBase me) {
        float inputF = Input.GetAxis (controls.forward), inputR = Input.GetAxis (controls.right);
        Vector3 MoveDirection = default(Vector3);
        if (inputF != 0 || inputR != 0) {
            Transform t = myCamera.transform;
            MoveDirection = (inputR * t.right) + (inputF * t.forward);
            MoveDirection.Normalize ();
        } else if (AutoSlowdown) {
            me.IsBrakeOn = true;
        }
        if (!me.AutomaticallyTurnToFaceLookDirection) {
            me.TurnToFace (myCamera.transform.forward, myCamera.transform.up);
        }
        return MoveDirection;
    }
    public virtual void GetMoveVectors(Vector3 upVector, out Vector3 forward, out Vector3 right) {
        Transform importantT = myCamera.transform;
        right = importantT.right; forward = importantT.forward;
    }
}

public class GroundedInputController : BaseInputController {
    MovingEntity pc;
    public GroundedInputController() { }
    public GroundedInputController(BaseInputController cameraControl) { base.Copy(cameraControl); }
    public override void LateUpdate(MovingEntityBase me) {
        if (!pc.AutomaticallyFollowPositionOnPlatform && pc.gravity.application != MovingEntity.GravityState.none) {
            pc.FollowPositionOnPlatform ();
        }
        bool mustUpdateCamera = OrientUp () | ChangeCameraDistanceBasedOnScrollWheel();
        UpdateCamera (me, mustUpdateCamera);
    }
    public override void UpdateCameraAngles(float dx, float dy) {
        if(pc.gravity.application != MovingEntity.GravityState.none) {
            currentPitch -= dy; // rotate accordingly, minus because of standard 'inverted' Y axis rotation
            camHandle.Rotate(Vector3.right, -currentPitch);// un-rotate zero-out camera's "up", re-applied soon
            if(cameraUp == Vector3.zero) return;
            Vector3 rightSide = (cameraUp != camHandle.forward)?Vector3.Cross(camHandle.forward, cameraUp):camHandle.right;
            Vector3 unrotatedMoveForward = Vector3.Cross(cameraUp, rightSide);
            camHandle.rotation = Quaternion.LookRotation(unrotatedMoveForward, cameraUp); // force zero rotation
            camHandle.Rotate(Vector3.up, dx); // re-apply rotation
            while(currentPitch > 180) { currentPitch -= 360; } // normalize the angles to be between -180 and 180
            while(currentPitch < -180) { currentPitch += 360; }
            currentPitch = Mathf.Clamp(currentPitch, minCameraPitch, maxCameraPitch);
            camHandle.Rotate(Vector3.right, currentPitch);
        } else {
            base.UpdateCameraAngles(dx,dy); // simplistic gravity-less rotation
            cameraUp = camHandle.up;
            currentPitch = 0;
        }
    }
    public override void Start(MovingEntityBase me) {
        pc = me as MovingEntity;
        base.Start (me);
        // calculate current pitch based on camera
        Vector3 currentRight = Vector3.Cross(cameraUp, camHandle.forward);
        Vector3 currentMoveForward = Vector3.Cross(currentRight, cameraUp);
        Quaternion playerIdentity = Quaternion.LookRotation(currentMoveForward, cameraUp);
        currentPitch = Quaternion.Angle(playerIdentity, camHandle.rotation);
        pc.AutomaticallyFollowPositionOnPlatform = false;
        pc.AutomaticallyTurnToFaceLookDirection = false;
    }
    public override void Release(MovingEntityBase me) {
        if (pc == null) { Debug.LogError (pc+" strange state... camera/input controller should always have a target"); }
        if (me != pc) { Debug.LogError (pc+" never released!"); }
        pc.AutomaticallyFollowPositionOnPlatform = true;
        pc.AutomaticallyTurnToFaceLookDirection = true;
        pc = null;
    }
    public override Vector3 Update(MovingEntityBase me) {
        float input_forward = Input.GetAxis(controls.forward);
        float input_right = Input.GetAxis(controls.right);
        Vector3 MoveDirection = Vector3.zero;
        if (input_forward != 0 || input_right != 0) {
            Vector3 currentRight, currentMoveForward;
            if (pc.gravity.application == MovingEntity.GravityState.useGravity) {
                GetMoveVectors (pc.GroundNormal /*isStableOnGround?GroundNormal:GetUpOrientation()*/, out currentMoveForward, out currentRight);
            } else {
                currentMoveForward = camHandle.forward; currentRight = camHandle.right;
            }
            MoveDirection = (currentRight * input_right) + (currentMoveForward * input_forward);
            MoveDirection.Normalize ();
        } else if (AutoSlowdown) {
            me.IsBrakeOn = true;
        }
        pc.jump.SecondsToPressJump = Input.GetButton (controls.jump)?1:0;
        if(!pc.AutomaticallyTurnToFaceLookDirection) {
            if (pc.gravity.application != MovingEntity.GravityState.none) { pc.UpdateFacing (); }
            else { pc.TurnToFace (myCamera.transform.forward, myCamera.transform.up); }
        }
        return MoveDirection;
    }
    public override void GetMoveVectors(Vector3 upVector, out Vector3 forward, out Vector3 right) {
        Transform importantT = myCamera.transform;
        if(pc.gravity.application != MovingEntity.GravityState.none) {
            Vector3 generalDir = importantT.forward;
            if(importantT.forward == upVector) { generalDir = -importantT.up; } else if(importantT.forward == -upVector) { generalDir = importantT.up; }
            MovingEntity.CalculatePlanarMoveVectors(generalDir, upVector, out forward, out right);
            if(currentPitch > 90) { right *= -1; forward *= -1; } // if we're upside-down, flip to keep it consistent
        } else {
            right = importantT.right; forward = importantT.forward;
        }
    }
    public void GetMoveVectors(out Vector3 forward, out Vector3 right) { GetMoveVectors(pc.GroundNormal, out forward, out right); }
    private bool OrientUp() {
        if (pc.gravity.application != MovingEntity.GravityState.none && cameraUp != -pc.gravity.dir) { 
            Vector3 delta = -pc.gravity.dir - cameraUp;
            float upDifference = delta.magnitude;
            float movespeed = Time.deltaTime * pc.MoveSpeed / 2;
            if (upDifference < movespeed) {
                cameraUp = -pc.gravity.dir;
            } else {
                cameraUp += delta * movespeed;
            }
            return true;
        }
        return false;
    }
}