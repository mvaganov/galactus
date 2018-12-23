using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NS {
	public interface IStateRunner {
		void AdvanceStateTree();
	}

	namespace Story {
		public class Say : NS.StateMachine.State {
			public string text;
			public TMPro.TextAlignmentOptions align = TMPro.TextAlignmentOptions.BottomLeft;
			public Color bgcolor;
			public bool keepOnScreen;
			public ScreenArea screenArea = ScreenArea.Top;
			public string portrait;
			public ScreenArea portraitArea = ScreenArea.BottomRight;
			public float portraitScale;
			public Vector4 textPadding;
			class Vars {
				public Color prev_bgcolor;
			}
			private Vars vars;

			public override void Enter(IStateRunner sr) {
				Story story = sr as Story;
				story.letterIndex = 0;
				story.textOuput.text = "";
				vars = new Vars();
				if(story.textPanel) {
					Util.SetRect(story.textPanel, screenArea);
					story.textPanel.gameObject.SetActive(true);
					UnityEngine.UI.Image img = story.textPanel.GetComponent<UnityEngine.UI.Image>();
					if(img != null) {
						vars.prev_bgcolor = img.color;
						if(bgcolor != default(Color)) {
							img.color = bgcolor;
						}
						if(img.hasBorder){
							Vector4 bounds = img.sprite.border;
							Debug.Log(bounds);
							UnityEngine.UI.CanvasScaler cs = story.textPanel.GetComponentInParent<UnityEngine.UI.CanvasScaler>();
							float scale = cs.referencePixelsPerUnit / img.sprite.pixelsPerUnit;
							float l = bounds.x * scale, t = bounds.y * scale, r = bounds.z * scale, b = bounds.w * scale;
							if(textPadding != default(Vector4)){
								l += textPadding.x;
								t += textPadding.y;
								r += textPadding.z;
								b += textPadding.w;
							}
							RectTransform rect = story.textOuput.GetComponent<RectTransform>();
							rect.offsetMin = new Vector2(l, b);
							rect.offsetMax = new Vector2(-r, -t);
						}
					}
				}
				if(story.portraitPanel) {
					RectTransform r = story.portraitPanel;
					Util.AnchorRect(r, portraitArea);
					UnityEngine.UI.Image img = r.GetComponent<UnityEngine.UI.Image>();
					Sprite s = story.GetSprite(portrait);
					if(s != null){
						img.sprite = s;
						if(portraitScale == 0) { portraitScale = 1; }
						r.sizeDelta = new Vector2(img.preferredWidth, img.preferredHeight) * portraitScale;
					}
					img.enabled = s != null;
				}
				AdvanceLetter(story);
			}
			public override void Execute(IStateRunner sr) {
				Story story = sr as Story;
				if(Input.GetKeyDown(KeyCode.UpArrow)) {
					screenArea = Util.Shift(screenArea, ScreenArea.Top);
					Util.SetRect(story.textPanel, screenArea);
				}
				if(Input.GetKeyDown(KeyCode.DownArrow)) {
					screenArea = Util.Shift(screenArea, ScreenArea.Bottom);
					Util.SetRect(story.textPanel, screenArea);
				}
				if(Input.GetKeyDown(KeyCode.LeftArrow)) {
					screenArea = Util.Shift(screenArea, ScreenArea.Left);
					Util.SetRect(story.textPanel, screenArea);
				}
				if(Input.GetKeyDown(KeyCode.RightArrow)) {
					screenArea = Util.Shift(screenArea, ScreenArea.Right);
					Util.SetRect(story.textPanel, screenArea);
				}
				if(Input.GetButtonDown("Jump")) {
					if(story.letterIndex < text.Length) {
						story.letterIndex = text.Length;
					} else {
						story.AdvanceStateTree();
					}
				}
			}
			public override void Exit(IStateRunner sr) {
				if(!keepOnScreen) {
					Story story = sr as Story;
					if(story.textPanel) {
						UnityEngine.UI.Image img = story.textPanel.GetComponent<UnityEngine.UI.Image>();
						if(img != null) {
							img.color = vars.prev_bgcolor;
						}
					}
					vars = null;
					if(story.textPanel) story.textPanel.gameObject.SetActive(false);
					story.textOuput.text = "";
					story.letterIndex = 0;
				}
			}
			void AdvanceLetter(IStateRunner sr) {
				Story story = sr as Story;
				if(story.state.CurrentState != this) { return; }
				int i = story.letterIndex;
				if(i < text.Length) {
					float delay = story.msPerCharacter, mul;
					char nextLetter = text[i++];
					if(story.specificCharMultiplier.TryGetValue(nextLetter, out mul)) {
						delay *= mul;
					}
					NS.Chrono.setTimeoutRealtime(() => { AdvanceLetter(story); }, (long)delay);
					if(delay != 0) {
						story.TriggerTextNoise();
					}
				}
				if(story.textOuput.text.Length != i) {
					story.textOuput.text = text.Substring(0, i);
				}
				story.letterIndex = i;
			}
		}
		public class Option : NS.StateMachine.State {
			public string text;
			public object next;
			public List<object> commands;
			public ScreenArea screenArea = ScreenArea.Bottom;

			public override void Enter(IStateRunner sr) {
				Story story = sr as Story;
				MakeButton(story);
				// if there are multiple options in a row, initialize those too!
				NS.StateMachine.State s = story.state.PeekNextState(story);
				//Debug.Log(OMU.Util.ToScriptTiny(s));
				Option opt = s as Option;
				if(opt != null) { story.AdvanceStateTree(); }
			}

			public void FinishedWithOptions(IStateRunner sr) {
				Story story = sr as Story;
				for(int i = story.optionPanel.childCount - 1; i >= 0; i--) {
					// TODO instead of destroying the objects, hide and cache them for reuse?
					Object.Destroy(story.optionPanel.GetChild(i).gameObject);
				}
				story.optionPanel.gameObject.SetActive(false);
			}
			public RectTransform MakeButton(IStateRunner sr) {
				Story story = sr as Story;
				RectTransform button = Object.Instantiate(story.optionPrefab) as RectTransform;
				TMPro.TMP_Text t = button.GetComponentInChildren<TMPro.TMP_Text>();
				t.text = text;
				UnityEngine.UI.Button b = button.GetComponentInChildren<UnityEngine.UI.Button>();
				if(commands != null) {
					b.onClick.AddListener(() => {
						commands.ForEach((cmd) => {
							if(cmd is string) {
								CmdLine.DoCommand(cmd as string);
							} else {
								NS.ActivateAnything.DoActivate(cmd, b, story, true);
							}
						});
					});
				}
				if(next != null) {
					b.onClick.AddListener(() => {
						FinishedWithOptions(story);
						story.StartState(next);
					});
				}
				if(next == null && commands == null) {
					b.onClick.AddListener(() => {
						FinishedWithOptions(story); story.AdvanceStateTree();
					});
				}
				// regardless of whether or not there are actions to execute, picking a choice will end the options part of the dialog.
				b.transform.SetParent(story.optionPanel.transform);
				if(!story.optionPanel.gameObject.activeSelf) {
					Util.SetRect(story.optionPanel, screenArea);
					story.optionPanel.gameObject.SetActive(true);
				}
				return button;
			}
		}
		public class Cmd : NS.StateMachine.State {
			public string cmd;

			public override void Enter(IStateRunner sr) {
				CmdLine.DoCommand(cmd);
				sr.AdvanceStateTree();
			}
		}

		public class Story : MonoBehaviour, IStateRunner {
			public RectTransform textPanel;
			public RectTransform portraitPanel;
			public TMPro.TMP_Text textOuput;
			public RectTransform optionPanel;
			public RectTransform optionPrefab;
			public AudioClip textNoise;

			[HideInInspector]
			public NS.StateMachine.StateKeeper state = new NS.StateMachine.StateKeeper();
			[HideInInspector]
			public int commandIndex, letterIndex;
			public float msPerCharacter = 50;
			public Dictionary<char, float> specificCharMultiplier = new Dictionary<char, float>() {
				{',', 5}, {'.', 10}, {':', 5}, {';', 7}, {'\t', 0}, {'\'', 5}, {'\"', 5}, {'?', 15}, {'!', 15}, {'\n', 5},
			};

			private static Dictionary<string, NS.StateMachine.Branch> s_all_dialogs = 
				new Dictionary<string, NS.StateMachine.Branch>();

			public void TriggerTextNoise() {
				if(textNoise != null) {
					Noisy.PlaySound(textNoise);
				}
			}

			public static NS.StateMachine.State Error(string errorMessage) {
				return new Say { text = errorMessage, bgcolor = new Color(1, 0, 0, .25f) };
			}
			public void SetBranchState(NS.StateMachine.State command) {
				state.SetState(command, this);
			}

			public void AdvanceStateTree() {
				if(state.HasStateToAdvance(this)) { state.Advance(this); }
				else { state.SetState(null, this); }
			}

			public void AddBranch(NS.StateMachine.Branch branch) {
				NS.StateMachine.Branch d;
				if(s_all_dialogs.TryGetValue(branch.name, out d)) {
					if(d == branch) {
						throw new System.Exception(branch.name + " is being re-added!");
					} else {
						throw new System.Exception(branch.name + " already exists!");
					}
				}
				s_all_dialogs[branch.name] = branch;
			}

			public void AddBranches(IList branches) {
				for(int i = 0; i < branches.Count; ++i) {
					AddBranch(branches[i] as NS.StateMachine.Branch);
				}
			}

			public void StartState(object a_state) {
				NS.StateMachine.State nextState = a_state as NS.StateMachine.State;
				if(nextState == null){
					string n = a_state as string;
					NS.StateMachine.Branch b = null;
					if(n != null && !s_all_dialogs.TryGetValue(n, out b)) {
						nextState = Error("Could not find state \"" + n + "\"");
					} else {
						nextState = b;
					}
				}
				if(nextState == null) {
					Debug.LogWarning("Could not parse state " + a_state);
				}
				state.SetState(nextState, this);
			}

			public NS.TextPtr.ObjectPtr textSource;
			public string startBranchName = "intro";

			[Tooltip("where to put images, icons, and portraits for dialog, to be referenced by the script")]
			public List<Object> knownResources = new List<Object>();

			public Sprite GetSprite(string name){
				Sprite found = null;
				if(name != null) {
					for(int i = 0; i < knownResources.Count; ++i) {
						Sprite s = knownResources[i] as Sprite;
						if(s.name == name) {
							found = s;
							break;
						}
					}
				}
				return found;
			}

			void Update() {
				state.Execute(this);
			}

			void Start() {
				textPanel.gameObject.SetActive(false);
				optionPanel.gameObject.SetActive(false);
				object ob = OMU.Util.FromScript(textSource.ToString(), textSource.SourceName);
				//Debug.Log(OMU.Util.ToScript(ob, true));
				Debug.Log(OMU.Util.ToScriptTiny(ob));
				AddBranches(ob as IList);
				if(startBranchName != null) {
					StartState(startBranchName);
				}
			}
		}
	}
	/// <summary><c>
	/// <para> 4 0 5 </para>
	/// <para> 2 8 3 </para>
	/// <para> 6 1 7 </para>
	/// </c></summary>
	public enum ScreenArea {
		Top = 0, Bottom = 1, Left = 2, Right = 3, TopLeft = 4, TopRight = 5, BottomLeft = 6, BottomRight = 7, Center = 8
	}
	public static class Util {
		private static readonly Vector2
			vuc = new Vector2(0.5f, 1.0f),
			vlc = new Vector2(0.5f, 0.0f),
			vml = new Vector2(0.0f, 0.5f),
			vmr = new Vector2(1.0f, 0.5f),
			vul = new Vector2(0.0f, 1.0f),
			vur = new Vector2(1.0f, 1.0f),
			vll = new Vector2(0.0f, 0.0f),
			vlr = new Vector2(1.0f, 0.0f),
			vmc = new Vector2(0.5f, 0.5f);
		private static readonly Vector2[] screenAreaPositions = new Vector2[] { 
			vuc,vlc,vml,vmr,vul,vur,vll,vlr,vmc
		};
		private static readonly Vector2[,] screenBoundsPositions = new Vector2[,]{
			{vml,vur}, {vll,vmr}, {vll,vuc}, {vlc,vur}, {vml,vuc}, {vmc,vur}, {vll,vmc}, {vlc,vmr}, {vll,vur}
		};
		public static void SetRect(RectTransform r, ScreenArea area, bool pivotOnly = false) {
			r.pivot = screenAreaPositions[(int)area];
			if(!pivotOnly) {
				r.anchorMin = screenBoundsPositions[(int)area, 0];
				r.anchorMax = screenBoundsPositions[(int)area, 1];
			}
		}
		public static void AnchorRect(RectTransform r, ScreenArea area) {
			r.pivot = screenAreaPositions[(int)area];
			r.anchorMin = r.anchorMax = r.pivot;
			r.offsetMin = r.offsetMax = Vector2.zero;
		}
		/// <summary>The ScreenArea shift matrix. If ScreenArea changes, this will break.</summary>
		private static readonly int[,] screenAreaShiftMatrix = new int[,]{
			{0,8,4,5,2,3,2,3,0},
			{8,1,6,7,2,3,6,7,1},
			{4,6,2,8,4,0,6,1,2},
			{5,7,8,3,0,5,1,7,3},
			{0,2,2,0,4,0,2,8,4},
			{0,3,0,3,0,5,8,3,5},
			{2,1,2,1,2,8,6,1,6},
			{3,1,1,3,8,3,1,7,7},
			{0,1,2,3,4,5,6,7,8}        
		};
		public static ScreenArea Shift(ScreenArea start, ScreenArea mod) {
			return (ScreenArea)screenAreaShiftMatrix[(int)start,(int)mod];
		}
	}
}
public class Story : NS.Story.Story{}