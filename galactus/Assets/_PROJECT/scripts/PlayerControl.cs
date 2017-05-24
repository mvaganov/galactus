using UnityEngine;

/// <summary>A custom Unity3D character controller
/// Latest version at: https://pastebin.com/9M9qyBmP </summary>
/// <description>MIT License - TL;DR - This code is free, don't bother me about it!</description>
/// <author email="mvaganov@hotmail.com">Michael Vaganov</author>

/// a basic Mobile Entity controller (for MOBs, like seeking fireballs, or very basic enemies)
public class MobileEntity : MonoBehaviour {
	/// <summary>if being controlled by player, this value is constanly reset by user input. otherwise, useable as AI controls.</summary>
	[HideInInspector]
	public Vector3 MoveDirection, LookDirection;
	[Tooltip("movement speed")]
	public float MoveSpeed = 5;
	[Tooltip("how quickly the PlayerControl transform rotates to match the intended direction, measured in degrees-per-second")]
	public float TurnSpeed = 180;
	[Tooltip("how quickly the PlayerControl reaches MoveSpeed. If less-than zero, jump straight to move speed.")]
	public float acceleration = -1;  // TODO capitalize
	[Tooltip("if true, mouse controls and WASD function to move around the player. Otherwise, uses Non Player Control settings")]
	public bool PlayerControlled = true;
	[Tooltip("if true, automatically bring velocity to zero if there is no user-input or NonPlayerControlledDirection")]
	public bool AutoSlowdown = true;
	/// <summary>the player's RigidBody, which lets Unity do all the physics for us</summary>
	[HideInInspector]
	public Rigidbody rb { get; protected set; }

	/// <summary>cached variables required for a host of calculations</summary>
	public float CurrentSpeed { get; protected set; }
	public float BrakeDistance { get; protected set; } // TODO capitalize
	[HideInInspector]
	public bool IsBrakeOn = false;
	protected bool brakesOnLastFrame = false;
	public bool IsMovingIntentionally { get; protected set; }
	[Tooltip("If true, the player will constantly move as though forward is being pressed. Useful for VR controls that rely on looking only")]
	public bool IsAlwaysPressingForward = false;

	#region Camera Control
	public CameraControl cameraControl;

	public virtual Vector3 CameraCenter() { return transform.position; }
	[System.Serializable]
	public class CameraControl {
		[Tooltip("Camera for the PlayerControl to use. Will automagically find one if not set.")]
		public Camera myCamera;
		/// <summary>the transform controlling where the camera should go. Might be different than myCamera if a VR headset is plugged in.</summary>
		[HideInInspector]
		public Transform camHandle;
		/// <summary>how the 3D camera should move with the player.</summary>
		[Tooltip("how the 3D camera should move with the player\n"+
			"* Fixed Camera: other code should control the camera\n"+
			"* Lock To Player: follow player with current offset\n"+
			"* Rotate 3rd Person: 3rd person, scrollwheel zoom\n"+
			"* Lock-and-Rotate-with-RMB: like the Unity editor Scene view")]
		public ControlStyle controlMode = ControlStyle.lockAndRotateWithRMB;
		public enum ControlStyle { fixedCamera, lockToPlayer, rotate3rdPerson, lockAndRotateWithRMB }
		/// <summary>how far away the camera should be from the player</summary>
		protected Vector3 cameraOffset;
		[Tooltip("how far the camera should be from the PlayerControl transform")]
		public float cameraDistance;
		public float horizontalMouselookSpeed = 5, verticalMouselookSpeed = 5;
		[Tooltip("If true, a raycast is sent to make sure the camera doesn't clip through solid objects.")]
		public bool cameraWontClip = true;

