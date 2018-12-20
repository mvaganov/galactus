using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NS {
	public enum ScreenArea {
		top = 0, bottom, left, right, upperLeft, upperRight, lowerLeft, lowerRight, middle
	}
	public interface IStateRunner {
		void AdvanceStateTree();
	}
	public class State {
		public string desc;
		public virtual void Enter(IStateRunner sr) { sr.AdvanceStateTree(); }
		public virtual void Execute(IStateRunner sr) {}
		public virtual void Exit(IStateRunner sr) {}
	}
	public class NestedState : State {
		protected State currentState;
		public State CurrentState {
			get {
				NestedState ns = currentState as NestedState;
				return ns != null ? ns.CurrentState : currentState;
			}
		}
		public void SetState(State s, IStateRunner sr){
			if(currentState != null) { currentState.Exit(sr); }
			currentState = s;
			if(currentState != null) { currentState.Enter(sr); }
		}
		public virtual bool IsDone(IStateRunner sr) {
			if(currentState == null) return true;
			NestedState ns = currentState as NestedState;
			return ns == null || ns.IsDone(sr);
		}
		public virtual void Advance(IStateRunner sr) {
			(currentState as NestedState).Advance(sr);
		}
		public virtual State PeekNextState(IStateRunner sr) {
			NestedState ns = currentState as NestedState;
			return ns != null && !ns.IsDone(sr) ? ns.PeekNextState(sr) : null;
		}
		public override void Enter(IStateRunner sr) {}
		public override void Execute(IStateRunner sr) {
			if(currentState != null) { currentState.Execute(sr); }
		}
		public override void Exit(IStateRunner sr) {}
	}
	public class Branch : NestedState {
		public string name;
		public List<State> list;
		public class Vars { public int index; }
		protected Vars vars;

		public override bool IsDone(IStateRunner sr) { 
			return vars.index >= list.Count;
		}
		public override State PeekNextState(IStateRunner sr) {
			State nextState = base.PeekNextState(sr);
			if(nextState == null) {
				if(vars == null) {
					nextState = list[0];
				} else if(vars.index < list.Count - 1) {
					nextState = list[vars.index + 1];
				}
				NestedState ns = nextState as NestedState;
				if(ns != null) {
					ns.PeekNextState(sr);
				}
			}
			return nextState;
		}
		public override void Advance(IStateRunner sr) {
			State s = list[vars.index];
			NestedState ns = s as NestedState;
			bool advancedChild = false;
			if(ns != null) {
				if(!ns.IsDone(sr)){
					ns.Advance(sr);
					advancedChild = true;
				}
			}
			if(!advancedChild) {
				vars.index++;
				if(vars.index < list.Count) { SetState(list[vars.index], sr); }
				else { SetState(null, sr); }
			}
		}
		public override void Enter(IStateRunner sr) {
			vars = new Vars();
			if(list.Count > 0) { SetState(list[0], sr); }
			else { sr.AdvanceStateTree(); }
		}
		public override void Exit(IStateRunner sr) { vars = null; }
	}
	namespace Story {
		public class Say : State {
			public string text;
			public Color bgcolor;
			public bool keepOnScreen;
			public ScreenArea screenArea = ScreenArea.top;
			public ScreenArea portrainArea = ScreenArea.lowerRight;
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
					}
				}
				AdvanceLetter(story);
			}
			public override void Execute(IStateRunner sr) {
				Story story = sr as Story;
				if(Input.GetKeyDown(KeyCode.UpArrow)) {
					screenArea = Util.Shift(screenArea, ScreenArea.top);
					Util.SetRect(story.textPanel, screenArea);
				}
				if(Input.GetKeyDown(KeyCode.DownArrow)) {
					screenArea = Util.Shift(screenArea, ScreenArea.bottom);
					Util.SetRect(story.textPanel, screenArea);
				}
				if(Input.GetKeyDown(KeyCode.LeftArrow)) {
					screenArea = Util.Shift(screenArea, ScreenArea.left);
					Util.SetRect(story.textPanel, screenArea);
				}
				if(Input.GetKeyDown(KeyCode.RightArrow)) {
					screenArea = Util.Shift(screenArea, ScreenArea.right);
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
				}
				if(story.textOuput.text.Length != i) {
					story.textOuput.text = text.Substring(0, i);
				}
				story.letterIndex = i;
			}
		}
		public class Option : State {
			public string text;
			public object next;
			public List<object> commands;
			public ScreenArea screenArea = ScreenArea.bottom;

			public override void Enter(IStateRunner sr) {
				Story story = sr as Story;
				MakeButton(story);
				// if there are multiple options in a row, initialize those too!
				State s = story.state.PeekNextState(story);
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
						story.StartBranch(next);
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
		public class Cmd : State {
			public string cmd;

			public override void Enter(IStateRunner sr) {
				CmdLine.DoCommand(cmd);
				sr.AdvanceStateTree();
			}
		}

		public class Story : MonoBehaviour, IStateRunner {
			public RectTransform textPanel;
			public TMPro.TMP_Text textOuput;
			public RectTransform optionPanel;
			public RectTransform optionPrefab;

			[HideInInspector]
			public NestedState state = new NestedState();
			[HideInInspector]
			public int commandIndex, letterIndex;
			public float msPerCharacter = 50;
			public Dictionary<char, float> specificCharMultiplier = new Dictionary<char, float>() {
			{',', 4}, {'.', 2}, {':', 3}, {';', 5}, {'\t', 0}, {'\'', 2}, {'\"', 2}, {'?', 3}, {'!', 3}, {'\n', 2},
		};

			private static Dictionary<string, Branch> s_all_dialogs = new Dictionary<string, Branch>();

			public static State Error(string errorMessage) {
				Debug.Log("ERROR " + errorMessage);
				return new Say { text = errorMessage, bgcolor = new Color(1, 0, 0, .25f) };
			}
			public void SetBranchState(State command) {
				state.SetState(command, this);
			}

			public void AdvanceStateTree() {
				if(!state.IsDone(this)) { state.Advance(this); } else { state.SetState(null, this); }
			}

			public void AddBranch(Branch branch) {
				Branch d;
				if(s_all_dialogs.TryGetValue(branch.name, out d)) {
					if(d == branch) {
						throw new System.Exception(branch.name + " is being re-added!");
					} else {
						throw new System.Exception(branch.name + " already exists!");
					}
				}
				s_all_dialogs[branch.name] = branch;
			}

			public void AddBranches(IList dialogs) {
				for(int i = 0; i < dialogs.Count; ++i) {
					AddBranch(dialogs[i] as Branch);
				}
			}

			public void StartBranch(object branch) {
				Branch currentBranch = branch as Branch;
				if(currentBranch == null){
					string n = branch as string;
					if(n != null && !s_all_dialogs.TryGetValue(n, out currentBranch)) {
						state.SetState(Error("Could not find Branch \"" + n + "\""), this);
					}
				}
				if(branch != null && currentBranch == null) {
					Debug.LogWarning("Could not parse branch " + branch.ToString());
				}
				state.SetState(currentBranch, this);
			}

			// TODO optionally read this from a plain text file
			[TextArea(3, 5)]
			public string input =
				"#type B NS.Branch #type S NS.Story.Say #type O NS.Story.Option\n" +
				"[\n" +
				"  B{\n" +
				"    name:'intro'\n" +
				"    list:[\n" +
				"      S{text:'Hello World!'}\n" +
				"      S{text:'What an amazing start to my dialog!',keepOnScreen:true}\n" +
				"      O{text:'I know, right?',next:'ikr'}\n" +
				"      O{text:'Kinda lame...',next:'lame'}\n" +
				"      O{text:'Quit',commands:[exit]}\n" +
				"      O{text:'what?',next:'intro'}\n" +
				"    ]\n" +
				"  }\n" +
				"  B{\n" +
				"    name:ikr\n" +
				"    list:[\n" +
				"      {desc:'testing the text functionality'}\n" +
				"      S{text:'Right!'}\n" +
				"      S{text:'totally.'}\n" +
				"    ]\n" +
				"  }\n" +
				"  B{\n" +
				"    name:lame\n" +
				"    list:[\n" +
				"      {desc:'testing the quit functionality'}\n" +
				"      S{text:'super lame'}\n" +
				"      NS.Story.Cmd{cmd:exit}\n" +
				"    ]\n" +
				"  }\n" +
				"]";
			public string startBranch = "intro";

			[Tooltip("where to put images, icons, and portraits for dialog, to be referenced by the script")]
			public List<Object> knownResources = new List<Object>();

			void Update() {
				state.Execute(this);
			}

			void Start() {
				textPanel.gameObject.SetActive(false);
				optionPanel.gameObject.SetActive(false);
				object ob = OMU.Util.FromScript(input);
				//Debug.Log(OMU.Util.ToScript(ob, true));
				Debug.Log(OMU.Util.ToScriptTiny(ob));
				AddBranches(ob as IList);
				if(startBranch != null) {
					StartBranch(startBranch);
				}
			}
		}
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
		public static void SetRect(RectTransform r, ScreenArea area, bool pivotOnly = false) {
			switch(area) {
			case ScreenArea.top: r.pivot = vuc; if(!pivotOnly) { r.anchorMin = vml; r.anchorMax = vur; } break;
			case ScreenArea.bottom: r.pivot = vlc; if(!pivotOnly) { r.anchorMin = vll; r.anchorMax = vmr; } break;
			case ScreenArea.left: r.pivot = vml; if(!pivotOnly) { r.anchorMin = vll; r.anchorMax = vuc; } break;
			case ScreenArea.right: r.pivot = vmr; if(!pivotOnly) { r.anchorMin = vlc; r.anchorMax = vur; } break;
			case ScreenArea.upperLeft: r.pivot = vul; if(!pivotOnly) { r.anchorMin = vml; r.anchorMax = vuc; } break;
			case ScreenArea.upperRight: r.pivot = vur; if(!pivotOnly) { r.anchorMin = vmc; r.anchorMax = vur; } break;
			case ScreenArea.lowerLeft: r.pivot = vll; if(!pivotOnly) { r.anchorMin = vll; r.anchorMax = vmc; } break;
			case ScreenArea.lowerRight: r.pivot = vlr; if(!pivotOnly) { r.anchorMin = vlc; r.anchorMax = vmr; } break;
			case ScreenArea.middle: r.pivot = vmc; if(!pivotOnly) { r.anchorMin = vll; r.anchorMax = vur; } break;
			}
		}
		public static ScreenArea Shift(ScreenArea start, ScreenArea mod) {
			switch(start) {
			case ScreenArea.top:
				switch(mod) {
				case ScreenArea.top: start = ScreenArea.top; break;
				case ScreenArea.bottom: start = ScreenArea.middle; break;
				case ScreenArea.left: start = ScreenArea.upperLeft; break;
				case ScreenArea.right: start = ScreenArea.upperRight; break;
				case ScreenArea.upperLeft: start = ScreenArea.left; break;
				case ScreenArea.upperRight: start = ScreenArea.right; break;
				case ScreenArea.lowerLeft: start = ScreenArea.left; break;
				case ScreenArea.lowerRight: start = ScreenArea.right; break;
				}
				break;
			case ScreenArea.bottom:
				switch(mod) {
				case ScreenArea.top: start = ScreenArea.middle; break;
				case ScreenArea.bottom: start = ScreenArea.bottom; break;
				case ScreenArea.left: start = ScreenArea.lowerLeft; break;
				case ScreenArea.right: start = ScreenArea.lowerRight; break;
				case ScreenArea.upperLeft: start = ScreenArea.left; break;
				case ScreenArea.upperRight: start = ScreenArea.right; break;
				case ScreenArea.lowerLeft: start = ScreenArea.lowerLeft; break;
				case ScreenArea.lowerRight: start = ScreenArea.lowerRight; break;
				}
				break;
			case ScreenArea.left:
				switch(mod) {
				case ScreenArea.top: start = ScreenArea.upperLeft; break;
				case ScreenArea.bottom: start = ScreenArea.lowerLeft; break;
				case ScreenArea.left: start = ScreenArea.left; break;
				case ScreenArea.right: start = ScreenArea.middle; break;
				case ScreenArea.upperLeft: start = ScreenArea.upperLeft; break;
				case ScreenArea.upperRight: start = ScreenArea.top; break;
				case ScreenArea.lowerLeft: start = ScreenArea.lowerLeft; break;
				case ScreenArea.lowerRight: start = ScreenArea.bottom; break;
				}
				break;
			case ScreenArea.right:
				switch(mod) {
				case ScreenArea.top: start = ScreenArea.upperRight; break;
				case ScreenArea.bottom: start = ScreenArea.lowerRight; break;
				case ScreenArea.left: start = ScreenArea.middle; break;
				case ScreenArea.right: start = ScreenArea.right; break;
				case ScreenArea.upperLeft: start = ScreenArea.top; break;
				case ScreenArea.upperRight: start = ScreenArea.upperRight; break;
				case ScreenArea.lowerLeft: start = ScreenArea.bottom; break;
				case ScreenArea.lowerRight: start = ScreenArea.lowerRight; break;
				}
				break;
			case ScreenArea.upperLeft:
				switch(mod) {
				case ScreenArea.top: start = ScreenArea.top; break;
				case ScreenArea.bottom: start = ScreenArea.left; break;
				case ScreenArea.left: start = ScreenArea.left; break;
				case ScreenArea.right: start = ScreenArea.top; break;
				case ScreenArea.upperLeft: start = ScreenArea.upperLeft; break;
				case ScreenArea.upperRight: start = ScreenArea.top; break;
				case ScreenArea.lowerLeft: start = ScreenArea.left; break;
				case ScreenArea.lowerRight: start = ScreenArea.middle; break;
				}
				break;
			case ScreenArea.upperRight:
				switch(mod) {
				case ScreenArea.top: start = ScreenArea.top; break;
				case ScreenArea.bottom: start = ScreenArea.right; break;
				case ScreenArea.left: start = ScreenArea.top; break;
				case ScreenArea.right: start = ScreenArea.right; break;
				case ScreenArea.upperLeft: start = ScreenArea.top; break;
				case ScreenArea.upperRight: start = ScreenArea.upperRight; break;
				case ScreenArea.lowerLeft: start = ScreenArea.middle; break;
				case ScreenArea.lowerRight: start = ScreenArea.right; break;
				}
				break;
			case ScreenArea.lowerLeft:
				switch(mod) {
				case ScreenArea.top: start = ScreenArea.left; break;
				case ScreenArea.bottom: start = ScreenArea.bottom; break;
				case ScreenArea.left: start = ScreenArea.left; break;
				case ScreenArea.right: start = ScreenArea.bottom; break;
				case ScreenArea.upperLeft: start = ScreenArea.left; break;
				case ScreenArea.upperRight: start = ScreenArea.middle; break;
				case ScreenArea.lowerLeft: start = ScreenArea.lowerLeft; break;
				case ScreenArea.lowerRight: start = ScreenArea.bottom; break;
				}
				break;
			case ScreenArea.lowerRight:
				switch(mod) {
				case ScreenArea.top: start = ScreenArea.right; break;
				case ScreenArea.bottom: start = ScreenArea.bottom; break;
				case ScreenArea.left: start = ScreenArea.bottom; break;
				case ScreenArea.right: start = ScreenArea.right; break;
				case ScreenArea.upperLeft: start = ScreenArea.middle; break;
				case ScreenArea.upperRight: start = ScreenArea.right; break;
				case ScreenArea.lowerLeft: start = ScreenArea.bottom; break;
				case ScreenArea.lowerRight: start = ScreenArea.lowerRight; break;
				}
				break;
			case ScreenArea.middle:
				start = mod;
				break;
			}
			return start;
		}
	}
}
public class Story : NS.Story.Story{}