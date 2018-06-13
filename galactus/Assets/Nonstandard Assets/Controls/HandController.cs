using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NS;

public class HandController : MonoBehaviour {
	[Tooltip("If null, will search for Camera.mainCamera")]
	public Transform mainCamera;
	CameraControls mainCamControls;
	private CameraControls.ControlSettings fpsSettings, handSettings;

	public Transform[] whereControllerScriptsGo = new Transform[2];
	public Transform[] moveControllerRoots = new Transform[2];
	public VisualizeHandEvents[] handVisuals = new VisualizeHandEvents[2];
	private bool[] grabButtonHeld, triggerButtonHeld;
	// GameObject[] touched;

	/// <summary>The hand rotation relative to camera, to keep hands oriented consistently when the user turns the camera.</summary>
	Quaternion[] handRotationRelativeToCamera;
	/// <summary>The original positions of the user's hands, which are returned to after the user stops moving them. TODO make the auto-return an option</summary>
	TransformData[] originalPositions;

	/// <summary>The index of the currently active move controller.</summary>
	private int currentMoveControllerIndex = 0;

	public KeyCode gripButton = KeyCode.Mouse1;
	public KeyCode triggerButton = KeyCode.Mouse0;
	// public enum ButtonUse { holdButton, toggleButton };
	// public ButtonUse howToGrab = ButtonUse.toggleButton;
	// public ButtonUse howToUse = ButtonUse.holdButton;

	public int GetMoveControllerIndex() { return currentMoveControllerIndex; }
	public Transform CurrentMoveController { get { return moveControllerRoots [currentMoveControllerIndex]; } }

	/// <summary>reference to MoveControls.</summary>
	private MoveControls body;
	float targetDistance = 0;

	public float transitionTime = 0.125f;
	public bool heldCollidersDontCollideWithPlayer = true;

	public enum ControlState { controlHead, transition, controlArm, flyingHand, rotateHand }
	private ControlState state = ControlState.controlHead;
	private int transitionedMoveControllerIndex = -1;
	private ControlState CurrentControlState { get { return state; } set { state = value; RefreshHelpText (); } }

	public KeyCode headControlKey = KeyCode.Escape;
	public KeyCode handControlKey = KeyCode.LeftAlt;
	public KeyCode handFlyKey = KeyCode.Return;
    public KeyCode handRotateKey = KeyCode.LeftControl;
	public KeyCode switchControllerKey = KeyCode.Tab;
	public KeyCode toggleInstructionsKey = KeyCode.F1;

	public static int GetKeyUp(KeyCode[]k){for(int i=0;i<k.Length;++i){if(Input.GetKeyUp(k[i])){return i;}}return -1;}
	public static int GetKeyDown(KeyCode[]k){for(int i=0;i<k.Length;++i){if(Input.GetKeyDown(k[i])){return i;}}return -1;}
	public static int GetKeyUp(KeyCode k){if(Input.GetKeyUp(k)){return 0;}return -1;}
	public static int GetKeyDown(KeyCode k){if(Input.GetKeyDown(k)){return 0;}return -1;}

	static Canvas helpCanvas;
	static Text helpText;

	public static Canvas CreateHelpCanvas() {
		helpCanvas = new GameObject ("Canvas").AddComponent<Canvas> ();
		helpCanvas.renderMode = RenderMode.ScreenSpaceCamera;
		helpCanvas.gameObject.AddComponent<CanvasScaler> ();
		helpCanvas.gameObject.AddComponent<GraphicRaycaster> ();
		RectTransform controlHints = new GameObject ("Control Hints").
			AddComponent<HorizontalLayoutGroup> ().GetComponent<RectTransform> ();
		controlHints.SetParent (helpCanvas.transform);
		controlHints.anchorMax = controlHints.anchorMin = new Vector2 (0, 1);
		controlHints.pivot = new Vector2 (0, 1);
		controlHints.offsetMin = new Vector2(5, -200);
		controlHints.offsetMax = new Vector2(300,-5);

		RectTransform panel = new GameObject ("Panel").
			AddComponent<HorizontalLayoutGroup> ().GetComponent<RectTransform> ();
		panel.SetParent (controlHints);
		Image img = new GameObject ("Background").AddComponent<Image> ();
		RectTransform background = img.GetComponent<RectTransform> ();
		background.gameObject.AddComponent<LayoutElement> ();
		background.SetParent (panel);
		RectTransform hints = new GameObject ("Hints").
			AddComponent<Text> ().GetComponent<RectTransform> ();
		hints.transform.SetParent (background);
		hints.anchorMin = Vector2.zero;
		hints.anchorMax = Vector2.one;
		hints.offsetMax = new Vector2 (-10, -10);
		hints.offsetMin = new Vector2 (10, 10);
		helpText = hints.GetComponent<Text> ();
		if(helpText.font==null){helpText.font=Resources.GetBuiltinResource<Font>("Arial.ttf");}
		helpText.color = new Color (0, 0, 0, .5f);
		img.color = new Color (1, 1, 1, 0.25f);
		return helpCanvas;
	}