		public void Copy(CameraControl cameraControl) {
			myCamera = cameraControl.myCamera;
			camHandle = cameraControl.camHandle;
			controlMode = cameraControl.controlMode;
			cameraDistance = cameraControl.cameraDistance;
			horizontalMouselookSpeed = cameraControl.horizontalMouselookSpeed;
			verticalMouselookSpeed = cameraControl.verticalMouselookSpeed;
			cameraWontClip = cameraControl.cameraWontClip;
		}
		public virtual void UpdateCameraAngles(MobileEntity me, float dx, float dy) {
			// simplistic gravity-less rotation
			camHandle.Rotate(Vector3.up, dx);
			camHandle.Rotate(Vector3.right, -dy);
		}
		public virtual void Start(MobileEntity p) {
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
			if(UnityEngine.VR.VRDevice.isPresent) {
				camHandle = (new GameObject("<camera handle>")).transform;
				myCamera.transform.position = Vector3.zero;
				myCamera.transform.SetParent(camHandle);
			} else {
				camHandle = myCamera.transform;
			}
			LateUpdate(p, true);
		}
		public bool ChangeCameraDistanceBasedOnScrollWheel() {
			float scroll = Input.GetAxis("Mouse ScrollWheel");
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
		public virtual void LateUpdate(MobileEntity me, bool mustUpdateCamera) {
			mustUpdateCamera |= ChangeCameraDistanceBasedOnScrollWheel();
			UpdateCamera (me, mustUpdateCamera);
		}
		public virtual void UpdateCamera(MobileEntity me, bool mustUpdate) {
			if(controlMode != ControlStyle.fixedCamera || mustUpdate) {
				bool updatingWithMouseInput = (controlMode == ControlStyle.rotate3rdPerson) || (controlMode == ControlStyle.lockAndRotateWithRMB && Input.GetMouseButton(1));
				// camera rotation
				if (updatingWithMouseInput) {
					// rotate from the controller
					float dx = Input.GetAxis("Oculus_GearVR_RThumbstickX"), dy = Input.GetAxis("Oculus_GearVR_RThumbstickY");
					if(dx == 0 || Mathf.Abs(dx) < 1.0f/16) { dx = Input.GetAxis("Mouse X"); } else dx/=2; // controller input should be less sensitive than the mouse
					if(dy == 0 || Mathf.Abs(dy) < 1.0f/16) { dy = Input.GetAxis("Mouse Y"); } else dy/=2;
					// get the rotations that the user input is indicating
					UpdateCameraAngles(me, dx * horizontalMouselookSpeed, dy * verticalMouselookSpeed);
				} else if (mustUpdate) {
					UpdateCameraAngles (me, 0, 0);
				}
				me.LookDirection = camHandle.forward;
				Vector3 eyeFocus = me.CameraCenter();
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
		}
	}
	#endregion // Camera Control
	#region movement and directional control
	public bool IsBraking() { return IsBrakeOn || brakesOnLastFrame; }
	public virtual void MoveLogic() {
		if(!IsBrakeOn) {
			IsMovingIntentionally = false;
			if (PlayerControlled) {
				float inputF = IsAlwaysPressingForward?1:Input.GetAxis ("Vertical"), inputR = Input.GetAxis ("Horizontal");
				if (IsMovingIntentionally = (inputF != 0 || inputR != 0)) {
					Transform t = cameraControl.myCamera.transform;
					MoveDirection = (inputR * t.right) + (inputF * t.forward);
					MoveDirection.Normalize ();
				} else {
					MoveDirection = default(Vector3);
				}
			} else {
				IsMovingIntentionally = MoveDirection != Vector3.zero;
			}
		}
		if (transform.forward != LookDirection && TurnSpeed != 0) { TurnToFace(LookDirection, Vector3.zero); }	// turn body as needed
		ApplyMove();
	}

	protected bool TurnToFace(Vector3 forward, Vector3 up) {
		if((forward != Vector3.zero && transform.forward != forward) || (up != transform.up && up != Vector3.zero)) {
			if(up == Vector3.zero) {
				Vector3 r = (forward == transform.right) ? -transform.forward : ((forward == -transform.right) ? transform.forward : transform.right);
				up = Vector3.Cross(forward, r);
			}
			Quaternion target = Quaternion.LookRotation(forward, up);
			Quaternion q = Quaternion.RotateTowards(transform.rotation, target, TurnSpeed * Time.deltaTime);
			transform.rotation = q;
			return true;
		}
		return false;
	}
	public virtual void GetMoveVectors(Vector3 upVector, out Vector3 forward, out Vector3 right) {
		Transform importantT = (PlayerControlled) ? cameraControl.myCamera.transform : transform;
		right = importantT.right; forward = importantT.forward;
	}
	protected virtual void EnforceSpeedLimit() {
		float actualSpeed = rb.velocity.magnitude;
		if(actualSpeed > MoveSpeed) {
			rb.velocity = rb.velocity.normalized*MoveSpeed;
		}
	}
	/// <summary>Applies AccelerationDirection to the velocity. if brakesOn, slows things down.</summary>
	protected void ApplyMove() {
		if(acceleration <= 0) {
			rb.velocity = (!IsBrakeOn) ? (MoveDirection * MoveSpeed) : Vector3.zero;
		} else {
			float amountToMove = acceleration * Time.deltaTime;
			if(!IsBrakeOn && IsMovingIntentionally) {
				rb.velocity += MoveDirection * amountToMove;
				EnforceSpeedLimit();
			} else if(IsBrakeOn || AutoSlowdown) {
				MoveDirection = -rb.velocity.normalized;
				float actualSpeed = rb.velocity.magnitude;
				if(actualSpeed > acceleration * amountToMove) {
					rb.velocity += MoveDirection * amountToMove;
				} else {
					rb.velocity = Vector3.zero;
				}
			}
		}
	}
	public static float CalculateBrakeDistance(float speed, float acceleration) { return (speed * speed) / (2 * acceleration); }
	#endregion // movement and directional control
	#region Steering Behaviors
	protected Vector3 SeekMath(Vector3 directionToLookToward) {
		Vector3 desiredVelocity = directionToLookToward * MoveSpeed;
		Vector3 desiredChangeToVelocity = desiredVelocity - rb.velocity;
		float desiredChange = desiredChangeToVelocity.magnitude;
		if (desiredChange < Time.deltaTime * acceleration) { return directionToLookToward; }
		Vector3 steerDirection = desiredChangeToVelocity.normalized;
		return steerDirection;
	}
	/// <summary>used by the Grounded controller, to filter positions into ground-space (for better tracking on curved surfaces)</summary>
	protected virtual Vector3 CalculatePositionForAutomaticMovement(Vector3 position) { return position; }
	/// <summary>called during a FixedUpdate process to give this agent simple AI movement</summary>
	public void CalculateSeek(Vector3 target, out Vector3 directionToMoveToward, ref Vector3 directionToLookToward) {
		Vector3 delta = target - transform.position;
		if (delta == Vector3.zero) { directionToMoveToward = Vector3.zero; return; }
		float desiredDistance = delta.magnitude;
		directionToLookToward = delta / desiredDistance;
		directionToMoveToward = (acceleration > 0)?SeekMath (directionToLookToward):directionToLookToward;
	}
	/// <summary>called during a FixedUpdate process to give this agent simple AI movement</summary>
	public void CalculateArrive (Vector3 target, out Vector3 directionToMoveToward, ref Vector3 directionToLookToward) {
		Vector3 delta = target - transform.position;
		if (delta == Vector3.zero) { directionToMoveToward = Vector3.zero; return; }
		float desiredDistance = delta.magnitude;
		directionToLookToward = delta / desiredDistance;
		if (desiredDistance > 1.0f / 1024 && desiredDistance > BrakeDistance) {
			directionToMoveToward = SeekMath (directionToLookToward);
			return;
		}
		directionToMoveToward = -directionToLookToward;
		IsBrakeOn = true;
	}
	/// <summary>call this during a FixedUpdate process to give this agent simple AI movement</summary>
	public void Seek(Vector3 target) {
		CalculateSeek (CalculatePositionForAutomaticMovement(target), out MoveDirection, ref LookDirection);
	}
	/// <summary>call this during a FixedUpdate process to give this agent simple AI movement</summary>
	public void Flee(Vector3 target) {
		CalculateSeek (CalculatePositionForAutomaticMovement(target), out MoveDirection, ref LookDirection);
		MoveDirection *= -1;
	}
	/// <summary>call this during a FixedUpdate process to give this agent simple AI movement</summary>
	public void Arrive(Vector3 target) {
		CalculateArrive (CalculatePositionForAutomaticMovement(target), out MoveDirection, ref LookDirection);
	}
	/// <summary>call this during a FixedUpdate process to give this agent simple AI movement</summary>
	public void RandomWalk(float weight = 0, Vector3 weightedTowardPoint = default(Vector3)) {
		if (weight != 0) {
			Vector3 dir = (CalculatePositionForAutomaticMovement (weightedTowardPoint) - transform.position).normalized;
			dir = (dir * weight) + (Random.onUnitSphere * (1 - weight));
			Vector3 p = CalculatePositionForAutomaticMovement (transform.position + transform.forward + dir);
			Vector3 delta = p - transform.position;
			MoveDirection = LookDirection = delta.normalized;
		} else {
			Vector3 p = CalculatePositionForAutomaticMovement (transform.position + transform.forward + Random.onUnitSphere);
			Vector3 delta = p - transform.position;
			MoveDirection = LookDirection = delta.normalized;
		}
	}
	#endregion // Steering Behaviors
	#region MonoBehaviour
	void Start() {
		rb = GetComponent<Rigidbody>();
		if(!rb) { rb = gameObject.AddComponent<Rigidbody>(); }
		rb.useGravity = false; rb.freezeRotation = true;
		if(PlayerControlled) {
			if (cameraControl == null) cameraControl = new CameraControl ();
			cameraControl.Start(this);
		}
		if(PlayerControlled && transform.tag == "Untagged" || transform.tag.Length == 0) { transform.tag = "Player"; }
	}
	public void UpdateMotionVariables() {
		CurrentSpeed = rb.velocity.magnitude;
		BrakeDistance = (acceleration > 0)?CalculateBrakeDistance (CurrentSpeed,acceleration/rb.mass)-(CurrentSpeed*Time.deltaTime):0;
		if (IsBrakeOn) {
			brakesOnLastFrame = true;
			IsBrakeOn = false;
		} else {
			brakesOnLastFrame = false;
		}
	}
	/// <summary>where physics-related changes happen</summary>
	void FixedUpdate() {
		MoveLogic();
		UpdateMotionVariables ();
	}
	/// <summary>where visual-related updates should happen</summary>
	void LateUpdate() { if(PlayerControlled) { cameraControl.LateUpdate(this, false); } }
	#endregion MonoBehaviour
}

public class PlayerControl : MobileEntity {
	#region Public API
	/// <summary>true if on the ground</summary>
	[HideInInspector]
	public bool IsStableOnGround { get; protected set; }
	/// <summary>if OnCollision event happens with ground, this is true</summary>
	[HideInInspector]
	public bool IsCollidingWithGround { get; protected set; }
	[Tooltip("if false, disables gravity's effects, and enables flying")]
	public bool ApplyGravity = true;
	/// <summary>the ground-plane normal, used to determine which direction 'forward' is when it comes to movement on the ground</summary>
	[Tooltip("if true, keep camera's 'down' aligned with gravity, even if not applying gravity")]
	public bool AlwaysUseGravityAsUpDirection = false;
	public enum HowToDealWithGround {none, withPlaceholder, withParenting}
	[Tooltip("how the player should stay stable if the ground is moving\n"+
		"* none: don't bother trying to be stable\n"+
		"* withPlaceholder: placeholder transform keeps track\n"+
		"* withParenting: player is parented to ground (not safe if ground is scaled)\n")]
	public HowToDealWithGround stickToGround = HowToDealWithGround.withPlaceholder;
	[HideInInspector]
	public Vector3 GroundNormal { get; protected set; }
	[HideInInspector]
	/// <summary>What object is being stood on</summary>
	public GameObject StandingOnObject { get; protected set; }
	/// <summary>the angle of standing, compared to gravity, used to determine if the player can reliably walk on this surface (it might be too slanted)</summary>
	[HideInInspector]
	public float StandAngle { get; protected set; }
	/// <summary>keeps the camera from looking too far</summary>
	[Tooltip("keeps the camera from looking too far")]
	public float maxCameraPitch = 90, minCameraPitch = -90;
	[Tooltip("The maximum angle the player can move forward at while standing on the ground")]
	public float MaxWalkAngle = 45;
	/// <summary>if very far from ground, this value is infinity</summary>
	[HideInInspector]
	public float HeightFromGround { get; protected set; }
	[HideInInspector]
	public Collider CollideBox { get; protected set; }
	/// <summary>expected distance from the center of the collider to the ground. auto-populates based on Box of Capsule collider</summary>
	[HideInInspector]
	public float ExpectedHeightFromGround;
	/// <summary>expected distance from the center of the collider to a horizontal edge. auto-populates based on Box of Capsule collider</summary>
	[HideInInspector]
	public float ExpectedHorizontalRadius;
	/// <summary>how much downward velocity the player is experiencing. non-zero when jumping or falling.</summary>
	[HideInInspector]
	public float DownwardVelocity { get; protected set; }

