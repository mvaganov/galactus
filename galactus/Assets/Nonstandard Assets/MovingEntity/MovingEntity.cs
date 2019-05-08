#define DEBUG_LINES
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif // UNITY_EDITOR

/// <summary>A custom Unity3D character controller, using Rigidbody physics, useful for player characters and AI characters
/// license: Copyfree, public domain.</summary>
/// <author>Michael Vaganov</author>
public class MovingEntity : MOB
{
    #region Public API
    /// <summary>true if on the ground</summary>
    [HideInInspector]
    public bool IsStableOnGround { get; protected set; }
    /// <summary>if OnCollision event happens with ground, this is true</summary>
    [HideInInspector]
    public bool IsCollidingWithGround { get; protected set; }
    //private bool wasStableOnGroundLastFrame;
    public enum GravityState { none, useGravity, noFalling }
    /// <summary>the ground-plane normal, used to determine which direction 'forward' is when it comes to movement on the ground</summary>
    public enum HowToParentOnGround { none, placeholderTransform, ownTransform }
    public enum HowToPushOnWalls { smoothMove, smoothMoveAll, allowWallSticking }
    public enum HowToAlign { alignToGravity, alignToGroundNormal }
    [HideInInspector]
    public Vector3 GroundNormal { get; protected set; }
    /// force applied by a wall that the player is pressing against
    private Vector3 wallForce;
    [HideInInspector]
    /// <summary>What object is being stood on</summary>
    public GameObject StandingOnObject { get; protected set; }
    /// <summary>the angle of standing, compared to gravity, used to determine if the player can reliably walk on this surface (it might be too slanted)</summary>
    [HideInInspector]
    public float StandAngle { get; protected set; }

    /// <summary>if very far from ground, this value is infinity</summary>
    [HideInInspector]
    public float HeightFromGround { get; protected set; }
    [HideInInspector]
    public Collider CollideBox { get; protected set; }
    /// <summary>expected distance from the center of the collider to the ground. auto-populates based on Box of Capsule collider</summary>TODO rename to IdealHeightFromGround
    public float ExpectedHeightFromGround = 2;
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
    public static void CalculatePlanarMoveVectors(Vector3 generalForward, Vector3 upVector, out Vector3 forward, out Vector3 right)
    {
        right = Vector3.Cross(upVector, generalForward).normalized;
        forward = Vector3.Cross(right, upVector).normalized;
    }
    /// <summary>if a null pointer happens while trying to access a rigidbody variable, call this first.</summary>
    public void EnsureRigidBody()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (!rb) { rb = gameObject.AddComponent<Rigidbody>(); }
        }
        rb.useGravity = false; rb.freezeRotation = true;
    }
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
    public void FollowPositionOnPlatform()
    {
        // if on a platform, update position on the platform based on velocity
        if (transformOnPlatform != null && wallGroundInteraction.stickToGround == HowToParentOnGround.placeholderTransform &&
            (IsStableOnGround /*|| wasStableOnGroundLastFrame*/))
        {
            transform.position = transformOnPlatform.position + Velocity * Time.deltaTime;
        }
    }
#if DEBUG_LINES
    GameObject legLine;
    GameObject orientation;
