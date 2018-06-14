#define SHOW_MATH
#define TEST_W_MOUSE_AND_KEYBOARD
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bobber : MonoBehaviour {
	// should start walking when head follows this pattern:
	// 3 bobs
	// at least 10 degrees per bob, but not more than 45 degrees per bob
	// all while forward/backward tilt stays between -10 and +10

	#if SHOW_MATH
	float orbit = 1.25f;
	GameObject arrow_up, arrow_forward, plane_bisector, line_side, line_up, line_step, q_axis, q_angle;
	public TMPro.TextMeshPro label_sidetilt, label_forwardtilt;
	public GameObject icon;
	#endif
	#if TEST_W_MOUSE_AND_KEYBOARD
	public float mouseSensitivity = 5;
	#endif
	public ParticleSystem ps;

	Quaternion lastRotation;
	Vector3 currentAxis;
	float lastAngle;
	float sameAxisEpsilon = 30;

//	bool inWalkMode = false;
	public float warmupTiltRequired = 30;
	int warmupSteps = 3;
	public Vector3 pitchYawRoll = Vector3.zero;
	public float Pitch { get { return pitchYawRoll.x; } set { pitchYawRoll.x = value; } }
	public float Yaw   { get { return pitchYawRoll.y; } set { pitchYawRoll.y = value; } }
	public float Roll  { get { return pitchYawRoll.z; } set { pitchYawRoll.z = value; } }

	public Vector3 gravityUp = Vector3.up;
	public Vector3 gait = new Vector3 (1, 0, 1);

	void Start () {
		CmdLine.Instance.EnableDebugLogIntercept();
		CmdLine.Instance.activeOnStart = false;
		CmdLine.Instance.hideInWorldSpace = true;
		//Debug.LogWarning ("hi");
		//Debug.LogError ("hi");
		//throw new System.Exception ("oh");
	}

	public enum KindOfStep {none, left, right}

	float ClampAngle(float a){ while(a > 180) { a -= 360; } while(a < -180) { a += 360; } return a; }

	// Update is called once per frame
	void Update () {
		#if TEST_W_MOUSE_AND_KEYBOARD
		Roll += -Input.GetAxis ("Mouse X") * mouseSensitivity;
		Pitch += Input.GetAxis ("Mouse Y") * mouseSensitivity;
		Yaw += Input.GetAxis ("Horizontal");
		#endif
		Transform head = transform;
		Vector3 p = head.position;

		#if TEST_W_MOUSE_AND_KEYBOARD
		Vector3 bodyRightSide = Vector3.Cross (gravityUp, head.forward);
		if(bodyRightSide == Vector3.zero) { bodyRightSide = head.right; } else { bodyRightSide.Normalize (); }

		Vector3 bodyForward = Vector3.Cross (bodyRightSide, gravityUp);
		if(bodyForward == Vector3.zero) { bodyForward = head.forward; } else { bodyForward.Normalize (); }
		#else
		Vector3 bodyForward = Vector3.Cross (head.right, gravityUp);
		if(bodyForward == Vector3.zero) { bodyForward = head.forward; } else { bodyForward.Normalize (); }

		Vector3 bodyRightSide = Vector3.Cross (gravityUp, bodyForward);
		if(bodyRightSide == Vector3.zero) { bodyRightSide = head.right; } else { bodyRightSide.Normalize (); }
		#endif

		Vector3 bodyUp = Vector3.Cross (bodyForward, bodyRightSide);
		bodyUp.Normalize ();

		Vector3 bodyUpWithForwardTilt = Vector3.Cross (head.forward, bodyRightSide);
		bodyUpWithForwardTilt.Normalize ();

		Vector3 a = lastRotation.eulerAngles, b = transform.rotation.eulerAngles;
		Quaternion rotationThisFrame = Quaternion.Euler (b.x - a.x, b.y - a.y, b.z - a.z);
		float rotAngle;
		Vector3 rotAxis;
		rotationThisFrame.ToAngleAxis (out rotAngle, out rotAxis);
		rotAngle = ClampAngle (rotAngle);
		float axisShiftAngle = Vector3.Angle (currentAxis, rotAxis);
//		float alignF = Vector3.Dot (currentAxis, transform.forward);
		float alignR = Vector3.Dot (currentAxis, transform.right);
		if (alignR < -.5f) {
			rotAngle *= -1;
			rotAxis *= -1;
		}
		if (axisShiftAngle > sameAxisEpsilon && axisShiftAngle < (180 - sameAxisEpsilon) && rotAngle > 1) {
			currentAxis = rotAxis;
			ps.Emit (1);
			lastRotation = transform.rotation;
		}
//		Debug.Log (alignR);
			//Quaternion..FromToRotation(lastRotation, transform.rotation);

		#if SHOW_MATH
		NS.Lines.MakeQuaternion(ref q_axis, ref q_angle, rotationThisFrame, transform.position, Color.black, transform.rotation, startPoint:Vector3.up);
		NS.Lines.MakeArrow (ref arrow_up, p, p + head.up, Color.gray);
		NS.Lines.MakeArrow (ref arrow_forward, p, p + head.forward, Color.blue);
		NS.Lines.MakeArcArrow (ref plane_bisector, 180, 24, bodyRightSide, -bodyForward, p, Color.cyan);
		NS.Lines.MakeArrow (ref line_side, p, p + bodyRightSide, Color.red);
		NS.Lines.Make (ref line_up, p, p + bodyUpWithForwardTilt, Color.green);
		#endif

		float alignmentWithRight = Vector3.Dot (head.up, bodyRightSide);
		bool currentlyOnRight = alignmentWithRight > 0;
		float alignmentWithForward = Vector3.Dot (head.up, bodyForward);
		bool currentlyForward = alignmentWithForward > 0;

		currentForwardTilt = Vector3.Angle (bodyUpWithForwardTilt, bodyUp) * ((currentlyForward)?1:-1);
		float angle_sideTilt = Vector3.Angle (bodyUpWithForwardTilt, head.up) * ((currentlyOnRight)?1:-1);

		#if SHOW_MATH
		if (label_sidetilt != null) {
			string txt = ((currentlyOnRight)?"R ":"L ");
			txt += ((int)angle_sideTilt).ToString ();
			label_sidetilt.text = txt;
		}
		if(label_forwardtilt != null){
			string txt = ((currentlyForward)?"F ":"B ");
			txt += ((int)currentForwardTilt).ToString ();
			label_forwardtilt.text = txt;
		}
		#endif

		Quaternion q = Quaternion.Euler (pitchYawRoll);
		head.rotation = q;

		#if SHOW_MATH
		if (icon != null) {
			icon.transform.position = p + head.up * orbit;
		}
		#endif

		if (angle_sideTilt != currentAngle) {
			float delta = angle_sideTilt - currentAngle;
			currentAngle = angle_sideTilt;
//			float extraStep = delta / 90.0f;
			KindOfStep kindOfStep = onRightSide?KindOfStep.right:KindOfStep.left;
			ContinueStep (kindOfStep, delta, bodyForward, bodyUp);
		}
//		if (currentlyOnRight != onRightSide) {
//			steps.Add (currentAngle);
//			onRightSide = currentlyOnRight;
//			currentAngle = 0;
//			if (steps.Count > 10) {
//				steps.RemoveAt (0);
//			}
//		}
	}
	float currentDirection = 0;
	public float currentAngleRotated = 0, currentForwardTilt;

	void ContinueStep(KindOfStep kind, float headTiltDelta, Vector3 forward, Vector3 up) {
		if (headTiltDelta == 0)
			return;
		if ((currentDirection <= 0) != (headTiltDelta <= 0)) {
			steps.Add (new Step(NS.Timer.now(), currentAngleRotated, currentForwardTilt));
			if (steps.Count > warmupSteps) {
				steps.RemoveAt (0);
			}
			currentAngleRotated = 0;
			currentDirection = headTiltDelta;
			float totalAngle = 0;
			for (int i = 0; i < steps.Count; ++i) {
				if (currentForwardTilt > 5) {
					totalAngle += steps [i].sideAngle;
				}
			}
//			inWalkMode = (totalAngle >= warmupTiltRequired);
		}
		currentAngleRotated += headTiltDelta;
		Vector3 thisStep = gait;
//		if (headTiltDelta < 0) {
//			headTiltDelta *= -1;
//			if(kind == KindOfStep.left) kind = KindOfStep.right;
//			if(kind == KindOfStep.right) kind = KindOfStep.left;
//		}
		if (headTiltDelta < 0) {
			thisStep.x *= -1;
		}
		if (forward == Vector3.zero) {
			throw new System.Exception ("forward zero");
		}
		if (up == Vector3.zero) {
			throw new System.Exception ("up zero");
		}
		Quaternion rotateAlongForwardBisectArc = Quaternion.LookRotation (forward, up);
		thisStep = rotateAlongForwardBisectArc * thisStep;
		#if SHOW_MATH
		NS.Lines.MakeArrow (ref line_step, transform.position, transform.position + thisStep, Color.yellow);
		#endif
//		if(inWalkMode) {
//			Locomotion (thisStep * (Mathf.Abs(headTiltDelta) / 90));
//		}
	}

	void Locomotion(Vector3 delta) {
		transform.position += delta;
	}
	[System.Serializable]
	public struct Step {
		public long time;
		public float sideAngle, forwardAngle;
		public Step(long time, float sideAngle, float forwardAngle){
			this.time=time;this.sideAngle=sideAngle;this.forwardAngle=forwardAngle;
		}
	}
	public List<Step> steps = new List<Step>();
	bool onRightSide = true;
	float currentAngle = 0;


}