	public Vector3 GetUpOrientation() { return -gravity.dir; }
	public Vector3 GetUpBodyDirection() { return GroundNormal; }
	public override void GetMoveVectors(Vector3 upVector, out Vector3 forward, out Vector3 right) {
		Transform importantT = (PlayerControlled) ? cameraControl.myCamera.transform : transform;
		if(ApplyGravity) {
			Vector3 generalDir = importantT.forward;
			if(importantT.forward == upVector) { generalDir = -importantT.up; } else if(importantT.forward == -upVector) { generalDir = importantT.up; }
			CalculatePlanarMoveVectors(generalDir, upVector, out forward, out right);
			if(PlayerControlled && currentPitch > 90) { right *= -1; forward *= -1; } // if we're upside-down, flip to keep it consistent
		} else {
			right = importantT.right; forward = importantT.forward;
		}
	}
	public void GetMoveVectors(out Vector3 forward, out Vector3 right) { GetMoveVectors(GroundNormal, out forward, out right); }
	/// <summary>Used for temporary disabling of controls. Helpful for cutscenes/dialog</summary>
	public void Toggle() { this.enabled = !this.enabled; }
	public void RandomColor() { GetComponent<Renderer>().material.color = Random.ColorHSV(); }
	/// <summary>Calculates correct look vectors given a general forward and an agreeable up</summary>
	public static void CalculatePlanarMoveVectors(Vector3 generalForward, Vector3 upVector, out Vector3 forward, out Vector3 right) {
		right = Vector3.Cross(upVector, generalForward).normalized;
		forward = Vector3.Cross(right, upVector).normalized;
	}
	public float DistanceTo(Vector3 loc) {
		return Vector3.Distance(transform.position, loc) - ExpectedHorizontalRadius;
	}
	public float DistanceTo(Transform mob) {
		float otherRad = mob.transform.localScale.z;
		Collider c = mob.GetComponent<Collider>();
		if(c != null) { otherRad *= c.bounds.extents.z; }
		return Vector3.Distance(transform.position, mob.transform.position) - (ExpectedHorizontalRadius + otherRad);
	}
	public Vector3 GetVelocity() { return rb.velocity; }
	/// <summary>if a null pointer happens while trying to access a rigidbody variable, call this first.</summary>
	public void EnsureRigidBody() {
		if(rb == null) {
			rb = GetComponent<Rigidbody>();
			if(!rb) { rb = gameObject.AddComponent<Rigidbody>(); }
			rb.useGravity = false; rb.freezeRotation = true;
		}
	}
	#endregion // Public API
	#region Grounded Camera Control
	[Tooltip("how far the eye-focus (where the camera rests) should be above the PlayerControl transform")]
	public float eyeHeight = 0.125f;
	/// <summary>'up' direction for the player, used to orient the camera</summary>
	[HideInInspector]
	public Vector3 cameraUp = Vector3.up;
	/// <summary>the vertical tilt of the camera</summary>
	[HideInInspector]
	public float currentPitch { get; protected set; }
	public override Vector3 CameraCenter() { return transform.position + transform.up * eyeHeight; }
	public void UpdateCameraAngles(float dx, float dy) {
		currentPitch -= dy; // rotate accordingly, minus because of standard 'inverted' Y axis rotation
		cameraControl.camHandle.Rotate(Vector3.right, -currentPitch);// un-rotate zero-out camera's "up", re-applied soon
		if(cameraUp == Vector3.zero || cameraUp == cameraControl.camHandle.forward) return;
		Vector3 rightSide = Vector3.Cross(cameraControl.camHandle.forward, cameraUp);
		Vector3 unrotatedMoveForward = Vector3.Cross(cameraUp, rightSide);
		cameraControl.camHandle.rotation = Quaternion.LookRotation(unrotatedMoveForward, cameraUp); // force zero rotation
		cameraControl.camHandle.Rotate(Vector3.up, dx); // re-apply rotation
		while(currentPitch > 180) { currentPitch -= 360; } // normalize the angles to be between -180 and 180
		while(currentPitch < -180) { currentPitch += 360; }
		currentPitch = Mathf.Clamp(currentPitch, minCameraPitch, maxCameraPitch);
		cameraControl.camHandle.Rotate(Vector3.right, currentPitch);
	}
	private bool OrientUp() {
		if (ApplyGravity && cameraUp != -gravity.dir) { 
			Vector3 delta = -gravity.dir - cameraUp;
			float upDifference = delta.magnitude;
			float movespeed = Time.deltaTime * MoveSpeed / 2;
			if (upDifference < movespeed) {
				cameraUp = -gravity.dir;
			} else {
				cameraUp += delta * movespeed;
			}
			return true;
		}
		return false;
	}
	public class GroundedCameraControl : MobileEntity.CameraControl {
		public GroundedCameraControl(MobileEntity.CameraControl cameraControl) { base.Copy(cameraControl); }
		public override void UpdateCameraAngles(MobileEntity me, float dx, float dy) {
			PlayerControl p = (me as PlayerControl);
			if(p.ApplyGravity || p.AlwaysUseGravityAsUpDirection) {
				p.UpdateCameraAngles (dx, dy);
			} else {
				base.UpdateCameraAngles(me, dx,dy); // simplistic gravity-less rotation
				p.cameraUp = camHandle.up;
				p.currentPitch = 0;
			}
		}
		public override void Start(MobileEntity me) {
			base.Start (me);
			PlayerControl p = me as PlayerControl;
			// calculate current pitch based on camera
			Vector3 currentRight = Vector3.Cross(p.cameraUp, camHandle.forward);
			Vector3 currentMoveForward = Vector3.Cross(currentRight, p.cameraUp);
			Quaternion playerIdentity = Quaternion.LookRotation(currentMoveForward, p.cameraUp);
			p.currentPitch = Quaternion.Angle(playerIdentity, camHandle.rotation);
		}
	}
	#endregion // Grounded Camera Control
	#region Steering Behaviors
	protected override Vector3 CalculatePositionForAutomaticMovement(Vector3 position) {
		if (ApplyGravity && IsStableOnGround) {
			Vector3 delta = position - transform.position;
			float notOnGround = Vector3.Dot (GroundNormal, delta);
			delta -= GroundNormal * notOnGround;
			if (delta == Vector3.zero) {
				return Vector3.zero;
			}
			position = transform.position + delta;
		}
		return position;
	}
	#endregion
	#region Core Controller Logic
	/// <summary>used when the player controller needs to stay on a moving platform or vehicle</summary>
	private Transform transformOnPlatform;
	/// <summary>true if player is walking on ground that is too steep.</summary>
	public bool tooSteep { get; protected set; }
	/// <summary>where the 'leg' raycast comes down from. changes if the leg can't find footing.</summary>
	private Vector3 legOffset;
	/// <summary>cache the data structure with the object, to improve performance</summary>
	private RaycastHit rayDownHit = new RaycastHit();

