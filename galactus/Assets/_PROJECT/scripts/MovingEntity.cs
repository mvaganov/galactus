using UnityEngine;

/// <summary>A custom Unity3D character controller, useful for player characters and AI characters
/// Latest version at: https://pastebin.com/xFUD4tk2
/// The complementary MOvingEntity_CameraInput script is at: https://pastebin.com/pC0Ddjsi </summary>
/// <description>MIT License - TL;DR - This code is free, don't bother me about it!</description>
/// <author email="mvaganov@hotmail.com">Michael Vaganov</author>
public class MovingEntity : MovingEntityBase {
	#region Public API
	/// <summary>true if on the ground</summary>
	[HideInInspector]
	public bool IsStableOnGround { get; protected set; }
	/// <summary>if OnCollision event happens with ground, this is true</summary>
	[HideInInspector]
	public bool IsCollidingWithGround { get; protected set; }
	private bool wasStableOnGroundLastFrame;
	public enum GravityState { none, useGravity, noFalling }
	[Tooltip("if false, disables gravity's effects, and enables flying")]
	public GravityState gravityApplication = GravityState.useGravity;
	/// <summary>the ground-plane normal, used to determine which direction 'forward' is when it comes to movement on the ground</summary>
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
	public float VerticalVelocity { get; protected set; }

