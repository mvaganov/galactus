using UnityEngine;

/// <summary>A custom Unity3D character controller
/// Latest version at: https://pastebin.com/9M9qyBmP </summary>
/// <description>MIT License - TL;DR - This code is free, don't bother me about it!</description>
/// <author email="mvaganov@hotmail.com">Michael Vaganov</author>
public class PlayerControl : MonoBehaviour {
	#region Public API
	/// <summary>movement AI should assign what direction to move toward</summary>
	[HideInInspector]
	public Vector3 NonPlayerControlledDirection;
	[Tooltip("movement speed")]
	public float MoveSpeed = 5;
	[Tooltip("how quickly the PlayerControl transform rotates to match the intended direction, measured in degrees-per-second")]
	public float TurnSpeed = 180;
	[Tooltip("how quickly the PlayerControl reaches MoveSpeed. If less-than zero, jump straight to move speed.")]
	public float acceleration = -1;
	/// <summary>the ground-plane normal, used to determine which direction 'forward' is when it comes to movement on the ground</summary>
	[HideInInspector]
	public Vector3 GroundNormal = Vector3.up;
	[HideInInspector]
	/// <summary>What object is being stood on</summary>
	public GameObject StandingOnObject;
	/// <summary>the angle of standing, compared to gravity, used to determine if the player can reliably walk on this surface (it might be too slanted)</summary>
	[HideInInspector]
	public float StandAngle;
	[Tooltip("The maximum angle the player can move forward at while standing on the ground")]
	public float MaxWalkAngle = 45;
	/// <summary>if very far from ground, this value is infinity</summary>
	[HideInInspector]
	public float HeightFromGround;
	/// <summary>the player's RigidBody, which lets Unity do all the physics for us</summary>
	[HideInInspector]
	public Rigidbody rb;
	[HideInInspector]
	public Collider CollideBox;
	/// <summary>expected distance from the center of the collider to the ground. auto-populates based on Box of Capsule collider</summary>
	[HideInInspector]
	public float ExpectedHeightFromGround;
	/// <summary>expected distance from the center of the collider to a horizontal edge. auto-populates based on Box of Capsule collider</summary>
	[HideInInspector]
	public float ExpectedHorizontalRadius;
	/// <summary>how much downward velocity the player is experiencing. non-zero when jumping or falling.</summary>
	[HideInInspector]
	public float DownwardVelocity;
	/// <summary>true if on the ground</summary>
	[HideInInspector]
	public bool IsStableOnGround = false;
	[HideInInspector]
	public bool IsMovingIntentionally = false;
	/// <summary>if OnCollision event happens with ground, this is true</summary>
	[HideInInspector]
	public bool IsCollidingWithGround = false;
	[Tooltip("if true, mouse controls and WASD function to move around the player. Otherwise, uses Non Player Control settings")]
	public bool PlayerControlled = true;
	[Tooltip("if true, keep track of where on a ground object this player is standing, and stay near that spot as it moves. If this player will not be on moving platforms, set to false to improve performance.")]
	public bool SticksToMovingPlatforms = true;
	[Tooltip("if false, disables gravity's effects, and enables flying")]
	public bool UseGravity = true;

