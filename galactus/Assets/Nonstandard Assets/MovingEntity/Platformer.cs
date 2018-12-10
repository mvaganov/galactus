#define DEBUG_LINES
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif // UNITY_EDITOR

// TODO bug that causes weird ground clipping with transformOnPlatform, because fixedUpdate changes transformOnPlatform and LateUpdate uses it.
// TODO slow down when colliding a wall! make movement simpler, with a simple limit of moveSpeed (argument not member).
// TODO formalize temporary modifications to MoveSpeed.
// TODO when colliding with a wall in air, with smoothMove set, don't stick, fall!
// TODO stop moving forward while just jumping up and down.

public class Platformer : MOB
{
    public float modifiedMoveSpeed = 1;
    public TMPro.TextMeshPro stats;
    /// <value>whether or not to apply actual force in the InputMoveDirection</value>
    public float MoveEffort;// { get; set; }
    public float standHeight = 1;
    public float halfWidth = 1;
    /// <summary>if circular, this value is 0, and halfWidth is used for radius</summary>
    public float halfLength = 0;

    /// <summary>cache the data structure with the object, to improve performance</summary>
    private RaycastHit standRay = new RaycastHit();
    public Gravity gravity = new Gravity();
    public bool IsStableOnGround = false;
    public Vector3 GroundNormal { get; protected set; }
    public float StandAngle { get; protected set; }
    /// <summary>true if player is walking on ground that is too steep.</summary>
    public bool tooSteep { get; protected set; }
    public Vector3 UpDirection { get { return -gravity.dir; } }
    public float VerticalVelocity { get; protected set; }
    [HideInInspector]
    public bool PositionOnPlatformDuringFixedUpdate;
    /// <summary>used when the player controller needs to stay on a moving platform or vehicle</summary>
    private Transform transformOnPlatform;
    private Vector3 wallForce;