	void StandLogic() {
		Vector3 hip = (legOffset!=Vector3.zero) ? transform.TransformPoint(legOffset) : transform.position;
		const float epsilon = 0.0625f;
		float lengthOfLeg = ExpectedHeightFromGround+ExpectedHorizontalRadius*5;
		// use a raycast to check if we're on the ground
		CollideBox.enabled = false;
		// if we're grounded enough (close enough to the ground)
		if(Physics.Raycast(hip, /*gravity.dir*/ -transform.up, out rayDownHit, lengthOfLeg)) {
			// record some important details about the space we're standing at
			GroundNormal = rayDownHit.normal;
			StandAngle = Vector3.Angle(GetUpOrientation(), GroundNormal);
			tooSteep = (StandAngle > MaxWalkAngle);
			HeightFromGround = rayDownHit.distance;
			if(IsCollidingWithGround || HeightFromGround < ExpectedHeightFromGround + epsilon) {
				IsStableOnGround = true;
				StandingOnObject = rayDownHit.collider.gameObject;
				switch(stickToGround){
				case HowToDealWithGround.withPlaceholder:
					if (transformOnPlatform == null) {
						transformOnPlatform = (new GameObject ("<where " + name + " stands>")).transform;
					}
					transformOnPlatform.SetParent (StandingOnObject.transform);
					break;
				case HowToDealWithGround.withParenting:
					transform.SetParent (StandingOnObject.transform);
					break;
				}
				legOffset = Vector3.zero;
				float downwardV = Vector3.Dot(rb.velocity, gravity.dir); // check if we're falling.
				if(downwardV > 0) {
					rb.velocity = rb.velocity - downwardV * gravity.dir; // stop falling, we're on the ground.
				}
				if(HeightFromGround < ExpectedHeightFromGround - epsilon) {
					rb.velocity += GetUpOrientation() * epsilon; // if we're not standing tall enough, edge up a little.
				}
			} else {
				IsStableOnGround = false;
			}
		} else {
			HeightFromGround = float.PositiveInfinity;
			IsStableOnGround = false;
		}
		if(!IsStableOnGround) { // if we're not grounded enough, mark it
			IsStableOnGround = false;
			StandingOnObject = null;
			tooSteep = false;
			switch(stickToGround){
			case HowToDealWithGround.withPlaceholder:
				if (transformOnPlatform != null) {
					transformOnPlatform.SetParent (null);
				}
				break;
			}
			// if we couldn't find ground with our leg here, try another location.
			legOffset = new Vector3(
				Random.Range(-1.0f, 1.0f) * ExpectedHorizontalRadius, 0,
				Random.Range(-1.0f, 1.0f) * ExpectedHorizontalRadius);
		}
		CollideBox.enabled = true;
	}
	public override void MoveLogic() {
		if(PlayerControlled) {
			float input_forward = IsAlwaysPressingForward?1:Input.GetAxis("Vertical");
			float input_right = Input.GetAxis("Horizontal");
			if (IsMovingIntentionally = (input_forward != 0 || input_right != 0)) {
				Vector3 currentRight, currentMoveForward;
				GetMoveVectors (GroundNormal,//(isStableOnGround)?groundNormal:GetUpOrientation(),//
					out currentMoveForward, out currentRight);
				MoveDirection = (currentRight * input_right) + (currentMoveForward * input_forward);
				MoveDirection.Normalize ();
			} else {
				MoveDirection = Vector3.zero;
			}
		} else {
			IsMovingIntentionally = MoveDirection != Vector3.zero;
			if (IsMovingIntentionally) {
				float amountOfVertical = Vector3.Dot (MoveDirection, GroundNormal);
				MoveDirection -= GroundNormal * amountOfVertical;
				IsMovingIntentionally = MoveDirection != Vector3.zero;
				if (IsMovingIntentionally) {  MoveDirection.Normalize (); }
			}
		}
		if(ApplyGravity) {
			float walkAngle = 90 - Vector3.Angle(GetUpOrientation(), rb.velocity);
			if(walkAngle > MaxWalkAngle || StandAngle > MaxWalkAngle) {
				IsStableOnGround = false;
			}
			if(!IsStableOnGround) {
				rb.velocity += gravity.power * gravity.dir * Time.deltaTime; // TODO mass into account for gravity calc?
			}
			DownwardVelocity = Vector3.Dot(rb.velocity, gravity.dir);
			if(tooSteep && IsMovingIntentionally) {
				Vector3 acrossSlope = Vector3.Cross(GroundNormal, -gravity.dir).normalized;
				Vector3 intoSlope = Vector3.Cross(acrossSlope, -gravity.dir).normalized;
				float upHillAmount = Vector3.Dot(MoveDirection, intoSlope);
				if(upHillAmount > 0) {
					MoveDirection -= intoSlope * upHillAmount;
					MoveDirection.Normalize ();
				}
			}
			ApplyMove();
			if(!IsStableOnGround) {
				rb.velocity -= gravity.dir * Vector3.Dot(rb.velocity, gravity.dir); // subtract donward-force from move speed
				rb.velocity += gravity.dir * DownwardVelocity; // re-apply downward-force from gravity
			}
			jump.Update(this);
		} else {
			ApplyMove();
		}
		// turn body as needed
		if(TurnSpeed != 0) {
			Vector3 rG, correctUp;
			if(!IsCollidingWithGround && !IsStableOnGround && float.IsInfinity(HeightFromGround)) {
				correctUp = GetUpOrientation();
			} else {
				correctUp = (GroundNormal!=Vector3.zero)?GroundNormal:transform.up;
			}
			// turn IF needed
			if ((LookDirection != Vector3.zero && LookDirection != transform.forward) ||
			   (correctUp != transform.up)) {
				if (PlayerControlled) {
					GetMoveVectors (correctUp, out LookDirection, out rG); // TODO maybe the previous GetMoveVectors has the same results?
					correctUp = cameraUp;
				} else {
					Vector3 r = Vector3.Cross (LookDirection, correctUp);
					correctUp = Vector3.Cross (r, LookDirection);
				}
				TurnToFace (LookDirection, correctUp);
			}
		}
	}
	protected override void EnforceSpeedLimit() {
		if(ApplyGravity) {
			float actualSpeed = Vector3.Dot(MoveDirection, rb.velocity);
			if(actualSpeed > MoveSpeed) {
				rb.velocity -= MoveDirection * actualSpeed;
				rb.velocity += MoveDirection * MoveSpeed;
			}
		} else {
			base.EnforceSpeedLimit();
		}
	}
	#endregion // Core Controller Logic
	#region Jumping
	public Jumping jump = new Jumping();