#endif
    void StandLogic()
    {
        Ray ray = new Ray(transform.position, gravity.dir);
        float gravMoveThisFram = gravity.power * Time.deltaTime;
        float initialGroundCheck = ExpectedHeightFromGround + gravity.power;
        bool groundIsNearby = Physics.Raycast(ray, out rayDownHit, initialGroundCheck);
        if (groundIsNearby)
        {
#if Debug_Lines
            NS.Lines.Make(ref legLine, transform.position, rayDownHit.point, Color.red);
#endif
            if (rayDownHit.distance <= ExpectedHeightFromGround + gravMoveThisFram)
            {
                IsStableOnGround = true;
                GroundNormal = rayDownHit.normal; // TODO smooth
            }
            if (IsStableOnGround && !jump.IsJumping())
            {
                Vector3 idealPosition = rayDownHit.point - gravity.dir * ExpectedHeightFromGround;
                transform.position = idealPosition; // TODO smooth
            }
        }
        else
        {
#if Debug_Lines
            NS.Lines.Make(ref legLine, ray.origin, ray.direction * initialGroundCheck, Color.red);
#endif
        }
        Vector3 r = Vector3.Cross(GroundNormal, InputLookDirection).normalized;
#if Debug_Lines
        NS.Lines.MakeArcArrow(ref orientation, 90, 12, r, -InputLookDirection, 
                              transform.position, Color.blue);
#endif
        //// this might be better done in a LateUpdate for a camera, to make smoother motion
        //if(AutomaticallyFollowPositionOnPlatform) { FollowPositionOnPlatform(); }
        //wasStableOnGroundLastFrame = IsStableOnGround;
        //Vector3 hip = (legOffset!=Vector3.zero) ? transform.TransformPoint(legOffset) : transform.position;
        //const float epsilon = 0.0625f;
        //float lengthOfLeg = ExpectedHeightFromGround+ExpectedHorizontalRadius * 5;
        //// use a raycast to check if we're on the ground
        //CollideBox.enabled = false;
        //// if we're grounded enough (close enough to the ground)
        //if(Physics.Raycast(hip, /*gravity.dir*/ -transform.up, out rayDownHit, lengthOfLeg)) {
        //    // record some important details about the space we're standing at
        //    GroundNormal = rayDownHit.normal;
        //    StandAngle = Vector3.Angle(GetUpOrientation(), GroundNormal);
        //    tooSteep = (StandAngle > wallGroundInteraction.MaxWalkAngle);
        //    HeightFromGround = rayDownHit.distance;
        //    if(IsCollidingWithGround || HeightFromGround < ExpectedHeightFromGround + epsilon) {
        //        IsStableOnGround = HeightFromGround < ExpectedHeightFromGround + epsilon;//true;
        //        GameObject newStand = rayDownHit.collider.gameObject;
        //        //if (newStand != StandingOnObject) { IsStableOnGround = false; } else { IsStableOnGround = true; }
        //        StandingOnObject = newStand;
        //        switch(wallGroundInteraction.stickToGround){
        //            case HowToParentOnGround.placeholderTransform:
                        if (transformOnPlatform == null) {
                            transformOnPlatform = (new GameObject ("<where " + name + " stands>")).transform;
                        }
        //                transformOnPlatform.SetParent (StandingOnObject.transform);
        //                break;
        //            case HowToParentOnGround.ownTransform:
        //                transform.SetParent (StandingOnObject.transform);
        //                break;
        //        }
        //        legOffset = Vector3.zero;
        //        float downwardV = Vector3.Dot(rb.velocity, gravity.dir); // check if we're falling.
        //        if(downwardV > 0) {
        //            rb.velocity = rb.velocity - downwardV * gravity.dir; // stop falling, we're on the ground.
        //        }
        //        if(HeightFromGround < ExpectedHeightFromGround - epsilon) {
        //            rb.velocity += GetUpOrientation() * epsilon; // if we're not standing tall enough, edge up a little.
        //        }
        //    } else {
        //        IsStableOnGround = false;
        //    }
        //} else {
        //    HeightFromGround = float.PositiveInfinity;
        //    IsStableOnGround = false;
        //}
        //if(!IsStableOnGround) { // if we're not grounded enough, mark it
        //    IsStableOnGround = false;
        //    StandingOnObject = null;
        //    tooSteep = false;
        //    switch(wallGroundInteraction.stickToGround){
        //        case HowToParentOnGround.placeholderTransform:
        //            if (transformOnPlatform != null) {
        //                transformOnPlatform.SetParent (null);
        //            }
        //            break;
        //    }
        //    // if we couldn't find ground with our leg here, try another location.
        //    legOffset = new Vector3(
        //        Random.Range(-1.0f, 1.0f) * ExpectedHorizontalRadius, 0,
        //        Random.Range(-1.0f, 1.0f) * ExpectedHorizontalRadius);
        //}
        //CollideBox.enabled = true;
    }
    public Vector3 NormalizeDirectionTo(Vector3 direction, Vector3 groundNormal) {
        float amountOfVertical = Vector3.Dot (direction, groundNormal);
        direction -= groundNormal * amountOfVertical;
        if (direction != Vector3.zero) {  direction.Normalize (); }
        return direction;
    }
    protected override void ApplyMove(float acceleration, float moveSpeed) {
        // validate that the agent should be able to walk at this angle
        float walkAngle = 90 - Vector3.Angle(GetUpOrientation(), rb.velocity);
        if(walkAngle > wallGroundInteraction.MaxWalkAngle || StandAngle > wallGroundInteraction.MaxWalkAngle) { IsStableOnGround = false; }
        // only apply gravity if the agent can't walk on the ground.
        if(!IsStableOnGround) {
            Vector3 gravityApplied = gravity.power * gravity.dir;
#if DEBUG_LINES
            Vector3 topOfHead = transform.position - gravity.dir;
            NS.Lines.MakeArrow(ref gravityForceLine, topOfHead - gravityApplied, topOfHead, Color.green);
#endif
            rb.velocity += gravityApplied * Time.deltaTime;
        }
#if DEBUG_LINES
        else
        {
            Vector3 topOfHead = transform.position - gravity.dir;
            NS.Lines.MakeCircle(ref gravityForceLine, topOfHead, gravity.dir, Color.green);
        }
#endif
        VerticalVelocity = Vector3.Dot(rb.velocity, gravity.dir); // remember vertical velocity (for jumping!)
        // prevent moving up hill if the hill is too steep (videogame logic!)
        if(IsMovingIntentionally) {
            Vector3 against = Vector3.zero;
            if(tooSteep) {
                Vector3 acrossSlope = Vector3.Cross(GroundNormal, -gravity.dir).normalized;
                Vector3 intoSlope = Vector3.Cross(acrossSlope, -gravity.dir).normalized;
                float upHillAmount = Vector3.Dot(InputMoveDirection, intoSlope);
                if (upHillAmount > 0) { against = -intoSlope * upHillAmount; }
            } else if(wallGroundInteraction.pressAgainstWalls != HowToPushOnWalls.allowWallSticking && wallForce != Vector3.zero) {
                float againstMove = Vector3.Dot(-wallForce, InputMoveDirection);
                against = wallForce * againstMove;
            }
            if(against != Vector3.zero) {
                Debug.Log("INCLINE");
                InputMoveDirection += against;
                InputMoveDirection.Normalize ();
            }
        }
        base.ApplyMove(acceleration, moveSpeed);
        //if(!IsStableOnGround) {
        //    rb.velocity -= gravity.dir * Vector3.Dot(rb.velocity, gravity.dir); // subtract donward-force from move speed
        //    rb.velocity += gravity.dir * VerticalVelocity; // re-apply vertical velocity from gravity/jumping
        //}
        NS.Lines.MakeArrow(ref line_v, transform.position, transform.position + rb.velocity, Color.magenta, 0.0675f);
    }
    GameObject line_v;
    public override void MoveLogic() {
        if (IsBrakeOn ){//}&& (IsStableOnGround || gravity.application != GravityState.useGravity)) {
            ApplyBrakes (Acceleration);
        }
        IsMovingIntentionally = InputMoveDirection != Vector3.zero;
        if(gravity.application == GravityState.useGravity) {
            if (IsMovingIntentionally) {
                // keeps motion aligned to ground, also makes it easy to put on the brakes
                //InputMoveDirection = NormalizeDirectionTo (InputMoveDirection, GroundNormal);
                //IsMovingIntentionally = InputMoveDirection != Vector3.zero;
            }
            ApplyMove (Acceleration, MoveSpeed);
            //jump.FixedUpdate(this);
        } else if(!IsBrakeOn) {
            base.ApplyMove(Acceleration, MoveSpeed);
        }
		UpdateFacing();
    }
    public delegate void delegate_UpdateFacing(Vector3 forward,Vector3 up);
    public delegate_UpdateFacing UpdateFacingDelegate = null;

    public void CalculateDirections(Vector3 lookDir, Vector3 up, out Vector3 right, out Vector3 forward) {
        right = Vector3.Cross(up, lookDir);
        // if dir is pointing either up or down
        if (right == Vector3.zero) {
            // get a right by using another useful forward/up value
            right = Vector3.Cross(up,
              (lookDir != transform.forward && lookDir != -transform.forward) 
              ? transform.forward : transform.up);
        }
        forward = Vector3.Cross(right, up).normalized;
    }