    public enum GravityState { none, useGravity, noFalling }
    public enum HowToParentOnGround { none, placeholderTransform, ownTransform }
    public enum HowToPushOnWalls { smoothMove, smoothMoveAll, allowWallSticking }
    public enum HowToAlign { alignToGravity, alignToGroundNormal }
    [System.Serializable]
    public class Gravity
    {
        [Tooltip("'down' direction for the player, which pulls the player")]
        public Vector3 dir = Vector3.down;
        [SerializeField]
        public float power = 9.81f;
        [Tooltip("if false, disables gravity's effects, and enables flying")]
        public GravityState application = GravityState.useGravity;
    }
    [System.Serializable]
    public struct WallGroundInteraction
    {
        // TODO step up stairs, hang on ledges, climb, wall running
        [Tooltip("how the player should stay stable if the ground is moving\n" +
            "* none: don't bother trying to be stable\n" +
            "* withPlaceholder: placeholder transform keeps track\n" +
            "* withParenting: player is parented to ground (not safe if ground is scaled)")]
        public HowToParentOnGround stickToGround;
        [Tooltip("How the player should deal with walls\n" +
            "* smoothMove: move around non-rigidbody obstacles when pressing against them\n" +
            "* smoothMoveAll: move around ALL obstacles when pressing against them\n" +
            "* allowWallSticking: always press againstwalls")]
        public HowToPushOnWalls pressAgainstWalls;
        public float wallFriction;
        [Tooltip("How the player should align the transform\n" +
            "* alignToGravity: align the top of the model to match gravity\n" +
            "* alignToGroundNormal: align the top of the model to match the ground normal")]
        public HowToAlign howToAlign;
        [Tooltip("The maximum angle the player can move forward at while standing on the ground")]
        public float MaxWalkAngle;
        public WallGroundInteraction(float maxWalkAngle, HowToParentOnGround groundMove, HowToPushOnWalls wallMove, float wallFric, HowToAlign align)
        {
            MaxWalkAngle = maxWalkAngle;
            pressAgainstWalls = wallMove;
            wallFriction = wallFric;
            stickToGround = groundMove;//HowToParentOnGround.placeholderTransform;
            howToAlign = align;

        }
    }
    public WallGroundInteraction wallGroundInteraction =
        new WallGroundInteraction(45, HowToParentOnGround.placeholderTransform,
          HowToPushOnWalls.smoothMove, 0.5f, HowToAlign.alignToGravity);
#if DEBUG_LINES
    GameObject legLine, line_v, gravityForceLine;
    GameObject orientation;
#endif
    /// <summary>useful to call in LateUpdate for camera</summary>
    public void FollowPositionOnPlatform() {
        // if on a platform, update position on the platform based on velocity
        if (transformOnPlatform != null && wallGroundInteraction.stickToGround == HowToParentOnGround.placeholderTransform &&
            !jump.IsJumping && (IsStableOnGround /*|| wasStableOnGroundLastFrame*/))
        {
            // causes funny clipping. I think it's because this is called in LateUpdate, and transformOnPlatform changes in FixedUpdate...
            transform.position = transformOnPlatform.position + Velocity * Time.deltaTime;
        }
    }
    Vector3 hipOffset = Vector3.zero;
    Vector3 lastPlatformPosition; // used to calculate platform velocity
    float balancePercent = 0;
    public float TimeToBalance = 0.5f;
    /// <summary>SHOULD ONLY BE CALLED IN FIXEDUPDATE!</summary>
    private void NotSteppingOnAnything() {
        switch (wallGroundInteraction.stickToGround) {
            case HowToParentOnGround.placeholderTransform:
                if (transformOnPlatform != null && transformOnPlatform.parent != null) {
                    Vector3 platformVelocity = transformOnPlatform.position - lastPlatformPosition - Velocity * Time.deltaTime;
                    if (platformVelocity != Vector3.zero) {
                        platformVelocity /= Time.deltaTime;
                        rb.velocity += platformVelocity;
                    }
                    transformOnPlatform.SetParent(null);
                }
                break;
            case HowToParentOnGround.ownTransform:
                transform.SetParent(null);
                break;
        }
        IsStableOnGround = false;
        balancePercent = 0;
    }
    void StandLogic() {
        if (!IsStableOnGround) {
            balancePercent = 0;
        } else {
            balancePercent += Time.deltaTime / TimeToBalance;
            if (balancePercent > 1) { balancePercent = 1; }
        }
        if (!IsStableOnGround && halfWidth > 0) {
            if (halfLength > 0) {
                hipOffset = new Vector3(Random.Range(-1f, 1f) * halfWidth, 0, Random.Range(1f, -1f) * halfLength);
            } else {
                hipOffset = Random.insideUnitCircle * halfWidth;
                hipOffset.z = hipOffset.y; hipOffset.y = 0;
            }
            hipOffset = transform.TransformVector(hipOffset);
        }
        Ray ray = new Ray(transform.position + hipOffset, gravity.dir);
        float gravMoveThisFram = gravity.power * Time.deltaTime;
        float possibleMoveThisFrame = Acceleration * Time.deltaTime;
        float initialGroundCheck = standHeight + gravMoveThisFram + possibleMoveThisFrame;
        bool groundIsNearby = Physics.Raycast(ray, out standRay, initialGroundCheck);
        if (groundIsNearby)
        {
            if (standRay.distance <= standHeight + gravMoveThisFram) {
#if DEBUG_LINES
                NS.Lines.Make(ref legLine, ray.origin, standRay.point, Color.red);
#endif
                if (!IsStableOnGround) {
                    //Debug.Log("touchdown!");
                    // now that we're standing, cancel gravity.
                    VerticalVelocity = Vector3.Dot(rb.velocity, gravity.dir);
                    rb.velocity -= gravity.dir * VerticalVelocity;
                    VerticalVelocity = 0;
                }
                IsStableOnGround = true;
                //if(GroundNormal != rayDownHit.normal) {
                //    GroundNormal = Vector3.Slerp(GroundNormal, rayDownHit.normal, Time.deltaTime);
                //}
                GroundNormal = standRay.normal;
                StandAngle = Vector3.Angle(UpDirection, GroundNormal);
                if(Vector3.Dot(GroundNormal, rb.velocity) > 0){
                    StandAngle *= -1;
                }
                if(StandAngle > wallGroundInteraction.MaxWalkAngle) {
                    tooSteep = true;
                    IsStableOnGround = false;
                }
            } else {
#if DEBUG_LINES
                NS.Lines.MakeArrow(ref legLine, ray.origin, standRay.point, Color.red);
#endif
                NotSteppingOnAnything();
            }
            if (IsStableOnGround && !jump.IsJumping) {
                Vector3 idealPosition = standRay.point - (gravity.dir * standHeight) - hipOffset;
                transform.position = Vector3.Lerp(transform.position, idealPosition, balancePercent);
                Transform newParent = standRay.collider.transform;
                switch (wallGroundInteraction.stickToGround) {
                    case HowToParentOnGround.placeholderTransform:
                        if(transformOnPlatform == null) {
                            transformOnPlatform = new GameObject("where "+name+" stands").transform;
                        }
                        transformOnPlatform.position = transform.position;
                        if(transformOnPlatform.parent != newParent) {
                            transformOnPlatform.SetParent(newParent);
                        }
                    break;
                    case HowToParentOnGround.ownTransform:
                        if(newParent.localScale != Vector3.one) {
                            Debug.LogWarning("parenting to transform with scale "+newParent.localScale+", expect strange results...");
                        }
                        transform.SetParent(newParent);
                        break;
                }
            }
        } else {
#if DEBUG_LINES
            NS.Lines.MakeArrow(ref legLine, ray.origin, ray.origin + (ray.direction * initialGroundCheck), Color.magenta);
#endif
            NotSteppingOnAnything();
        }
        if(transformOnPlatform != null) {
            lastPlatformPosition = transformOnPlatform.position;
        }
    }