	public Vector3 GetUpOrientation() { return -gravity.dir; }
	public Vector3 GetUpBodyDirection() { return GroundNormal; }
	public void GetMoveVectors(Vector3 upVector, out Vector3 forward, out Vector3 right) {
		Transform importantT = (PlayerControlled) ? cameraControl.myCamera.transform : transform;
		if(UseGravity) {
			Vector3 generalDir = importantT.forward;
			if(importantT.forward == upVector) { generalDir = -importantT.up; } else if(importantT.forward == -upVector) { generalDir = importantT.up; }
			CalculatePlanarMoveVectors(generalDir, upVector, out forward, out right);
			if(PlayerControlled && cameraControl.currentPitch > 90) { right *= -1; forward *= -1; } // if we're upside-down, flip to keep it consistent
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
	#endregion // Public API
	#region Core Controller Logic
	/// <summary>used when the player controller needs to stay on a moving platform or vehicle</summary>
	private Transform transformOnPlatform;
	/// <summary>true if player is walking on ground that is too steep.</summary>
	private bool tooSteep = false;
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
				if(SticksToMovingPlatforms && transformOnPlatform == null) {
					transformOnPlatform = (new GameObject("<where " + name + " stands>")).transform;
				}
				transformOnPlatform.SetParent(StandingOnObject.transform);
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
			if(transformOnPlatform) {
				transformOnPlatform.SetParent(null);
			}
			// if we couldn't find ground with our leg here, try another location.
			legOffset = new Vector3(
				Random.Range(-1.0f, 1.0f) * ExpectedHorizontalRadius, 0,
				Random.Range(-1.0f, 1.0f) * ExpectedHorizontalRadius);
		}
		CollideBox.enabled = true;
	}
	void MoveLogic() {
		Vector3 dir = default(Vector3);
		if(PlayerControlled) {
			float input_forward = Input.GetAxis("Vertical");
			float input_right = Input.GetAxis("Horizontal");
			if(IsMovingIntentionally = (input_forward != 0 || input_right != 0)) {
				Vector3 currentRight, currentMoveForward;
				GetMoveVectors(GroundNormal,//(isStableOnGround)?groundNormal:GetUpOrientation(),//
					out currentMoveForward, out currentRight);
				dir = (currentRight * input_right) + (currentMoveForward * input_forward);
				dir.Normalize();
			}
		} else {
			dir = NonPlayerControlledDirection;
			IsMovingIntentionally = dir != Vector3.zero;
		}
		if(UseGravity) {
			float walkAngle = 90 - Vector3.Angle(GetUpOrientation(), rb.velocity);
			if(walkAngle > MaxWalkAngle || StandAngle > MaxWalkAngle) {
				IsStableOnGround = false;
			}
			if(!IsStableOnGround) {
				rb.velocity += gravity.power * gravity.dir * Time.deltaTime; // TODO take mass into account?
			}
			DownwardVelocity = Vector3.Dot(rb.velocity, gravity.dir);
			if(tooSteep && IsMovingIntentionally) {
				Vector3 acrossSlope = Vector3.Cross(GroundNormal, -gravity.dir).normalized;
				Vector3 intoSlope = Vector3.Cross(acrossSlope, -gravity.dir).normalized;
				float upHillAmount = Vector3.Dot(dir, intoSlope);
				if(upHillAmount > 0) { dir -= intoSlope * upHillAmount; }
			}
			ApplyMove(dir, true);
			if(!IsStableOnGround) {
				rb.velocity -= gravity.dir * Vector3.Dot(rb.velocity, gravity.dir); // subtract donward-force from move speed
				rb.velocity += gravity.dir * DownwardVelocity; // re-apply downward-force from gravity
			}
			jump.Update(this);
		} else {
			ApplyMove(dir, true);
		}
		// turn body as needed
		if(TurnSpeed != 0) {
			Vector3 fG, rG, up;
			if(!IsCollidingWithGround && !IsStableOnGround && float.IsInfinity(HeightFromGround)) {
				up = GetUpOrientation();
			} else {
				up = GroundNormal;
			}
			if(PlayerControlled) {
				GetMoveVectors(up, out fG, out rG);
			} else {
				fG = (IsMovingIntentionally) ? dir : transform.forward;
				Vector3 r = Vector3.Cross(dir, up);
				up = Vector3.Cross(r, dir);
			}
			TurnToFace(fG, cameraControl.playerUp);
		}
	}
	private void ApplyMove(Vector3 dir, bool autoSlowdown) {
		if(acceleration <= 0) {
			rb.velocity = dir * MoveSpeed;
		} else {
			float actualSpeed = rb.velocity.magnitude;
			if(IsMovingIntentionally) {
				rb.velocity += dir*acceleration * Time.deltaTime;
				if(actualSpeed > MoveSpeed) {
					rb.velocity = (rb.velocity*MoveSpeed) / actualSpeed;
				}
			} else if(autoSlowdown) {
				float amountToMove = acceleration * Time.deltaTime;
				if(actualSpeed > acceleration *amountToMove) {
					Vector3 counterForce = rb.velocity.normalized * (amountToMove);
					rb.velocity -= counterForce;
				} else {
					rb.velocity = Vector3.zero;
				}
			}
		}
	}
	bool TurnToFace(Vector3 forward, Vector3 up) {
		if(up == Vector3.zero) {
			Vector3 r = (forward == transform.right) ? -transform.forward : ((forward == -transform.right) ? transform.forward : transform.right);
			up = Vector3.Cross(r, forward);
		}
		if(transform.forward != forward || transform.up != up) {
			Quaternion target = Quaternion.LookRotation(forward, up);
			Quaternion q = Quaternion.RotateTowards(transform.rotation, target, TurnSpeed * Time.deltaTime);
			transform.rotation = q;
			return true;
		}
		return false;
	}
	#endregion // Core Controller Logic
	#region Camera Control
	/// <summary>if the user is pressing the right-mouse button</summary>
	private bool isPressingRMB = false;
	public CameraControl cameraControl = new CameraControl();

