//#define DEBUG_CAMERA_ROTATE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NS {
	// should not be parented to the body...
	public class CameraControls : MonoBehaviour {
		/// <summary>made into a settings class so that the same camera can have its settings switched at runtime without having to adjust other camera referencing scripts</summary>
		[System.Serializable]
		public class ControlSettings {
			// for third-person view
			public Transform targetToCenterOn;
			public enum ScrollWheelBehavior{ none, zoomOutAndIn, zoomOutAndInUnconstrained, zoomInAndOut, zoomInAndOutUnconstrained, roll, invalid }
			public ScrollWheelBehavior scrollWheelBehavior = ScrollWheelBehavior.zoomInAndOut;
			public float mouseSensitivity = 5;
			public float distanceFromTarget = 0;
			[Tooltip("how fast the camera turns itself")]
			public float autoTurnSpeed = 180;
			[Tooltip("which direction 'up' is")]
			public Vector3 currentStandDirection = Vector3.up;
			[Tooltip("Keep 'up' oriented consistently")]
			public bool maintainStandDirection = true;
			[Tooltip("If false, view will adjust with 'up'. If true, view direction will maintain even if 'up' changes.")]
			public bool maintainLookDirection = false;
			public bool dontClipIntoWalls = true;

			public void Copy(ControlSettings toCopy) {
				targetToCenterOn = toCopy.targetToCenterOn;
				scrollWheelBehavior = toCopy.scrollWheelBehavior;
				mouseSensitivity = toCopy.mouseSensitivity;
				distanceFromTarget = toCopy.distanceFromTarget;
				autoTurnSpeed = toCopy.autoTurnSpeed;
				currentStandDirection = toCopy.currentStandDirection;
				maintainStandDirection = toCopy.maintainStandDirection;
				maintainLookDirection = toCopy.maintainLookDirection;
				dontClipIntoWalls = toCopy.dontClipIntoWalls;
			}
			public ControlSettings(ControlSettings toCopy) { Copy(toCopy); }
			public ControlSettings() { }
		}
		public ControlSettings settings = new ControlSettings();

		private bool wasStandingLastFrame = false;

		// if not zero, the camera will incrementally turn to have this as the stand direction (for when 'up' changes).
		private Vector3 targetStandDirection = Vector3.zero;
		[HideInInspector]
		private float pitch = 0, yaw = 0, roll = 0;
		/// <summary>the rotation when (pitch == 0 && yaw == 0 && roll == 0)</summary>
		private Quaternion identityRotation = Quaternion.identity;


		/// <summary>what to do whenever the camera moves, just before it renders</summary>
		public System.Action onMove;

		public float Pitch { get { return pitch; } }
		public float Yaw { get { return yaw; } }
		public float Roll { get { return roll; } }

		public Vector3 GetCorrectUp() {
			return (targetStandDirection != Vector3.zero) ? targetStandDirection : settings.currentStandDirection;
		}

		public Vector3 GetHorizonalDirection(Vector3 up = default(Vector3)) {
			if (up == default(Vector3)) { up = GetCorrectUp (); }
			float upAlign = Vector3.Dot(transform.forward, up);
			Vector3 movHorizonDir = transform.forward - up * upAlign;
			return movHorizonDir.normalized;
		}

		#if DEBUG_CAMERA_ROTATE
		GameObject upDir, forDir, startforDir;
		#endif

		// Use this for initialization
		void Start () {
			if (settings.targetToCenterOn == null && transform.parent != null) {
				settings.targetToCenterOn = transform.parent;
			}
		}

		public static void DupTransform(Transform dest, Transform src) {
			dest.position = src.position;
			dest.rotation = src.rotation;
			dest.localScale = src.localScale;
		}

		public void SetStandDirectionTarget(Vector3 newStandToTransitionInto) {
			if (newStandToTransitionInto == targetStandDirection) {
				return;
			}
			targetStandDirection = newStandToTransitionInto;
			if (targetStandDirection == -transform.up) { // if the up is exact opposite, flip with velocity 
				Vector3 flipdir = transform.forward;
				Rigidbody rb = GetComponent<Rigidbody> ();
				if(rb == null) { rb = gameObject.GetComponentInParent<Rigidbody> (); }
				if (rb != null && rb.velocity != Vector3.zero) {
					flipdir = rb.velocity;
				}
				ForceCurrentStandDirection ( (transform.up + (flipdir * 0.0675f)).normalized );
			} else {
				if (settings.maintainLookDirection) {
					ForceCurrentStandDirection (transform.up);
				}
			}
		}

		public void AcceptCurrentTransform() {
			pitch = 0;
			yaw = 0;
			ForceCurrentStandDirection (transform.up);
		}
		public void ForceCurrentStandDirection(Vector3 newStandUp) {
			settings.currentStandDirection = newStandUp;
			#if DEBUG_CAMERA_ROTATE
			Lines.MakeArrow (ref startforDir, Vector3.zero, transform.forward, 3, Color.cyan);
			Lines.MakeArrow (ref upDir, Vector3.zero, newStandUp, 3, Color.green);
			#endif
			Vector3 identityForward = CalculateIdentityForward ();
			#if DEBUG_CAMERA_ROTATE
			Lines.MakeArrow (ref forDir, Vector3.zero, identityForward, 3, Color.blue);
			#endif
			identityRotation = Quaternion.LookRotation (identityForward, newStandUp);
			if (settings.maintainLookDirection) {
				pitch = CalculateCurrentPitch(identityForward);// nextPitch;
				NormalizeAngle (ref pitch);
			}
			// set pitch/yaw/roll (and rotation) to be correct given the new identity
			yaw = 0;
		}
		Vector3 CalculateIdentityForward() {
			// calculate correct new identity
			Vector3 right = (transform.forward != settings.currentStandDirection && transform.forward != -settings.currentStandDirection)
				? Vector3.Cross (settings.currentStandDirection, transform.forward).normalized : transform.right;
			// if it's upside-down, flip it
			Vector3 identityForward = Vector3.Cross (right, settings.currentStandDirection).normalized;
			if(Vector3.Dot(transform.up, settings.currentStandDirection) < 0) {
				identityForward *= -1;
			}
			return identityForward;
		}
		public float CalculateCurrentPitch(Vector3 identityForward = default(Vector3)) {
			if (identityForward == Vector3.zero) {
				identityForward = CalculateIdentityForward ();
			}
			float currentPitch = Vector3.Angle (identityForward, transform.forward);
			if (Vector3.Dot (transform.forward, settings.currentStandDirection) > 0) {
				currentPitch *= -1;
			}
			return currentPitch;
		}
		public static void NormalizeAngle(ref float angle) {
			while (angle < -180) { angle += 360; }
			while (angle > 180) { angle -= 360; }
		}

		bool isUpdsideDown;
		public void CalculateDirections(out Vector3 right, out Vector3 forward) {
			if (transform.forward != settings.currentStandDirection &&
				transform.forward != -settings.currentStandDirection) {
				Vector3 f = transform.forward;
				right = Vector3.Cross (f, -settings.currentStandDirection).normalized;
				forward = Vector3.Cross (-settings.currentStandDirection, right).normalized;
				float alignedWithGravity = Vector3.Dot (-settings.currentStandDirection, transform.up);
				bool wasUpsideDown = isUpdsideDown;
				isUpdsideDown = false;
				if (alignedWithGravity >= 0) { // if 'up' is in the direction of gravity
					isUpdsideDown = true;
					right *= -1; // we're upside-down
					forward *= -1;
				}
				if (wasUpsideDown != isUpdsideDown) {
					Debug.Log ("CHANGE");
				}
			} else {
				right = transform.right;
				forward = Vector3.Cross (right, -settings.currentStandDirection).normalized;
			}
		}

		void FixedUpdate() {
			NS.CursorLock.LockUpdate();
		}

		public bool IsActivelyRightingSelf() {
			return settings.maintainStandDirection && targetStandDirection != Vector3.zero;
		}

		// Update is called once per frame
		void LateUpdate () {
			if (settings.maintainStandDirection && !wasStandingLastFrame) {
				SetStandDirectionTarget (settings.currentStandDirection);
			}
			if (IsActivelyRightingSelf()) {
				float angle = Vector3.Angle (settings.currentStandDirection, targetStandDirection);
				float angleThisFrame = UnityEngine.Time.deltaTime * settings.autoTurnSpeed;
				if (angle < angleThisFrame) {
					ForceCurrentStandDirection (targetStandDirection);
					targetStandDirection = Vector3.zero;
				} else {
					float t = (settings.autoTurnSpeed * UnityEngine.Time.deltaTime) / angle;
					Vector3 next = Vector3.Lerp (settings.currentStandDirection, targetStandDirection, t).normalized;
					ForceCurrentStandDirection (next);
				}
			}
			float v = Input.GetAxis ("Mouse Y") * settings.mouseSensitivity;
			float h = Input.GetAxis ("Mouse X") * settings.mouseSensitivity;
			float wheel = Input.GetAxis ("Mouse ScrollWheel") * settings.mouseSensitivity;
			if (wheel != 0) {
				switch (settings.scrollWheelBehavior) {
				case ControlSettings.ScrollWheelBehavior.none: break;
				case ControlSettings.ScrollWheelBehavior.roll: if(settings.maintainStandDirection) { roll += wheel; } break;
				case ControlSettings.ScrollWheelBehavior.zoomOutAndInUnconstrained: settings.distanceFromTarget += wheel; break;
				case ControlSettings.ScrollWheelBehavior.zoomOutAndIn:
					settings.distanceFromTarget += wheel;
					if (settings.distanceFromTarget < 0) { settings.distanceFromTarget = 0; }
					break;
				case ControlSettings.ScrollWheelBehavior.zoomInAndOutUnconstrained: settings.distanceFromTarget -= wheel; break;
				case ControlSettings.ScrollWheelBehavior.zoomInAndOut:
					settings.distanceFromTarget -= wheel;
					if (settings.distanceFromTarget < 0) { settings.distanceFromTarget = 0; }
					break;
				}
			}
			if (settings.maintainStandDirection) {
				pitch -= v;
				yaw += h;
				transform.rotation = identityRotation;
				transform.Rotate (pitch, yaw, roll);
				NormalizeAngle (ref pitch);
				NormalizeAngle (ref yaw);
			} else {
				if(settings.scrollWheelBehavior == ControlSettings.ScrollWheelBehavior.roll) {
					transform.Rotate (-v, h, wheel);
				} else {
					transform.Rotate (-v, h, 0);
				}
			}
			if (settings.targetToCenterOn != null) {
				if (settings.distanceFromTarget < 0) {
					settings.distanceFromTarget = 0;
				}
				transform.position = CalculatePositionForCamera ();
			}
			wasStandingLastFrame = settings.maintainStandDirection;
			if (onMove != null) {
				onMove.Invoke ();
			}
		}
		public Vector3 GetZoomOutOffset() {
			float d = settings.distanceFromTarget;
			if (settings.dontClipIntoWalls && settings.targetToCenterOn != null) {
				RaycastHit rh = new RaycastHit ();
				if (Physics.Raycast (settings.targetToCenterOn.position, -transform.forward, out rh, settings.distanceFromTarget)) {
					d = rh.distance;
				}
			}
			return -transform.forward * d;
		}
		public Vector3 CalculatePositionForCamera() {
			return settings.targetToCenterOn.position + GetZoomOutOffset();
		}
	}
}