	public Vector3 GetUpOrientation() { return -gravity.dir; }
	public Vector3 GetUpBodyDirection() { return GroundNormal; }
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
	#endregion // Public API
	#region Core Controller Logic
	/// <summary>used when the player controller needs to stay on a moving platform or vehicle</summary>
	private Transform transformOnPlatform;
	/// <summary>true if player is walking on ground that is too steep.</summary>
	public bool tooSteep { get; protected set; }
	[HideInInspector]
	public bool AutomaticallyFollowPositionOnPlatform = true;
	/// <summary>where the 'leg' raycast comes down from. changes if the leg can't find footing.</summary>
	private Vector3 legOffset;
	/// <summary>cache the data structure with the object, to improve performance</summary>
	private RaycastHit rayDownHit = new RaycastHit();
	/// <summary>useful to call in LateUpdate for camera</summary>
	public void FollowPositionOnPlatform() {
		// if on a platform, update position on the platform based on velocity
		if(transformOnPlatform != null && stickToGround == HowToDealWithGround.withPlaceholder &&
			(IsStableOnGround || wasStableOnGroundLastFrame)) {
			transform.position = transformOnPlatform.position + rb.velocity * Time.deltaTime;
		}
	}
	void StandLogic() {
		// this might be better done in a LateUpdate for a camera, to make smoother motion
		if(AutomaticallyFollowPositionOnPlatform) { FollowPositionOnPlatform(); }
		wasStableOnGroundLastFrame = IsStableOnGround;
		Vector3 hip = (legOffset!=Vector3.zero) ? transform.TransformPoint(legOffset) : transform.position;
		const float epsilon = 0.0625f;
		float lengthOfLeg = ExpectedHeightFromGround+ExpectedHorizontalRadius * 5;
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
	public Vector3 NormalizeDirectionTo(Vector3 direction, Vector3 groundNormal) {
		float amountOfVertical = Vector3.Dot (direction, groundNormal);
		direction -= groundNormal * amountOfVertical;
		if (direction != Vector3.zero) {  direction.Normalize (); }
		return direction;
	}
	protected override void ApplyMove(float acceleration) {
		// validate that the agent should be able to walk at this angle
		float walkAngle = 90 - Vector3.Angle(GetUpOrientation(), rb.velocity);
		if(walkAngle > MaxWalkAngle || StandAngle > MaxWalkAngle) { IsStableOnGround = false; }
		// only apply gravity if the agent can't walk.
		if(!IsStableOnGround) { rb.velocity += gravity.GetGravityPower() * gravity.dir * Time.deltaTime; }
		VerticalVelocity = Vector3.Dot(rb.velocity, gravity.dir); // remember vertical velocity (for jumping!)
		// prevent moving up hill if the hill is too steep (videogame logic!)
		if(tooSteep && IsMovingIntentionally) {
			Vector3 acrossSlope = Vector3.Cross(GroundNormal, -gravity.dir).normalized;
			Vector3 intoSlope = Vector3.Cross(acrossSlope, -gravity.dir).normalized;
			float upHillAmount = Vector3.Dot(MoveDirection, intoSlope);
			if(upHillAmount > 0) {
				MoveDirection -= intoSlope * upHillAmount;
				MoveDirection.Normalize ();
			}
		}
		base.ApplyMove(acceleration);
		if(!IsStableOnGround) {
			rb.velocity -= gravity.dir * Vector3.Dot(rb.velocity, gravity.dir); // subtract donward-force from move speed
			rb.velocity += gravity.dir * VerticalVelocity; // re-apply vertical velocity from gravity/jumping
		}
	}
	public override void MoveLogic() {
		if (IsBrakeOn && (IsStableOnGround || gravityApplication != GravityState.useGravity)) {
			ApplyBrakes (Acceleration);
		}
		IsMovingIntentionally = MoveDirection != Vector3.zero;
		if(gravityApplication == GravityState.useGravity) {
			if (IsMovingIntentionally) {
				// keeps motion aligned to ground, also makes it easy to put on the brakes
				MoveDirection = NormalizeDirectionTo (MoveDirection, GroundNormal);
				IsMovingIntentionally = MoveDirection != Vector3.zero;
			}
			ApplyMove (Acceleration);
			jump.FixedUpdate(this);
		} else if(!IsBrakeOn) {
			base.ApplyMove(Acceleration);
		}
		// turn body as needed
		if(AutomaticallyTurnToFaceLookDirection && TurnSpeed != 0) { UpdateFacing (); }
	}
	public override void UpdateFacing() {
		Vector3 correctUp;		// TODO make this a member variable, and only assign here if it's zero? or just set it to ground normal?
		if(!IsCollidingWithGround && !IsStableOnGround && float.IsInfinity(HeightFromGround)) {
			correctUp = GetUpOrientation();
		} else {
			correctUp = (GroundNormal != Vector3.zero && gravityApplication != GravityState.none) ? GroundNormal:transform.up;
		}
		// turn IF needed
		if ((LookDirection != Vector3.zero && LookDirection != transform.forward) || (correctUp != transform.up)) {
			Vector3 correctRight, correctForward;
			correctRight = Vector3.Cross (correctUp, LookDirection);
			if (correctRight == Vector3.zero) {
				correctRight = Vector3.Cross (correctUp, (LookDirection != transform.forward)?transform.forward:transform.up);
			}
			correctForward = Vector3.Cross (correctRight, correctUp).normalized;
			TurnToFace (correctForward, correctUp);
		}
	}
	protected override void EnforceSpeedLimit() {
		if(gravityApplication == GravityState.useGravity) {
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
	#region Steering Behaviors
	protected override Vector3 CalculatePositionForAutomaticMovement(Vector3 position) {
		if (gravityApplication == GravityState.useGravity && IsStableOnGround) {
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
	#endregion // Steering Behaviors
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
		public float SecondsToPressJump;
		protected float currentJumpVelocity, heightReached, heightReachedTotal, timeHeld, targetHeight;
		protected bool impulseActive, inputHeld, peaked = false;
		[Tooltip("if false, double jumps won't 'restart' a jump, just add jump velocity")]
		private bool jumpStartResetsVerticalMotion = true;
		public int jumpsSoFar { get; protected set; }
		/// <returns>if this instance is trying to jump</returns>
		public bool IsJumping() { return inputHeld; }
		/// <summary>pretends to hold the jump button for the specified duration</summary>
		/// <param name="jumpHeldDuration">Jump held duration.</param>
		public void FixedUpdate(MovingEntity p) {
			if (inputHeld = (SecondsToPressJump > 0)) { SecondsToPressJump -= Time.deltaTime; }
			if(impulseActive && !inputHeld) { impulseActive = false; }
			if(!inputHeld) return;
			// check stable footing for the jump
			if(p.IsStableOnGround) {
				jumpsSoFar = 0;
				heightReached = 0;
				currentJumpVelocity = 0;
				timeHeld = 0;
			}
			// calculate the jump
			float gForce = p.gravity.GetGravityPower() * p.rb.mass;
			Vector3 jump_force = Vector3.zero, jumpDirection = -p.gravity.dir;
			// if the user wants to jump, and is allowed to jump again
			if(!impulseActive && (jumpsSoFar < maxJumps)) {
				heightReached = 0;
				timeHeld = 0;
				jumpsSoFar++;
				targetHeight = minJumpHeight * p.rb.mass;
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
						targetHeight *= p.rb.mass;
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
		[SerializeField]
		public float power = 9.81f;
		public float GetGravityPower() { return power; }
	}
	#endregion // Gravity
	#region MonoBehaviour
	void Start() {
		GroundNormal = Vector3.up;
		EnsureRigidBody();
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
		if(gravityApplication != GravityState.none) { StandLogic(); }
		MoveLogic();
		// keep track of where we are in relation to the parent platform object
		if(gravityApplication != GravityState.none && transformOnPlatform && transformOnPlatform.parent != null &&
			stickToGround == HowToDealWithGround.withPlaceholder) {
			transformOnPlatform.position = transform.position;
		}
		UpdateStateIndicators ();
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
} // MovingEntity

/// a basic Mobile Entity controller (for MOBs, like seeking fireballs, or very basic enemies)
public class MovingEntityBase : MonoBehaviour {
	#region Public API
	/// <summary>if being controlled by player, this value is constanly reset by user input. otherwise, useable as AI controls.</summary>
	[HideInInspector]
	public Vector3 MoveDirection, LookDirection;
	[Tooltip("speed limit")]
	public float MoveSpeed = 5;
	[Tooltip("how quickly the PlayerControl transform rotates to match the intended direction, measured in degrees-per-second")]
	public float TurnSpeed = 180;
	[Tooltip("how quickly the PlayerControl reaches MoveSpeed. If less-than zero, jump straight to move speed.")]
	public float Acceleration = -1;
	/// <summary>the player's RigidBody, which lets Unity do all the physics for us</summary>
	[HideInInspector]
	public Rigidbody rb { get; protected set; }
	/// <summary>cached variables required for a host of calculations in other scripts</summary>
	public float CurrentSpeed { get; protected set; }
	public float BrakeDistance { get; protected set; }
	[HideInInspector]
	public bool IsBrakeOn = false, AutomaticallyTurnToFaceLookDirection = true;
	protected bool brakesOnLastFrame = false;
	public bool IsMovingIntentionally { get; protected set; }

	public void Copy(MovingEntityBase toCopy) {
		MoveSpeed = toCopy.MoveSpeed;
		TurnSpeed = toCopy.TurnSpeed;
		Acceleration = toCopy.Acceleration;
	}
	public void UpdateStateIndicators() {
		CurrentSpeed = rb.velocity.magnitude;
		BrakeDistance = (Acceleration > 0)?CalculateBrakeDistance (CurrentSpeed,Acceleration/rb.mass)-(CurrentSpeed*Time.deltaTime):0;
		if (IsBrakeOn) {
			brakesOnLastFrame = true;
			IsBrakeOn = false;
		} else {
			brakesOnLastFrame = false;
		}
	}
	#endregion // Public API
	#regionCore Controller Logic
	/// <summary>if a null pointer happens while trying to access a rigidbody variable, call this first.</summary>
	public void EnsureRigidBody() {
		if(rb == null) {
			rb = GetComponent<Rigidbody>();
			if(!rb) { rb = gameObject.AddComponent<Rigidbody>(); }
			rb.useGravity = false; rb.freezeRotation = true;
		}
	}
	public bool IsBraking() { return IsBrakeOn || brakesOnLastFrame; }
	public virtual void MoveLogic() {
		IsMovingIntentionally = MoveDirection != Vector3.zero;
		if (IsBrakeOn) {
			ApplyBrakes (Acceleration);
		} else {
			ApplyMove (Acceleration);
		}
		if (AutomaticallyTurnToFaceLookDirection && transform.forward != LookDirection && TurnSpeed != 0)
		{ UpdateFacing (); }	// turn body as needed
	}
	public virtual void UpdateFacing() {
		if((LookDirection != Vector3.zero && transform.forward != LookDirection)) {
			Vector3 r = (LookDirection == transform.right) ? -transform.forward : ((LookDirection == -transform.right) ? transform.forward : transform.right);
			Vector3 up = Vector3.Cross(LookDirection, r);
			TurnToFace (LookDirection, up);
		}
	}
	public void TurnToFace(Vector3 forward, Vector3 up) {
		Quaternion target = Quaternion.LookRotation(forward, up);
		if (TurnSpeed > 0) {
			target = Quaternion.RotateTowards (transform.rotation, target, TurnSpeed * Time.deltaTime);
		}
		transform.rotation = target;
	}
	protected virtual void EnforceSpeedLimit() {
		float actualSpeed = rb.velocity.magnitude;
		if(actualSpeed > MoveSpeed) {
			rb.velocity = rb.velocity.normalized*MoveSpeed;
		}
	}
	public virtual void ApplyBrakes(float deceleration) {
		if (deceleration > 0) {
			float amountToMove = deceleration * Time.deltaTime;
			MoveDirection = -rb.velocity.normalized;
			float actualSpeed = rb.velocity.magnitude;
			if (actualSpeed > Acceleration * amountToMove) {
				rb.velocity += MoveDirection * amountToMove;
				return;
			}
		}
		rb.velocity = Vector3.zero;
	}
	/// <summary>Applies AccelerationDirection to the velocity. if brakesOn, slows things down.</summary>
	protected virtual void ApplyMove(float acceleration) {
		if (acceleration > 0) {
			float amountToMove = acceleration * Time.deltaTime;
			rb.velocity += MoveDirection * amountToMove;
			EnforceSpeedLimit ();
		} else {
			rb.velocity = MoveDirection * MoveSpeed;
		}
	}
	public static float CalculateBrakeDistance(float speed, float acceleration) { return (speed * speed) / (2 * acceleration); }
	#endregion // Core Controller Logic
	#region Steering Behaviors
	protected Vector3 SeekMath(Vector3 directionToLookToward) {
		Vector3 desiredVelocity = directionToLookToward * MoveSpeed;
		Vector3 desiredChangeToVelocity = desiredVelocity - rb.velocity;
		float desiredChange = desiredChangeToVelocity.magnitude;
		if (desiredChange < Time.deltaTime * Acceleration) { return directionToLookToward; }
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
		directionToMoveToward = (Acceleration > 0)?SeekMath (directionToLookToward):directionToLookToward;
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

	public enum DirectionMovementIsPossible { none, all, forwardOnly, forwardOrBackward, wasd }
	/// <summary>Filters the direction.</summary>
	/// <returns>The direction.</returns>
	/// <param name="dir">Dir.</param>
	/// <param name="dirs">Dirs.</param>
	/// <param name="minimumMoveAlignment">Minimum move alignment. from 0 to 1, the minimum Dot product for moving</param>
	public Vector3 FilterDirection(Vector3 dir, DirectionMovementIsPossible dirs, float minimumMoveAlignment = 0) {
		if (dir == Vector3.zero || dirs == DirectionMovementIsPossible.none) return Vector3.zero;
		float fdot = Vector3.Dot (dir, transform.forward);
		switch (dirs) {
		case DirectionMovementIsPossible.all: break;
		case DirectionMovementIsPossible.forwardOnly:
			if (fdot > 0 && (minimumMoveAlignment <= 0 || fdot > minimumMoveAlignment))
			{ dir = transform.forward; } else { dir = Vector3.zero; } break;
		case DirectionMovementIsPossible.forwardOrBackward:
			if (fdot != 0 && (minimumMoveAlignment <= 0 || Mathf.Abs(fdot) > minimumMoveAlignment)) 
			{ dir = transform.forward * Mathf.Sign(fdot); } else { dir = Vector3.zero; } break;
		case DirectionMovementIsPossible.wasd: {
				float rdot = Vector3.Dot (dir, transform.right);
				if (Mathf.Abs (rdot) > Mathf.Abs (fdot)) { 
					if(minimumMoveAlignment <= 0 || Mathf.Abs(rdot) > minimumMoveAlignment)
					{ dir = transform.right * Mathf.Sign (rdot); } else { dir = Vector3.zero; }
				}
				else if(minimumMoveAlignment <= 0 || Mathf.Abs(fdot) > minimumMoveAlignment)
				{ dir = transform.forward * Mathf.Sign(fdot); } else { dir = Vector3.zero; }
			} break;
		}
		return dir;
	}
	/// <summary>call this during a FixedUpdate process to give this agent simple AI movement</summary>
	public void Seek(Vector3 target, DirectionMovementIsPossible dirs = DirectionMovementIsPossible.all, 
		float minimumMoveAlignment = 1-1.0f/128, bool stopIfAlignmentIsntGoodEnough = true) {
		CalculateSeek (CalculatePositionForAutomaticMovement(target), out MoveDirection, ref LookDirection);
		if (dirs != DirectionMovementIsPossible.all) { MoveDirection = FilterDirection (MoveDirection, dirs, minimumMoveAlignment); }
		if (stopIfAlignmentIsntGoodEnough && MoveDirection == Vector3.zero && Acceleration > 0) { IsBrakeOn = true; }
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
	void Start() { EnsureRigidBody (); }
	/// <summary>where physics-related changes happen</summary>
	void FixedUpdate() {
		MoveLogic();
		UpdateStateIndicators ();
	}
	#endregion // MonoBehaviour
} // MovingEntityBase