    public float WalkingAgainstInclineLogic() {
        float accelMod = 0;
        // validate that the agent should be able to walk at this angle
        //float walkAngle = -1;
        if(StandAngle > wallGroundInteraction.MaxWalkAngle || 
           (rb.velocity != Vector3.zero && 
            (/*walkAngle =*/ 90 - Vector3.Angle(UpDirection, rb.velocity)) > wallGroundInteraction.MaxWalkAngle)) {
            //Debug.Log(rb.velocity+" Bad walk "+walkAngle+" > "+ wallGroundInteraction.MaxWalkAngle+" || "+StandAngle+" > "+ wallGroundInteraction.MaxWalkAngle);
            IsStableOnGround = false;
        }

        // prevent moving up hill if the hill is too steep (videogame logic!)
        if(IsMovingIntentionally) {
            Vector3 against = Vector3.zero;
            if(tooSteep) {
                Vector3 acrossSlope = Vector3.Cross(GroundNormal, -gravity.dir).normalized;
                Vector3 intoSlope = Vector3.Cross(acrossSlope, -gravity.dir).normalized;
                float upHillAmount = Vector3.Dot(InputMoveDirection, intoSlope);
                if (upHillAmount > 0) {
                    accelMod = upHillAmount;// * wallGroundInteraction.wallFriction;
                    Debug.Log("toosteep: "+accelMod);
                    against = -intoSlope * upHillAmount;
                }
            } else if(wallGroundInteraction.pressAgainstWalls != HowToPushOnWalls.allowWallSticking
                      && wallForce != Vector3.zero) {
                float againstMove = Vector3.Dot(-wallForce, InputMoveDirection);
                accelMod = againstMove;// * wallGroundInteraction.wallFriction;
                //Debug.Log("wall: "+accelMod);
                against = wallForce * againstMove;
            } else {

            }
            if(against != Vector3.zero) {
                InputMoveDirection += against;
                InputMoveDirection.Normalize ();
            }
        }
        return accelMod;
    }

    public override void MoveLogic() {
        IsMovingIntentionally = InputMoveDirection != Vector3.zero;
        // player-controlled agents do this during LateUpdate, AI does it during FixedUpdate.
        if (PositionOnPlatformDuringFixedUpdate) {
            FollowPositionOnPlatform();
            UpdateFacing();
        }
        modifiedMoveSpeed = MoveSpeed * MoveEffort;
        if (gravity.application == GravityState.useGravity) {
            // gravity calculations before any moves
            Vector3 gravityApplied = Vector3.zero;
            float accel = Acceleration;
            // only apply gravity if the agent can't walk on the ground.
            if (!IsStableOnGround) {
                gravityApplied = gravity.power * gravity.dir;
#if DEBUG_LINES
                Vector3 topOfHead = transform.position - gravity.dir;
                NS.Lines.MakeArrow(ref gravityForceLine, topOfHead - gravityApplied, topOfHead, Color.green);
#endif
            } else {
                if (IsMovingIntentionally) {
                    float speedAdjust = WalkingAgainstInclineLogic();
                    if(speedAdjust != 0) {
                        modifiedMoveSpeed -= MoveSpeed * speedAdjust;
                    }
                }
#if DEBUG_LINES
                Vector3 topOfHead = transform.position - gravity.dir;
                NS.Lines.MakeCircle(ref gravityForceLine, topOfHead, gravity.dir, Color.green);
#endif
            }
            if (!IsStableOnGround) {
                rb.velocity += gravityApplied * Time.deltaTime;
            }
            VerticalVelocity = Vector3.Dot(rb.velocity, gravity.dir); // remember vertical velocity (for jumping!)
            if (!IsStableOnGround) {
                rb.velocity -= gravity.dir * VerticalVelocity; // subtract donward-force from move speed. gravity is independent from player movement.
            }
            // TODO re-apply intended up/downward force, from climbing/descending.

            if (IsBrakeOn && (IsStableOnGround || gravity.application != GravityState.useGravity)) {
                ApplyBrakes(Acceleration);
            } else {
                if(MoveEffort > 0) {
                    //Debug.Log(accel);
                    ApplyMove(accel, modifiedMoveSpeed);
                }
            }
            // force slowdown if moving too fast on the ground
            if(IsStableOnGround && VelocityMagnitude > MoveSpeed) {
                rb.velocity -= rb.velocity * (MoveSpeed / VelocityMagnitude) * Time.deltaTime;
            }
            if(!IsStableOnGround) {
                rb.velocity += gravity.dir * VerticalVelocity; // re-apply vertical velocity from gravity
            }
            jump.FixedUpdate(this);
            if (jump.IsJumping && IsStableOnGround) {
                NotSteppingOnAnything();
            }
            CachePhysicsVariables();
            EnforceSpeedLimit();
        } else {
            if (IsBrakeOn) {
                base.ApplyBrakes (Acceleration);
            } else {
                base.ApplyMove (Acceleration, modifiedMoveSpeed);
            }
        }
    }

