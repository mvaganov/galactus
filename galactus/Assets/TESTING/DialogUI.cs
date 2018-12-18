using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Z {
	public class DialogState {
		public string desc;
		public virtual void Enter(DialogUI dui) {
			dui.AdvanceDialog();
		}
		public virtual void Execute(DialogUI dui) {}
		public virtual void Exit(DialogUI dui) {}

		public enum ScreenArea { top = 0, bottom, left, right, upperLeft, upperRight, lowerLeft, lowerRight, middle }
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
			case ScreenArea.top:		r.pivot = vuc; if(!pivotOnly) { r.anchorMin = vml; r.anchorMax = vur; } break;
			case ScreenArea.bottom:		r.pivot = vlc; if(!pivotOnly) { r.anchorMin = vll; r.anchorMax = vmr; } break;
			case ScreenArea.left:		r.pivot = vml; if(!pivotOnly) { r.anchorMin = vll; r.anchorMax = vuc; } break;
			case ScreenArea.right:		r.pivot = vmr; if(!pivotOnly) { r.anchorMin = vlc; r.anchorMax = vur; } break;
			case ScreenArea.upperLeft:	r.pivot = vul; if(!pivotOnly) { r.anchorMin = vml; r.anchorMax = vuc; } break;
			case ScreenArea.upperRight:	r.pivot = vur; if(!pivotOnly) { r.anchorMin = vmc; r.anchorMax = vur; } break;
			case ScreenArea.lowerLeft:	r.pivot = vll; if(!pivotOnly) { r.anchorMin = vll; r.anchorMax = vmc; } break;
			case ScreenArea.lowerRight:	r.pivot = vlr; if(!pivotOnly) { r.anchorMin = vlc; r.anchorMax = vmr; } break;
			case ScreenArea.middle:		r.pivot = vmc; if(!pivotOnly) { r.anchorMin = vll; r.anchorMax = vur; } break;
			}
		}
		public static ScreenArea Shift(ScreenArea start, ScreenArea mod) {
			switch(start){
			case ScreenArea.top: switch(mod){
				case ScreenArea.top:		start = ScreenArea.top; break;
				case ScreenArea.bottom:		start = ScreenArea.middle; break;
				case ScreenArea.left:		start = ScreenArea.upperLeft; break;
				case ScreenArea.right:		start = ScreenArea.upperRight; break;
				case ScreenArea.upperLeft:	start = ScreenArea.left; break;
				case ScreenArea.upperRight:	start = ScreenArea.right; break;
				case ScreenArea.lowerLeft:	start = ScreenArea.left; break;
				case ScreenArea.lowerRight:	start = ScreenArea.right; break;
				} break;
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
				} break;
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
				} break;
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
				} break;
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
				} break;
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
				} break;
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
				} break;
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
				} break;
			case ScreenArea.middle:
				start = mod;
				break;
			}
			return start;
		}
	}
	public class Say : DialogState {
		public string text;
		public bool keepOnScreen;
		public ScreenArea screenArea = ScreenArea.top;
		public ScreenArea portrainArea = ScreenArea.lowerRight;

		public override void Enter(DialogUI dui) {
			dui.letterIndex = 0;
			dui.textOuput.text = "";
			if(dui.textPanel) {
				SetRect(dui.textPanel, screenArea);
				dui.textPanel.gameObject.SetActive(true);
			}
			AdvanceLetter(dui);
		}
		public override void Execute(DialogUI dui) {
			if(Input.GetKeyDown(KeyCode.UpArrow)){
				screenArea = Shift(screenArea, ScreenArea.top);
				SetRect(dui.textPanel, screenArea);
			}
			if(Input.GetKeyDown(KeyCode.DownArrow)) {
				screenArea = Shift(screenArea, ScreenArea.bottom);
				SetRect(dui.textPanel, screenArea);
			}
			if(Input.GetKeyDown(KeyCode.LeftArrow)) {
				screenArea = Shift(screenArea, ScreenArea.left);
				SetRect(dui.textPanel, screenArea);
			}
			if(Input.GetKeyDown(KeyCode.RightArrow)) {
				screenArea = Shift(screenArea, ScreenArea.right);
				SetRect(dui.textPanel, screenArea);
			}
			if(Input.GetButtonDown("Jump")) {
				if(dui.letterIndex < text.Length) {
					dui.letterIndex = text.Length;
				} else {
					dui.AdvanceDialog();
				}
			}
		}
		public override void Exit(DialogUI dui) {
			if(!keepOnScreen) {
				if(dui.textPanel) dui.textPanel.gameObject.SetActive(false);
				dui.textOuput.text = "";
				dui.letterIndex = 0;
			}
		}
		void AdvanceLetter(DialogUI dui) {
			if(dui.CurrentCommand != this) { Debug.Log("Say cancelled!"); return; }
			int i = dui.letterIndex;
			if(i < text.Length) {
				float delay = dui.msPerCharacter, mul;
				char nextLetter = text[i++];
				if(dui.specificCharMultiplier.TryGetValue(nextLetter, out mul)) {
					delay *= mul;
				}
				NS.Chrono.setTimeoutRealtime(() => { AdvanceLetter(dui); }, (long)delay);
			}
			if(dui.textOuput.text.Length != i) {
				dui.textOuput.text = text.Substring(0, i);
			}
			dui.letterIndex = i;
		}
	}
	public class Option : DialogState {
		public string text, next, cmd;
		public ScreenArea screenArea = ScreenArea.bottom;

		public override void Enter(DialogUI dui) {
			MakeButton(dui);
			// if there are multiple options in a row, initialize those too!
			int i = dui.commandIndex+1;
			while(i < dui.currentDialog.commands.Length) {
				Z.Option opt = dui.currentDialog.commands[i] as Z.Option;
				if(opt != null) {
					opt.MakeButton(dui);
					i++;
				} else {
					break;
				}
			}
			dui.commandIndex = i - 1;
		}
		public override void Exit(DialogUI dui) {
			for(int i = dui.optionPanel.childCount - 1; i >= 0; i--) {
				Object.Destroy(dui.optionPanel.GetChild(i).gameObject); // TODO instead of destroying the objects, hide and cache them for reuse?
			}
			dui.optionPanel.gameObject.SetActive(false);
		}
		public RectTransform MakeButton(DialogUI dui) {
			RectTransform button = Object.Instantiate(dui.optionPrefab) as RectTransform;
			TMPro.TMP_Text t = button.GetComponentInChildren<TMPro.TMP_Text>();
			t.text = text;
			UnityEngine.UI.Button b = button.GetComponentInChildren<UnityEngine.UI.Button>();
			if(cmd != null) { b.onClick.AddListener(() => { Debug.Log(cmd); }); }
			if(next != null) { b.onClick.AddListener(() => { dui.StartDialog(next); }); }
			if(next == null && cmd == null) { b.onClick.AddListener(() => { dui.AdvanceDialog(); }); }
			// regardless of whether or not there are actions to execute, picking a choice will end the options part of the dialog.
			b.transform.SetParent(dui.optionPanel.transform);
			if(!dui.optionPanel.gameObject.activeSelf) {
				SetRect(dui.optionPanel, screenArea);
				dui.optionPanel.gameObject.SetActive(true);
			}
			return button;
		}
	}
	public class Cmd : DialogState {
		public string cmd;

		public override void Enter(DialogUI dui) {
			Debug.Log(cmd); // TODO insert script execution here...
			dui.AdvanceDialog();
		}
	}
	public class Dialog {
		public string name;
		public DialogState[] commands;

		public static Dialog Error(string errorMessage) {
			return new Dialog {
				name = "error message",
				commands = new DialogState[] {
					new Z.Say { text = errorMessage }
				}
			};
		}
	}
	public class DialogUI : MonoBehaviour {
		public RectTransform textPanel;
		public TMPro.TMP_Text textOuput;
		public RectTransform optionPanel;
		public RectTransform optionPrefab;

		[HideInInspector]
		public Dialog currentDialog;
		public DialogState CurrentCommand;
		[HideInInspector]
		public int commandIndex, letterIndex;
		public float msPerCharacter = 50;
		public Dictionary<char, float> specificCharMultiplier = new Dictionary<char, float>() {
			{',', 4}, {'.', 2}, {':', 3}, {';', 5}, {'\t', 0}, {'\'', 2}, {'\"', 2}, {'?', 3}, {'!', 3}, {'\n', 2},
		};

		private static Dictionary<string, Dialog> s_all_dialogs = new Dictionary<string, Dialog>();

		public void SetDialogState(DialogState command) {
			if(CurrentCommand != null) { CurrentCommand.Exit(this); }
			CurrentCommand = command;
			if(CurrentCommand != null) { CurrentCommand.Enter(this); }
		}
		public void AdvanceDialog() {
			commandIndex++;
			bool oob = commandIndex >= currentDialog.commands.Length;
			DialogState s = !oob ? currentDialog.commands[commandIndex] : null;
			SetDialogState(s);
			if(oob) { currentDialog = null; }
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
			if(!s_all_dialogs.TryGetValue(name, out currentDialog)){
				currentDialog = Dialog.Error("Could not find dialog \"" + name + "\"");
			}
			commandIndex = 0;
			SetDialogState(currentDialog.commands[commandIndex]);
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
"      {desc:'testing the quit functionality'}\n" +
"      Z.Say{text:'super lame'},\n" +
"      Z.Cmd{cmd:'quit'}\n" +
"    ]\n"+
"  }\n"+
"]";
		public string startDialog = "intro";

		[Tooltip("where to put images, icons, and portraits for dialog, to be referenced by the script")]
		public List<Object> knownResources = new List<Object>();

		void Update() {
			if(currentDialog != null) {
				if(commandIndex >= currentDialog.commands.Length) {
					Debug.Log("Done with commands!");
					SetDialogState(null);
					currentDialog = null;
				}
			}
			if(CurrentCommand != null) { CurrentCommand.Execute(this); }
		}
		void Start() {
			//var threadTest = new System.Threading.Thread(delegate () {
			//	for(int i = 0; i < 10; i++){
			//		Debug.Log(i);
			//		System.Threading.Thread.Sleep(1000);
			//	}
			//});
			//threadTest.Start();
			var thread = new System.Threading.Thread(delegate () {
				string commandName = "";//say";//"cmd.exe";
				string strCmdText = "hello world";
				//Debug.Log(strCmdText);
				var processInfo = new System.Diagnostics.ProcessStartInfo(commandName, strCmdText) {
					CreateNoWindow = true,
					UseShellExecute = false
				};

				var process = System.Diagnostics.Process.Start(processInfo);

				process.WaitForExit();
				process.Close();
				 Debug.Log("!!!!!!"+strCmdText);


				//System.Diagnostics.Process.Start("cmd.exe", strCmdText);

				//Debug.Log("!!!!!!");
				//string command = "Hello World.";
				//System.Diagnostics.ProcessStartInfo startInfo = 
				//	//new System.Diagnostics.ProcessStartInfo("/bin/bash");
				//	new System.Diagnostics.ProcessStartInfo("open");
				//startInfo.WorkingDirectory = "/";
				//startInfo.UseShellExecute = false;
				//startInfo.RedirectStandardInput = true;
				//startInfo.RedirectStandardOutput = true;

				//System.Diagnostics.Process process = new System.Diagnostics.Process();
				//process.StartInfo = startInfo;
				//process.Start();

				//process.StandardInput.WriteLine("say " + command);
				//Debug.Log("say " + command);
				//process.StandardInput.WriteLine("exit");  // if no exit then WaitForExit will lockup your program
				//process.StandardInput.Flush();

				//string line = process.StandardOutput.ReadLine();
				//Debug.Log(line);
				//process.WaitForExit();
				//Debug.Log("!!!!!!??????");
			});
			thread.Start();

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
}
public class DialogUI : Z.DialogUI{}