#define SHOW_LINES
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NS {
	[RequireComponent(typeof(Rigidbody))]
	public class MoveControls : MonoBehaviour {
		public static int playerControlledLayer = -1;
		public static string playerControlledLayerName = "player controlled";

		[HideInInspector]
		public Vector3 gravityDirection = Vector3.down; // setter forces normalization
		public float gravityPower = 10;

		public float hoverDistance = .125f;
		public float maxStandHeight = .25f;
		public float maxIncline = 45;

		#if SHOW_LINES
		public bool showLines = true;
		#endif
		public bool alignBody = true;
		public bool alignBodyWithAngularVelocity = false;
		public bool alignBodyToGround = false;
		public bool canMoveInAir = true;
		public bool disabledControls = false;
		[Tooltip("whether to temporarily disable controls when collider is hit. being stable on the ground allows control again.")]
		public bool interruptControlWhenHit = true;

		public bool useGravity = true;
		private bool onGround = false;
		public bool OnGround { get { return onGround; } }
		[HideInInspector]
		public bool standing = false;
		public bool Standing { get { return standing; } }
		/// <summary>if control should be interrupted because player is moving inappropriately</summary>
		private bool controlInterrupted = false;

		private CapsuleCollider capsule;
		private GameObject standingOn = null;

		public float speed = 3;
		public float acceleration = 10;
		public float maxAcceleration = 10;

		public Jumping jump = new Jumping();
		[System.Serializable]
		public class Jumping {
			public float minJumpHeight = 0.125f, maxJumpHeight = 0.5f;
			[Tooltip("How long the jump button must be pressed to jump the maximum height")]
			public float fullJumpPressDuration = 0.25f;
			[Tooltip("for double-jumping, put a 2 here. To eliminate jumping, put a 0 here.")]
			public int maxJumps = 1;
			[HideInInspector]
			/// <summary>for implementing AI that can jump</summary>
			public float SecondsToPressJump;
			protected float currentJumpVelocity, heightReached, heightReachedTotal, timeHeld, targetHeight;
			protected bool impulseActive = false;
			protected bool peaked = false;
			[HideInInspector]
			public bool inputHeld;
			[Tooltip("if false, double jumps won't 'restart' a jump, just add jump velocity")]
			private bool jumpStartResetsVerticalMotion = true;
			public int jumpsSoFar { get; protected set; }
			/// <returns>if this instance is trying to jump</returns>
			public bool IsJumping() { return inputHeld; }
			/// <summary>pretends to hold the jump button for the specified duration</summary>
			/// <param name="jumpHeldDuration">Jump held duration.</param>
			public void FixedUpdate(MoveControls p) {
				// handle AI jumping if the jump input is not held
				if (!inputHeld && (inputHeld = (SecondsToPressJump > 0))) { SecondsToPressJump -= UnityEngine.Time.deltaTime; }
				// check whether player should be going up during the jump
				if(impulseActive && !inputHeld) { impulseActive = false; }
				if(!inputHeld) return; // if there is no jump input, the user doesnt want to jump. no more to do.
				// check stable footing for the jump and start the jump
				if(p.OnGround) {
					jumpsSoFar = 0;
					heightReached = 0;
					currentJumpVelocity = 0;
					timeHeld = 0;
				}
				// calculate the jump forces
				float gForce = p.gravityPower * p.rb.mass;
				Vector3 jump_force = Vector3.zero, jumpDirection = -p.gravityDirection;
				// if the user is not jumping, and is allowed to jump again
				if(!impulseActive && (jumpsSoFar < maxJumps)) {
					heightReached = 0; // start a new jump
					timeHeld = 0;
					jumpsSoFar++;
					targetHeight = minJumpHeight * p.rb.mass;
					float velocityRequiredToJump = Mathf.Sqrt(targetHeight * 2 * gForce);
					// cancel out current jump/fall forces
					if(jumpStartResetsVerticalMotion) {
						float motionInVerticalDirection = Vector3.Dot(jumpDirection, p.rb.velocity);
						jump_force -= (motionInVerticalDirection * jumpDirection) / UnityEngine.Time.deltaTime;
					}
					// apply proper jump force
					currentJumpVelocity = velocityRequiredToJump;
					peaked = false;
					jump_force += (jumpDirection * currentJumpVelocity) / UnityEngine.Time.deltaTime;
					impulseActive = true;
				} 
				// if a jump is happening  
				else if(currentJumpVelocity > 0) {
					// handle jump height: the longer you hold jump, the higher you jump
					if(inputHeld) {
						timeHeld += UnityEngine.Time.deltaTime;
						if(timeHeld >= fullJumpPressDuration) {
							targetHeight = maxJumpHeight;
							timeHeld = fullJumpPressDuration;
						} else {
							targetHeight = minJumpHeight + ((maxJumpHeight-minJumpHeight) * timeHeld / fullJumpPressDuration);
							targetHeight *= p.rb.mass;
						}
						if(heightReached < targetHeight) {
							float requiredJumpVelocity = Mathf.Sqrt((targetHeight - heightReached) * 2 * gForce);
							float forceNeeded = requiredJumpVelocity - currentJumpVelocity;
							jump_force += (jumpDirection * forceNeeded) / UnityEngine.Time.deltaTime;
							currentJumpVelocity = requiredJumpVelocity;
						}
					}
				} else {
					impulseActive = false;
				}
				if(currentJumpVelocity > 0) {
					float moved = currentJumpVelocity * UnityEngine.Time.deltaTime;
					heightReached += moved;
					heightReachedTotal += moved;
					currentJumpVelocity -= gForce * UnityEngine.Time.deltaTime;
				} else if(!peaked && !p.OnGround) {
					peaked = true;
					impulseActive = false;
				}
				p.rb.AddForce(jump_force);
			}
		}

		private float incline = 0;
		public float Incline { get { return incline; } }
		private float currentSpeed;
		public float CurrentSpeed { get { return currentSpeed; } }

		private Vector3 groundNormal, foreDir, sideDir, foreHorizon, dirHorizon;
		Camera viewCamera;
		CameraControls camCon;

		public Vector3 GetDirectionOnHorizon() { return foreHorizon; }

		Rigidbody rb;
		// Use this for initialization
		void Start () {
			playerControlledLayer = LayerMask.NameToLayer(playerControlledLayerName);
			rb = GetComponent<Rigidbody> ();
			rb.useGravity = false;
			capsule = GetComponent<CapsuleCollider> ();
			if (capsule == null) {
				capsule = gameObject.AddComponent<CapsuleCollider> ();
				capsule.center = new Vector3(0, 0.6f, 0);
				capsule.radius = 0.25f;
				capsule.height = 1.25f;
				capsule.direction = 1; // Y-axis
				gameObject.layer = playerControlledLayer;
			}
			groundNormal = -gravityDirection;

			viewCamera = Camera.main;
			camCon = viewCamera.GetComponent<CameraControls>();
			if (camCon) {
				camCon.onMove += BodyOrientationUpdate;
			}
			SetFacing ();
			footTouch = gameObject.AddComponent<SphereCollider> ();
			footTouch.radius = .0125f;
			footTouch.center = transform.InverseTransformVector (gravityDirection * (hoverDistance - footTouch.radius));
			footTouch.enabled = false;
		}

		public void SetFacing(Vector3 lookDir = default(Vector3)) {
			if (lookDir == Vector3.zero) {
				lookDir = viewCamera.transform.forward;
			}
			Vector3 r = Vector3.Cross (lookDir, -gravityDirection);
			if (camCon) {
				if (camCon.Pitch == 90 || camCon.Pitch == -90) {
					r = camCon.transform.right;
				} else {
					bool upsideDown = camCon.Pitch > 90 || camCon.Pitch < -90;
					if (upsideDown) {
						r *= -1;
					}
				}
			}
			if (useGravity) {
				foreDir = Vector3.Cross (groundNormal, r).normalized;
				sideDir = Vector3.Cross (groundNormal, foreDir).normalized;
			} else {
				foreDir = viewCamera.transform.forward;
				sideDir = viewCamera.transform.right;
			}
			foreHorizon = Vector3.Cross(sideDir, -gravityDirection).normalized;
		}
		#if SHOW_LINES
		GameObject line_f, line_s, line_leg, line_horizon, line_velocity, line_against, line_accel, line_shadow, line_ray;
		#endif

		public float horizontalRotationOffset;
		void BodyOrientationUpdate() {
			SetFacing ();
			#if SHOW_LINES
			if(showLines){
			Vector3 p = transform.position;
			NS.Lines.MakeArrow (ref line_f, p, p + foreDir*2, Color.blue);
			NS.Lines.MakeArrow (ref line_s, p, p + sideDir*2, Color.red);
			NS.Lines.MakeArrow (ref line_velocity, p, p + rb.velocity*2, Color.magenta);
			NS.Lines.MakeArrow (ref line_horizon, p, p + dirHorizon*2, Color.cyan);
			}
			#endif

			Transform par = viewCamera.transform.parent;
			viewCamera.transform.SetParent (null);

			if (//onGround && 
				alignBody) {
				Quaternion idealRotation = transform.rotation;
				if (alignBodyToGround) {
					// if body alignment follows ground normal
					idealRotation = Quaternion.LookRotation (foreDir, groundNormal);
				} else {
					// if body alignment follows opposes gravity
					if (foreHorizon != Vector3.zero) {
						idealRotation = Quaternion.LookRotation (foreHorizon, -gravityDirection);
					}
				}
				if (horizontalRotationOffset != 0) {
					idealRotation = idealRotation * Quaternion.AngleAxis (horizontalRotationOffset, transform.up);
				}
				if (!alignBodyWithAngularVelocity) {
					transform.rotation = idealRotation;
					rb.angularVelocity = Vector3.zero;
				} else {
					Vector3 e = transform.rotation.eulerAngles;
					Vector3 ie = idealRotation.eulerAngles;
					Vector3 d = ie - e;
					CameraControls.NormalizeAngle (ref d.x);
					CameraControls.NormalizeAngle (ref d.y);
					CameraControls.NormalizeAngle (ref d.z);
					NS.Lines.MakeArcArrow (ref xrot, d.x, 24, transform.right, transform.up, transform.position, Color.red);
					NS.Lines.MakeArcArrow (ref yrot, d.y, 24, transform.up, transform.forward, transform.position, Color.green);
					NS.Lines.MakeArcArrow (ref zrot, d.z, 24, transform.forward, transform.up, transform.position, Color.blue);
					d *= Time.deltaTime;
	//				transform.rotation = idealRotation;
					float angularVelocity = 360;
	//				if(Mathf.Abs(d.x) > 1) d.x = angularVelocity * Mathf.Sign(d.x); else d.x = 0;
	//				if(Mathf.Abs(d.y) > 1) d.y = angularVelocity * Mathf.Sign(d.y); else d.y = 0;
	//				if (Mathf.Abs(d.y) < Time.deltaTime * angularVelocity) {
	//					d.y = d.y / Time.deltaTime;
	//				} else {
	//					d.y = angularVelocity * Mathf.Sign (d.y);
	//				}
					ChangeDeltaToSpeed (ref d.x, angularVelocity);
					ChangeDeltaToSpeed (ref d.y, angularVelocity);
					ChangeDeltaToSpeed (ref d.z, angularVelocity);
	//				if(Mathf.Abs(d.z) > 1) d.z = angularVelocity * Mathf.Sign(d.z); else d.z = 0;
	//				d.z = 0;
	//				d.x = 0;
					rb.angularVelocity = d;//Vector3.zero;//
				}
			}
			viewCamera.transform.SetParent (par);
		}
		void ChangeDeltaToSpeed(ref float currentDelta, float speed) {
			if (Mathf.Abs(currentDelta) < Time.deltaTime * speed) {
				currentDelta = currentDelta / Time.deltaTime;
			} else {
				currentDelta = speed * Mathf.Sign (currentDelta);
			}
		}
		GameObject xrot, yrot, zrot;

		void LateUpdate() {
			BodyOrientationUpdate ();
		}
		private Vector3 hip = Vector3.zero;
		private SphereCollider footTouch = null;
		private const float epsilon = 1/1024.0f;
		// Update is called once per frame
		void FixedUpdate () {
			// handle user movement
			bool stableEnoughToMove = OnGround || !useGravity;
			Vector3 accel = Vector3.zero;
			if (!disabledControls && (stableEnoughToMove || canMoveInAir)) {
				float haccel = 0, vaccel = 0, daccel = 0; 
				float h = controlInterrupted ? 0 : Input.GetAxis ("Horizontal") * speed;
				if (h != 0 || stableEnoughToMove) {
					float hCurrent = Vector3.Dot (sideDir, rb.velocity);
					haccel = h - hCurrent;
					haccel = Mathf.Clamp (haccel * acceleration, -maxAcceleration, maxAcceleration);
					accel += sideDir * haccel * Time.deltaTime;
				}
				float v = controlInterrupted ? 0 : Input.GetAxis ("Vertical") * speed;
				if (v != 0 || stableEnoughToMove) {
					float vCurrent = Vector3.Dot (foreDir, rb.velocity);
					vaccel = v - vCurrent;
					vaccel = Mathf.Clamp (vaccel * acceleration, -maxAcceleration, maxAcceleration);
					accel += foreDir * vaccel * Time.deltaTime;
				}
				if (!useGravity) {
					float d = controlInterrupted ? 0 : (Input.GetKey (KeyCode.Q))?speed:(Input.GetKey (KeyCode.E)?-speed:0);
					if (d != 0 || stableEnoughToMove) {
						float dCurrent = Vector3.Dot (viewCamera.transform.up, rb.velocity);
						daccel = d - dCurrent;
						daccel = Mathf.Clamp (daccel * acceleration, -maxAcceleration, maxAcceleration);
						accel += viewCamera.transform.up * daccel * Time.deltaTime;
					}
				}
				if (vaccel != 0 || haccel != 0 || daccel != 0) {
					rb.velocity += accel;
				}
			}
			if (useGravity) {
				// handle incline limitations
				float verticalMotion = Vector3.Dot (gravityDirection, rb.velocity);
				dirHorizon = rb.velocity - gravityDirection * verticalMotion;
				incline = Vector3.Angle (groundNormal, -gravityDirection);
				// if movement is aligned with the ground at all
				if (Vector3.Dot (dirHorizon, groundNormal) > 0) {
					incline *= -1; // we're going down-hill
				}
				if (Incline > maxIncline) {
					Vector3 groundR = Vector3.Cross (groundNormal, gravityDirection).normalized;
					Vector3 againstHill = Vector3.Cross (gravityDirection, groundR).normalized;
					Vector3 p = transform.position;
					#if SHOW_LINES
					if(showLines) NS.Lines.MakeArrow (ref line_against, p, p + againstHill, Color.black);
					#endif

					float againstWallAmount = Vector3.Dot (againstHill, rb.velocity);
					rb.velocity -= againstHill * againstWallAmount;

					float opposingGravity = Vector3.Dot (-gravityDirection, rb.velocity);
					if (opposingGravity > 0) {
						rb.velocity += gravityDirection * opposingGravity;
					}
					dirHorizon = rb.velocity - gravityDirection * verticalMotion;
				} 
				// keep gravity working correctly
				float towardGravity = useGravity ? Vector3.Dot (gravityDirection, rb.velocity) : 0;
				Vector3 lastGroundNormal = groundNormal;
				footTouch.enabled = false;
				if (OnGround || towardGravity > 0) {
					RaycastHit rh = new RaycastHit ();
					if (!Standing && Incline < maxIncline) {
						hip = Random.insideUnitCircle * capsule.radius;
						hip.z = hip.y;
						hip.y = 0;
						hip = transform.TransformVector (hip);
					}
					#if SHOW_LINES
					if(showLines) NS.Lines.Make (ref line_leg, transform.position+hip, 
					                      transform.position+hip + gravityDirection * -maxStandHeight, Color.magenta);
					#endif
					if (Physics.Raycast (transform.position + hip, gravityDirection, out rh, maxStandHeight)
					&& rh.collider.gameObject != gameObject) {
						#if SHOW_LINES
						if(showLines){
						LineRenderer lr = line_leg.GetComponent<LineRenderer> ();
						lr.startColor = Random.ColorHSV ();
						}
						#endif
						onGround = true;
						groundNormal = rh.normal;
						if (rh.distance <= hoverDistance + epsilon) {
							bool newGround = standingOn != rh.collider.gameObject;
							standingOn = rh.collider.gameObject;
							if (!newGround) {
								rb.velocity -= gravityDirection * towardGravity;
							}
							transform.position = rh.point - gravityDirection * hoverDistance - hip;
							if (newGround) {
								footTouch.enabled = true;
							}
							controlInterrupted = false;
							standing = true;
						} else {
							standing = false;
						} // commented out to stop tiny bouncing caused by foot
						#if SHOW_LINES
						if(showLines) NS.Lines.MakeCircle (ref line_shadow, rh.point, rh.normal, Color.yellow, 0.25f);
						#endif
					} else {
						#if SHOW_LINES
						if(showLines) NS.Lines.Make (ref line_shadow, rh.point, rh.point, Color.yellow);
						#endif
						onGround = false;
						standingOn = null;
						standing = false;
						groundNormal = -gravityDirection;
					}
				}
				if (!Standing) {
					Vector3 gravityThisFrame = gravityDirection * gravityPower * UnityEngine.Time.deltaTime;
					rb.velocity += gravityThisFrame;
				}
				if (jump.inputHeld = !disabledControls && Input.GetButton ("Jump")) {
					jump.FixedUpdate (this);
				}
				// orient the body correctly
				if (lastGroundNormal != groundNormal) {
					BodyOrientationUpdate ();
				}
			}
			// state calculations
			currentSpeed = rb.velocity.magnitude;
		}
		void OnCollisionEnter(Collision col) {
			if (useGravity && !OnGround && interruptControlWhenHit) {
				controlInterrupted = true;
			}
		}
	}
}