#if DEBUG_LINES
    GameObject gravityForceLine, line_f, line_r;
#endif

    public override void UpdateFacing() {
        Vector3 correctUp;      // TODO make this a member variable, and only assign here if it's zero? or just set it to ground normal?
        if(!IsCollidingWithGround && !IsStableOnGround && float.IsInfinity(HeightFromGround)) {
            correctUp = GetUpOrientation();
        } else {
            correctUp = (GroundNormal != Vector3.zero && gravity.application != GravityState.none) ? GroundNormal : transform.up;
        }
        // turn IF needed
        if ((InputLookDirection != Vector3.zero
        && InputLookDirection != transform.forward) || (correctUp != transform.up)) {
            Vector3 correctRight, correctForward;
            //correctRight = Vector3.Cross (correctUp, InputLookDirection);
            //if (correctRight == Vector3.zero) {
            //    correctRight = Vector3.Cross (correctUp, (InputLookDirection != transform.forward)?transform.forward:transform.up);
            //}
            //correctForward = Vector3.Cross (correctRight, correctUp).normalized;
            CalculateDirections(InputLookDirection, correctUp, out correctRight, out correctForward);
            TurnToFace (correctForward, correctUp);
            if (UpdateFacingDelegate != null) {
                Vector3 upOr = GetUpOrientation();
                if (correctUp != upOr) {
                    correctUp = upOr;
                    correctRight = Vector3.Cross(correctUp, InputLookDirection);
                    if (correctRight == Vector3.zero) {
                        correctRight = Vector3.Cross(correctUp, (InputLookDirection != transform.forward) ? transform.forward : transform.up);
                    }
                    correctForward = Vector3.Cross(correctRight, correctUp).normalized;
                }
                UpdateFacingDelegate(correctForward, correctUp);
            }
        }
    }

    protected override void EnforceSpeedLimit() {
        if(gravity.application == GravityState.useGravity) {
            float actualSpeed = Vector3.Dot(InputMoveDirection, rb.velocity);
            if(actualSpeed > MoveSpeed) {
                rb.velocity -= InputMoveDirection * actualSpeed;
                rb.velocity += InputMoveDirection * MoveSpeed;
            }
        } else {
            base.EnforceSpeedLimit();
        }
    }