	public static string KeysToString(KeyCode k) { return k.ToString (); }
	public static string KeysToString(KeyCode[] k) {
		string s = "";
		for(int i=0;i<k.Length;++i) {
			if (i > 0) s += ", ";
			s += k [i].ToString();
		}
		return s;
	}
	public void RefreshHelpText() {
		string s = "";
		switch(CurrentControlState) {
		case ControlState.controlHead:
			s = "Toggle Control Hints: <b>"+KeysToString(toggleInstructionsKey)+"</b>\n\n" +
				"Move Player/Playspace: <b>W, A, S, D</b>\n" +
				"Head Rotation: <b>Mouse Move</b>\n" +
				"1st/3rd person zoom: <b>Mouse Wheel</b>\n" +
				"Switch hand: <b>"+KeysToString(switchControllerKey)+"</b>\n" +
				"Hand Control Mode:<b>"+KeysToString(handControlKey)+"</b>\n" +
				"Hand Fly Mode:<b>"+KeysToString(handFlyKey)+"</b>\n" +
				"Hand Rotate Mode:<b>"+KeysToString(handRotateKey)+"</b>\n" +
				"> Head Control Mode: <b>"+KeysToString(headControlKey)+"</b>";
			break;
		case ControlState.controlArm:
			s = "Toggle Control Hints: <b>"+KeysToString(toggleInstructionsKey)+"</b>\n\n" +
				"Move Player/Playspace: <b>W, A, S, D</b>\n" +
				"Arm Rotation: <b>Mouse Move</b>\n" +
				"Push/Pull hand: <b>Mouse Wheel</b>\n" +
				"Switch hand: <b>"+KeysToString(switchControllerKey)+"</b>\n" +
				"> Hand Control Mode:<b>"+KeysToString(handControlKey)+"</b>\n" +
				"Hand Fly Mode:<b>"+KeysToString(handFlyKey)+"</b>\n" +
				"Hand Rotate Mode:<b>"+KeysToString(handRotateKey)+"</b>\n" +
				"Head Control Mode: <b>"+KeysToString(headControlKey)+"</b>";
			break;
		case ControlState.flyingHand:
			s = "Toggle Control Hints: <b>"+KeysToString(toggleInstructionsKey)+"</b>\n\n" +
				"Move Hand: <b>W</b>, <b>A</b>, <b>S</b>, <b>D</b>, <b>Q</b>, <b>E</b>\n" +
				"Rotate Hand: <b>Mouse Move</b>\n" +
				"Roll hand: <b>Mouse Wheel</b>\n" +
				"Switch hand: <b>"+KeysToString(switchControllerKey)+"</b>\n" +
				"Hand Control Mode:<b>"+KeysToString(handControlKey)+"</b>\n" +
				"> Hand Fly Mode:<b>"+KeysToString(handFlyKey)+"</b>\n" +
				"Hand Rotate Mode:<b>"+KeysToString(handRotateKey)+"</b>\n" +
				"Head Control Mode: <b>"+KeysToString(headControlKey)+"</b>";
			break;
		case ControlState.rotateHand:
			s = "Toggle Control Hints: <b>"+KeysToString(toggleInstructionsKey)+"</b>\n\n" +
				"Rotate Hand: <b>W, A, S, D, Q, E</b>\n" +
				"Rotate Camera: <b>Mouse Move</b>\n" +
				"Push/Pull hand: <b>Mouse Wheel</b>\n" +
				"Switch hand: <b>"+KeysToString(switchControllerKey)+"</b>\n" +
				"Hand Control Mode:<b>"+KeysToString(handControlKey)+"</b>\n" +
				"Hand Fly Mode:<b>"+KeysToString(handFlyKey)+"</b>\n" +
				"> Hand Rotate Mode:<b>"+KeysToString(handRotateKey)+"</b>\n" +
				"Head Control Mode: <b>"+KeysToString(headControlKey)+"</b>";
			break;
		case ControlState.transition:
			break;
		}
		helpText.text = s;
	}

