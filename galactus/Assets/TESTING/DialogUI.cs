using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Z {
	public class DialogPart {
		public string desc;
	}
	public class Say : DialogPart {
		public string text;
	}
	public class Cmd : DialogPart {
		public string cmd;
	}
	public class Option : DialogPart {
		public string text;
		public string next;
		public string cmd;
	}
	public class Dialog {
		public string name;
		public DialogPart[] commands;
	}
	public class DialogUI : MonoBehaviour {
		//GameObject mainUI;
		public TMPro.TMP_Text textOuput;
		public RectTransform optionArea;
		public RectTransform optionPrefab;
		// TODO reference the panel holding the text, which should be invisible without any say command, and should vanish once all say is gone.

		Dialog currentDialog;
		public enum State { none, init, run, waiting, done }
		State state;
		int commandIndex, letterIndex;
		float secondsPerCharacter = 1f / 32;
		public Dictionary<char, float> secondsPerSpecificChar = new Dictionary<char, float>() {
			{',', 1f/2},
			{'.', 3f/4},
			{':', 1},
			{';', 1f/2},
			{'\t', 0},
			{'\'', 1f/4},
			{'\"', 1f/4},
			{'?', 1f},
			{'!', 1f/4f},
			{'\n', 1.25f},
		};

		private static Dictionary<string, Dialog> s_all_dialogs = new Dictionary<string, Dialog>();

		public void SetDialog(Dialog dialog) {
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

		public void SetDialogs(IList dialogs) {
			for(int i = 0; i < dialogs.Count; ++i) {
				SetDialog(dialogs[i] as Dialog);
			}
		}

		public void StartDialog(string name) {
			currentDialog = s_all_dialogs[name];
			commandIndex = 0;
			letterIndex = 0;
			state = State.init;
		}

		void AdvanceLetter() {
			Z.Say s = currentDialog.commands[commandIndex] as Z.Say;
			if(letterIndex >= s.text.Length) {
				state = State.waiting;
			} else {
				if(state == State.waiting) {
					Debug.Log("SKIPPING AHEAD");
					letterIndex = s.text.Length;
				} else {
					float delay;
					char nextLetter = s.text[letterIndex++];
					if(!secondsPerSpecificChar.TryGetValue(nextLetter, out delay)) {
						delay = secondsPerCharacter;
					}
					long delayLong = (long)(delay * 1000);
					//Debug.Log(letterIndex+" "+nextLetter+" "+delayLong);
					NS.Chrono.setTimeoutRealtime(AdvanceLetter, delayLong);
				}
				textOuput.text = s.text.Substring(0, letterIndex);
			}
		}

		void ClearOptions(){
			for(int i = optionArea.childCount - 1; i >= 0; i--){
				Destroy(optionArea.GetChild(i).gameObject);
			}
			optionArea.gameObject.SetActive(false);
			commandIndex++;
		}

		void Initialize() {
			bool needToRepeatThis = false;
			do {
				needToRepeatThis = false;
				DialogPart p = currentDialog.commands[commandIndex];
				System.Type ptype = p.GetType();
				if(ptype == typeof(Z.Say)) {
					state = State.run;
					AdvanceLetter();
				} else if(ptype == typeof(Z.Option)) {
					Z.Option opt = p as Z.Option;
					RectTransform button = Instantiate(optionPrefab) as RectTransform;
					TMPro.TMP_Text t = button.GetComponentInChildren<TMPro.TMP_Text>();
					t.text = opt.text;
					UnityEngine.UI.Button b = button.GetComponentInChildren<UnityEngine.UI.Button>();
					if(opt.cmd != null){ b.onClick.AddListener(() => { Debug.Log(opt.cmd); }); }
					if(opt.next != null) { b.onClick.AddListener(() => { StartDialog(opt.next); }); }
					// regardless of whether or not there are actions to execute, picking a choice will end the options part of the dialog.
					b.onClick.AddListener(() => { ClearOptions(); });
					b.transform.SetParent(optionArea.transform);
					if(!optionArea.gameObject.activeSelf){
						optionArea.gameObject.SetActive(true);
					}
					// if there are multiple options in a row, get the next one too!
					if(commandIndex + 1 < currentDialog.commands.Length
					&& currentDialog.commands[commandIndex + 1].GetType() == typeof(Z.Option)) {
						commandIndex++;
						needToRepeatThis = true;
						state = State.init;
					} else {
						state = State.run;
					}
				} else if(ptype == typeof(Z.DialogPart)) {
					commandIndex++;
					needToRepeatThis = true;
				}
				if(needToRepeatThis) {
					needToRepeatThis = commandIndex < currentDialog.commands.Length;
				}
			} while(needToRepeatThis);
		}

		// TODO optionally read this from a plain text file
		[TextArea(3, 5)]
		public string input =
"[\n" +
"  Z.Dialog{\n" +
"    name:'intro'\n"+
"    commands:[\n"+
"      Z.DialogPart{desc:'the first dialog'}\n"+
"      Z.Say{text:'Hello World!'},\n"+
"      Z.Say{text:'What an amazing start to my dialog!'}\n"+
"      Z.Option{text:'I know, right?',next:'ikr'}\n"+
"      Z.Option{text:'Kinda lame...',next:'lame'}\n"+
"      Z.Option{text:'Quit',cmd:'quit'}\n" +
"      Z.Option{text:'what?',next:'intro'}\n" +
"    ]\n"+
"  }\n"+
"  Z.Dialog{\n"+
"    name:'ikr'\n"+
"    commands:[\n"+
"      Z.Say{text:'Right!'},\n" +
"    ]\n"+
"  }\n"+
"  Z.Dialog{\n"+
"    name:'lame'\n"+
"    commands:[\n"+
"      Z.DialogPart{desc:'testing the quit functionality'}\n" +
"      Z.Say{text:'super lame'},\n" +
"      Z.Cmd{cmd:'quit'}\n" +
"    ]\n"+
"  }\n"+
"]";
		public string startDialog = "intro";

		void Update() {
			if(currentDialog != null) {
				if(commandIndex >= currentDialog.commands.Length) {
					Debug.Log("Done with commands!");
					commandIndex = 0;
					letterIndex = 0;
					currentDialog = null;
					state = State.none;
				}
				switch(state) {
				case State.none: break;
				case State.init:
					Initialize();
					state = State.run;
					break;
				case State.run:
					if(Input.GetButtonDown("Jump")) { state = State.waiting; }
					break;
				case State.waiting:
					if(Input.GetButtonDown("Jump")) { state = State.done; }
					break;
				case State.done:
					commandIndex++;
					letterIndex = 0;
					state = State.init;
					break;
				}
			}
		}
		void Start() {
			object ob = OMU.Util.FromScript(input);
			Debug.Log(OMU.Util.ToScript(ob, true));
			SetDialogs(ob as IList);
			if(startDialog != null) {
				StartDialog(startDialog);
			}
		}
	}
}
