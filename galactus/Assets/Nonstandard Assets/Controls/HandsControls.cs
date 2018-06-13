using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NS {
	public class HandsControls : MonoBehaviour {
		[Tooltip("If null, will search for Camera.mainCamera")]
		public Transform mainCamera;
		CameraControls mainCamControls;
		public static CameraControls movingCamera;
		private CameraControls.ControlSettings fpsSettings, handSettings;
		public Transform[] moveControllers = new Transform[2];//handRight, handLeft;
		private bool[] grabButtonHeld, useButtonHeld;
		GameObject[] touched;
		Quaternion[] handRotationRelativeToCamera;
		TransformData[] originalPositions;

		private int currentMoveControllerIndex = 0;

		public enum MouseButton { leftClick = 0, rightClick = 1, middleClick = 2, noClick = -1 }
		public MouseButton grabButton = MouseButton.rightClick;
		public MouseButton useButton = MouseButton.leftClick;
		public enum ButtonUse { holdButton, toggleButton };
		public ButtonUse howToGrab = ButtonUse.toggleButton;
		public ButtonUse howToUse = ButtonUse.holdButton;

		public Transform CurrentMoveController { get { return moveControllers [currentMoveControllerIndex]; } }

		/// <summary>reference to MoveControls.</summary>
		private MoveControls body;
		float targetDistance = 0;


		public float transitionTime = 0.125f;

		public enum ControlState { controlHead, transition, controlArm, flyingHand, rotateHand }
		private ControlState state = ControlState.controlHead;
		private int transitionedMoveControllerIndex = -1;
		private ControlState CurrentControlState { get { return state; } set { state = value; RefreshHelpText (); } }

		public KeyCode[] headControlKeys = new KeyCode[]{KeyCode.Escape};
		public KeyCode[] armControlKeys = new KeyCode[]{KeyCode.LeftAlt,KeyCode.RightAlt};
		public KeyCode[] handFlyKeys = new KeyCode[]{KeyCode.LeftControl,KeyCode.RightControl};
		public KeyCode[] handRotateKeys = new KeyCode[]{KeyCode.LeftShift,KeyCode.RightShift};
		public KeyCode[] switchControllerKeys = new KeyCode[]{KeyCode.Tab};
		public KeyCode[] toggleInstructionsKeys = new KeyCode[]{KeyCode.F1};

		public static int GetKeyUp(KeyCode[]k){for(int i=0;i<k.Length;++i){if(Input.GetKeyUp(k[i])){return i;}}return -1;}
		public static int GetKeyDown(KeyCode[]k){for(int i=0;i<k.Length;++i){if(Input.GetKeyDown(k[i])){return i;}}return -1;}

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

		public static string KeysToString(KeyCode[] k){
			string s = "";
			for(int i=0;i<k.Length;++i){
				if (i > 0) s += ", ";
				s += k [i].ToString();
			}
			return s;
		}
		public void RefreshHelpText() {
			string s = "";
			switch(CurrentControlState) {
			case ControlState.controlHead:
				s = "Toggle Control Hints: <b>"+KeysToString(toggleInstructionsKeys)+"</b>\n\n" +
					"Move Player/Playspace: <b>W, A, S, D</b>\n" +
					"Head Rotation: <b>Mouse Move</b>\n" +
					"1st/3rd person zoom: <b>Mouse Wheel</b>\n" +
					"Switch hand: <b>"+KeysToString(switchControllerKeys)+"</b>\n" +
					"Hand Control Mode:<b>"+KeysToString(armControlKeys)+"</b>\n" +
					"Hand Fly Mode:<b>"+KeysToString(handFlyKeys)+"</b>\n" +
					"Hand Rotate Mode:<b>"+KeysToString(handRotateKeys)+"</b>\n" +
					"> Head Control Mode: <b>"+KeysToString(headControlKeys)+"</b>";
				break;
			case ControlState.controlArm:
				s = "Toggle Control Hints: <b>"+KeysToString(toggleInstructionsKeys)+"</b>\n\n" +
					"Move Player/Playspace: <b>W, A, S, D</b>\n" +
					"Arm Rotation: <b>Mouse Move</b>\n" +
					"Push/Pull hand: <b>Mouse Wheel</b>\n" +
					"Switch hand: <b>"+KeysToString(switchControllerKeys)+"</b>\n" +
					"> Hand Control Mode:<b>"+KeysToString(armControlKeys)+"</b>\n" +
					"Hand Fly Mode:<b>"+KeysToString(handFlyKeys)+"</b>\n" +
					"Hand Rotate Mode:<b>"+KeysToString(handRotateKeys)+"</b>\n" +
					"Head Control Mode: <b>"+KeysToString(headControlKeys)+"</b>";
				break;
			case ControlState.flyingHand:
				s = "Toggle Control Hints: <b>"+KeysToString(toggleInstructionsKeys)+"</b>\n\n" +
					"Move Hand: <b>W</b>, <b>A</b>, <b>S</b>, <b>D</b>, <b>Q</b>, <b>E</b>\n" +
					"Rotate Hand: <b>Mouse Move</b>\n" +
					"Roll hand: <b>Mouse Wheel</b>\n" +
					"Switch hand: <b>"+KeysToString(switchControllerKeys)+"</b>\n" +
					"Hand Control Mode:<b>"+KeysToString(armControlKeys)+"</b>\n" +
					"> Hand Fly Mode:<b>"+KeysToString(handFlyKeys)+"</b>\n" +
					"Hand Rotate Mode:<b>"+KeysToString(handRotateKeys)+"</b>\n" +
					"Head Control Mode: <b>"+KeysToString(headControlKeys)+"</b>";
				break;
			case ControlState.rotateHand:
				s = "Toggle Control Hints: <b>"+KeysToString(toggleInstructionsKeys)+"</b>\n\n" +
					"Rotate Hand: <b>W, A, S, D, Q, E</b>\n" +
					"Rotate Camera: <b>Mouse Move</b>\n" +
					"Push/Pull hand: <b>Mouse Wheel</b>\n" +
					"Switch hand: <b>"+KeysToString(switchControllerKeys)+"</b>\n" +
					"Hand Control Mode:<b>"+KeysToString(armControlKeys)+"</b>\n" +
					"Hand Fly Mode:<b>"+KeysToString(handFlyKeys)+"</b>\n" +
					"> Hand Rotate Mode:<b>"+KeysToString(handRotateKeys)+"</b>\n" +
					"Head Control Mode: <b>"+KeysToString(headControlKeys)+"</b>";
				break;
			case ControlState.transition:
				break;
			}
			helpText.text = s;
		}

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
				fpsSettings = mainCamControls.settings;
//			}
			handSettings = new CameraControls.ControlSettings(fpsSettings);
			handSettings.targetToCenterOn = null;
			handSettings.maintainLookDirection = true;
			handSettings.scrollWheelBehavior = CameraControls.ControlSettings.ScrollWheelBehavior.none;

			if (movingCamera == null) {
				GameObject go = new GameObject ("<moving camera>");
				go.AddComponent<Camera> ();
				go.AddComponent<FlareLayer> ();
				go.AddComponent<AudioListener> ();
				movingCamera = go.AddComponent<CameraControls> ();
				Vector3 head = GetCameraPivotPoint ();
				float avgDist = 0;
				for (int i = 0; i < moveControllers.Length; ++i) {
					avgDist += Vector3.Distance (head, moveControllers [i].position);
				}
				avgDist /= moveControllers.Length;
				movingCamera.settings.distanceFromTarget = avgDist;
				go.SetActive (false);
				if (body != null) {
					movingCamera.transform.SetParent (body.transform);
				}
			}
			if (body == null) {
				body = GetComponent<MoveControls> ();
			}
			grabButtonHeld = new bool[moveControllers.Length];
			useButtonHeld = new bool[moveControllers.Length];
			touched = new GameObject[moveControllers.Length];
			handRotationRelativeToCamera = new Quaternion[moveControllers.Length];
			originalPositions = new TransformData[moveControllers.Length];
			for (int i = 0; i < moveControllers.Length; ++i) {
				PopulateController (moveControllers [i].gameObject);
				originalPositions[i] = new TransformData(moveControllers [i].transform, true);
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
			VRTK.VRTK_InteractUse user = Procure<VRTK.VRTK_InteractUse> (hand);
			user.interactTouch = toucher;
			user.interactGrab = grabber;
		}

		Vector3 GetCameraPivotPoint() {
			Vector3 p = mainCamera.transform.position;
			if (mainCamControls != null) {
				p = mainCamControls.settings.targetToCenterOn.transform.position;
			}
			return p;
		}

		void EnableNormalPlayerControls(bool enable) {
			if (body != null) {
				body.disabledControls = !enable;
			}
		}

//		float oldHeadDistance = -1;
//		CameraControls.ScrollWheelBehavior oldScrollBehavior = CameraControls.ScrollWheelBehavior.invalid;
		float mainCameraVSMovingCameraHorizonalOffset;

		GameObject line_test; // TODO remove

		// TODO instead of using a movingCamera, swap the mainCamControls.settings with handControls
		void TransitionToHand(ControlState goalState, bool enableNormalPlayerControls) {
			if(CurrentControlState == goalState && transitionedMoveControllerIndex == currentMoveControllerIndex) return;
			CurrentControlState = ControlState.transition;
			Vector3 upUsedByPlayer = (body != null)?-body.gravityDirection:Vector3.up;

			handSettings.currentStandDirection = upUsedByPlayer;
			handSettings.maintainStandDirection = true;

			CameraControls camToMove = movingCamera;
			// TODO find out what is causing the currentUpDirection of the movingCamera to not be set correctly until it is shifted...
			camToMove.settings.maintainStandDirection = true;
//			camToMove.maintainLookDirection = true;
//			camToMove.SetStandDirectionTarget(upUsedByPlayer);
			camToMove.AcceptCurrentTransform ();

			Transform hand = CurrentMoveController;
			if (camToMove != mainCamControls) {
				SwitchToMovingCameraJustBeforeTransition (true);
			}
			Vector3 targetCamPosition = GetCameraPivotPoint ();
			Vector3 targetDelta = hand.position - targetCamPosition;
			targetDistance = targetDelta.magnitude;
			Vector3 targetDir = targetDelta / targetDistance;

			Vector3 targetRight = Vector3.Cross (upUsedByPlayer, targetDir);
			Vector3 targetCameraUp = Vector3.Cross (targetDir, targetRight);
			Quaternion targetCamRotation = Quaternion.LookRotation(targetDir, targetCameraUp);
			camToMove.settings.targetToCenterOn = null;

//	if (mainCamControls.settings.targetToCenterOn != null) {
//		originalCameraCenter = mainCamControls.settings.targetToCenterOn;
//	}
//	mainCamControls.settings.targetToCenterOn = null;
			TransformData.Lerp (camToMove.transform, targetCamPosition, targetCamRotation, Vector3.one, transitionTime, () => {
//	TransformData.Lerp (mainCamControls.transform, targetCamPosition, targetCamRotation, Vector3.one, transitionTime, () => {
				CurrentControlState = goalState;
				transitionedMoveControllerIndex = currentMoveControllerIndex;
				// once we get there, find out how much the hand is offset from the body
//				Debug.Log("??????");
				Vector3 movHorizonDir = camToMove.GetHorizonalDirection(upUsedByPlayer);
				Vector3 bodyHorizonDir = body.GetDirectionOnHorizon();
				mainCameraVSMovingCameraHorizonalOffset = Vector3.Angle(movHorizonDir, bodyHorizonDir);
				if(Vector3.Dot(movHorizonDir, Vector3.Cross(upUsedByPlayer, bodyHorizonDir)) < 0) {
					mainCameraVSMovingCameraHorizonalOffset *= -1;
				}
//				bodyRoot.transform.rotation = Quaternion.Euler(0, -mainCameraVSMovingCameraHorizonalOffset, 0);
				// setup the movingCamera so it can be used
				camToMove.settings.maintainStandDirection = true;
				camToMove.settings.maintainLookDirection = true;
				camToMove.settings.scrollWheelBehavior = CameraControls.ControlSettings.ScrollWheelBehavior.none;
				camToMove.SetStandDirectionTarget(upUsedByPlayer);
				camToMove.AcceptCurrentTransform ();
				camToMove.enabled = true;
				// remember the delta between the hand rotation and the camera's forward rotation
				Transform handRotationOffsetTransform = MoveControllerTransform ();
				handRotationOffsetTransform.rotation = targetCamRotation;
				prevAngleOfMouse = float.PositiveInfinity;
				handRotationRelativeToCamera[currentMoveControllerIndex] = Quaternion.Inverse(targetCamRotation) * hand.rotation;
				if(enableNormalPlayerControls){
					EnableNormalPlayerControls(true);
				}
			});
		}

		void SwitchToMovingCameraJustBeforeTransition(bool controller){
			mainCamera.gameObject.SetActive (!controller);
			if (mainCamera.gameObject.activeInHierarchy) {
				CameraControls.DupTransform (movingCamera.transform, mainCamera);
			}
			movingCamera.gameObject.SetActive (controller);
			movingCamera.enabled = false;
			EnableNormalPlayerControls (false);
		}

		void TransitionToHead() {
			if(CurrentControlState == ControlState.controlHead) return;
			for (int i = 0; i < moveControllers.Length; ++i) {
				TransformData.LerpLocal (moveControllers [i], originalPositions [i], transitionTime, null);
			}
			CurrentControlState = ControlState.transition;
			CameraControls camToMove = movingCamera;
			camToMove.enabled = false;
			camToMove.settings.targetToCenterOn = null;
			if (mainCamControls != null) {
				mainCamera.transform.position += mainCamControls.GetZoomOutOffset ();
			}
			TransformData.Lerp (camToMove.transform, 
				mainCamera.position, mainCamera.rotation, mainCamera.localScale, transitionTime, () => {
				CurrentControlState = ControlState.controlHead;
				camToMove.enabled = false;
				camToMove.gameObject.SetActive (false);
				mainCamera.gameObject.SetActive (true);
			});
			EnableNormalPlayerControls (true);
		}

		void NextMoveController() {
			if(CurrentControlState != ControlState.transition) {
				currentMoveControllerIndex++;
				currentMoveControllerIndex %= moveControllers.Length;
				switch (CurrentControlState) {
				case ControlState.controlArm: TransitionToHand (ControlState.controlArm, true);	break;
				case ControlState.flyingHand: TransitionToHand (ControlState.flyingHand, false); break;
				case ControlState.rotateHand: TransitionToHand (ControlState.rotateHand, false);	break;
				}
			}
		}

		public static GameObject GetInteractableAt(Vector3 p, float r) {
			Collider[] c = Physics.OverlapSphere (p, r);
			for (int i = 0; i < c.Length; ++i) {
				if (c [i].gameObject.GetComponent<VRTK.VRTK_InteractableObject> () != null) {
					return c[i].gameObject;
				}
			}
			return null;
		}
		void Update() {
			GameObject hand = CurrentMoveController.gameObject;
			bool wasGrab = grabButtonHeld[currentMoveControllerIndex], wasTrigger = useButtonHeld[currentMoveControllerIndex];
			switch (howToGrab) {
			case ButtonUse.holdButton: grabButtonHeld[currentMoveControllerIndex] = Input.GetMouseButton ((int)grabButton); break;
			case ButtonUse.toggleButton: if (Input.GetMouseButtonDown ((int)grabButton)) { grabButtonHeld[currentMoveControllerIndex] = !grabButtonHeld[currentMoveControllerIndex]; } break;
			}
			switch (howToUse) {
			case ButtonUse.holdButton: useButtonHeld[currentMoveControllerIndex] = Input.GetMouseButton ((int)useButton); break;
			case ButtonUse.toggleButton: if (Input.GetMouseButtonDown ((int)useButton))  { useButtonHeld[currentMoveControllerIndex] =  !useButtonHeld[currentMoveControllerIndex];  } break;
			}
			if (wasGrab != grabButtonHeld[currentMoveControllerIndex] || wasTrigger != useButtonHeld[currentMoveControllerIndex]) {
				VisualizeHandEvents v = hand.GetComponent<VisualizeHandEvents> ();
				if (v != null) {
					if (wasGrab != grabButtonHeld[currentMoveControllerIndex]) { if (grabButtonHeld[currentMoveControllerIndex]) { v.DoGrip (); } else { v.UndoGrip (); } }
					if (wasTrigger != useButtonHeld[currentMoveControllerIndex]) { if (useButtonHeld[currentMoveControllerIndex]) { v.DoTrigger (); } else { v.UndoTrigger (); } }
				}
			}
			VRTK.VRTK_InteractGrab grabber = hand.GetComponent<VRTK.VRTK_InteractGrab> ();
			if (grabButtonHeld[currentMoveControllerIndex]) {
				if (grabber.GetGrabbedObject () == null) {
					GameObject grabbed = GetInteractableAt(hand.transform.position, .125f);
					if (grabbed != null) {
						touched[currentMoveControllerIndex] = grabbed;
						hand.GetComponent<VRTK.VRTK_InteractTouch> ().ForceTouch (grabbed);
						grabber.AttemptGrab ();
					}
				}
			} else {
				if (grabber.GetGrabbedObject () != null) {
					grabber.ForceRelease (true);
				} else {
					GameObject touchedJustNow = GetInteractableAt(hand.transform.position, .125f);
					VRTK.VRTK_InteractTouch toucher = hand.GetComponent<VRTK.VRTK_InteractTouch> ();
					if (touchedJustNow != touched[currentMoveControllerIndex]) {
						toucher.ForceStopTouching();
						if (touched[currentMoveControllerIndex] != null) {
							VRTK.VRTK_InteractableObject iobj = touched[currentMoveControllerIndex].GetComponent<VRTK.VRTK_InteractableObject> ();
							iobj.StopTouching (toucher);
						}
					}
					touched[currentMoveControllerIndex] = touchedJustNow;
					if (touchedJustNow != null) {
						toucher.ForceTouch (touchedJustNow);
					}
				}
			}
			VRTK.VRTK_InteractUse user = hand.GetComponent<VRTK.VRTK_InteractUse> ();
			if (useButtonHeld [currentMoveControllerIndex]) {
				user.AttemptUse ();
			} else {
				user.ForceStopUsing ();
			}
		}
		public void LetGoOfEverything() {
			for (int i = 0; i < moveControllers.Length; ++i) {
				Transform t = moveControllers [i];
				if (t != null) {
					GameObject hand = t.gameObject;
					VRTK.VRTK_InteractUse user = hand.GetComponent<VRTK.VRTK_InteractUse> ();
					if (user != null) {
						user.ForceStopUsing ();
					}
					VRTK.VRTK_InteractTouch toucher = hand.GetComponent<VRTK.VRTK_InteractTouch> ();
					if (toucher != null) {
						if (touched != null && touched [i] != null) {
							VRTK.VRTK_InteractableObject iobj = touched [i].GetComponent<VRTK.VRTK_InteractableObject> ();
							iobj.StopTouching (toucher);
						}
						toucher.ForceStopTouching ();
					}
				}
			}
		}
		void OnDestroy() {
			// TODO make sure everything is released before the level is unloaded
			LetGoOfEverything ();
		}

		public Transform bodyRoot;
		void LateUpdate () {
			bool headControlRequest = GetKeyUp (headControlKeys) >= 0;
			bool handFlyRequest = GetKeyUp(handFlyKeys) >= 0;
			bool armControlRequest = GetKeyUp(armControlKeys) >= 0;
			bool handRotateRequest = GetKeyUp (handRotateKeys) >= 0;
			bool switchControllerRequest = GetKeyUp (switchControllerKeys) >= 0;
			if (GetKeyUp (toggleInstructionsKeys) >= 0) {
				helpCanvas.enabled = !helpCanvas.enabled;
			}
			if (armControlRequest) { TransitionToHand (ControlState.controlArm, true); return; }
			if (headControlRequest) { TransitionToHead (); return; }
			if (handFlyRequest) { TransitionToHand (ControlState.flyingHand, false); return; }
			if (handRotateRequest) { TransitionToHand (ControlState.rotateHand, false); return; }
			if (switchControllerRequest && moveControllers.Length > 0) { NextMoveController (); return; }

			Transform hand = CurrentMoveController;
			// TODO proper state machines?
			switch (CurrentControlState) {
			case ControlState.controlHead: break;
			case ControlState.transition: break;
			case ControlState.controlArm: {
					body.disabledControls = false;
					Vector3 movHorizonDir = movingCamera.GetHorizonalDirection(-body.gravityDirection);
					Quaternion q = Quaternion.AngleAxis (-mainCameraVSMovingCameraHorizonalOffset, -body.gravityDirection);
					Vector3 constantForward = q * movHorizonDir;
					Lines.MakeArrow (ref line_test, body.transform.position, body.transform.position + constantForward, Color.black, .1f, .1f);
//					mainCamera.transform.rotation = Quaternion.LookRotation (constantForward, -body.gravityDirection);
//					body.horizontalRotationOffset = -mainCameraVSMovingCameraHorizonalOffset;
					Transform t = movingCamera.transform;
					t.position = GetCameraPivotPoint ();
					float wheel = Input.GetAxis ("Mouse ScrollWheel");
					targetDistance += wheel;
					hand.position = GetCameraPivotPoint () + t.forward * targetDistance;
					hand.rotation = t.rotation * handRotationRelativeToCamera[currentMoveControllerIndex];
//					Debug.Log ("!!!");
//					body.SetFacing(t.forward);
					mainCamera.transform.rotation = movingCamera.transform.rotation;
				} break;
			case ControlState.flyingHand: {
					Transform t = movingCamera.transform;
//					movingCamera.distanceFromTarget = targetDistance;
					Vector3 offset = t.forward * movingCamera.settings.distanceFromTarget;
					float v = Input.GetAxis ("Vertical");
					float h = Input.GetAxis ("Horizontal");
					float d = Input.GetKey (KeyCode.E) ? 1 : (Input.GetKey (KeyCode.Q) ? -1 : 0);
					CurrentMoveController.rotation = t.rotation * HandRotationOffset();
					t.position += (t.forward * v + t.right * h + t.up * d) * UnityEngine.Time.deltaTime;
					hand.position = t.position + offset;
				} break;
			case ControlState.rotateHand: {
					Transform t = movingCamera.transform;
					float v = Input.GetAxis ("Vertical");
					float h = Input.GetAxis ("Horizontal");
					float wheel = Input.GetAxis ("Mouse ScrollWheel");
					float dep = Input.GetKey (KeyCode.Q) ? 1 : Input.GetKey (KeyCode.E) ? -1 : 0;
					movingCamera.enabled = false;
					if (Cursor.lockState != CursorLockMode.Locked && Cursor.visible) {
						Vector3 mPos = Input.mousePosition - new Vector3 (Screen.width / 2, Screen.height / 2, 0);
						mPos.Normalize ();
						float angle = Mathf.Acos (mPos.x) * 180 / Mathf.PI * ((mPos.y < 0) ? -1 : 1);
						if (prevAngleOfMouse == float.PositiveInfinity) {
							prevAngleOfMouse = angle;
						}
						float angleDelta = angle - prevAngleOfMouse;
						t.position = GetCameraPivotPoint ();
						prevAngleOfMouse = angle;
						hand.Rotate (Quaternion.Inverse (hand.rotation) * t.forward, angleDelta);
					}
//					else {
//						hand.Rotate (Quaternion.Inverse (hand.rotation) * t.forward, Input.GetAxis("Mouse X"));
//					}
					hand.Rotate (Quaternion.Inverse(hand.rotation) * t.forward, dep * movingCamera.settings.mouseSensitivity);
					hand.Rotate (Quaternion.Inverse(hand.rotation) * t.right,   v * movingCamera.settings.mouseSensitivity);
					hand.Rotate (Quaternion.Inverse(hand.rotation) * t.up,      h * movingCamera.settings.mouseSensitivity);
					targetDistance += wheel * Time.deltaTime;
					hand.position = GetCameraPivotPoint () + t.forward * targetDistance;
				} break;
			}
		}
		private float prevAngleOfMouse = float.PositiveInfinity;
		private static string moveControllerName = "<camera offset>";
		public Transform GetControllerObserver() {
			Transform t = CurrentMoveController.Find (moveControllerName);
			return t;
		}
		public Quaternion HandRotationOffset() {
			Transform t = GetControllerObserver ();
			return (t == null) ? Quaternion.identity : Quaternion.Inverse(t.localRotation);
		}
		public Transform HandRotationTransform() {
			Transform t = GetControllerObserver ();
			return (t == null) ? CurrentMoveController : t;
		}
		public Transform MoveControllerTransform() {
			Transform adjustHandControl = GetControllerObserver();
			if (adjustHandControl == null) {
				Transform focus = CurrentMoveController;
				adjustHandControl = (new GameObject (moveControllerName)).transform;
				adjustHandControl.position = focus.position;
				adjustHandControl.SetParent (focus);
				adjustHandControl.rotation = movingCamera.transform.rotation;
			}
			return adjustHandControl;
		}
	}
}