	// Use this for initialization
	void Start () {
		if (helpCanvas == null) {
			helpCanvas = CreateHelpCanvas ();
			RefreshHelpText ();
		}
		helpCanvas.transform.SetParent (transform);
		if (mainCamera == null) {
			if (Camera.main == null) {
				Camera[] candidates = GameObject.FindObjectsOfType<Camera> ();
				string candidatesString = "";
				for (int i = 0; i < candidates.Length; ++i) {
					if (i > 0) {
						candidatesString += "\n";
					}
					candidatesString += candidates [i].name;
				}
				string errorMessage = "There must be a main camera!";
				if (candidates.Length > 0) {
					errorMessage += "\nCandidate Game Object(s):\n" + candidatesString;
				}
				throw new UnityException (errorMessage);
			}
			mainCamera = Camera.main.transform;
		}
		//			if (mainCamera != null) {
		mainCamControls = mainCamera.gameObject.GetComponent<CameraControls> ();
		mainCamControls.onMove += UpdateHandControls;
		if (mainCamControls == null) {
			throw new UnityException ("HandController requires a CameraControls object on the mainCamera");
		}
		fpsSettings = mainCamControls.settings;
		//			}
		handSettings = new CameraControls.ControlSettings(fpsSettings);
		handSettings.targetToCenterOn = null;
		handSettings.maintainLookDirection = true;
		handSettings.scrollWheelBehavior = CameraControls.ControlSettings.ScrollWheelBehavior.none;

		if (body == null) {
			body = GetComponent<MoveControls> ();
		}
		if (whereControllerScriptsGo [0] == null) {
			throw new System.Exception ("Hey, you need to set values for moveControllers. Thanks bud.");
		}
		int countControllers = whereControllerScriptsGo.Length;
		grabButtonHeld = new bool[countControllers];
		triggerButtonHeld = new bool[countControllers];
		// touched = new GameObject[countControllers];
		handRotationRelativeToCamera = new Quaternion[countControllers];
		originalPositions = new TransformData[countControllers];
		for (int i = 0; i < countControllers; ++i) {
			PopulateController (whereControllerScriptsGo [i].gameObject);
			originalPositions[i] = new TransformData(moveControllerRoots [i].transform, true);
		}
		// make sure the hand visual models are attached to the moving hand transform
		for(int i = 0; i < handVisuals.Length; ++i) {
			if(handVisuals[i] != null) {
				bool isParentedToHand = false;
				Transform t = handVisuals[i].transform;
				int iter = 0;
				while(t != null) {
					if(t == moveControllerRoots[i].transform) {
						isParentedToHand = true;
						break;
					}
					if(iter++ > 1000){ Debug.LogError("deep recursion bad"); break; }
					t = t.parent;
				}
				if(!isParentedToHand) {
					handVisuals[i].transform.SetParent(moveControllerRoots[i]);
				}
			}
		}
	}

	public static T Procure<T>(GameObject g) where T : Component {
		T t = g.GetComponent<T>();
		if (t == null) { t = g.AddComponent<T> (); }
		return t;
	}