    public void CalculateDirections(Vector3 lookDir, Vector3 up, out Vector3 right, out Vector3 forward) {
        if(up == Vector3.zero) {
            forward = lookDir;
            right = Vector3.Cross(transform.up, lookDir);
            return;
        }
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

    public override Vector3 MoveDirectionAlignedWith(Vector3 directionVector) {
        if (gravity.application != GravityState.useGravity) {
            return base.MoveDirectionAlignedWith(directionVector);
        }
        Vector3 r, f;
        CalculateDirections(directionVector, -gravity.dir, out r, out f);
        return f;
    }

    public override void UpdateFacing() {
        Vector3 u = (wallGroundInteraction.howToAlign == HowToAlign.alignToGravity
                           || gravity.application == GravityState.noFalling || !IsStableOnGround)
    ? UpDirection : GroundNormal;
        // turn IF needed
        if ((InputLookDirection != Vector3.zero
        && InputLookDirection != transform.forward) || (u != transform.up)) {
            Vector3 correctRight, correctForward;
            CalculateDirections(InputLookDirection, u, out correctRight, out correctForward);
            TurnToFace (correctForward, u);
        }
    }

    /// <summary>if a null pointer happens while trying to access a rigidbody variable, call this first.</summary>
    public void EnsureRigidBody() {
        if(rb == null) {
            rb = GetComponent<Rigidbody>();
            if(!rb) { rb = gameObject.AddComponent<Rigidbody>(); }
        }
        rb.useGravity = false; rb.freezeRotation = true;
    }
    void Start() {
        GroundNormal = Vector3.up;
        EnsureRigidBody();
        // set dimension values based on collider
        do {
            //  sphere - width, height if less than radius
            SphereCollider sc = GetComponent<SphereCollider>();
            if (sc) {
                halfWidth = sc.radius; halfLength = 0;
                if (standHeight < halfWidth) {
                    standHeight = halfWidth;
                }
                break;
            }
            //  capsule - width, height if less than collider.height/2
            CapsuleCollider cc = GetComponent<CapsuleCollider>();
            if(cc) {
                halfWidth = cc.radius; halfLength = 0;
                if(standHeight < cc.height/2) {
                    standHeight = cc.height / 2;
                }
                break;
            }
            //  box - width, length, height if less than collider.height/2
            BoxCollider bc = GetComponent<BoxCollider>();
            if(bc) {
                halfWidth = bc.size.x / 2;
                halfLength = bc.size.z / 2;
                if(standHeight < bc.size.y/2){
                    standHeight = bc.size.y / 2;
                }
                break;
            }
        } while (false);
    }

    /// <summary>where physics-related changes happen</summary>
    protected override void FixedUpdate() {
        if (gravity.application != GravityState.none) { StandLogic(); }
        MoveLogic();
        UpdateStateIndicators();
        wallForce = Vector3.zero; // forget wall forces after they are processed (in MoveLogic)
        if (TurnToMatchCameraDuringFixedUpdate && TurnSpeed != 0
            && transform.forward != InputLookDirection) { UpdateFacing(); }
        stats.text = "speed:" + VelocityMagnitude + "\nangle:" + StandAngle;
    }

#if DEBUG_LINES
    GameObject wallForceLine;
#endif
    void OnCollisionStay(Collision c) {
        ////if(transformOnPlatform && c.gameObject == transformOnPlatform.parent) {
        //if(standRay.collider == c.collider) {
        //    //if(!IsStableOnGround && c.gameObject.GetComponent<Rigidbody>() == null) { IsCollidingWithGround = true; } 
        //    //else { GroundNormal = c.contacts[0].normal; }
        //} else 
        if (wallGroundInteraction.pressAgainstWalls == HowToPushOnWalls.smoothMoveAll
        || (wallGroundInteraction.pressAgainstWalls == HowToPushOnWalls.smoothMove
        && c.gameObject.GetComponent<Rigidbody>() == null)) {
            Vector3 n = c.contacts[0].normal;
            float alignmentWithPushing = Vector3.Dot(n, VelocityDirection);
            float angle = Vector3.Angle(n, VelocityDirection);
            if (alignmentWithPushing < 0 && (angle - 90) > wallGroundInteraction.MaxWalkAngle) {
                //Debug.Log(angle+" "+c.gameObject);
                wallForce = n;
#if DEBUG_LINES
                Vector3 p = transform.position + GroundNormal;
                NS.Lines.MakeArrow(ref wallForceLine, p, p + wallForce, Color.black);
#endif
            } else {
                wallForce = Vector3.zero;
            }
        }
    }

#region Jumping
    public Jumping jump = new Jumping();

    [System.Serializable]
    public class Jumping {
        public float minJumpHeight = 0.25f, maxJumpHeight = 1;
        [Tooltip("How long the jump button must be pressed to jump the maximum height")]
        public float fullJumpPressDuration = 0.5f;
        [Tooltip("for double-jumping, put a 2 here. To eliminate jumping, put a 0 here.")]
        public int maxJumps = 1;
        /// <summary>Whether or not the jumper wants to press jump (specifically, how many seconds of jump)</summary>
        [HideInInspector]
        public float PressJump;
        protected float currentJumpVelocity, heightReached, heightReachedTotal, timeHeld, targetHeight;
        protected bool impulseActive, inputHeld, peaked = false;

        [Tooltip("if false, double jumps won't 'restart' a jump, just add jump velocity")]
        private bool jumpStartResetsVerticalMotion = true;
        public int jumpsSoFar { get; protected set; }
        /// <returns>if this instance is trying to jump</returns>
        public bool IsJumping { get { return inputHeld; } set { inputHeld = value; } }
        /// <summary>pretends to hold the jump button for the specified duration</summary>
        public void FixedUpdate(Platformer p) {
            if (inputHeld = (PressJump > 0)) { PressJump -= Time.deltaTime; }
            if (impulseActive && !inputHeld) { impulseActive = false; }
            if (!inputHeld) { return; }
            // check stable footing for the jump
            if (p.IsStableOnGround) {
                jumpsSoFar = 0;
                heightReached = 0;
                currentJumpVelocity = 0;
                timeHeld = 0;
            }
            // calculate the jump
            float gForce = p.gravity.power * p.rb.mass;
            Vector3 jump_force = Vector3.zero, jumpDirection = -p.gravity.dir;
            // if the user wants to jump, and is allowed to jump again
            if (!impulseActive && (jumpsSoFar < maxJumps)) {
                heightReached = 0;
                timeHeld = 0;
                jumpsSoFar++;
                targetHeight = minJumpHeight * p.rb.mass;
                float velocityRequiredToJump = Mathf.Sqrt(targetHeight * 2 * gForce);
                // cancel out current jump/fall forces
                if (jumpStartResetsVerticalMotion) {
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
                if (currentJumpVelocity > 0)
            {
                // handle jump height: the longer you hold jump, the higher you jump
                if (inputHeld) {
                    timeHeld += Time.deltaTime;
                    if (timeHeld >= fullJumpPressDuration) {
                        targetHeight = maxJumpHeight;
                        timeHeld = fullJumpPressDuration;
                    } else {
                        targetHeight = minJumpHeight + ((maxJumpHeight - minJumpHeight) * timeHeld / fullJumpPressDuration);
                        targetHeight *= p.rb.mass;
                    }
                    if (heightReached < targetHeight) {
                        float requiredJumpVelocity = Mathf.Sqrt((targetHeight - heightReached) * 2 * gForce);
                        float forceNeeded = requiredJumpVelocity - currentJumpVelocity;
                        jump_force += (jumpDirection * forceNeeded) / Time.deltaTime;
                        currentJumpVelocity = requiredJumpVelocity;
                    }
                }
            } else {
                impulseActive = false;
            }
            if (currentJumpVelocity > 0) {
                float moved = currentJumpVelocity * Time.deltaTime;
                heightReached += moved;
                heightReachedTotal += moved;
                currentJumpVelocity -= gForce * Time.deltaTime;
            } else if (!peaked && !p.IsStableOnGround) {
                peaked = true;
                impulseActive = false;
            }
            p.rb.AddForce(jump_force);
        }
    }
#endregion // Jumping
}