#endregion // Core Controller Logic
#region Steering Behaviors
    protected override Vector3 FilterPositionToBeReachable(Vector3 position) {
        if (gravity.application == GravityState.useGravity && IsStableOnGround) {
            Vector3 delta = position - transform.position;
            float notOnGround = Vector3.Dot (GroundNormal, delta);
            delta -= GroundNormal * notOnGround;
            if (delta == Vector3.zero) {
                return position;
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
        /// <summary>Easy way to tell AI to press jump for a specified number of seconds</summary>
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
            float gForce = p.gravity.power * p.rb.mass;
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
        public Vector3 dir = Vector3.down;
        [SerializeField]
        public float power = 9.81f;
        [Tooltip("if false, disables gravity's effects, and enables flying")]
        public GravityState application = GravityState.useGravity;
    }
#endregion // Gravity

    public WallGroundInteraction wallGroundInteraction = 
        new WallGroundInteraction(45, HowToParentOnGround.placeholderTransform, 
          HowToPushOnWalls.smoothMove, HowToAlign.alignToGravity);

    [System.Serializable]
    public struct WallGroundInteraction {
        // TODO step up stairs, hang on ledges, climb, wall running
        [Tooltip("how the player should stay stable if the ground is moving\n"+
            "* none: don't bother trying to be stable\n"+
            "* withPlaceholder: placeholder transform keeps track\n"+
            "* withParenting: player is parented to ground (not safe if ground is scaled)")]
        public HowToParentOnGround stickToGround;
        [Tooltip("How the player should deal with walls\n"+
            "* smoothMove: move around non-rigidbody obstacles when pressing against them\n"+
            "* smoothMoveAll: move around ALL obstacles when pressing against them\n"+
            "* allowWallSticking: always press againstwalls")]
        public HowToPushOnWalls pressAgainstWalls;
        [Tooltip("How the player should align the transform\n" +
            "* alignToGravity: align the top of the model to match gravity\n" +
            "* alignToGroundNormal: align the top of the model to match the ground normal")]
        public HowToAlign howToAlign;
        [Tooltip("The maximum angle the player can move forward at while standing on the ground")]
        public float MaxWalkAngle;
        public WallGroundInteraction(float maxWalkAngle, HowToParentOnGround groundMove, HowToPushOnWalls wallMove, HowToAlign align){
            MaxWalkAngle = maxWalkAngle;
            pressAgainstWalls = wallMove;
            stickToGround = groundMove;//HowToParentOnGround.placeholderTransform;
            howToAlign = align;
        }
    }

#region MonoBehaviour
    void Start() {
        GroundNormal = Vector3.up;
        EnsureRigidBody();
        //CollideBox = GetComponent<Collider>();
        //if(CollideBox == null) { CollideBox = gameObject.AddComponent<CapsuleCollider>(); }
        //ExpectedHeightFromGround = CollideBox.bounds.extents.y;
        //if(CollideBox is CapsuleCollider) {
        //    CapsuleCollider cc = CollideBox as CapsuleCollider;
        //    ExpectedHorizontalRadius = cc.radius;
        //} else if (CollideBox is SphereCollider) {
        //    SphereCollider cc = CollideBox as SphereCollider;
        //    ExpectedHeightFromGround -= cc.center.y;
        //    ExpectedHorizontalRadius = cc.radius;
        //} else {
        //    Vector3 ex = CollideBox.bounds.extents;
        //    ExpectedHorizontalRadius = Mathf.Max(ex.x, ex.z);
        //}
    }
    /// <summary>where physics-related changes happen</summary>
    protected override void FixedUpdate() {
        //Debug.Log("Platformer");
        if(gravity.application != GravityState.none) { StandLogic(); }
        MoveLogic();
        //// keep track of where we are in relation to the parent platform object
        //if(gravity.application != GravityState.none && transformOnPlatform && transformOnPlatform.parent != null &&
        //    wallGroundInteraction.stickToGround == HowToParentOnGround.placeholderTransform) {
        //    transformOnPlatform.position = transform.position;
        //}
        UpdateStateIndicators ();
        wallForce = Vector3.zero;

        if (TurnToMatchCameraDuringFixedUpdate && TurnSpeed != 0
            && transform.forward != InputLookDirection) { UpdateFacing(); }
    }
    void OnCollisionStay(Collision c) {
        if(c.gameObject == StandingOnObject) {
            if(!IsStableOnGround && c.gameObject.GetComponent<Rigidbody>() == null) { IsCollidingWithGround = true; } 
            else { GroundNormal = c.contacts[0].normal; }
        } else if(wallGroundInteraction.pressAgainstWalls == HowToPushOnWalls.smoothMoveAll
            || (wallGroundInteraction.pressAgainstWalls == HowToPushOnWalls.smoothMove && c.gameObject.GetComponent<Rigidbody>() == null)) {
            wallForce = c.contacts [0].normal;
        }
    }
    //void OnCollisionExit(Collision c) {
    //    if(c.gameObject == StandingOnObject) {
    //        if(IsStableOnGround || IsCollidingWithGround) {
    //            IsCollidingWithGround = false;
    //        }
    //    }
    //    IsStableOnGround = false;
    //}
#endregion // MonoBehaviour
} // MovingEntity

/// a basic Mobile Entity controller (for Mobile OBjects, like seeking fireballs, or very basic enemies). No jumping or gravity code, just moving, facing, and stopping.
[RequireComponent(typeof(Rigidbody))]
public class MOB : MonoBehaviour {
#region Public API
    private Vector3 _MoveDirection;
    public Vector3 InputMoveDirection {
        get { return _MoveDirection; }
        set { _MoveDirection = MoveDirectionAlignedWith(value); }
    }
    /// <value>How the controller is looking</value>
    public Vector3 InputLookDirection { get; set; }

    [Tooltip("the fastest that an agent can go under their own power")]
    public float MoveSpeed = 5;
    [Tooltip("the fastest an agent can go period.")]
    public float SpeedLimit = 100;
    [Tooltip("how quickly the PlayerControl transform rotates to match the intended direction, measured in degrees-per-second")]
    public float TurnSpeed = 180;
    [Tooltip("how quickly the PlayerControl reaches MoveSpeed. If less-than zero, jump straight to move speed.")]
    public float Acceleration = -1;
    private float _velocityMagnitude;
    private Vector3 _velocityDirection;
    public float BrakeDistance { get; protected set; }
    /// <summary>the player's RigidBody, which lets Unity do all the physics for us</summary>
    [HideInInspector]
    public Rigidbody rb { get; protected set; }
    /// <summary>cached variables required for a host of calculations in other scripts</summary>
    [HideInInspector]
    public float VelocityMagnitude { get { return _velocityMagnitude; } set {
            if(value == float.NaN || value == float.PositiveInfinity) {
                throw new System.Exception("bad value for magnitude! "+value);
            }
            _velocityMagnitude = value;
            rb.velocity = _velocityDirection * value;
            BrakeDistance = CalculateBrakeDistance(value, Acceleration);
        } }
    /// <value>The velocity direction, calculated at the beginning of FixedUpdate, and again at the end. Use this instead of rb.velocity.normalized</value>
    [HideInInspector]
    public Vector3 VelocityDirection { get { return _velocityDirection; } set {
            if(IsVectorBad(value)) {
                throw new System.Exception("bad value for velocity direction! " + rb.velocity);
            }
            _velocityDirection = value;
            rb.velocity = value * _velocityMagnitude;
        } }
    public Vector3 Velocity { get { return rb.velocity; } }
//    [HideInInspector]
    public bool IsBrakeOn = false;
    /// <summary>Camera-controlled agents should turn during LateUpdate, for best visual experience</summary>
    [HideInInspector]
    public bool TurnToMatchCameraDuringFixedUpdate = true;
    protected bool brakesOnLastFrame = false;
    public bool IsMovingIntentionally { get; protected set; }
#endregion // Public API
#region Convenience functions
    protected bool IsVectorBad(Vector3 v) {
        return v.x == float.NaN || v.y == float.NaN || v.z == float.NaN;
    }
    protected bool IsVelocityIlleagal { get { return IsVectorBad(rb.velocity); } }
    public void Copy(MOB toCopy) {
        MoveSpeed = toCopy.MoveSpeed;
        TurnSpeed = toCopy.TurnSpeed;
        Acceleration = toCopy.Acceleration;
    }
    protected void CachePhysicsVariables() {
        if(IsVelocityIlleagal) {
            throw new System.Exception("bad value for velocity! " + rb.velocity);
        }
        if (rb.velocity != Vector3.zero) {
            _velocityMagnitude = rb.velocity.magnitude;
            _velocityDirection = rb.velocity / VelocityMagnitude;
            BrakeDistance = CalculateBrakeDistance(VelocityMagnitude, Acceleration);// /rb.mass)-(CurrentSpeed*Time.deltaTime):0;
        } else {
            _velocityMagnitude = 0; _velocityDirection = Vector3.zero; BrakeDistance = 0;
        }
    }
    protected void CachePhysicsVariables(float magnitude) {
        VelocityMagnitude = magnitude; BrakeDistance = CalculateBrakeDistance(VelocityMagnitude, Acceleration);
        if(IsVelocityIlleagal) {
            throw new System.Exception("bad value for velocity! " + rb.velocity);
        }
    }
    protected void CalculatePhysicsVariables(float magnitude, Vector3 direction) {
        VelocityMagnitude = magnitude; VelocityDirection = direction; BrakeDistance = CalculateBrakeDistance(VelocityMagnitude, Acceleration);
        if(IsVelocityIlleagal) {
            throw new System.Exception("bad value for velocity! " + rb.velocity);
        }
    }
    protected void CachePhysicsVariables(float magnitude, Vector3 direction, float brakeDistance) {
        VelocityMagnitude = magnitude; VelocityDirection = direction; BrakeDistance = brakeDistance;
        if(IsVelocityIlleagal) {
            throw new System.Exception("bad value for velocity! " + rb.velocity);
        }
    }
    protected void UpdateStateIndicators() {
        CachePhysicsVariables();
        if (IsBrakeOn) {
            brakesOnLastFrame = true;
            IsBrakeOn = false;
        } else {
            brakesOnLastFrame = false;
        }
#if DEBUG_LINES
        if (BrakeDistance != 0) {
            NS.Lines.MakeArrow(ref brakeLine, transform.position,
               transform.position + VelocityDirection * BrakeDistance,
               IsBraking() ? Color.red : Color.black);
        }
#endif
    }
#if DEBUG_LINES
    GameObject brakeLine;
#endif
#endregion // Convenience functions
#region Core Controller Logic
    public bool IsBraking() { return IsBrakeOn || brakesOnLastFrame; }
    public virtual void MoveLogic() {
        IsMovingIntentionally = InputMoveDirection != Vector3.zero;
        if (IsBrakeOn) {
            ApplyBrakes (Acceleration);
        } else {
            ApplyMove (Acceleration, MoveSpeed);
        }
    }
    public virtual void UpdateFacing() {
        if((InputLookDirection != Vector3.zero && transform.forward != InputLookDirection)) {
            Vector3 r = (InputLookDirection == transform.right) ? -transform.forward : ((InputLookDirection == -transform.right) ? transform.forward : transform.right);
            Vector3 up = Vector3.Cross(InputLookDirection, r);
            TurnToFace (InputLookDirection, up);
        }
    }
    public virtual void TurnToFace(Vector3 forward, Vector3 up) {
        Quaternion target = Quaternion.LookRotation(forward, up);
        if (TurnSpeed > 0) {
            target = Quaternion.RotateTowards (transform.rotation, target, TurnSpeed * Time.deltaTime);
        }
        transform.rotation = target;
    }
    protected virtual void EnforceSpeedLimit() {
        if(VelocityMagnitude > SpeedLimit) {
            rb.velocity = VelocityDirection * SpeedLimit;
            CachePhysicsVariables(SpeedLimit);
        }
    }
    public virtual void ApplyBrakes(float deceleration) {
        if (VelocityMagnitude == 0) return;
        if (deceleration > 0) {
            float amountToMove = deceleration * Time.deltaTime;
            //Debug.Log("BRAKING");
            InputMoveDirection = MoveDirectionAlignedWith(- VelocityDirection);
            if (VelocityMagnitude > amountToMove) {
                rb.velocity += InputMoveDirection * amountToMove;
                float lastBrakeDistance = BrakeDistance;
                CachePhysicsVariables();
                //if(BrakeDistance < lastBrakeDistance)
                    return;
            }
        }
        rb.velocity = Vector3.zero;
        CachePhysicsVariables(0, Vector3.zero, 0);
    }
    /// <returns>The direction aligned with.</returns>
    /// <param name="directionVector">Direction vector, must be unit Vector3!.</param>
    public virtual Vector3 MoveDirectionAlignedWith(Vector3 directionVector) {
        return directionVector;
    }
    public enum MovementCalculations {
        none,
        simpleSpeedLimit,
        allowHighSpeedPreventAcceleration
    }
    public MovementCalculations movementCalculations = MovementCalculations.simpleSpeedLimit;
    /// <summary>Applies InputMoveDirection to the velocity.</summary>
    protected virtual void ApplyMove(float acceleration, float moveSpeed) {
        if (acceleration > 0) {
            switch(movementCalculations){
                case MovementCalculations.simpleSpeedLimit:{
                        float amountToMove = acceleration * Time.deltaTime;
                        rb.velocity += InputMoveDirection * amountToMove; // increase speed
                        float newspeed = rb.velocity.magnitude;
                        if(newspeed > moveSpeed) {
                            rb.velocity = rb.velocity * moveSpeed / newspeed;
                        }
                        CachePhysicsVariables();
                        EnforceSpeedLimit();
                    }break;
                case MovementCalculations.allowHighSpeedPreventAcceleration:{
                        // TODO make a simplified movement, which always limits movement to moveSpeed, instead of doing goofy math to allow greater-than-movespeed
                        //const bool allowFasterThanMoveSpeedTravel = false;
                        float speedBeforeAddition = rb.velocity.magnitude;
                        float amountToMove = acceleration * Time.deltaTime;
                        float speedInDesiredDirection = Vector3.Dot(rb.velocity, InputMoveDirection);
                        if(speedInDesiredDirection <= moveSpeed) { // full speed hasn't been reached
                            if(speedInDesiredDirection+amountToMove > moveSpeed) { // if this addition would push over max speed
                                amountToMove = moveSpeed - speedInDesiredDirection; // set the addition to a smaller value
                            }
                            if(amountToMove > 0) {
                                rb.velocity += InputMoveDirection * amountToMove; // increase speed
                                if(speedBeforeAddition > moveSpeed) {
                                    float speedAfterAddition = rb.velocity.magnitude;
                                    // don't allow this special-case directional speed increase to increase net speed
                                    if (speedAfterAddition > speedBeforeAddition) {
                                        rb.velocity = rb.velocity.normalized * speedBeforeAddition;
                                    }
                                }
                            }
                        }
                        CachePhysicsVariables();
                        EnforceSpeedLimit();
                    }break;
            }
        } else if (acceleration < 0) {
            rb.velocity = InputMoveDirection * moveSpeed;
            CachePhysicsVariables();
        }
    }
    public static float CalculateBrakeDistance(float speed, float acceleration) {
        return (acceleration > 0) ? (speed * speed) / (2 * acceleration) : 
              ((acceleration < 0) ? 0 : float.PositiveInfinity);
    }
#endregion // Core Controller Logic
#region Steering Behaviors
    protected Vector3 SeekMath(Vector3 directionToLookToward) {
        Vector3 desiredVelocity = directionToLookToward * MoveSpeed;
        Vector3 desiredChangeToVelocity = desiredVelocity - Velocity;
        float desiredChange = desiredChangeToVelocity.magnitude;
        if (desiredChange < Time.deltaTime * Acceleration) { return directionToLookToward; }
        Vector3 steerDirection = desiredChangeToVelocity.normalized;
        return steerDirection;
    }
    /// <summary>used by the Grounded controller, to filter positions into ground-space (for better tracking on curved surfaces)</summary>
    protected virtual Vector3 FilterPositionToBeReachable(Vector3 position) { return position; }
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
    /// <summary>Filters the direction based on limitations to movement.</summary>
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
        dir = MoveDirectionAlignedWith(dir);
        return dir;
    }
    /// <summary>call this during a FixedUpdate process to give this agent simple AI movement</summary>
    public void Seek(Vector3 target, DirectionMovementIsPossible dirs = DirectionMovementIsPossible.all, 
        float minimumMoveAlignment = 1-1.0f/128, bool stopIfAlignmentIsntGoodEnough = true) {
        Vector3 move, look = InputLookDirection;
        CalculateSeek (FilterPositionToBeReachable(target), out move, ref look);
        if (dirs != DirectionMovementIsPossible.all) {
            move = FilterDirection (move, dirs, minimumMoveAlignment);
        }
        InputMoveDirection = move;
        InputLookDirection = look;
        if (stopIfAlignmentIsntGoodEnough && InputMoveDirection == Vector3.zero && Acceleration > 0) { IsBrakeOn = true; }
    }
    /// <summary>call this during a FixedUpdate process to give this agent simple AI movement</summary>
    public void Flee(Vector3 target) {
        Vector3 move, look = InputLookDirection;
        CalculateSeek (FilterPositionToBeReachable(target), out move, ref look);
        look *= -1;
        InputMoveDirection = move;
        InputLookDirection = look;
    }
    /// <summary>call this during a FixedUpdate process to give this agent simple AI movement</summary>
    public void Arrive(Vector3 target) {
        Vector3 move, look = InputLookDirection;
        CalculateArrive(FilterPositionToBeReachable(target), out move, ref look);
        InputMoveDirection = move;
        InputLookDirection = look;
    }
    /// <summary>call this during a FixedUpdate process to give this agent simple AI movement</summary>
    public void RandomWalk(float weight = 0, Vector3 weightedTowardPoint = default(Vector3)) {
        if (weight != 0) {
            Vector3 dir = (FilterPositionToBeReachable (weightedTowardPoint) - transform.position).normalized;
            dir = (dir * weight) + (Random.onUnitSphere * (1 - weight));
            Vector3 p = FilterPositionToBeReachable (transform.position + transform.forward + dir);
            Vector3 delta = p - transform.position;
            InputMoveDirection = InputLookDirection = delta.normalized;
        } else {
            Vector3 p = FilterPositionToBeReachable (transform.position + transform.forward + Random.onUnitSphere);
            Vector3 delta = p - transform.position;
            InputMoveDirection = InputLookDirection = delta.normalized;
        }
    }
#endregion // Steering Behaviors
#region MonoBehaviour
    void Start() {
        rb = GetComponent<Rigidbody>();
        if(!rb) { rb = gameObject.AddComponent<Rigidbody>(); }
        rb.useGravity = false; rb.freezeRotation = true;
        CachePhysicsVariables();
    }
    /// <summary>where physics-related changes happen</summary>
    protected virtual void FixedUpdate() {
        MoveLogic();
        UpdateStateIndicators ();
        if (TurnToMatchCameraDuringFixedUpdate && TurnSpeed != 0
            && transform.forward != InputLookDirection) { UpdateFacing(); }
    }
#endregion // MonoBehaviour
} // MovingEntityBase