	private void PopulateController(GameObject hand) {
		Rigidbody rb = Procure<Rigidbody>(hand);
		rb.useGravity = false; rb.isKinematic = true;
		VRTK.VRTK_InteractTouch toucher = Procure<VRTK.VRTK_InteractTouch>(hand);
		//	sphere collider, or collider that matches fingers+hands
		Collider c = hand.GetComponent<Collider>();
		if (c == null) {
			SphereCollider sc = hand.AddComponent<SphereCollider> ();
			sc.isTrigger = true;
			sc.radius = 0.0375f;
			c = sc;
		}
		VRTK.VRTK_InteractGrab grabber = Procure<VRTK.VRTK_InteractGrab> (hand);
		grabber.controllerAttachPoint = rb;
		grabber.interactTouch = toucher;
		grabber.ControllerStartGrabInteractableObject += (sender, args)=>{
			CollisionSwitch (args.target, true);
		};
		grabber.ControllerUngrabInteractableObject += (sender, args)=>{
			CollisionSwitch (args.target, false);
		};
		VRTK.VRTK_InteractUse user = Procure<VRTK.VRTK_InteractUse> (hand);
		user.interactTouch = toucher;
		user.interactGrab = grabber;
	}

	Vector3 GetCameraPivotPoint() {
		Vector3 p = mainCamera.transform.position;
		if (fpsSettings != null && fpsSettings.targetToCenterOn != null) {
			p = fpsSettings.targetToCenterOn.transform.position;
		}
		return p;
	}

	void EnableNormalPlayerControls(bool enable) {
		if (body != null) {
//			Debug.Log ("ACTIVE?" +enable);
			body.disabledControls = !enable;
		}
	}

	float mainCameraVSMovingCameraHorizonalOffset;

	GameObject line_test, line_leftHandDelta, line_rightHandDelta; // TODO remove
	// Update is called once per frame
	void Update () {
		// for some reason, the VRTK develoeprs decided to hard-code the hand and hand-model.
		// I need to see if that hard-coded hand has moved with the other hand model, and if not, move it along with my model.
		Vector3 ldelta = whereControllerScriptsGo[1].position - moveControllerRoots[1].position, 
		rdelta = whereControllerScriptsGo[0].position - moveControllerRoots[0].position;
		NS.Lines.MakeArrow(ref line_rightHandDelta, moveControllerRoots[0].position, moveControllerRoots[0].position+rdelta, Color.green);
		NS.Lines.MakeArrow(ref line_leftHandDelta, moveControllerRoots[1].position, moveControllerRoots[1].position+ldelta, Color.red);
	}

	// public void LetGoOfEverything() {
	// 	for (int i = 0; i < moveControllers.Length; ++i) {
	// 		Transform t = moveControllers [i];
	// 		if (t != null) {
	// 			GameObject hand = t.gameObject;
	// 			VRTK.VRTK_InteractUse user = hand.GetComponent<VRTK.VRTK_InteractUse> ();
	// 			if (user != null) {
	// 				user.ForceStopUsing ();
	// 			}
	// 			VRTK.VRTK_InteractTouch toucher = hand.GetComponent<VRTK.VRTK_InteractTouch> ();
	// 			if (toucher != null) {
	// 				if (touched != null && touched [i] != null) {
	// 					VRTK.VRTK_InteractableObject iobj = touched [i].GetComponent<VRTK.VRTK_InteractableObject> ();
	// 					iobj.StopTouching (toucher);
	// 				}
	// 				toucher.ForceStopTouching ();
	// 			}
	// 		}
	// 	}
	// }
	// void OnDestroy() {
	// 	LetGoOfEverything ();
	// }

	void LateUpdate() {
		bool headControlRequest = GetKeyUp (headControlKey) >= 0;
		bool handFlyRequest = GetKeyUp(handFlyKey) >= 0;
		bool armControlRequest = GetKeyUp(handControlKey) >= 0;
		bool handRotateRequest = GetKeyUp (handRotateKey) >= 0;
		bool switchControllerRequest = GetKeyUp (switchControllerKey) >= 0;
		if (GetKeyUp (toggleInstructionsKey) >= 0) {
			helpCanvas.enabled = !helpCanvas.enabled;
		}
		if (switchControllerRequest) { NextMoveController (); }
		if (armControlRequest) { TransitionToHand (ControlState.controlArm, true); return; }
		if (headControlRequest) { TransitionToHead (); return; }
		if (handRotateRequest) { TransitionToHand (ControlState.rotateHand, false); return; }
		if (handFlyRequest) { TransitionToHand (ControlState.flyingHand, false); return; }
	}

