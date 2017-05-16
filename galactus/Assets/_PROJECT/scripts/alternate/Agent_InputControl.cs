using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Agent_InputControl : MonoBehaviour {

	public Agent_MOB controlled;
	public float mouseSensitivityX = 4, mouseSensitivityY = -4;
	public float cameraDistance = 3;
	public bool stopWithoutInput = true;
	private bool useBrakes = false;

	/// <summary>movement decision making (user input)</summary>
	private float inputFore = 1, inputSide;
	/// <summary>what to set a held eat-sphere transparency to, to reduce obstruction of visibility</summary>
	private const float userEatSphereTransparencyDuringHold = 0.125f;

	Vector3 CalculateUserAcceleration () {
		Vector3 directionToMoveToward = Vector3.zero;
		if (inputFore != 0 || inputSide != 0) {
			if (inputFore != 0) {
				directionToMoveToward = inputFore * transform.forward;
			}
			if (inputSide != 0) {
				directionToMoveToward = inputSide * transform.right;
				if (inputFore != 0) {
					directionToMoveToward.Normalize ();
				}
			}
		}
		return directionToMoveToward;
	}

	public bool IsControllingAgent() { return controlled != null; }

	public bool IsBraking() { return useBrakes; }

	void Start () {
		Posess (controlled);
	}

	// TODO make some kind of "undoable value change" object, and a manager for it. or better yet, make a "user controlled" effect that adjusts the transparency
	private float oldTransparency;
	public void Posess(Agent_MOB toBeControlled) {
		if (controlled) {
			// re-enable old mob
			Agent_TargetFinder tf = controlled.GetComponent<Agent_TargetFinder> ();
			Agent_SizeAndEffects sizeAndEffects = controlled.GetComponent<Agent_SizeAndEffects> ();
			if (sizeAndEffects) {
				sizeAndEffects.GetEatSphere ().holdTransparency = oldTransparency;
			}
			if (tf) {
				tf.enabled = true;
			} else {
			}
		}
		controlled = toBeControlled;
		if (controlled) {
			// deactivate new mob, so player input can take control.
			Agent_TargetFinder tf = controlled.GetComponent<Agent_TargetFinder> ();
			if (tf) {
				tf.enabled = false;
			}
			controlled.EnsureRigidBody ();
		}
		if (controlled) {
//			EnergyAgent ea = controlled.GetComponent<EnergyAgent> ();
			Agent_Prediction prediction = GetComponent<Agent_Prediction> ();
			if (prediction) {
				List<Agent_MOB> body = new List<Agent_MOB> ();
				body.Add (controlled);
				prediction.SetBodies (body);
				Agent_UI ui = GetComponent<Agent_UI> ();
				if (ui) {
					ui.SetSubject(controlled.gameObject);
				}
				Agent_SizeAndEffects sizeAndEffects = controlled.GetComponent<Agent_SizeAndEffects> ();
				if (sizeAndEffects) {
					this.oldTransparency = sizeAndEffects.GetEatSphere ().holdTransparency;
					sizeAndEffects.GetEatSphere ().holdTransparency = userEatSphereTransparencyDuringHold;
				}
			}
			Agent_SensorLabeler asense = GetComponent<Agent_SensorLabeler> ();
			asense.RefreshSensorOwner (controlled.gameObject);
		}
	}
	
	// Update is called once per frame
	void Update () {
		// control with mouse-look
		transform.Rotate (Input.GetAxis ("Mouse Y") * mouseSensitivityY, Input.GetAxis ("Mouse X") * mouseSensitivityX, 0);
		if (controlled) {
			// control with forward/strafe keys
			inputFore = Input.GetAxis ("Vertical");
			inputSide = Input.GetAxis ("Horizontal");
			if (stopWithoutInput && inputFore == 0 && inputSide == 0) {
				useBrakes = true;
			}
			controlled.UpdateLookDirection (transform.forward, transform.up);
		}
		// scroll wheel to zoom
        var d = Input.GetAxis("Mouse ScrollWheel");
        if (d > 0f) { cameraDistance -= 0.125f; if (cameraDistance < 0) cameraDistance = 0; }
        else if (d < 0f) { cameraDistance += 0.125f; }
	}

	void FixedUpdate() {
		if (controlled) {
			Vector3 directionToMoveToward = Vector3.zero;
			if (useBrakes) {
				directionToMoveToward = controlled.ApplyBrakes ();
				useBrakes = false;
			} else {
				directionToMoveToward = CalculateUserAcceleration ();
			}
			if (directionToMoveToward != Vector3.zero) {
				controlled.ApplyForceToward (directionToMoveToward);
			}
		}
	}

	void LateUpdate() {
		if (controlled) {
			transform.position = controlled.transform.position - transform.forward * cameraDistance * controlled.transform.localScale.z;
		}
	}
}