public static class Reflection {
    public static void AssignValues(object dest, object src) {
        System.Reflection.FieldInfo[] srcF = src.GetType().GetFields();
        System.Type dtype = dest.GetType();
        for (int i = 0; i < srcF.Length; ++i) {
            System.Reflection.FieldInfo destF = dtype.GetField(srcF[i].Name);
            if (destF != null) {
                //if(destF.FieldType.IsAssignableFrom(srcF.FieldType)) {
                //    Debug.Log(dest);
                try {
                    destF.SetValue(dest, srcF[i].GetValue(src));
                }catch(System.Exception){
                //} else {
                    Debug.Log("Couldn't assign "+ destF.FieldType+" with "+ srcF[i].FieldType);
                    //AssignValues(destF.GetValue(dest), srcF[i].GetValue(src));
                //}
                }
            }
        }
    }
#if UNITY_EDITOR
    public static System.Type[] GenerateAssignableTypeListFor(System.Type _base, bool alsoIncludeBase = true) {
        IEnumerable<System.Type> types = (System.Reflection.Assembly.GetExecutingAssembly()
            .GetTypes().Where(t => t.IsClass && (alsoIncludeBase || t != _base) && _base.IsAssignableFrom(t)));
        return types.ToArray();
    }
    public static int GetComponentIndex(Component c) {
        return System.Array.IndexOf(c.GetComponents<Component>(), c);
    }
    public static void SetComponentIndex(Component c, int index) {
        int indexOfNew = GetComponentIndex(c);
        while (indexOfNew > index) {
            UnityEditorInternal.ComponentUtility.MoveComponentUp(c);
            indexOfNew = GetComponentIndex(c);
        }
        while (indexOfNew < index) {
            UnityEditorInternal.ComponentUtility.MoveComponentDown(c);
            indexOfNew = GetComponentIndex(c);
        }
    }
    public static void OnInspectorGUI_AllowRecast<T>(Object target,
        string label, ref System.Type[] types, ref string[] typeNames,
        ref int selectedTypeIndex) where T : Component
    {
        if (types == null) {
            types = Reflection.GenerateAssignableTypeListFor(typeof(T));
            typeNames = types.Select(t => t.Name).ToArray();
            //Debug.Log(string.Join(", ", typeNames));
            selectedTypeIndex = System.Array.IndexOf(types, target.GetType());
        }
        T m = (T)target;
        //EditorGUILayout.LabelField("Some help", "Some other text");
        int newSelection = EditorGUILayout.Popup(label, selectedTypeIndex, typeNames);
        if (newSelection != selectedTypeIndex) {
            selectedTypeIndex = newSelection;
            T newm = m.gameObject.AddComponent(types[selectedTypeIndex]) as T;
            // push all relevantly named values from this script into the new one
            Reflection.AssignValues(newm, m);
            // put the new component in place of the old component
            Reflection.SetComponentIndex(newm, Reflection.GetComponentIndex(m));
            // delete the original script
            Object.DestroyImmediate(m);
        }
    }
#endif
}

#if UNITY_EDITOR
public class CustomEditor_TYPE_ADJUSTABLE<T> : Editor where T : Component {
    public static string TypecastLabel = "Change Type", ShowHideLabel = " Typecasting";
    int selectedTypeIndex;
    bool showExtras;
    System.Type[] types;
    string[] typeNames;
    public override void OnInspectorGUI() {
        // Show default inspector property editor
        DrawDefaultInspector();
        if (GUILayout.Button((showExtras ? "Hide" : "Show") + ShowHideLabel)) {
            showExtras = !showExtras;
        }
        if (showExtras) {
            Reflection.OnInspectorGUI_AllowRecast<T>(target, TypecastLabel,
               ref types, ref typeNames, ref selectedTypeIndex);
        }
    }
}

[CustomEditor(typeof(MOB), true)]
public class CustomEditor_MOB : CustomEditor_TYPE_ADJUSTABLE<MOB>{}
#endif