	void NextMoveController() {
		if(CurrentControlState != ControlState.transition) {
			ReturnControllerToOriginalPosition (currentMoveControllerIndex);
			currentMoveControllerIndex++;
			currentMoveControllerIndex %= whereControllerScriptsGo.Length;
			switch (CurrentControlState) {
			case ControlState.controlArm: TransitionToHand (ControlState.controlArm, true);	break;
			case ControlState.flyingHand: TransitionToHand (ControlState.flyingHand, false); break;
			case ControlState.rotateHand: TransitionToHand (ControlState.rotateHand, false);	break;
			}
		}
	}

	void UpdateArmControl() {
		Vector3 movHorizonDir = mainCamControls.GetHorizonalDirection(-body.gravityDirection);
		mainCamControls.transform.position = GetCameraPivotPoint();
		Quaternion q = Quaternion.AngleAxis (-mainCameraVSMovingCameraHorizonalOffset, -body.gravityDirection);
		Vector3 constantForward = q * movHorizonDir;
		Lines.MakeArrow (ref line_test, body.transform.position, body.transform.position + constantForward, Color.black, .1f, .1f);
		Transform t = mainCamControls.transform;
		t.position = GetCameraPivotPoint ();
		float wheel = Input.GetAxis ("Mouse ScrollWheel");
		targetDistance += wheel;
		Transform hand = CurrentMoveController;
		hand.position = GetCameraPivotPoint () + t.forward * targetDistance;
		hand.rotation = t.rotation * handRotationRelativeToCamera[currentMoveControllerIndex];
	}

	GameObject line_mouseXp, line_mouseXn, line_mouseYn, line_mouseYp;

	void TutorialArrows() {
//		LineRenderer[] lr = new LineRenderer[] { 
//			// up
//			NS.Lines.MakeArcArrow (ref line_mouseYp, -15, 8, Vector3.right, 
//				Quaternion.AngleAxis (-10, Vector3.right) * Vector3.forward, 
//				Vector3.zero, Color.green, 0.0125f, 0.0125f),
//			// down
//			NS.Lines.MakeArcArrow (ref line_mouseYn, 15, 8, Vector3.right, 
//				Quaternion.AngleAxis (10, Vector3.right) * Vector3.forward, 
//				Vector3.zero, Color.green, 0.0125f, 0.0125f),
//			// right
//			NS.Lines.MakeArcArrow (ref line_mouseXp, 15, 8, Vector3.up, 
//				Quaternion.AngleAxis (10, Vector3.up) * Vector3.forward, 
//				Vector3.zero, Color.red, 0.0125f, 0.0125f),
//			// left
//			NS.Lines.MakeArcArrow (ref line_mouseXn, -15, 8, Vector3.up, 
//				Quaternion.AngleAxis (-10, Vector3.up) * Vector3.forward, 
//				Vector3.zero, Color.red, 0.0125f, 0.0125f),
//		};
//		for (int i = 0; i < lr.Length; ++i) {
//			lr[i].useWorldSpace = false;
//			lr[i].transform.SetParent (mainCamera);
//			lr[i].transform.localPosition = Vector3.zero;
//			lr[i].transform.localRotation = Quaternion.identity;
//		}
	}

	void UpdateHandControls() {
		Transform hand = CurrentMoveController;
		// TODO proper state machines? or move this to the onMove for the mainCamControls?
		switch (CurrentControlState) {
		case ControlState.controlHead: break;
		case ControlState.transition: break;
		case ControlState.controlArm: {
				EnableNormalPlayerControls (true);
				UpdateArmControl ();
			} break;
		case ControlState.rotateHand: {
				Transform t = mainCamera.transform;
				float v = Input.GetAxis ("Vertical");
				float h = Input.GetAxis ("Horizontal");
				float wheel = Input.GetAxis ("Mouse ScrollWheel");
				float dep = Input.GetKey (KeyCode.Q) ? 1 : Input.GetKey (KeyCode.E) ? -1 : 0;
				dep *= 0.5f;
				EnableNormalPlayerControls(false);
				hand.Rotate (Quaternion.Inverse(hand.rotation) * t.forward, dep * mainCamControls.settings.mouseSensitivity);
				hand.Rotate (Quaternion.Inverse(hand.rotation) * t.right,   v * mainCamControls.settings.mouseSensitivity);
				hand.Rotate (Quaternion.Inverse(hand.rotation) * t.up,      h * mainCamControls.settings.mouseSensitivity);
				targetDistance += wheel * Time.deltaTime;
				hand.position = GetCameraPivotPoint () + t.forward * targetDistance;
			} break;
		}
	}