	[System.Serializable]
	public class Jumping {
		public float minJumpHeight = 0.25f, maxJumpHeight = 1;
		[Tooltip("How long the jump button must be pressed to jump the maximum height")]
		public float fullJumpPressDuration = 0.5f;
		[Tooltip("for double-jumping, put a 2 here. To eliminate jumping, put a 0 here.")]
		public int maxJumps = 1;
		[HideInInspector]
		private float currentJumpVelocity, heightReached, heightReachedTotal, timeHeld, holdJumpPlz, targetHeight;
		private bool impulseActive, input, inputHeld, peaked = false;
		[Tooltip("if false, double jumps won't 'restart' a jump, just add jump velocity")]
		private bool jumpStartResetsVerticalMotion = true;
		public int jumpsSoFar { get; protected set; }
		/// <returns>if this instance is trying to jump</returns>
		public bool IsJumping() { return inputHeld; }
		/// <summary>pretends to hold the jump button for the specified duration</summary>
		/// <param name="jumpHeldDuration">Jump held duration.</param>
		public void ScriptedJump(float jumpHeldDuration) { holdJumpPlz = jumpHeldDuration; }
		public void Update(PlayerControl p) {
			if(p.PlayerControlled) {
				input = Input.GetButtonDown("Jump");
				inputHeld = Input.GetButton("Jump");
				if(holdJumpPlz > 0) {
					inputHeld = true;
					holdJumpPlz -= Time.deltaTime;
				}
			}
			if(impulseActive && !inputHeld) { impulseActive = false; }
			if(!input && !inputHeld) return;
			// check stable footing for the jump
			if(p.IsStableOnGround) {
				jumpsSoFar = 0;
				heightReached = 0;
				currentJumpVelocity = 0;
				timeHeld = 0;
			}
			// calculate the jump
			float gForce = Physics.gravity.magnitude;
			Vector3 jump_force = Vector3.zero, jumpDirection = -p.gravity.dir;
			// if the user wants to jump, and is allowed to jump again
			if(!impulseActive && (jumpsSoFar < maxJumps)) {
				heightReached = 0;
				timeHeld = 0;
				jumpsSoFar++;
				targetHeight = minJumpHeight;
				float velocityRequiredToJump = Mathf.Sqrt(targetHeight * 2 * gForce);
				// cancel out current jump/fall forces
				if(jumpStartResetsVerticalMotion) {
					float motionInVerticalDirection = Vector3.Dot(jumpDirection, p.rb.velocity);
					jump_force -= (motionInVerticalDirection * jumpDirection) / Time.deltaTime;
				}
				// apply proper jump force
				currentJumpVelocity = velocityRequiredToJump;
				peaked = false;
				jump_force += (jumpDirection * currentJumpVelocity) / Time.deltaTime;
				impulseActive = true;
			} else
				// if a jump is happening      
				if(currentJumpVelocity > 0) {
				// handle jump height: the longer you hold jump, the higher you jump
				if(inputHeld) {
					timeHeld += Time.deltaTime;
					if(timeHeld >= fullJumpPressDuration) {
						targetHeight = maxJumpHeight;
						timeHeld = fullJumpPressDuration;
					} else {
						targetHeight = minJumpHeight + ((maxJumpHeight-minJumpHeight) * timeHeld / fullJumpPressDuration);
					}
					if(heightReached < targetHeight) {
						float requiredJumpVelocity = Mathf.Sqrt((targetHeight - heightReached) * 2 * gForce);
						float forceNeeded = requiredJumpVelocity - currentJumpVelocity;
						jump_force += (jumpDirection * forceNeeded) / Time.deltaTime;
						currentJumpVelocity = requiredJumpVelocity;
					}
				}
			} else {
				impulseActive = false;
			}
			if(currentJumpVelocity > 0) {
				float moved = currentJumpVelocity * Time.deltaTime;
				heightReached += moved;
				heightReachedTotal += moved;
				currentJumpVelocity -= gForce * Time.deltaTime;
			} else if(!peaked && !p.IsStableOnGround) {
				peaked = true;
				impulseActive = false;
			}
			p.rb.AddForce(jump_force);
		}
	}
	#endregion // Jumping
	#region Gravity
	public Gravity gravity = new Gravity();

