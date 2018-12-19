using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Z {
	public enum ScreenArea { top = 0, bottom, left, right, upperLeft, upperRight, lowerLeft, lowerRight, middle }
	public class State {
		public string desc;
		public virtual void Enter(Ztory z) {
			z.AdvanceStateTree();
		}
		public virtual void Execute(Ztory z) {}
		public virtual void Exit(Ztory z) {}
	}
	public class NestedState : State {
		protected State currentState;
		public State CurrentState {
			get {
				NestedState ns = currentState as NestedState;
				return ns != null ? ns.CurrentState : currentState;
			}
		}
		public void SetState(State s, Ztory z){
			if(currentState != null){
				currentState.Exit(z);
			}
			currentState = s;
			if(currentState != null) {
				currentState.Enter(z);
			}
		}
		public virtual bool IsDone(Ztory z) {
			if(currentState == null) return true;
			NestedState ns = currentState as NestedState;
			return ns == null || ns.IsDone(z);
		}
		public virtual void Advance(Ztory z) {
			(currentState as NestedState).Advance(z);
			//if(currentState == null) {
			//	throw new System.Exception("can't advance 'NestedState'");
			//}
			//NestedState ns = currentState as NestedState;
		}
		public virtual State PeekNextState(Ztory z) {
			NestedState ns = currentState as NestedState;
			return ns != null && !ns.IsDone(z) 
				? ns.PeekNextState(z) : null;
		}
		public override void Enter(Ztory z) {}
		public override void Execute(Ztory z) {
			if(currentState != null) { currentState.Execute(z); }
		}
		public override void Exit(Ztory z) {}
	}
	public class Say : State {
		public string text;
		public bool keepOnScreen;
		public ScreenArea screenArea = ScreenArea.top;
		public ScreenArea portrainArea = ScreenArea.lowerRight;

		public override void Enter(Ztory z) {
			z.letterIndex = 0;
			z.textOuput.text = "";
			if(z.textPanel) {
				Util.SetRect(z.textPanel, screenArea);
				z.textPanel.gameObject.SetActive(true);
			}
			AdvanceLetter(z);
		}
		public override void Execute(Ztory z) {
			if(Input.GetKeyDown(KeyCode.UpArrow)){
				screenArea = Util.Shift(screenArea, ScreenArea.top);
				Util.SetRect(z.textPanel, screenArea);
			}
			if(Input.GetKeyDown(KeyCode.DownArrow)) {
				screenArea = Util.Shift(screenArea, ScreenArea.bottom);
				Util.SetRect(z.textPanel, screenArea);
			}
			if(Input.GetKeyDown(KeyCode.LeftArrow)) {
				screenArea = Util.Shift(screenArea, ScreenArea.left);
				Util.SetRect(z.textPanel, screenArea);
			}
			if(Input.GetKeyDown(KeyCode.RightArrow)) {
				screenArea = Util.Shift(screenArea, ScreenArea.right);
				Util.SetRect(z.textPanel, screenArea);
			}
			if(Input.GetButtonDown("Jump")) {
				if(z.letterIndex < text.Length) {
					z.letterIndex = text.Length;
				} else {
					z.AdvanceStateTree();
				}
			}
		}
		public override void Exit(Ztory z) {
			if(!keepOnScreen) {
				if(z.textPanel) z.textPanel.gameObject.SetActive(false);
				z.textOuput.text = "";
				z.letterIndex = 0;
			}
		}
		void AdvanceLetter(Ztory z) {
			if(z.state.CurrentState != this) { Debug.Log("Leters stopped! "+text); return; }
			int i = z.letterIndex;
			if(i < text.Length) {
				float delay = z.msPerCharacter, mul;
				char nextLetter = text[i++];
				if(z.specificCharMultiplier.TryGetValue(nextLetter, out mul)) {
					delay *= mul;
				}
				NS.Chrono.setTimeoutRealtime(() => { AdvanceLetter(z); }, (long)delay);
			}
			if(z.textOuput.text.Length != i) {
				z.textOuput.text = text.Substring(0, i);
			}
			z.letterIndex = i;
		}
	}
	public class Option : State {
		public string text, next;
		public List<object> commands;
		public ScreenArea screenArea = ScreenArea.bottom;

		public override void Enter(Ztory z) {
			MakeButton(z);
			// if there are multiple options in a row, initialize those too!
			State s = z.state.PeekNextState(z);
			//Debug.Log(OMU.Util.ToScriptTiny(s));
			Option opt = s as Option;
			if(opt != null) { z.AdvanceStateTree(); }
		}

		public void FinishedWithOptions(Ztory z) {
			for(int i = z.optionPanel.childCount - 1; i >= 0; i--) {
				// TODO instead of destroying the objects, hide and cache them for reuse?
				Object.Destroy(z.optionPanel.GetChild(i).gameObject);
			}
			z.optionPanel.gameObject.SetActive(false);
		}
		public RectTransform MakeButton(Ztory z) {
			RectTransform button = Object.Instantiate(z.optionPrefab) as RectTransform;
			TMPro.TMP_Text t = button.GetComponentInChildren<TMPro.TMP_Text>();
			t.text = text;
			UnityEngine.UI.Button b = button.GetComponentInChildren<UnityEngine.UI.Button>();
			if(commands != null) { b.onClick.AddListener(() => {
				commands.ForEach((cmd) => Debug.Log(cmd));
			}); }
			if(next != null) { b.onClick.AddListener(() => {
				FinishedWithOptions(z); z.StartDialog(next);
			}); }
			if(next == null && commands == null) { b.onClick.AddListener(() => {
				FinishedWithOptions(z); z.AdvanceStateTree(); 
			}); }
			// regardless of whether or not there are actions to execute, picking a choice will end the options part of the dialog.
			b.transform.SetParent(z.optionPanel.transform);
			if(!z.optionPanel.gameObject.activeSelf) {
				Util.SetRect(z.optionPanel, screenArea);
				z.optionPanel.gameObject.SetActive(true);
			}
			return button;
		}
	}
	public class Cmd : State {
		public string cmd;

		public override void Enter(Ztory z) {
			CmdLine.DoCommand(cmd);
			z.AdvanceStateTree();
		}
	}
	public class Dialog : NestedState {
		public string name;
		public State[] commands;
		public class Vars { public int index; }
		protected Vars vars;

		public override bool IsDone(Ztory z) { 
			return vars.index >= commands.Length;
		}
		public override State PeekNextState(Ztory z) {
			State nextState = base.PeekNextState(z);
			if(nextState == null) {
				if(vars == null) {
					nextState = commands[0];
				} else if(vars.index < commands.Length - 1) {
					nextState = commands[vars.index + 1];
				}
				NestedState ns = nextState as NestedState;
				if(ns != null) {
					ns.PeekNextState(z);
				}
			}
			return nextState;
		}
		public override void Advance(Ztory z) {
			State s = commands[vars.index];
			NestedState ns = s as NestedState;
			bool advancedChild = false;
			if(ns != null) {
				if(!ns.IsDone(z)){
					ns.Advance(z);
					advancedChild = true;
				}
			}
			if(!advancedChild) {
				vars.index++;
				if(vars.index < commands.Length) {
					SetState(commands[vars.index], z);
				} else {
					SetState(null, z);
				}
			}
		}
		public override void Enter(Ztory z) {
			vars = new Vars();
			if(commands.Length > 0) {
				SetState(commands[0], z);
			} else {
				z.AdvanceStateTree();
			}
		}
		public override void Exit(Ztory z) {
			vars = null;
		}

		public static Dialog Error(string errorMessage) {
			return new Dialog {
				name = "error message",
				commands = new State[] {
					new Z.Say { text = errorMessage }
				}
			};
		}
	}
	public class Ztory : MonoBehaviour {
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

		private static Dictionary<string, Dialog> s_all_dialogs = new Dictionary<string, Dialog>();

		public void SetDialogState(State command) {
			state.SetState(command, this);
		}

		public void AdvanceStateTree() {
			if(!state.IsDone(this)) {
				state.Advance(this);
			} else {
				Debug.Log("Done!");
				state.SetState(null, this);
			}
		}

		public void AddDialog(Dialog dialog) {
			Dialog d;
			if(s_all_dialogs.TryGetValue(dialog.name, out d)) {
				if(d == dialog) {
					throw new System.Exception(dialog.name + " is being re-added!");
				} else {
					throw new System.Exception(dialog.name + " already exists!");
				}
			}
			s_all_dialogs[dialog.name] = dialog;
		}

		public void AddDialogs(IList dialogs) {
			for(int i = 0; i < dialogs.Count; ++i) { AddDialog(dialogs[i] as Dialog); }
		}

		public void StartDialog(string name) {
			Dialog currentDialog = null;
			if(!s_all_dialogs.TryGetValue(name, out currentDialog)) {
				state.SetState(Dialog.Error("Could not find dialog \"" + name + "\""), this);
			}
			state.SetState(currentDialog, this);
		}

		// TODO optionally read this from a plain text file
		[TextArea(3, 5)]
		public string input =
"[\n" +
"  Z.Dialog{\n" +
"    name:'intro'\n"+
"    commands:[\n"+
"      {desc:'the first dialog'}\n"+
"      Z.Say{text:'Hello World!'},\n"+
"      Z.Say{text:'What an amazing start to my dialog!',keepOnScreen:true}\n"+
"      Z.Option{text:'I know, right?',next:'ikr'}\n"+
"      Z.Option{text:'Kinda lame...',next:'lame'}\n"+
"      Z.Option{text:'Quit',commands:[exit]}\n" +
"      Z.Option{text:'what?',next:'intro'}\n" +
"    ]\n"+
"  }\n"+
"  Z.Dialog{\n"+
"    name:ikr\n"+
"    commands:[\n"+
"      Z.Say{text:'Right!'},\n" +
"    ]\n"+
"  }\n"+
"  Z.Dialog{\n"+
"    name:lame\n"+
"    commands:[\n"+
"      {desc:'testing the quit functionality'}\n" +
"      Z.Say{text:'super lame'},\n" +
"      Z.Cmd{cmd:exit}\n" +
"    ]\n"+
"  }\n"+
"]";
		public string startDialog = "intro";

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
			AddDialogs(ob as IList);
			if(startDialog != null) {
				StartDialog(startDialog);
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
public class Ztory : Z.Ztory{}