	void FixedUpdate() { UpdateGrabUse (); }

	public static GameObject GetInteractableAt(Vector3 p, float r) {
		Collider[] c = Physics.OverlapSphere (p, r);
		for (int i = 0; i < c.Length; ++i) {
			if (c [i].gameObject.GetComponent<VRTK.VRTK_InteractableObject> () != null) {
				return c[i].gameObject;
			}
		}
		return null;
	}

	Dictionary<GameObject, int> oldLayer = new Dictionary<GameObject, int>();
	void CollisionSwitch(GameObject obj, bool isHeldNow) {
		if(obj == null) { return; }
		if (isHeldNow) {
			if (obj.layer != MoveControls.playerControlledLayer) {
				oldLayer [obj] = obj.layer;
				obj.layer = MoveControls.playerControlledLayer;
			}
		} else {
			if (oldLayer.ContainsKey (obj)) {
				obj.layer = oldLayer [obj];
				oldLayer.Remove (obj);
			}
		}
		for (int i = 0; i < obj.transform.childCount; ++i) {
			CollisionSwitch (obj.transform.GetChild (i).gameObject, isHeldNow);
		}
	}

	void UpdateGrabUse() {
		GameObject hand = CurrentMoveController.gameObject;
		bool wasGrab = grabButtonHeld[currentMoveControllerIndex], wasTrigger = triggerButtonHeld[currentMoveControllerIndex];
		// switch (howToGrab) {
		// case ButtonUse.holdButton:
		grabButtonHeld[currentMoveControllerIndex] = Input.GetKey (gripButton);
		// break; case ButtonUse.toggleButton: if (Input.GetKeyDown (gripButton)) { grabButtonHeld[currentMoveControllerIndex] = !grabButtonHeld[currentMoveControllerIndex]; }
		// break;}
		// switch (howToUse) {
		// case ButtonUse.holdButton: 
		triggerButtonHeld[currentMoveControllerIndex] = Input.GetKey (triggerButton);
		// break; case ButtonUse.toggleButton: if (Input.GetKeyDown (triggerButton))  { triggerButtonHeld[currentMoveControllerIndex] =  !triggerButtonHeld[currentMoveControllerIndex];  } break;}
		if (wasGrab != grabButtonHeld[currentMoveControllerIndex] || wasTrigger != triggerButtonHeld[currentMoveControllerIndex]) {
			VisualizeHandEvents v = handVisuals[currentMoveControllerIndex];//hand.GetComponent<VisualizeHandEvents> ();
            if(v == null) {
                v = hand.GetComponentInChildren<VisualizeHandEvents>();
				handVisuals[currentMoveControllerIndex] = v;
            }
			if (v != null) {
				if (wasGrab != grabButtonHeld[currentMoveControllerIndex]) { if (grabButtonHeld[currentMoveControllerIndex]) { v.DoGrip (); } else { v.UndoGrip (); } }
				if (wasTrigger != triggerButtonHeld[currentMoveControllerIndex]) { if (triggerButtonHeld[currentMoveControllerIndex]) { v.DoTrigger (); } else { v.UndoTrigger (); } }
			}
		}
	}

