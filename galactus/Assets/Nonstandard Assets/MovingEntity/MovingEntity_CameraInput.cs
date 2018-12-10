#define DEBUG_LINES
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif
public class MovingEntity_CameraInput : MOB_CameraInput {
    //protected MovingEntity pc;
    protected Platformer pc;
    [Tooltip("how far the eye-focus (where the camera rests) should be 'above' the PlayerControl transform")]
    public float eyeHeight = 0.125f;
    [HideInInspector]
    public float CurrentPitch { get; protected set; }
    /// <summary>keeps the camera from looking too far (only effective with gravity)</summary>
    [Tooltip("keeps the camera from looking too far (only effective with gravity)")]
    public float maxCameraPitch = 90, minCameraPitch = -90;
    /// <summary>the vertical tilt of the camera</summary>
    /// <param name="t">target's transform</param>
    public override Vector3 CameraCenter(Transform t) { return (eyeHeight == 0) ? t.position : t.position + t.up * eyeHeight; }

    public override void MoveCamera(MOB me) {
        if (pc && !pc.PositionOnPlatformDuringFixedUpdate && pc.gravity.application != Platformer.GravityState.none) {
            pc.FollowPositionOnPlatform ();
        }
        bool mustUpdateCamera = OrientUp () | ChangeCameraDistanceBasedOnScrollWheel();
        UpdateCamera (me, mustUpdateCamera);
        me.InputLookDirection = camHandle.forward;
        if (pc && !pc.TurnToMatchCameraDuringFixedUpdate) {
            if (pc.gravity.application != Platformer.GravityState.none) { pc.UpdateFacing(); }
            else { pc.TurnToFace(myCamera.transform.forward, myCamera.transform.up); }
        }
    }
    public override void UpdateCameraAngles(float dx, float dy) {
        if(pc && pc.gravity.application != Platformer.GravityState.none) {
            CurrentPitch -= dy; // rotate accordingly, minus because of standard 'inverted' Y axis rotation
            camHandle.Rotate(Vector3.right, -CurrentPitch);// un-rotate zero-out camera's "up", re-applied soon
            if(cameraUp == Vector3.zero) return;
            Vector3 rightSide = (cameraUp != camHandle.forward)?Vector3.Cross(camHandle.forward, cameraUp):camHandle.right;
            Vector3 unrotatedMoveForward = Vector3.Cross(cameraUp, rightSide);
            camHandle.rotation = Quaternion.LookRotation(unrotatedMoveForward, cameraUp); // force zero rotation
            camHandle.Rotate(Vector3.up, dx); // re-apply rotation
            while(CurrentPitch > 180) { CurrentPitch -= 360; } // normalize the angles to be between -180 and 180
            while(CurrentPitch < -180) { CurrentPitch += 360; }
            CurrentPitch = Mathf.Clamp(CurrentPitch, minCameraPitch, maxCameraPitch);
            camHandle.Rotate(Vector3.right, CurrentPitch);
        } else {
            base.UpdateCameraAngles(dx,dy); // simplistic gravity-less rotation
            cameraUp = camHandle.up;
            CurrentPitch = 0;
        }
    }
    public override void StartControlling(MOB me) {
        pc = me as Platformer;
        base.StartControlling (me);
        // calculate current pitch based on camera
        Vector3 currentRight = Vector3.Cross(cameraUp, camHandle.forward);
        Vector3 currentMoveForward = Vector3.Cross(currentRight, cameraUp);
        Quaternion playerIdentity = Quaternion.LookRotation(currentMoveForward, cameraUp);
        CurrentPitch = Quaternion.Angle(playerIdentity, camHandle.rotation);
        if(pc) { pc.PositionOnPlatformDuringFixedUpdate = false; }
    }
    public override void ReleaseControl(MOB me) {
        if (me != controlling) { Debug.LogError (controlling+" never released!"); }
        pc.PositionOnPlatformDuringFixedUpdate = true;
        pc = null;
        base.ReleaseControl(me);
    }
#if DEBUG_LINES
    GameObject line_f, line_r, line_m;
#endif
    public override Vector3 UpdateInput() {
        float input_forward = Input.GetAxis(controls.forward);
        float input_right = Input.GetAxis(controls.right);
        Vector3 MoveDirection = Vector3.zero;
        Vector3 p = controlling.transform.position + pc.GroundNormal;
        if (input_forward != 0 || input_right != 0) {
            Vector3 currentRight, currentForward;
            if (pc.gravity.application == Platformer.GravityState.useGravity) {
                GetMoveVectors (pc.GroundNormal
                    /*isStableOnGround?GroundNormal:UpDirection*/,
                    out currentForward, out currentRight);
            } else {
                currentForward = camHandle.forward; currentRight = camHandle.right;
            }
            NS.Lines.MakeArrow(ref line_f, p, p + currentForward, Color.blue);
            NS.Lines.MakeArrow(ref line_r, p, p + currentRight, Color.red);
            MoveDirection = (currentRight * input_right) + (currentForward * input_forward);
            MoveDirection.Normalize ();
            if (pc) { pc.MoveEffort = Mathf.Min(Mathf.Abs(input_right) + Mathf.Abs(input_forward), 1); }
        } else if (AutoSlowdown) {
            controlling.IsBrakeOn = true;
            if (pc) { pc.MoveEffort = 0; }
        }
        NS.Lines.MakeArrow(ref line_m, p, p + MoveDirection*2, Color.magenta, 0.25f);
        if(pc) {
            pc.jump.PressJump = Input.GetButton (controls.jump)?1:0;
        }
        if(Input.GetKeyDown(KeyCode.X)) {
            controlling.rb.velocity += controlling.transform.forward * 10;
        }
        return MoveDirection;
    }
    public override void GetMoveVectors(Vector3 upVector, out Vector3 forward, out Vector3 right) {
        Transform importantT = myCamera.transform;
        if(pc.gravity.application != Platformer.GravityState.none) {
            Vector3 generalDir = importantT.forward;
            if(importantT.forward == upVector) { generalDir = -importantT.up; } else if(importantT.forward == -upVector) { generalDir = importantT.up; }
            MovingEntity.CalculatePlanarMoveVectors(generalDir, upVector, out forward, out right);
            if(CurrentPitch > 90) { right *= -1; forward *= -1; } // if we're upside-down, flip to keep it consistent
        } else {
            right = importantT.right; forward = importantT.forward;
        }
    }
    public void GetMoveVectors(out Vector3 forward, out Vector3 right) {
        GetMoveVectors(pc.GroundNormal, out forward, out right);
    }
    private bool OrientUp() {
        if (pc && pc.gravity.application != Platformer.GravityState.none && cameraUp != -pc.gravity.dir) { 
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
public class MOB_CameraInput : MonoBehaviour {
    [Tooltip("if left null, will search for a MobileEntity script on this object.")]
    public MOB controlling;
    /// <summary>the transform controlling where the camera should go. Might be different than myCamera if a VR headset is plugged in.</summary>
    [HideInInspector]
    public Transform camHandle;
    [Tooltip("Camera for the PlayerControl to use. Will automagically find one if not set.")]
    public Camera myCamera;
    /// <summary>how the 3D camera should move with the player.</summary>
    [Tooltip("how the 3D camera should move with the player\n" +
        "* Fixed Camera: other code should control the camera\n" +
        "* Lock To Player: follow player with current offset\n" +
        "* Rotate 3rd Person: 3rd person, scrollwheel zoom\n" +
        "* Lock-and-Rotate-with-RMB: like the Unity editor Scene view")]
    public ControlStyle mouseLookMode = ControlStyle.freeRotate;
    public enum ControlStyle { staticCamera, noRotate, freeRotate, rotateWithRMB }
    /// <summary>how far away the camera should be from the player</summary>
    protected Vector3 cameraOffset;
    [System.Serializable]
    public class ControlAxisNames
    {
        public string forward = "Vertical", right = "Horizontal",
        turnHorizontal = "Mouse X", turnVertical = "Mouse Y",
        jump = "Jump", zoomInAndOut = "Mouse ScrollWheel";
    }
    [Tooltip("Axis used for controls\n(see: Edit->Project Settings->Input")]
    public ControlAxisNames controls = new ControlAxisNames();
    [Tooltip("if true, automatically slow velocity to zero if there is no user-input")]
    public bool AutoSlowdown = true;
    [Tooltip("If true, a raycast is sent to make sure the camera doesn't clip through solid objects.")]
    public bool cameraWontPassWalls = true;
    public bool letMOBAlignDuringFixedUpdate = false;
    public float horizontalSensitivity = 5, verticalSensitivity = 5;
    [Tooltip("how far the camera should be from the PlayerControl transform")]
    public float cameraDistance = 5;
    /// <summary>'up' direction for the player, used to orient the camera when there is gravity</summary>
    [HideInInspector]
    public Vector3 cameraUp = Vector3.up;

    public MOB GetControlled() { return controlling; }

	public virtual void Initialize(){
		MOB toControl = controlling;
		controlling = null; // temporarily set controlling to null so that proper initialization happens to it
		if (toControl == null) { toControl = GetComponent<MOB> (); }
		Control(toControl);
	}
    void Start() {
		Initialize ();
    }
    public virtual void Control(MOB entity) {
        if (controlling != null) { ReleaseControl (controlling); } // un-control previous
        controlling = entity; StartControlling(controlling); // take control of next
    }
    public virtual void Update() {
        if(controlling != null) {
            controlling.InputMoveDirection = UpdateInput();
            //if(controlling.InputMoveDirection != Vector3.zero) {
            //    float inputF = Input.GetAxis(controls.forward), inputR = Input.GetAxis(controls.right);
            //    if(inputF == 0 && inputR == 0) {
            //        controlling.InputMoveDirection = Vector3.zero;
            //    }
            //    //Debug.Log("CAMERA UPDATE "+inputF+" "+inputR);
            //}
        }
    }
    // let the controlled entity call LateUpdate after any positional adjustments
    void LateUpdate() {
        if(controlling != null) {
            MoveCamera(controlling);
        }
    }
    /// <summary>the vertical tilt of the camera</summary>
    /// <param name="t">target's transform</param>
    public virtual Vector3 CameraCenter(Transform t) { return t.position; }

    public void Copy(MOB_CameraInput c) { Reflection.AssignValues(this, c); }

    public virtual void UpdateCameraAngles(float dx, float dy) {
        // simplistic gravity-less rotation
        camHandle.Rotate(Vector3.up, dx);
        camHandle.Rotate(Vector3.right, -dy);
    }
    protected void EnsureCamera(MOB mob) {
        if (!myCamera) { // make sure there is a camera to control!
            myCamera = Camera.main;
            if (myCamera == null) {
                myCamera = (new GameObject ("<main camera>")).AddComponent<Camera> ();
                myCamera.tag = "MainCamera";
            }
        } else {
            cameraOffset = camHandle.position - mob.transform.position;
            cameraDistance = cameraOffset.magnitude;
        }
        if(UnityEngine.XR.XRDevice.isPresent) {
            camHandle = (new GameObject("<camera handle>")).transform;
            myCamera.transform.position = Vector3.zero;
            myCamera.transform.SetParent(camHandle);
        } else {
            camHandle = myCamera.transform;
        }
        UpdateCamera (mob, true);
    }
    public virtual void StartControlling(MOB me) {
        if(me != null) {
            if(me.transform.tag == "Untagged" || controlling.transform.tag.Length == 0) { me.transform.tag = "Player"; }
            if(!letMOBAlignDuringFixedUpdate){
                me.TurnToMatchCameraDuringFixedUpdate = false;
            }
        }
        EnsureCamera (me);
    }
    public virtual void ReleaseControl(MOB me) {
        if (me.transform.tag == "Player") { me.transform.tag = "Untagged"; }
        if(!letMOBAlignDuringFixedUpdate){
            me.TurnToMatchCameraDuringFixedUpdate = true;
        }
    }
    public bool ChangeCameraDistanceBasedOnScrollWheel() {
        float scroll = Input.GetAxis(controls.zoomInAndOut);
        if(scroll != 0) {
            cameraDistance -= scroll * 10;
            cameraDistance = Mathf.Max(0, cameraDistance);
            if(cameraDistance > 0 && cameraOffset != Vector3.zero) {
                if(cameraOffset == Vector3.zero) {
                    cameraOffset = -camHandle.forward;
                }
                cameraOffset = cameraOffset.normalized * cameraDistance;
            }
            return true;
        }
        return false;
    }
    public virtual void MoveCamera(MOB me) {
        bool scrollWheelAdjusted = ChangeCameraDistanceBasedOnScrollWheel();
        UpdateCamera(me, scrollWheelAdjusted);
        me.InputLookDirection = camHandle.forward;
        if (!me.TurnToMatchCameraDuringFixedUpdate) {
            me.TurnToFace(myCamera.transform.forward, myCamera.transform.up);
        }
    }
    public virtual void UpdateCamera(MOB me, bool cameraMoved) {
        if(mouseLookMode == ControlStyle.staticCamera && !cameraMoved) { return; }
        bool updatingWithMouseInput = (mouseLookMode == ControlStyle.freeRotate) || 
            (mouseLookMode == ControlStyle.rotateWithRMB && Input.GetMouseButton(1));
        // camera rotation
        if (updatingWithMouseInput) {
            // get the rotations that the user input is indicating
            UpdateCameraAngles (Input.GetAxis (controls.turnHorizontal) * horizontalSensitivity, Input.GetAxis (controls.turnVertical) * verticalSensitivity);
        } else if (cameraMoved) {
            UpdateCameraAngles (0, 0);
        }
        Vector3 eyeFocus = CameraCenter(me.transform);
        // move the camera to be looking at the player's eyes/head, ideally with no geometry in the way
        RaycastHit rh;
        float calculatedDistForCamera = cameraDistance;
        if(cameraWontPassWalls && Physics.SphereCast(eyeFocus, myCamera.nearClipPlane, -camHandle.forward, out rh, cameraDistance)) {
            calculatedDistForCamera = rh.distance;
        }
        if(calculatedDistForCamera != 0) { cameraOffset = -myCamera.transform.forward * calculatedDistForCamera; }
        Vector3 nextLocation = eyeFocus + ((cameraDistance > 0) ? cameraOffset : Vector3.zero);
        camHandle.position = nextLocation;
    }
    public virtual Vector3 UpdateInput() {
        float inputF = Input.GetAxis (controls.forward), inputR = Input.GetAxis (controls.right);
        Vector3 MoveDirection = Vector3.zero;
        const float EPSILON = 1f / 1024;
        if (System.Math.Abs(inputF) > EPSILON || System.Math.Abs(inputR) > EPSILON) {
            Transform t = myCamera.transform;
            MoveDirection = (inputR * t.right) + (inputF * t.forward);
            MoveDirection.Normalize ();
        } else if (AutoSlowdown) {
            controlling.IsBrakeOn = true;
        }
        if (MoveDirection != Vector3.zero)
        {
            Debug.Log("????>>>>>>>" + MoveDirection);
        }
        return MoveDirection;
    }
    public virtual void GetMoveVectors(Vector3 upVector, out Vector3 forward, out Vector3 right) {
        Transform importantT = myCamera.transform;
        right = importantT.right; forward = importantT.forward;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MOB_CameraInput), true)]
public class CustomEditor_CameraInput : CustomEditor_TYPE_ADJUSTABLE<MOB_CameraInput> { }
#endif