	[System.Serializable]
	public class Gravity {
		[Tooltip("'down' direction for the player, which pulls the player")]
		public Vector3 dir = -Vector3.up;
		public float power = 9.81f;
	}
	#endregion // Gravity
	#region MonoBehaviour
	void Start() {
		GroundNormal = Vector3.up;
		EnsureRigidBody();
		if(PlayerControlled) {
			if (!(cameraControl is GroundedCameraControl)) {
				cameraControl = new GroundedCameraControl (cameraControl);
			}
			cameraControl.Start(this);
		}
		if(PlayerControlled && transform.tag == "Untagged" || transform.tag.Length == 0) { transform.tag = "Player"; }
		CollideBox = GetComponent<Collider>();
		if(CollideBox == null) { CollideBox = gameObject.AddComponent<CapsuleCollider>(); }
		ExpectedHeightFromGround = CollideBox.bounds.extents.y;
		if(CollideBox is CapsuleCollider) {
			CapsuleCollider cc = CollideBox as CapsuleCollider;
			ExpectedHorizontalRadius = cc.radius;
		} else {
			Vector3 ex = CollideBox.bounds.extents;
			ExpectedHorizontalRadius = Mathf.Max(ex.x, ex.z);
		}
	}
	/// <summary>where physics-related changes happen</summary>
	void FixedUpdate() {
		if(ApplyGravity) { StandLogic(); }
		MoveLogic();
		// keep track of where we are in relation to the parent platform object
		if(ApplyGravity && transformOnPlatform && transformOnPlatform.parent != null &&
			stickToGround == HowToDealWithGround.withPlaceholder) {
			transformOnPlatform.position = transform.position;
		}
		UpdateMotionVariables ();
	}
	/// <summary>where visual-related updates should happen</summary>
	void LateUpdate() {
		// if on a platform, update position on the platform based on velocity
		if(IsStableOnGround && transformOnPlatform != null &&
			stickToGround == HowToDealWithGround.withPlaceholder) {
			transform.position = transformOnPlatform.position + rb.velocity * Time.deltaTime;
		}
		if(PlayerControlled) { cameraControl.LateUpdate(this, OrientUp ()); }
	}
	void OnCollisionStay(Collision c) {
		if(c.gameObject == StandingOnObject) {
			if(!IsStableOnGround) { IsCollidingWithGround = true; } else { GroundNormal = c.contacts[0].normal; }
		}
	}
	void OnCollisionExit(Collision c) {
		if(c.gameObject == StandingOnObject) {
			if(IsStableOnGround || IsCollidingWithGround) {
				IsCollidingWithGround = false;
				IsStableOnGround = false;
			}
		}
	}
	#endregion // MonoBehaviour
}