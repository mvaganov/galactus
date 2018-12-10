using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubetestController : MonoBehaviour {
    public float walkSpeed = 2;
    public float crawlSpeed = 1;
    public float runSpeed = 6;
    public float flySpeed = 5;
    public float jetSpeed = 20;
    public float turnSmoothTime = 0.2f;
    public float jumpHeight = 1;
    float turnSmoothVelocity;
    public float speedSmoothTime = 0.1f;
    float speedSmoothVelocity;
    float currentSpeed;
    public Transform cameraT;
    public CharacterController controller;
    public Animator animator;
    public float gravity = -10;
    float velocityY;
    [Range(0,1)]
    public float airControlPercent = .5f;
    public bool isFlying = false;
    public Transform modelRoot;

    void Start () {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (cameraT == null)
        {
            cameraT = Camera.main.transform;
        }
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }
	}
	
	// Update is called once per frame
	void Update () {
        // input
        Vector2 uInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        bool running = Input.GetKey(KeyCode.LeftShift);
        bool crouching = Input.GetKey(KeyCode.LeftControl);
        if(Input.GetKeyDown(KeyCode.Tab)) {
            isFlying = !isFlying;
        }
        if (running && crouching) { running = false; }
        // update
        if (!isFlying) {
            MoveOnGround(uInput, running);
            if (Input.GetButton("Jump"))
            {
                Jump();
            }
            // animation
            float actualSpeed = new Vector2(controller.velocity.x, controller.velocity.z).magnitude;
            float animationSpeedPercent = (running | crouching) ? 1 : .5f;
            float targetSpeed = uInput == Vector2.zero ? 0 : (crouching) ? crawlSpeed : (running) ? runSpeed : walkSpeed;
            if (targetSpeed != 0)
            {
                animationSpeedPercent *= actualSpeed / targetSpeed;
            }
            else { animationSpeedPercent = 0; }
            if (controller.isGrounded)
            {
                animator.SetFloat("speedPercent", animationSpeedPercent, GetModifiedSmoothTime(speedSmoothTime), Time.deltaTime);
            }
            else
            {
                animator.SetFloat("flyControl", 0);
            }
        } else {
            MoveInAir(uInput, running);
            // animation
            float actualSpeed = controller.velocity.magnitude;
            float animationSpeedPercent = (running) ? 1 : .5f;
            float targetSpeed = uInput == Vector2.zero ? 0 : (running) ? jetSpeed : flySpeed;
            if (targetSpeed != 0) {
                animationSpeedPercent *= actualSpeed / targetSpeed;
            }
            else { animationSpeedPercent = .5f; }
            //animator.SetFloat("speedPercent", animationSpeedPercent, GetModifiedSmoothTime(speedSmoothTime), Time.deltaTime);
            animator.SetFloat("flyControl", animationSpeedPercent);
        }
        animator.SetBool("isGrounded", controller.isGrounded);
        animator.SetBool("isCrawling", crouching);
    }

    public void MoveInAir(Vector2 uInput, bool running) {
        float targetSpeed = uInput == Vector2.zero ? 0 : (!running) ? flySpeed : jetSpeed;
        if(uInput != Vector2.zero) {
            Vector2 uDir = uInput.normalized;
            Vector3 targetForward = cameraT.forward;
            Vector3 rightwards = (targetForward!=Vector3.up) ? Vector3.Cross(targetForward, Vector3.up) : Vector3.right;
            Vector3 upwards = Vector3.Cross(rightwards, targetForward);
            Quaternion targetRotation = Quaternion.LookRotation(targetForward, upwards);
            transform.rotation = targetRotation;//Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime);
        }
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, GetModifiedSmoothTime(speedSmoothTime));
        Vector3 velocity = transform.forward * uInput.y + transform.right * uInput.x;
        velocity = velocity.normalized * currentSpeed;
        controller.Move(velocity * Time.deltaTime);
    }

    public void MoveOnGround(Vector2 uInput, bool running) {
        float targetSpeed = uInput == Vector2.zero ? 0 : (!running) ? walkSpeed : runSpeed;
        if (uInput != Vector2.zero)
        {
            Vector2 uDir = uInput.normalized;
            float targetRotation = Mathf.Atan2(uDir.x, uDir.y) * Mathf.Rad2Deg + cameraT.eulerAngles.y;
            transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, GetModifiedSmoothTime(turnSmoothTime));
        }
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, GetModifiedSmoothTime(speedSmoothTime));

        velocityY += Time.deltaTime * gravity;

        Vector3 velocity = transform.forward * currentSpeed + Vector3.up * velocityY;
        controller.Move(velocity * Time.deltaTime);


        if (controller.isGrounded)
        {
            velocityY = 0;
        }
    }

    public void Jump() {
        if(controller.isGrounded) {
            float jumpV = Mathf.Sqrt(-2 * gravity * jumpHeight);
            velocityY = jumpV;
        }
    }

    public float GetModifiedSmoothTime(float smoothTime) {
        if(controller.isGrounded){
            return smoothTime;
        }
        if(airControlPercent == 0){
            return float.MaxValue;
        }
        return smoothTime / airControlPercent;
    }
}