	[System.Serializable]
	public class CameraControl {
		/// <summary>'up' direction for the player, used to orient the camera</summary>
		[HideInInspector]
		public Vector3 playerUp = Vector3.up;
		[Tooltip("Camera for the PlayerControl to use. Will automagically find one if not set.")]
		public Camera myCamera;
		private Transform camHandle;
		[Tooltip("keeps the camera from looking too far")]/// <summary>keeps the camera from looking too far</summary>
		public float maximumPitch = 90, minimumPitch = -90;
		/// <summary>how the 3D camera should move with the player.</summary>
		[Tooltip("how the 3D camera should move with the player\n"+
			"* Fixed Camera: other code should control the camera\n"+
			"* Lock To Player: follow player with current offset\n"+
			"* Rotate 3rd Person: 3rd person, scrollwheel zoom\n"+
			"* Lock-and-Rotate-with-RMB: like the Unity editor Scene view")]
		public ControlStyle controlMode = ControlStyle.lockAndRotateWithRMB;
		public enum ControlStyle { fixedCamera, lockToPlayer, rotateH3rdPerson, rotate3rdPerson, lockAndRotateWithRMB }
		[Tooltip("how far the eye-focus (where the camera rests) should be above the PlayerControl transform")]
		public float eyeHeight = 0.125f;
		/// <summary>how far away the camera should be from the player</summary>
		private Vector3 cameraOffset;
		/// <summary>how far the camera should be from the PlayerControl transform</summary>
		private float userCamDist;
		[Tooltip("If true, a raycast is sent to make sure the camera doesn't clip through solid objects.")]
		public bool cameraWontClip = true;
		/// <summary>the vertical tilt of the camera</summary>
		[HideInInspector]
		public float currentPitch;
		public float horizontalMouselookSpeed = 5, verticalMouselookSpeed = 5;