	void TransitionToHand(ControlState goalState, bool enableNormalPlayerControls) {
		if(CurrentControlState == goalState && transitionedMoveControllerIndex == currentMoveControllerIndex) return;
		CurrentControlState = ControlState.transition;
		Vector3 upUsedByPlayer = (body != null)?-body.gravityDirection:Vector3.up;

		handSettings.currentStandDirection = upUsedByPlayer; // TODO make this Update, in case the character is moving
		handSettings.maintainStandDirection = true;

		Transform hand = CurrentMoveController;
		Vector3 targetCamPosition = GetCameraPivotPoint ();
		Vector3 targetDelta = hand.position - targetCamPosition;
		targetDistance = targetDelta.magnitude;
		Quaternion targetCamRotation = mainCamera.rotation;

		Vector3 handP = GetCameraPivotPoint () + mainCamera.forward * targetDistance;
		Quaternion handR = hand.rotation;
		TransformData.Lerp (hand, handP, handR, hand.lossyScale, transitionTime, ()=>{});

		mainCamControls.enabled = false; // turn off the camera controls during the animation
		mainCamControls.settings = handSettings; // when the camera controls wake up, use hand control settings
		EnableNormalPlayerControls(false); // turn off player controls during the animation

		TransformData.Lerp (mainCamera, targetCamPosition, targetCamRotation, Vector3.one, transitionTime, () => {
			// remember what state we are in
			CurrentControlState = goalState;
			transitionedMoveControllerIndex = currentMoveControllerIndex;
			// once we get there, find out how much the hand is horizontally offset from the body
			Vector3 movHorizonDir = mainCamControls.GetHorizonalDirection(upUsedByPlayer);
			Vector3 bodyHorizonDir = body.GetDirectionOnHorizon();
			mainCameraVSMovingCameraHorizonalOffset = Vector3.Angle(movHorizonDir, bodyHorizonDir);
			if(Vector3.Dot(movHorizonDir, Vector3.Cross(upUsedByPlayer, bodyHorizonDir)) < 0) {
				mainCameraVSMovingCameraHorizonalOffset *= -1;
			}
//				bodyRoot.transform.rotation = Quaternion.Euler(0, -mainCameraVSMovingCameraHorizonalOffset, 0);
			// setup the movingCamera so it can be used
			mainCamControls.AcceptCurrentTransform ();
			mainCamControls.enabled = true;
			// remember the delta between the hand rotation and the camera's forward rotation
			Transform handRotationOffsetTransform = MoveControllerTransform ();
			handRotationOffsetTransform.rotation = targetCamRotation;
			// prevAngleOfMouse = float.PositiveInfinity;
			handRotationRelativeToCamera[currentMoveControllerIndex] = Quaternion.Inverse(targetCamRotation) * hand.rotation;
			if(enableNormalPlayerControls){
				EnableNormalPlayerControls(true);
			}
			mainCamControls.ForceCurrentStandDirection(upUsedByPlayer);

			TutorialArrows();
		});
	}

	void ReturnControllerToOriginalPosition(int controllerID){
		TransformData.LerpLocal (moveControllerRoots [controllerID], originalPositions [controllerID], transitionTime, null);
	}
	void ReturnControllersToOriginalPosition(){
		// move hands back to their neutral position
		for (int i = 0; i < moveControllerRoots.Length; ++i) {
			ReturnControllerToOriginalPosition (i);
		}
	}
	void TransitionToHead() {
		if(CurrentControlState == ControlState.controlHead) return;
		CurrentControlState = ControlState.transition;
		ReturnControllersToOriginalPosition ();
		Vector3 upUsedByPlayer = (body != null)?-body.gravityDirection:Vector3.up;
		mainCamControls.enabled = false; // turn off the camera controls during the animation
		mainCamControls.settings = fpsSettings; // when the camera controls wake up, use standard FPS settings

		Vector3 p = mainCamControls.GetZoomOutOffset () + GetCameraPivotPoint ();
		TransformData.Lerp (mainCamera, p, mainCamera.rotation, mainCamera.localScale, transitionTime, () => {
			CurrentControlState = ControlState.controlHead;
			mainCamControls.enabled = true;
			mainCamControls.ForceCurrentStandDirection(upUsedByPlayer);
		});
		EnableNormalPlayerControls (true);
	}

	// TODO rename... this is the transform that is used to keep the hand's 'forward' controlled by the user (shift key)
	public Transform MoveControllerTransform() {
		Transform adjustHandControl = GetControllerObserver();
		if (adjustHandControl == null) {
			Transform focus = CurrentMoveController;
			adjustHandControl = (new GameObject (moveControllerName)).transform;
			adjustHandControl.position = focus.position;
			adjustHandControl.SetParent (focus);
			adjustHandControl.rotation = mainCamera.transform.rotation;
		}
		return adjustHandControl;
	}
	public Transform GetControllerObserver() {
		Transform t = CurrentMoveController.Find (moveControllerName);
		return t;
	}
	// private float prevAngleOfMouse = float.PositiveInfinity;
	private static string moveControllerName = "<camera offset>";
}
