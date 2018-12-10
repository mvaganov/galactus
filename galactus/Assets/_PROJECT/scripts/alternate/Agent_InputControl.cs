using UnityEngine;
using UnityEngine.VR;
using System.Collections.Generic;

public class Agent_InputControl : MovingEntity_CameraInput {

    private VRControls vr;
	private Agent_SizeAndEffects sizeAndEffects;

	/// <summary>what to set a held eat-sphere transparency to, to reduce obstruction of visibility</summary>
	private const float userEatSphereTransparencyDuringHold = 0.125f;

	public override void Initialize() {
		vr = Object.FindObjectOfType<VRControls>();
		if(vr) {
			if(!vr.enabled || !vr.gameObject.activeInHierarchy) {
				vr = null;
			} else {
				vr.TakeUserInterfaceControl(gameObject);
			}
		}
		base.Initialize ();
	}
	void Start () {
		Initialize ();
	}

//	float expectedRatio;
	public override void Update() {
		base.Update ();
	}

	private float lastKnownRadius = 0;
	public void OnRadiusChange(float oldRadius, float newRadius) {
		// rad can be set twice at the same time because of overlaps in game rules. prevent this from triggering twice.
		if (lastKnownRadius != newRadius || lastKnownRadius == 0) {
//			float ratio = inputController.cameraDistance / oldRadius;
//			float newDist = ratio * newRadius;
//			inputController.cameraDistance = newDist;
//			inputController.cameraDistance = inputController.cameraDistance * newRadius / oldRadius;
			lastKnownRadius = newRadius;
		}
	}

	// TODO make some kind of "undoable value change" object, and a manager for it. or better yet, make a "user controlled" effect that adjusts the transparency
	private float oldTransparency;
	public override void Control(MOB me) {
		if (controlling) {
			// re-enable old mob
			Agent_TargetFinder tf = controlling.GetComponent<Agent_TargetFinder> ();
			if (sizeAndEffects) {
				sizeAndEffects.GetEatSphere ().holdTransparency = oldTransparency;
				sizeAndEffects.onRadiusChange -= OnRadiusChange;
			}
			if (tf) {
				tf.enabled = true;
			}
		}
		base.Control (me);
		if (controlling) {
			sizeAndEffects = controlling.GetComponent<Agent_SizeAndEffects> ();
			// deactivate new mob, so player input can take control.
			Agent_TargetFinder tf = controlling.GetComponent<Agent_TargetFinder> ();
			if (tf) {
				tf.enabled = false;
			}
			//controlling.EnsureRigidBody ();
			Agent_Prediction prediction = GetComponent<Agent_Prediction> ();
			if (prediction) {
				List<Agent_MOB> body = new List<Agent_MOB> ();
				body.Add (controlling as Agent_MOB);
				prediction.SetBodies (body);
				Agent_UI ui = GetComponent<Agent_UI> ();
				if (ui) {
					ui.SetSubject(controlling.gameObject);
				}
				if (sizeAndEffects) {
					this.oldTransparency = sizeAndEffects.GetEatSphere ().holdTransparency;
					sizeAndEffects.GetEatSphere ().holdTransparency = userEatSphereTransparencyDuringHold;
					sizeAndEffects.onRadiusChange += OnRadiusChange;
				}
			}
			Agent_SensorLabeler asense = GetComponent<Agent_SensorLabeler> ();
			asense.RefreshSensorOwner (controlling.gameObject);
		}
	}

	public bool IsBraking() { return controlling != null && controlling.IsBraking (); }
}