		public void Start(PlayerControl p) {
			if(!myCamera) {// make sure there is a camera
				myCamera = Camera.main;
				if(myCamera == null) {
					myCamera = (new GameObject("<main camera>")).AddComponent<Camera>();
					myCamera.tag = "MainCamera";
				}
			}
			if(UnityEngine.VR.VRDevice.isPresent) {
				camHandle = (new GameObject("<camera handle>")).transform;
				myCamera.transform.position = Vector3.zero;
				myCamera.transform.SetParent(camHandle);
			} else {
				camHandle = myCamera.transform;
			}
			// calculate camera variables
			cameraOffset = camHandle.position - p.transform.position;
			userCamDist = cameraOffset.magnitude;
			Vector3 currentRight = Vector3.Cross(playerUp, camHandle.forward);
			Vector3 currentMoveForward = Vector3.Cross(currentRight, playerUp);
			Quaternion playerIdentity = Quaternion.LookRotation(currentMoveForward, playerUp);
			currentPitch = Quaternion.Angle(playerIdentity, camHandle.rotation);
			LateUpdate(p);
		}
		public void LateUpdate(PlayerControl p) {
			if(Input.GetMouseButton(1)) { p.isPressingRMB = true; }
			if(controlMode != ControlStyle.fixedCamera) {
				float distForCamera;
				bool needToUpdateUp = playerUp != -p.gravity.dir,
				updatingWithoutMouseInput = (controlMode == ControlStyle.rotate3rdPerson) ||
					(controlMode == ControlStyle.rotateH3rdPerson) ||
					(controlMode == ControlStyle.lockAndRotateWithRMB && p.isPressingRMB);
				if(needToUpdateUp) {
					Vector3 delta = -p.gravity.dir - playerUp;
					distForCamera = delta.magnitude;
					float movespeed = Time.deltaTime * p.MoveSpeed;
					if(distForCamera < movespeed) {
						playerUp = -p.gravity.dir;
					} else {
						playerUp += (delta / 2) * movespeed;
					}
				}
				float scroll = Input.GetAxis("Mouse ScrollWheel");
				if(scroll != 0) {
					userCamDist -= scroll * 10;
					userCamDist = Mathf.Max(0, userCamDist);
					if(userCamDist > 0 && cameraOffset != Vector3.zero) {
						cameraOffset = cameraOffset.normalized * userCamDist;
					}
				}
				// mouse-look update
				if(updatingWithoutMouseInput || needToUpdateUp) {
					// get the rotations that the user input is indicating
					float dx = updatingWithoutMouseInput ? Input.GetAxis("Mouse X") * horizontalMouselookSpeed : 0;
					float dy = updatingWithoutMouseInput && (controlMode != ControlStyle.rotateH3rdPerson) ?
						Input.GetAxis("Mouse Y") * verticalMouselookSpeed : 0;
					if(!needToUpdateUp && updatingWithoutMouseInput && dx == 0 && dy == 0) {
						updatingWithoutMouseInput = false;
					}
					if(p.UseGravity) {
						currentPitch -= dy; // rotate accordingly, minus because of standard 'inverted' Y axis rotation
						camHandle.Rotate(Vector3.right, -currentPitch);// un-rotate zero-out camera's "up", re-applied soon
						Vector3 rightSide = Vector3.Cross(camHandle.forward, playerUp);
						Vector3 unrotatedMoveForward = Vector3.Cross(playerUp, rightSide);
						camHandle.rotation = Quaternion.LookRotation(unrotatedMoveForward, playerUp); // force zero rotation
						camHandle.Rotate(Vector3.up, dx); // re-apply rotation
						while(currentPitch > 180) { currentPitch -= 360; } // normalize the angles to be between -180 and 180
						while(currentPitch < -180) { currentPitch += 360; }
						if(p.UseGravity) { currentPitch = Mathf.Clamp(currentPitch, minimumPitch, maximumPitch); }
						camHandle.Rotate(Vector3.right, currentPitch);
					} else {
						// simplistic gravity-less rotation
						camHandle.Rotate(Vector3.up, dx);
						camHandle.Rotate(Vector3.right, -dy);
						// set user interface artifacts up correctly
						playerUp = camHandle.up;
						currentPitch = 0;
					}
				}
				// move the camera to be looking at the player's eyes/head, ideally with no geometry in the way
				RaycastHit rh;
				Vector3 eyeFocus = p.transform.position;
				if(eyeHeight != 0) {
					eyeFocus += playerUp * eyeHeight;
				}
				Color lineColor = Color.red;
				distForCamera = userCamDist;
				if(cameraWontClip &&
				Physics.SphereCast(eyeFocus, myCamera.nearClipPlane, -camHandle.forward, out rh, userCamDist) &&
				userCamDist > rh.distance) {
					distForCamera = rh.distance;
				}
				if(distForCamera != 0) { cameraOffset = -myCamera.transform.forward * distForCamera; }
				Vector3 nextLocation = eyeFocus + ((userCamDist > 0) ? cameraOffset : Vector3.zero);
				camHandle.position = nextLocation;
			}
			if(Input.GetMouseButtonUp(1)) { p.isPressingRMB = false; }
		}
	}
	#endregion // Camera Control
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
		private float currentVelocity, heightReached, heightReachedTotal, timeHeld,
		holdJumpPlz, targetHeight;
		private bool impulseActive, input, inputHeld, peaked = false;
		[Tooltip("if false, double jumps won't 'restart' a jump, just add jump velocity")]
		private bool jumpStartResetsVerticalMotion = true;
		private int jumpsSoFar = 0;
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
				currentVelocity = 0;
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
				currentVelocity = velocityRequiredToJump;
				peaked = false;
				jump_force += (jumpDirection * currentVelocity) / Time.deltaTime;
				impulseActive = true;
			} else
				// if a jump is happening      
				if(currentVelocity > 0) {
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
						float forceNeeded = requiredJumpVelocity - currentVelocity;
						jump_force += (jumpDirection * forceNeeded) / Time.deltaTime;
						currentVelocity = requiredJumpVelocity;
					}
				}
			} else {
				impulseActive = false;
			}
			if(currentVelocity > 0) {
				float moved = currentVelocity * Time.deltaTime;
				heightReached += moved;
				heightReachedTotal += moved;
				currentVelocity -= gForce * Time.deltaTime;
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
		rb = GetComponent<Rigidbody>();
		if(!rb) { rb = gameObject.AddComponent<Rigidbody>(); }
		rb.useGravity = false; rb.freezeRotation = true;
		if(PlayerControlled) { cameraControl.Start(this); }
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
	/// <summary>where physics-related changes happen</summary>
	void FixedUpdate() {
		if(UseGravity) { StandLogic(); }
		MoveLogic();
		// keep track of where we are in relation to the parent platform object
		if(UseGravity && transformOnPlatform && transformOnPlatform.parent != null) {
			transformOnPlatform.position = transform.position;
		}
	}
	/// <summary>where visual-related updates should happen</summary>
	void LateUpdate() {
		// if on a platform, update position on the platform based on velocity
		if(IsStableOnGround && transformOnPlatform != null) {
			transform.position = transformOnPlatform.position + rb.velocity * Time.deltaTime;
		}
		if(PlayerControlled) { cameraControl.LateUpdate(this); }
	}
	#endregion // MonoBehaviour
}