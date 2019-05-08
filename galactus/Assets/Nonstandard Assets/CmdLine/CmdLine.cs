#define CONNECT_TO_REAL_COMMAND_LINE_TERMINAL
//#define UNKNOWN_CMDLINE_APPEARS
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;
using TMPro;

/// <summary>A Command Line emulation for Unity3D
/// <description>Unliscence - This code is Public Domain, don't bother me about it!</description>
/// <author email="mvaganov@hotmail.com">Michael Vaganov</author>
public class CmdLine : MonoBehaviour {
	#region commands
	/// <summary>watching for commands *about to execute*.</summary>
	public event CommandHandler OnCommand;
	/// <summary>known commands</summary>
	private Dictionary<string, Command> commands = new Dictionary<string, Command>();
	/// <summary>every command can be executed by a different user</summary>
	[Serializable]
	public class Instruction {
		public string text; public object user;
		public bool IsUser(object a_user) { return user == a_user; }
	}
	/// <summary>queue of instructions that this command line needs to execute.</summary>
	private List<Instruction> instructionList = new List<Instruction>();
	private Instruction PopInstruction() {
		if(instructionList.Count > 0) {
			RecentInstruction = instructionList[0];
			instructionList.RemoveAt(0);
			return RecentInstruction;
		}
		return null;
	}
	[Tooltip("Easily accessible way of finding out what instruction was executed last")]
	/// <summary>useful for callbacks, for finding out what is going on right now</summary>
	public Instruction RecentInstruction;
	/// <summary>the user object that should be used for normal input into this CmdLine</summary>
	public object UserRawInput { get { return _tmpInputField; } }

	/// <summary>example of how to populate the command-line with commands</summary>
	public void PopulateWithBasicCommands() {
		//When adding commands, you must add a call below to registerCommand() with its name, implementation method, and help text.
		addCommand("help", (args, user) => {
			log(" - - - -\n" + CommandHelpString() + "\n - - - -");
		}, "prints this help.");
		addCommand("clear", (args, user) => {
			_text = "";
			setText(_text, true);
		}, "clears the command-line terminal.");
		addCommand("echo", (args, user) => {
			println(string.Join(" ", args, 1, args.Length - 1));
		}, "repeat given arguments as output");
		addCommand("load", (args, user) => {
			if(args.Length > 1) {
				if(args[1] == ".") { args[1] = SceneManager.GetActiveScene().name; }
				SceneManager.LoadScene(args[1]);
			} else {
				log("to reload current scene, type <#" + ColorSet.SpecialTextHex + ">load " +
					SceneManager.GetActiveScene().name + "</color>");
			}
		}, "loads given scene. use: load <noparse><scene name></noparse>");
		addCommand("pref", (args, user) => {
			bool didSomething = false;
			if(args.Length > 1) {
				didSomething = true;
				switch(args[1]){
				case "?": case "-?": case "help":
					log("<noparse>pref set <key> [value]       : sets pref value\n"+
					    "pref get [-v] [<key> ...]    : prints pref value\n"+
					    "          -v                 : only return values, no keys\n"+
					    "pref reset                   : clears all pref values</noparse>"); break;
				case "get":
					bool v = false;
					if(Array.IndexOf(args, "-v") >= 0) {
						v = true;
						int i = Array.IndexOf(args, "-v");
						for(; i < args.Length-1; ++i){
							args[i] = args[i + 1];
						}
						Array.Resize(ref args, args.Length - 1);
					}
					for(int i = 2; i < args.Length; ++i) {
						string output = null;
						try { output = PlayerPrefs.GetString(args[i]); if(!v) { output = "\"" + output + "\""; }}
						catch(Exception e) { output = v ? "" : "<#" + ColorSet.ErrorTextHex + ">" + e + "</color>"; }
						if(output == null) { output = v ? "" : "<#" + ColorSet.ErrorTextHex + ">null</color>"; }
						if(v) { log(output); } else { log(args[i] + ":" + output); }
					} break;
				case "set":
					if(args.Length > 2) {
						PlayerPrefs.SetString(args[2], (args.Length > 3) ? args[3] : null);
						PlayerPrefs.Save();
					} else { log("missing arguments"); } break;
				case "reset": PlayerPrefs.DeleteAll(); PlayerPrefs.Save(); break;
				default: didSomething = false; break; }
			}
			if(!didSomething) {
				log("use \"pref ?\" for more details");
			}
		}, "interfaces with player prefs. use \"pref ?\" for more details");
		addCommand("exit", (args, user) => {
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBPLAYER
				Application.OpenURL(webplayerQuitURL);
#else
				Application.Quit();
#endif
		}, "quits this application");
		#region os_commandline_terminal
#if CONNECT_TO_REAL_COMMAND_LINE_TERMINAL
		addCommand("cmd", (args, user) => {
			if(AllowSystemAccess) {
				bash.DoCommand(string.Join(" ", args, 1, args.Length - 1), this, null, this);
			} else {
				HandleLog("Access Denied", "", LogType.Warning);
			}
		}, "access the true system's command-line terminal");
#endif
	}
	public static void DoSystemCommand(string command, object whosAsking = null) {
		bool isNewInstance = _instance == null;
		Instance.doSystemCommand(command, whosAsking);
		if(isNewInstance) { Instance.Interactivity = InteractivityEnum.Disabled; }
	}
#if !CONNECT_TO_REAL_COMMAND_LINE_TERMINAL
	public void doSystemCommand(string command, object whosAsking = null) {
		Debug.LogWarning(whosAsking+" can't do system command '"+command+
			"', #define CONNECT_TO_REAL_COMMAND_LINE_TERMINAL");
	}
#else
	public void doSystemCommand(string command, object whosAsking = null) {
		bash.DoCommand(command, (whosAsking == null) ? bash : whosAsking, null, this);
	}

	private class BASH {
		System.Diagnostics.Process system_process;
		System.Threading.Thread thread;
		private string activeDir = null;
		private string currentCommand = "";
		private DoAfterStringIsRead currentCommand_callback;
		/// the outputs from the bash thread
		private List<string> log, err;
		private bool isInitialized = false;
		/// used to communicate to the CmdLine that the bash thread needs to refresh the prompt
		private bool promptNeedsRedraw = false;
		/// used to communicate to the CmdLine that the bash thread finished something
		private bool probablyFinishedCommand = true;

		public void DoCommand(string s, object whosAsking, DoAfterStringIsRead cb = null, CmdLine cmd = null) {
			if(thread == null) {
				currentCommand = s.Trim();
				currentCommand_callback = cb;
				log = new List<string>();
				err = new List<string>();
				thread = new System.Threading.Thread(delegate () {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || PLATFORM_STANDALONE_WIN
					activeDir = ".";
					string commandName = "cmd";
#else
					activeDir = PWD();
					string commandName = "/bin/bash";
#endif
					system_process = new System.Diagnostics.Process {
						StartInfo = new System.Diagnostics.ProcessStartInfo {
							FileName = commandName,
							Arguments = "",
							UseShellExecute = false,
							RedirectStandardOutput = true,
							RedirectStandardInput = true,
							RedirectStandardError = true,
							CreateNoWindow = true,
							WorkingDirectory = activeDir
						}
					};
					system_process.Start();
					bool ignoreOutput = true;
					system_process.OutputDataReceived += delegate (object sender, System.Diagnostics.DataReceivedEventArgs e) {
						if(ignoreOutput) { return; }
						if(currentCommand_callback == null) {
							log.Add(e.Data);
							probablyFinishedCommand = true;
						} else {
							currentCommand_callback(e.Data);
							currentCommand_callback = null;
						}
					};
					system_process.BeginOutputReadLine();
					bool ignoreErrors = true;
					system_process.ErrorDataReceived += delegate (object sender, System.Diagnostics.DataReceivedEventArgs e) {
						if(ignoreErrors) { return; }
						err.Add(e.Data);
						probablyFinishedCommand = true;
					};
					system_process.BeginErrorReadLine();
					system_process.StandardInput.WriteLine(' '); // force an error, because the StandardInput has a weird character in it to start with
					system_process.StandardInput.Flush();
					long waitForSysproc = 0;
					const long howLongToWaitForSysproc = 100;
					isInitialized = true;
					promptNeedsRedraw = true;
					string lastCommand = null;
					while(true) {
						if(!string.IsNullOrEmpty(currentCommand)) {
							ignoreErrors = false;
							ignoreOutput = false;
							promptNeedsRedraw = true;
							if(currentCommand == "exit") {
								lastCommand = currentCommand;
								break;
							}
							system_process.StandardInput.WriteLine(currentCommand);
							system_process.StandardInput.Flush();
							probablyFinishedCommand = false;
							lastCommand = currentCommand;
							currentCommand = "";
							waitForSysproc = 0;
						}
						System.Threading.Thread.Sleep(10);
						if(waitForSysproc < howLongToWaitForSysproc) {
							waitForSysproc += 10;
							if(waitForSysproc >= howLongToWaitForSysproc) {
								if(lastCommand == "cd") {
									activeDir = PWD();
								}
								if(!probablyFinishedCommand && cmd != null) {
									cmd.NeedToRefreshUserPrompt = true;
								}
								probablyFinishedCommand = true;
							}
						}
					}
					system_process.StandardInput.WriteLine("exit");
					system_process.StandardInput.Flush();
					System.Diagnostics.Process proc = system_process;
					System.Threading.Thread t = thread;
					if(cmd != null) { cmd.NeedToRefreshUserPrompt = true; }
					thread = null;
					system_process = null;
					isInitialized = false;
					proc.WaitForExit();
					proc.Close();
					t.Join(); // should be the last statement
				});
				thread.Start();
			} else {
				if(!string.IsNullOrEmpty(s)) {
					currentCommand = s;
					currentCommand_callback = cb;
				}
			}
		}

		private string COMMAND_LINE_GETTER(string call) {
			System.Diagnostics.Process proc = new System.Diagnostics.Process {
				StartInfo = new System.Diagnostics.ProcessStartInfo {
					FileName = call,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
				}
			};
			proc.Start();
			string r = proc.StandardOutput.ReadLine();
			return r;
		}
		public string PWD() {
			string pwd = COMMAND_LINE_GETTER("pwd");
			return pwd;
		}

		public bool IsProbablyIdle() {
			return (thread == null ||
			(string.IsNullOrEmpty(currentCommand) && probablyFinishedCommand));
		}

		public bool IsInitialized() { return isInitialized; }

		public string MachineName { get { return system_process.MachineName; } }

		public void Update(Instruction inst, CmdLine cmd) {
			bool somethingPrinted = false;
			if(log != null) {
				while(log.Count > 0) {
					cmd.HandleLog(log[0], "", LogType.Log);
					log.RemoveAt(0);
					somethingPrinted = true;
				}
			}
			if(err != null) {
				while(err.Count > 0) {
					cmd.HandleLog(err[0], "", LogType.Error);
					err.RemoveAt(0);
					somethingPrinted = true;
				}
			}
			string s = null;
			if(inst != null) {
				s = inst.text;
				if(s != null) { DoCommand(s, inst.user); }
			}
			if(string.IsNullOrEmpty(s) &&
			string.IsNullOrEmpty(currentCommand) &&
			(somethingPrinted || promptNeedsRedraw)) {
				cmd.NeedToRefreshUserPrompt = true;
			}
			if(cmd.NeedToRefreshUserPrompt) {
				promptNeedsRedraw = false;
			}
		}
	}
	private BASH bash;
#endif
	#endregion // os_commandline_terminal

	/// <param name="command">name of the command to add (case insensitive)</param>
	/// <param name="handler">code to execute with this command, think standard main</param>
	/// <param name="help">reference information, think concise man-pages. Make help <c>"\b"</c> for hidden commands</param>
	public void addCommand(string command, CommandHandler handler, string help) {
		commands.Add(command.ToLower(), new Command(command, handler, help));
	}
	/// <param name="command">name of the command to add (case insensitive)</param>
	/// <param name="handler">code to execute with this command, think standard main</param>
	/// <param name="help">reference information, think concise man-pages. Make help <c>"\b"</c> for hidden commands</param>
	public static void AddCommand(string command, CommandHandler handler, string help) {
		Instance.addCommand(command, handler, help);
	}
	/// <param name="commands">dictionary of commands to begin using, replacing old commands</param>
	public void SetCommands(Dictionary<string, Command> commands) { this.commands = commands; }
	/// <summary>replace current commands with no commands</summary>
	public void ClearCommands() { commands.Clear(); }
	/// <summary>command-line handler. think "standard main" from Java or C/C++.
	/// args[0] is the command, args[1] and beyond are the arguments.</summary>
	public delegate void CommandHandler(string[] args, object whosAsking);
	public class Command {
		public string command { get; private set; }
		public CommandHandler handler { get; private set; }
		public string help { get; private set; }
		public Command(string command, CommandHandler handler, string helpReferenceText) {
			this.command = command; this.handler = handler; this.help = helpReferenceText;
		}
	}
	/// <returns>a list of usable commands</returns>
	public string CommandHelpString() {
		StringBuilder sb = new StringBuilder();
		foreach(Command cmd in commands.Values) {
			if(cmd.help != "\b") // commands with a single backspace as help text are hidden commands 
				sb.Append(((sb.Length > 0) ? "\n" : "") + cmd.command + ": " + cmd.help);
		}
		return sb.ToString();
	}
	/// <summary>Enqueues a command to run, which will be run during the next Update</summary>
	public static void DoCommand(string commandWithArguments, object fromWho = null) {
		bool isNewInstance = _instance == null;
		Instance.EnqueueRun(new Instruction() { text = commandWithArguments, user = fromWho });
		if(isNewInstance) { Instance.Interactivity = InteractivityEnum.Disabled; }
	}
	/// <summary>Enqueues a command to run, which will be run during the next Update</summary>
	/// <param name="instruction">Command string, with arguments.</param>
	public void EnqueueRun(Instruction instruction) {
		instructionList.Add(instruction);
		if(instruction.IsUser(UserRawInput)) {
			indexWherePromptWasPrintedRecently = -1; // make sure this command stays visible
		}
	}
	private void Dispatch(Instruction instruction) {
		if(waitingToReadLine != null) {
			waitingToReadLine(instruction.text);
			waitingToReadLine = null;
		} else if(onInput != null) {
			onInput(instruction.text);
		} else {
			if(string.IsNullOrEmpty(instruction.text)) { return; }
			string s = instruction.text.Trim(Util.WHITESPACE); // cut leading & trailing whitespace
			string[] args = Util.ParseArguments(s).ToArray();
			if(args.Length < 1) { return; }
			if(OnCommand != null) { OnCommand(args, instruction.user); }
			RunDispatcher(args[0].ToLower(), args, instruction.user);
		}
	}
	/// <param name="command">Command.</param>
	/// <param name="args">Arguments. [0] is the name of the command, with [1] and beyond being the arguments</param>
	private void RunDispatcher(string command, string[] args, object user) {
		Command cmd = null;
		// try to find the given command. or the default command. if we can't find either...
		if(!commands.TryGetValue(command, out cmd) && !commands.TryGetValue("", out cmd)) {
			// error!
			string error = "Unknown command '" + NoparseFilterAroundTag(command) + "'";
			if(args.Length > 1) { error += " with arguments "; }
			for(int i = 1; i < args.Length; ++i) {
				if(i > 1) { error += ", "; }
				error += "'" + NoparseFilterAroundTag(args[i]) + "'";
			}
			log(error);
		}
		// if we have a command
		if(cmd != null) {
			// execute it if it has valid code
			if(cmd.handler != null) {
				cmd.handler(args, user);
			} else {
				log("Null command '" + command + "'");
			}
		}
	}
	#endregion // commands
	#region user interface
	public string PromptArtifact = "$ ";
	[Tooltip("the main viewable UI component")]
	private Canvas _mainView;
	public enum InteractivityEnum { Disabled, ScreenOverlayOnly, WorldSpaceOnly, ActiveScreenAndInactiveWorld };
	[SerializeField]
	private InteractivityEnum interactivity = InteractivityEnum.ActiveScreenAndInactiveWorld;
	public InteractivityEnum Interactivity {
		get { return interactivity; }
		set { interactivity = value; SetInteractive(IsInteractive()); }
	}
	[Tooltip("Which key shows the terminal")]
	public KeyCode KeyToActivate = KeyCode.BackQuote;
	[Tooltip("Which key hides the terminal")]
	public KeyCode KeyToDeactivate = KeyCode.Escape;
	[Tooltip("used to size the console Rect Transform on creation as a UI overlay")]
	public RectTransformSettings ScreenOverlaySettings;
	[Tooltip("fill this out to set the console in the world someplace")]
	public PutItInWorldSpace WorldSpaceSettings = new PutItInWorldSpace(0.005f, Vector2.zero);
	[Tooltip("used to color the console on creation")]
	public InitialColorSettings ColorSet = new InitialColorSettings();
	/// <summary>the real workhorse of this commandline system</summary>
	private TMPro.TMP_InputField _tmpInputField;
	/// <summary>used to prevent multiple simultaneous toggles of visibility</summary>
	private bool _togglingVisiblityWithMultitouch = false;
	[Tooltip("If true, will show up and take user input immediately")]
	public bool ActiveOnStart = true;
	[Tooltip("If true, will add every line entered into a queue as a command to execute")]
	public bool AcceptingCommands = true;
#if CONNECT_TO_REAL_COMMAND_LINE_TERMINAL
	public bool AllowSystemAccess = true;
#endif
	public bool overwriteMode = true;
	#region Debug.Log intercept
	[SerializeField, Tooltip("If true, all Debug.Log messages will be intercepted and duplicated here.")]
	private bool interceptDebugLog = false;
	public bool InterceptDebugLog { get { return interceptDebugLog; } set { interceptDebugLog = value; SetDebugLogIntercept(interceptDebugLog); } }
	/// <summary>if this object was intercepting Debug.Logs, this will ensure that it un-intercepts as needed</summary>
	private bool dbgIntercepted = false;

	public void EnableDebugLogIntercept() { SetDebugLogIntercept(InterceptDebugLog); }
	public void DisableDebugLogIntercept() { SetDebugLogIntercept(false); }
	public void SetDebugLogIntercept(bool intercept) {
#if UNITY_EDITOR
		if(!Application.isPlaying) return;
#endif
		if(intercept && !dbgIntercepted) {
			Application.logMessageReceived += HandleLog;
			dbgIntercepted = true;
		} else if(!intercept && dbgIntercepted) {
			Application.logMessageReceived -= HandleLog;
			dbgIntercepted = false;
		}
	}
	private void HandleLog(string logString, string stackTrace = "", LogType type = LogType.Log) {
		const string cEnd = "</color>\n";
		switch(type) {
		case LogType.Error:
			AddText("<#" + ColorSet.ErrorTextHex + ">" + logString + cEnd);
			break;
		case LogType.Exception:
			string c = "<#" + ColorSet.ExceptionTextHex + ">";
			AddText(c + logString + cEnd);
			AddText(c + stackTrace + cEnd);
			break;
		case LogType.Warning:
			AddText("<#" + ColorSet.SpecialTextHex + ">" + logString + cEnd);
			break;
		default:
			log(logString);
			break;
		}
	}
	#endregion // Debug.Log intercept
	public bool NeedToRefreshUserPrompt { get; set; }
	/// used to smartly (perhaps overly-smartly) over-write the prompt when printing things out-of-sync
	private int indexWherePromptWasPrintedRecently = -1;

	public int WriteCursor { get { return _indexWriteCursor; } set {
			if(_indexWriteCursor != value) {
				_indexWriteCursor = value;
				_coordinateWriteCursor = GetCursorCoordinate(_indexWriteCursor);
			}
		} }
	public Vector2Int WriteCoordinate { get { return _coordinateWriteCursor; } set {
			if(_coordinateWriteCursor != value) {
				_coordinateWriteCursor = value;
				SetCursorPosition(_coordinateWriteCursor.y, _coordinateWriteCursor.x);
			}
		}}
	public void context_test() {
		int row = 3, col = 10;
		SetCursorPosition(row, col);
	}
	public Vector2Int CursorCoordinate = new Vector2Int();
	public int CursorPosition = 0;
	[ContextMenuItem("test SetCursorPosition(0,4)", "context_test")]
	public int _indexWriteCursor = -1;
	private Vector2Int _coordinateWriteCursor = new Vector2Int();
	private int indexWhereUserInputStarts = -1;
	private int indexWhereUserInputEnded = -1;
	public bool IsUserInputting() { return indexWhereUserInputStarts >= 0 && indexWhereUserInputEnded >= 0; }
	private string userInputOverwrote = "";

	private const string mainTextObjectName = "MainText";
	[Tooltip("The TextMeshPro font used. If null, built-in-font should be used.")]
	public TMP_FontAsset textMeshProFont;
	public TMP_FontAsset TextMeshProFont {
		get { return textMeshProFont; }
		set {
			textMeshProFont = value;
			if(textMeshProFont != null && _mainView != null) {
				TMP_Text[] texts = _mainView.GetComponentsInChildren<TMP_Text>();
				for(int i = 0; i < texts.Length; ++i) {
					if(texts[i].gameObject.name == mainTextObjectName) {
						texts[i].font = textMeshProFont; break;
					}
				}
			}
		}
	}
	/// <summary>which command line is currently active, and disabling user controls</summary>
	private static CmdLine currentlyActiveCmdLine, disabledUserControls;
	/// <summary>used to check which command line is the best one for the user controlling the main camera</summary>
	private float viewscore;

	[System.Serializable]
	public class InitialColorSettings {
		public Color Background = new Color(0, 0, 0, 0.5f);
		public Color Text = new Color(1, 1, 1);
		public Color ErrorText = new Color(1, .5f, .5f);
		public Color SpecialText = new Color(1, .75f, 0);
		public Color ExceptionText = new Color(1, .5f, 1);
		public Color Scrollbar = new Color(1, 1, 1, 0.5f);
		public Color UserInput = new Color(.75f, .875f, .75f);
		public Color UserSelection = new Color(.5f, .5f, 1, .75f);
		private string _cachedUInputHex; private Color _cachedUInputColor;
		public string UserInputHex { get { if(_cachedUInputColor != UserInput) {
					_cachedUInputHex = Util.ColorToHexCode(UserInput);
					_cachedUInputColor = UserInput; } return _cachedUInputHex; } }
		public string ErrorTextHex { get { return Util.ColorToHexCode(ErrorText); } }
		public string SpecialTextHex { get { return Util.ColorToHexCode(SpecialText); } }
		public string ExceptionTextHex { get { return Util.ColorToHexCode(ExceptionText); } }
	}
	[System.Serializable]
	public class RectTransformSettings {
		public Vector2 AnchorMin = Vector2.zero;
		public Vector2 AnchorMax = Vector2.one;
		public Vector2 OffsetMin = Vector2.zero;
		public Vector2 OffsetMax = Vector2.zero;
	}
	[System.Serializable]
	public class PutItInWorldSpace {
		[Tooltip("If zero, will automatically set to current Screen's pixel size")]
		public Vector2 screenSize = new Vector2(0, 0);
		[Tooltip("how many meters each pixel should be")]
		public float textScale = 0.005f;
		public PutItInWorldSpace(float scale, Vector2 size) {
			this.textScale = scale;
			this.screenSize = size;
		}
		public void ApplySettingsTo(Canvas c) {
			if(screenSize == Vector2.zero) { screenSize = new Vector2(Screen.width, Screen.height); }
			RectTransform r = c.GetComponent<RectTransform>();
			r.sizeDelta = screenSize;
			c.transform.localPosition = Vector3.zero;
			c.transform.localRotation = Quaternion.identity;
			r.anchoredPosition = Vector2.zero;
			r.localScale = Vector3.one * textScale;
		}
	}
	private void PrintPrompt() {
		int indexBeforePrompt = GetRawText().Length;
		if(indexWherePromptWasPrintedRecently != -1) {
			indexBeforePrompt = indexWherePromptWasPrintedRecently;
		}
		string promptText = PromptArtifact;
#if CONNECT_TO_REAL_COMMAND_LINE_TERMINAL
		if(bash.IsInitialized()) {
			promptText = bash.MachineName + PromptArtifact;
		}
#endif
		AddText(promptText);
		indexWherePromptWasPrintedRecently = indexBeforePrompt;
		SetCursorIndex(WriteCursor);
	}
	public bool IsInOverlayMode() {
		return _mainView.renderMode == RenderMode.ScreenSpaceOverlay;
	}
	public void PositionInWorld(Vector3 center, Vector2 size = default(Vector2), float scale = 0.005f) {
		if(size == Vector2.zero) size = new Vector2(Screen.width, Screen.height);
		PutItInWorldSpace ws = new PutItInWorldSpace(scale, size);
		transform.position = center;
		if(_mainView == null) {
			WorldSpaceSettings = ws;
		} else {
			ws.ApplySettingsTo(_mainView);
		}
		RecalculateFontSize();
	}
	private void SetOverlayModeInsteadOfWorld(bool useOverlay) {
		if(useOverlay && _mainView.renderMode != RenderMode.ScreenSpaceOverlay) {
			_mainView.renderMode = RenderMode.ScreenSpaceOverlay;
		} else if(!useOverlay) {
			_mainView.renderMode = RenderMode.WorldSpace;
			WorldSpaceSettings.ApplySettingsTo(_mainView);
			RecalculateFontSize();
		}
	}
	private Canvas CreateUI() {
		_mainView = transform.GetComponentInParent<Canvas>();
		if(!_mainView) {
			_mainView = (new GameObject("canvas")).AddComponent<Canvas>(); // so that the UI can be drawn at all
			_mainView.renderMode = RenderMode.ScreenSpaceOverlay;
			if(!_mainView.GetComponent<CanvasScaler>()) {
				_mainView.gameObject.AddComponent<CanvasScaler>(); // so that text is pretty when zoomed
			}
			if(!_mainView.GetComponent<GraphicRaycaster>()) {
				_mainView.gameObject.AddComponent<GraphicRaycaster>(); // so that mouse can select input area
			}
			_mainView.transform.SetParent(transform);
		}
		GameObject tmpGo = new GameObject("user input");
		tmpGo.transform.SetParent(_mainView.transform);
		Image img = tmpGo.AddComponent<Image>();
		img.color = ColorSet.Background;
		if(ScreenOverlaySettings == null) {
			MaximizeRectTransform(tmpGo.transform);
		} else {
			RectTransform r = tmpGo.GetComponent<RectTransform>();
			r.anchorMin = ScreenOverlaySettings.AnchorMin;
			r.anchorMax = ScreenOverlaySettings.AnchorMax;
			r.offsetMin = ScreenOverlaySettings.OffsetMin;
			r.offsetMax = ScreenOverlaySettings.OffsetMax;
		}
		_tmpInputField = tmpGo.AddComponent<TMP_InputField>();
		_tmpInputField.lineType = TMP_InputField.LineType.MultiLineNewline;
		_tmpInputField.textViewport = _tmpInputField.GetComponent<RectTransform>();
		TextMeshProUGUI tmpText;
#if UNITY_EDITOR
		try {
#endif
			tmpText = (new GameObject(mainTextObjectName)).AddComponent<TextMeshProUGUI>();
#if UNITY_EDITOR
		} catch(System.Exception) {
			throw new System.Exception("Could not create a TextMeshProUGUI object. Did you get default fonts into TextMeshPro? Window -> TextMeshPro -> Import TMP Essential Resources");
		}
#endif
		if(textMeshProFont != null) {
			tmpText.font = textMeshProFont;
		}
		tmpText.fontSize = 20;
		tmpText.transform.SetParent(tmpGo.transform);
		_tmpInputField.textComponent = tmpText;
		_tmpInputField.fontAsset = tmpText.font;
		_tmpInputField.pointSize = tmpText.fontSize;
		MaximizeRectTransform(tmpText.transform);

		tmpGo.AddComponent<RectMask2D>();
		_tmpInputField.onFocusSelectAll = false;
		tmpText.color = ColorSet.Text;
		_tmpInputField.selectionColor = ColorSet.UserSelection;
		_tmpInputField.customCaretColor = true;
		_tmpInputField.caretColor = ColorSet.UserInput;
		_tmpInputField.caretWidth = 5;
		_tmpInputField.ActivateInputField();
		_tmpInputField.onValueChanged.AddListener(listener_OnValueChanged);
		_tmpInputField.onTextSelection.AddListener(listener_OnTextSelectionChange);
		_tmpInputField.characterValidation = TMP_InputField.CharacterValidation.CustomValidator;
		_tmpInputField.inputValidator = GetInputValidator();

		if(_tmpInputField.verticalScrollbar == null) {
			GameObject scrollbar = new GameObject("scrollbar vertical");
			scrollbar.transform.SetParent(_tmpInputField.transform);
			scrollbar.AddComponent<RectTransform>();
			_tmpInputField.verticalScrollbar = scrollbar.AddComponent<Scrollbar>();
			_tmpInputField.verticalScrollbar.direction = Scrollbar.Direction.TopToBottom;
			RectTransform r = scrollbar.GetComponent<RectTransform>();
			r.anchorMin = new Vector2(1, 0);
			r.anchorMax = Vector2.one;
			r.offsetMax = Vector3.zero;
			r.offsetMin = new Vector2(-16, 0);
		}
		if(_tmpInputField.verticalScrollbar.handleRect == null) {
			GameObject slideArea = new GameObject("sliding area");
			slideArea.transform.SetParent(_tmpInputField.verticalScrollbar.transform);
			RectTransform r = slideArea.AddComponent<RectTransform>();
			MaximizeRectTransform(slideArea.transform);
			r.offsetMin = new Vector2(10, 10);
			r.offsetMax = new Vector2(-10, -10);
			GameObject handle = new GameObject("handle");
			Image bimg = handle.AddComponent<Image>();
			bimg.color = ColorSet.Scrollbar;
			handle.transform.SetParent(slideArea.transform);
			r = handle.GetComponent<RectTransform>();
			r.anchorMin = r.anchorMax = Vector2.zero;
			r.offsetMax = new Vector2(5, 5);
			r.offsetMin = new Vector2(-5, -5);
			r.pivot = Vector2.one;
			_tmpInputField.verticalScrollbar.handleRect = r;
			_tmpInputField.verticalScrollbar.targetGraphic = img;
		}
		// an event system is required... if there isn't one, make one
		StandaloneInputModule input = FindObjectOfType(typeof(StandaloneInputModule)) as StandaloneInputModule;
		if(input == null) {
			input = (new GameObject("<EventSystem>")).AddComponent<StandaloneInputModule>();
		}
		// put all UI in the UI layer
		Util.SetLayerRecursive(_mainView.gameObject, LayerMask.NameToLayer("UI"));
		// turn it off and then back on again... that fixes some things.
		tmpGo.SetActive(false); tmpGo.SetActive(true);
		// put it in the world (if needed)
		if(Interactivity == InteractivityEnum.WorldSpaceOnly
		|| Interactivity == InteractivityEnum.ActiveScreenAndInactiveWorld) {
			WorldSpaceSettings.ApplySettingsTo(_mainView);
			RecalculateFontSize();
		}
		return _mainView;
	}
	private float CalculateIdealFontSize(TMP_Text tmpText, float idealCharsPerLine) {
		float normalCharacterWidth = tmpText.font.characterDictionary[(int)'e'].xAdvance;
		float idealFontSize = (WorldSpaceSettings.screenSize.x * tmpText.font.fontInfo.PointSize) / (idealCharsPerLine * normalCharacterWidth);
		return idealFontSize;
	}
	private void RecalculateFontSize() {
		TMP_Text tmpText = _tmpInputField.textComponent;
		tmpText.fontSize = CalculateIdealFontSize(tmpText, commandLineWidth + .125f);
	}
	private static RectTransform MaximizeRectTransform(Transform t) {
		return MaximizeRectTransform(t.GetComponent<RectTransform>());
	}
	private static RectTransform MaximizeRectTransform(RectTransform r) {
		r.anchorMax = Vector2.one;
		r.anchorMin = r.offsetMin = r.offsetMax = Vector2.zero;
		return r;
	}
	public bool IsVisible() {
		return _mainView != null && _mainView.gameObject.activeInHierarchy;
	}
	/// <summary>shows (true) or hides (false).</summary>
	public void SetVisibility(bool visible) {
		if(_mainView == null) {
			ActiveOnStart = visible;
		} else {
			_mainView.gameObject.SetActive(visible);
		}
	}
	/// <param name="enabled">If <c>true</c>, reads from keybaord. if <c>false</c>, stops reading from keyboard</param>
	public void SetInputActive(bool enabled) {
		if(enabled) { _tmpInputField.ActivateInputField(); } else { _tmpInputField.DeactivateInputField(); }
	}
	/// <param name="enableInteractive"><c>true</c> to turn this on (and turn the previous CmdLine off)</param>
	public void SetInteractive(bool enableInteractive) {
		if(_mainView == null && Interactivity != InteractivityEnum.Disabled) {
			CreateUI();
			string o = "";
			//char nbsp = (char)0xA0;
			//o += "  0 1 2 3 4 5 6 7 8 9 A B C D E F\n";
			//for(int row = 0; row < 16; ++row) {
			//	o += row.ToString("X1")+" ";
			//	for(int col = 0; col < 16; ++col){
			//		int c = row * 16 + col;
			//		switch(c){ case 0: case 10: case 13: c = 32; break; }
			//		o += (char)c + " ";
			//	}
			//	o += "\n";
			//}
			if(_text.Length == 0) { log(o+Application.productName + ", v" + Application.version); } 
			else { setText(_text, true); }
		}
		if(_tmpInputField == null) { return; }
		bool activityWhenStarted = _tmpInputField.interactable;
		if(enableInteractive && currentlyActiveCmdLine != null) {
			currentlyActiveCmdLine.SetInteractive(false);
		}
		_tmpInputField.interactable = enableInteractive; // makes focus possible
		switch(Interactivity) {
		case InteractivityEnum.Disabled:
			SetVisibility(false);
			break;
		case InteractivityEnum.ScreenOverlayOnly:
			if(!IsInOverlayMode()) {
				SetOverlayModeInsteadOfWorld(true);
			}
			SetVisibility(enableInteractive);
			break;
		case InteractivityEnum.WorldSpaceOnly:
			if(!IsVisible()) {
				SetVisibility(true);
			}
			if(enableInteractive)
				SetOverlayModeInsteadOfWorld(false);
			break;
		case InteractivityEnum.ActiveScreenAndInactiveWorld:
			//Debug.Log("switching "+ enableInteractive);
			if(!IsVisible()) {
				SetVisibility(true);
			}
			SetOverlayModeInsteadOfWorld(enableInteractive);
			break;
		}
		_tmpInputField.verticalScrollbar.value = 1; // scroll to the bottom
		MoveCaretToEnd(); // move caret focus to end
		SetInputActive(_tmpInputField.interactable); // request/deny focus
		if(enableInteractive) {
			currentlyActiveCmdLine = this;
		} else if(currentlyActiveCmdLine == this) {
			// if this command line has disabled the user
			if(disabledUserControls == currentlyActiveCmdLine) {
				// tell it to re-enable controls
				if(!callbacks.ignoreCallbacks && callbacks.whenThisDeactivates != null) callbacks.whenThisDeactivates.Invoke();
				disabledUserControls = null;
			}
			currentlyActiveCmdLine = null;
		}
	}
	public bool IsInteractive() { return _tmpInputField != null && _tmpInputField.interactable; }
	/// <summary>Moves the caret to the end, clearing all selections in the process</summary>
	public void MoveCaretToEnd() {
		int lastPoint = GetRawText().Length;
		SetCursorIndex(lastPoint);
	}
	#endregion // user interface
	#region input validation
	/// <summary>keep track of command line in a non-mutable place. The input field is otherwise very mutable...</summary>
	private string _text = "";
	private CmdLineValidator inputvalidator;
	//[HideInInspector]
	/// <summary>keeps track of user selection so that the text field can be fixed if selected text is removed</summary>
	public int selectBegin = -1, selectEnd = -1;
	/// <summary>what replaces an attempt to un-escape the TextMeshPro noparse boundary in the command line</summary>
	public const string NOPARSE_REPLACEMENT = ">NOPARSE<";
	/// <summary>flag to move text view to the bottom when content is added</summary>
	private bool showBottomWhenTextIsAdded = false;
	/// <summary>if text is being modified to refresh it after the user did something naughty</summary>
	private bool addingOnChanged = false;
	[Tooltip("Maximum number of lines to retain.")]
	public int maxLines = 99;
	[SerializeField, Tooltip("lines with more characters than this will count as more than one line.")]
	private int commandLineWidth = 80;
	public int CommandLineWidth { get { return commandLineWidth; } set { SetCommandLineWidth(value); }}
	/// last known location of the cursor.
	public string DEBUG_output = "";
	[TextArea(2,8)]
	public string DEBUG_INVISIBLES = "";

	private CmdLineValidator GetInputValidator() {
		if(inputvalidator == null) {
			inputvalidator = ScriptableObject.CreateInstance<CmdLineValidator>();
			inputvalidator.Init(this);
		}
		return inputvalidator;
	}
	private void listener_OnTextSelectionChange(string str, int start, int end) {
		selectBegin = Math.Min(start, end);
		selectEnd = Math.Max(start, end);
	}
	public string BEGIN_USER_INPUT() {
		return "<#" + ColorSet.UserInputHex + "><noparse>";
	}
	public bool HasProperInputTags() {
		List<Substring> tags = tagSubstrings;
		string text = GetAllText();
		return tags.Count >= 2
			&& tags[tags.Count - 1].StartsWith(text, "<noparse")
			&& tags[tags.Count - 2].StartsWith(text, "<#");
	}
	public string END_USER_INPUT() { return "</noparse></color>"; }
	/// <summary>the class that tries to keep the user from wrecking the command line terminal</summary>
	private class CmdLineValidator : TMP_InputValidator {
		public CmdLine cmd;
		private TMP_InputField inputField;
		public void Init(CmdLine cmd) {
			this.cmd = cmd;
			this.inputField = cmd._tmpInputField;
		}
		public void AddUserInput(string userInput) {
			string s = inputField.text;
			int cursor = inputField.caretPosition;
			for(int i = 0; i < userInput.Length; ++i) {
				char c = userInput[i];
				Validate(ref s, ref cursor, c);
			}
			inputField.text = s;
			inputField.caretPosition = cursor;
		}
		int InsertLetter(ref string text, char letter, int index, bool overwrite = true) {
			int cursorMove = 0;
			//Debug.Log("pre "+ cmd.IsUserInputting());
			//cmd.ValidateSubstringLists(text); // @debug
			int totalAdded = 0, overwritten = 0;
			string a = text.Substring(0, index);
			int endIndex = index;
			int indexToEat = -1;
			if(overwrite) {
				// this assumes that CursorCoordinate is at the same spot as index...
				IndexAndAdjustment indexAdjust = cmd.GetPositionIndex(cmd.CursorCoordinate.y, cmd.CursorCoordinate.x + 1);
				if(indexAdjust.adjustment == 0){ // if a regular existing character is at the next spot
					indexToEat = indexAdjust.index-1;
					cmd.userInputOverwrote += text[indexToEat];
					Debug.Log("remembering '" + cmd.userInputOverwrote + "'");
					overwritten = 1;
					cmd.NotifyOfInsertedText(indexToEat, -overwritten);
				} else {
					cmd.userInputOverwrote += ADDED_FOR_NULL_SPACE;
				}
			}
			string b;
			if(indexToEat == -1){
				b = text.Substring(endIndex);
			} else {
				b = text.Substring(endIndex, indexToEat - endIndex) + text.Substring(indexToEat + 1);
			}
			if(!cmd.IsUserInputting()) {
				string headr = cmd.BEGIN_USER_INPUT();
				string footr = cmd.END_USER_INPUT();
				text = a + headr + letter + footr + b;
				cmd.indexWhereUserInputStarts = index + headr.Length;
				cmd.indexWhereUserInputEnded = cmd.indexWhereUserInputStarts;
				cmd.WriteCursor = cmd.indexWhereUserInputStarts;
				cursorMove += headr.Length + 1; // advance outside by the cursor by header and letter, not the footer.
				totalAdded += headr.Length + 1 + footr.Length;

				//string o = cmd.lineSubstrings.Count + " lines [";
				//for(int i = 0; i < cmd.lineSubstrings.Count; ++i){
				//	if(i > 0) o += ", ";
				//	o+= cmd.lineSubstrings[i].index+"->"+cmd.lineSubstrings[i].Limit;
				//}
				//o += "], just added at " + totalAdded+ " at "+index;
				//Debug.Log(o);

				cmd.NotifyOfInsertedText(index, totalAdded);
				cmd.invisibleSubstrings.Add(new Substring { index = index, count = headr.Length });
				cmd.invisibleSubstrings.Add(new Substring { index = index + headr.Length + 1, count = footr.Length });
				cmd.tagSubstrings.Add(new Substring{index = index, count = totalAdded});

				//o = cmd.lineSubstrings.Count + " lines [";
				//for(int i = 0; i < cmd.lineSubstrings.Count; ++i) {
				//	if(i > 0) o += ", ";
				//	o += cmd.lineSubstrings[i].index + "->" + cmd.lineSubstrings[i].Limit;
				//}
				//o += "], just added at " + totalAdded + " at " + index;
				//Debug.Log(o);
			} else {
				if(cmd.indexWhereUserInputStarts == cmd.indexWhereUserInputEnded) {
					string headr = cmd.BEGIN_USER_INPUT();
					int invis = Substring.WhichSubstring(cmd.invisibleSubstrings, index);
					Substring invisTag = cmd.invisibleSubstrings[invis];
					// check if the user input tags have solidified into a single.
					if(invisTag.StartsWith(text, headr) && invisTag.count > headr.Length) {
						// need to break this one big invisible into 2 invisibles
						invisTag.count = headr.Length;
						cmd.invisibleSubstrings[invis] = invisTag;
						string footr = cmd.END_USER_INPUT();
						Substring closingTag = new Substring { index = invisTag.Limit, count = footr.Length };
						cmd.invisibleSubstrings.Insert(invis + 1, closingTag);
						Debug.Log("broke apart "+invisTag.Of(text) + " and " + closingTag.Of(text));
					}
					string o = "invisibles at this line:\n";
					int line = Substring.WhichSubstring(cmd.lineSubstrings, index);
					List<Substring> invisibles = Substring.Overlap(cmd.invisibleSubstrings, cmd.lineSubstrings[line]);
					for(int i = 0; i < invisibles.Count; i++) {
						if(i > 0) o += ", ";
						o += "'" + invisibles[i].Of(text) + "'";
					}
					Debug.Log(o);
				}
				text = a + letter + b;
				totalAdded += 1;
				cursorMove++; // advance by the single letter inserted

				cmd.NotifyOfInsertedText(index, totalAdded);
			}
			//Debug.Log("post");
			cmd.ValidateSubstringLists(text); // @debug
			return cursorMove;
		}
		public int EndUserInput(bool forced) {
			//if(forced)
				//isUserEnteringInput = true;
			string s = inputField.text;
			int returned = EndUserInput(ref s);
			inputField.text = s;
			return returned;
		}
		public bool CheckIfUserInputTagsArePresent(string text) {
			bool isUserEnteringInput = false;
			string beg = cmd.BEGIN_USER_INPUT();
			int len = beg.Length, pos = cmd.GetCursorIndex();
			if(pos >= len) {
				if(cmd.indexWherePromptWasPrintedRecently >= 0 &&
				   text.Substring(cmd.indexWherePromptWasPrintedRecently).Contains(beg)){
					//Debug.Log("have it! " + text.Substring(cmd.indexWherePromptWasPrintedRecently));
					isUserEnteringInput = true;
				} else if(text.Substring(pos - len).Contains(beg)) {
					isUserEnteringInput = true;
				} else {
					// check if the new text has the input tags opened
					isUserEnteringInput = cmd.HasProperInputTags();
				}
			}
			return isUserEnteringInput;
		}
		public int EndUserInput(ref string text) {
			int added = 0;
			if(cmd.IsUserInputting()) {
				string expectedheadr = cmd.BEGIN_USER_INPUT();
				string expectedfootr = cmd.END_USER_INPUT();
				if(cmd.GetUserInputLength() == 0) {
					int len = expectedheadr.Length;
					int whereBeginStarted = cmd.indexWhereUserInputEnded - len;
					if(whereBeginStarted >= 0
					&& text.Substring(whereBeginStarted, expectedheadr.Length) == expectedheadr
					&& text.Substring(cmd.indexWhereUserInputEnded, expectedfootr.Length) == expectedfootr) {
						string a = text.Substring(0, whereBeginStarted);
						string b = text.Substring(cmd.indexWhereUserInputEnded+ expectedfootr.Length);
						text = a + b;
						added -= len;
						cmd.WriteCursor = cmd.indexWhereUserInputStarts - len;
						// TODO adjust all calculated tags?
					}
				} else {
					//int end = cmd.indexWhereUserInputEnded;
					//if(end > text.Length) end = text.Length;
					//string a = text.Substring(0, end);
					//string b = text.Substring(end);
					//string footr = cmd.END_USER_INPUT();
					//text = a + footr + b;
					//added += footr.Length;
					//// TODO adjust all calculated tags?
				}
				cmd.indexWhereUserInputEnded = -1;
				cmd.indexWhereUserInputStarts = -1;
				cmd.userInputOverwrote = "";
				//if(text.EndsWith(expectedheadr)) {
				//	text = text.Substring(0, text.Length - expectedheadr.Length);
				//	added -= expectedheadr.Length;
				//} else {
				//	string footr = cmd.END_USER_INPUT();
				//	if(text.Substring(cmd.indexWhereUserInputEnded).StartsWith(footr)) {
				//		Debug.Log("already ended....");
				//	} else {
				//		cmd.indexWhereUserInputEnded = text.Length;
				//		text += footr;
				//		added += footr.Length;
				//	}
				//}
				cmd._text = text;
			}
			return added;
		}
		public override char Validate(ref string text, ref int pos, char ch) {
			int posAtStart = pos;
			if(!cmd.IsInteractive()) return '\0';
			char letter = '\0';
			if(pos < text.Length) {
				letter = text[pos];
			}
			pos = cmd.GetCursorBackToInput(pos);
			if(ch != '\n' || !cmd.AcceptingCommands) {
				int p = pos;
				pos += InsertLetter(ref text, ch, p, cmd.overwriteMode);
				cmd.indexWhereUserInputEnded = pos;
			}
			// if the user is attempting to break out of noparse...
			if(ch == '>') {
				// check if a tag is being ended
				int startOfTag = text.LastIndexOf('<');
				int endOfTag = text.LastIndexOf('>', text.Length - 2);
				if(startOfTag >= 0 && startOfTag > endOfTag) {
					string possibleTag = text.Substring(startOfTag).ToLower();
					// unescape, incase the user is being trixie with unescape sequences...
					possibleTag = Util.Unescape(possibleTag);
					// and if they are, just don't let them.
					if(possibleTag.Contains("noparse")) {
						text = text.Substring(0, startOfTag) + NOPARSE_REPLACEMENT;
					}
				}
			}
			// if the user wants to execute (because they pressed enter)
			else if(cmd.AcceptingCommands && ch == '\n') {
				object whoExecutes = cmd.UserRawInput; // the user-controlled input field
				string inpt = cmd.GetUserInput(-1); // why off by one? why -1?
				int start = 0, end = -1;
				string footr = cmd.END_USER_INPUT();
				cmd.WriteCursor = cmd.indexWhereUserInputEnded + footr.Length;

				do {
					end = inpt.IndexOf("\n", start);
					if(end >= start && start < inpt.Length) {
						int len = end - start;
						if(len > 0) {
							Debug.Log("enqueue! " + inpt.Substring(start, len));
							cmd.EnqueueRun(new Instruction() { text = inpt.Substring(start, len), user = whoExecutes });
						}
						start = end + 1; // start again after the newline character
					}
					// move one row down...
					if(cmd.WriteCoordinate.y == cmd.lineSubstrings.Count-1) {
						//cmd.AddText("\n", true);
						text += '\n';
						cmd.WriteCursor = text.Length;
					} else {
						Debug.Log("move down to line " + (cmd.WriteCoordinate.y + 1));
						cmd.SetCursorPosition(cmd.WriteCoordinate.y + 1, 0);
					}
				} while(end > 0);
				if(start < inpt.Length) {
					//Debug.Log("sending command [" + inpt.Substring(start) + "]");
					cmd.EnqueueRun(new Instruction() { text = inpt.Substring(start), user = whoExecutes });
				}
				EndUserInput(ref text);
				cmd.indexWhereUserInputStarts = -1;
				cmd.indexWhereUserInputEnded = -1;
				cmd.userInputOverwrote = "";
			}
			// if a bunch of letters were was added (either paste, or new user input)
			if(pos != posAtStart) {// && pos > posAtStart+1) {
				// recalculate invisible string locations.
				cmd.ProcessText(ref text);
			}
			return '\0';
		}
	}
	private void listener_OnValueChanged(string str) {
		if(addingOnChanged) return; // prevent listener_OnValueChanged from being called recursively by setText
		addingOnChanged = true;
		int deletedSomething = 0;
		//string newAddition = Input.inputString;
		//if(newAddition.Length > 0)
		//Debug.Log((int)(newAddition[0]));
		if(str.Length < _text.Length) { deletedSomething = _text.Length - str.Length; }

		// don't allow output text to be modified.
		//if(GetCaretPosition() < nonUserInput.Length) {
		if(deletedSomething > 0) {
			if(GetCursorIndex() < indexWhereUserInputStarts
			|| GetCursorIndex() > indexWhereUserInputEnded
			|| (selectBegin >= 0 && selectEnd >= 0 
			&& !(selectBegin >= indexWhereUserInputStarts && selectEnd < indexWhereUserInputEnded))) {
				//int offset = selectBegin - selectEnd;
				//string alreadyTyped = GetUserInput(offset);
				setText(_text, false);
				//Debug.Log("draining "+alreadyTyped);
				//MoveCaretToEnd();
				//SetCaretPosition(indexWhereUserInputStarts);
				Debug.Log("bad backspace? not: "+ indexWhereUserInputStarts+" >= "+GetCursorIndex()+ " <= "+indexWhereUserInputEnded+"    select: "+selectBegin+"->"+selectEnd);
				SetCursorBackToInput();
			} else {
				indexWhereUserInputEnded -= deletedSomething;
				int howMuchWasReAdded = 0, indexToAddItBack = -1;
				if(overwriteMode && userInputOverwrote.Length > 0) {
					string footr = END_USER_INPUT();
					string whatToPutBack;
					if(deletedSomething >= userInputOverwrote.Length){
						whatToPutBack = userInputOverwrote;
						userInputOverwrote = "";
					} else {
						int whereToSlice = userInputOverwrote.Length - deletedSomething;
						whatToPutBack = userInputOverwrote.Substring(whereToSlice);
						userInputOverwrote = userInputOverwrote.Substring(0, whereToSlice);
					}
					int partToIgnore = whatToPutBack.IndexOf(ADDED_FOR_NULL_SPACE);
					if(partToIgnore >= 0){
						whatToPutBack = whatToPutBack.Substring(0, partToIgnore);
					}
					howMuchWasReAdded += whatToPutBack.Length;
					if(howMuchWasReAdded > 0) {
						//Debug.Log("putting back '" + whatToPutBack + "', deleted total: " + deletedSomething + " readded total: " + howMuchWasReAdded);
						indexToAddItBack = indexWhereUserInputEnded + footr.Length;
						string a = str.Substring(0, indexToAddItBack);
						string b = str.Substring(indexToAddItBack);
						str = a + whatToPutBack + b;
					}
					//else {
					//	Debug.Log("nothing to put back.");
					//}
				}
				NotifyOfInsertedText(indexWhereUserInputStarts, -deletedSomething);
				if(howMuchWasReAdded > 0) {
					NotifyOfInsertedText(indexToAddItBack, howMuchWasReAdded);
				}
				ValidateSubstringLists(str); // @debug
				//nonUserInput = str;
				setText(str, false);
				Debug.Log("accepting backspace?");
			}
		}
		addingOnChanged = false;
	}
	//private void EndUserInputIfNeeded() {
	//	if(GetInputValidator().isUserEnteringInput) {
	//		inputvalidator.isUserEnteringInput = false;
	//		string text = GetAllText();
	//		GetInputValidator().EndUserInput(ref nonUserInput);
	//	}
	//}
	/// <summary>if the given text is a tag, returns the tag with noparse around it.</summary>
	private string NoparseFilterAroundTag(string text) {
		if(text.IndexOf('<') < 0) return text;
		return "<noparse>" + text + "</noparse>";
	}
	public int GetUserInputLength() {
		if(indexWhereUserInputStarts >= 0 && indexWhereUserInputEnded >= 0) {
			return indexWhereUserInputEnded - indexWhereUserInputStarts;
		}return 0;
		//return GetUserInput().Length;
	}
	public string GetUserInput() {
		return GetUserInput(0);
	}
	/// <returns>The user input, which is text that the user has entered</returns>
	private string GetUserInput(int offset) {
		string s = GetRawText();
		if(indexWhereUserInputStarts >= 0 && indexWhereUserInputEnded >= 0) {
			int pulledBack = 0;
			int count = indexWhereUserInputEnded - indexWhereUserInputStarts;
			if(indexWhereUserInputEnded > s.Length) {
				pulledBack = indexWhereUserInputEnded - s.Length;
				count -= pulledBack;
			}
			string userInputText = s.Substring(indexWhereUserInputStarts, count);
			if(pulledBack > 0)
				Debug.Log(userInputText + (pulledBack > 0?("  pulled back "+pulledBack):""));
			return userInputText;
		}
		return "";
		//Debug.LogError("Who is looking for user input before any has been typed?");
		//int len = s.Length - (nonUserInput.Length + offset);
		//if(len > 0) {
		//	s = s.Substring(nonUserInput.Length + offset, len);
		//	string inputHeader = BEGIN_USER_INPUT();
		//	if(s.StartsWith(inputHeader)) { s = s.Substring(inputHeader.Length); }
		//} else { s = ""; }
		//return s;
	}
	public struct Substring : IComparable {
		public int index, count;
		public int Limit { get { return index + count; } }
		public int Middle { get { return index + count / 2; } }
		public bool Contains(int index){ return this.index <= index && index < Limit; }
		public int CompareTo(object obj) {
			Substring other = (Substring)obj;
			if(other.index < index) return 1;
			if(other.index > index) return -1;
			return 0;
		}
		public bool Overlaps(Substring other) {
			return index < other.Limit && other.index < Limit; 
		}
		public string Of(string s) { return s.Substring(index, count); }
		public string OfSafe(string s) {
			int c = count; if(index + c > s.Length) { c = s.Length - index; }
			return s.Substring(index, c); }

		public bool StartsWith(string srcText, string conditionText){
			if(index + conditionText.Length > srcText.Length) { return false; }
			for(int i = 0; i < conditionText.Length; ++i) {
				if(conditionText[i] != srcText[index + i]) { return false; }
			}
			return true;
		}
		public bool EndsWith(string srcText, string conditionText) {
			if(index + conditionText.Length > srcText.Length) { return false; }
			int offset = index + count - conditionText.Length;
			for(int i = 0; i < conditionText.Length; ++i) {
				if(conditionText[i] != srcText[offset + i]) { return false; }
			}
			return true;
		}
		public bool CappedBy(string srcText, string startsWith, string endsWith) {
			return StartsWith(srcText, startsWith) && EndsWith(srcText, endsWith);
		}

		public bool IsValidFor(string s) { return index >= 0 && Limit <= s.Length; }
		public enum NotInSubstringBehavior { fail, nextValid, previousValid };
		/// <returns>index of the first Substring in orderedList where stringIndex is found</returns>
		/// <param name="orderedList">Ordered list.</param>
		/// <param name="stringIndex">String index.</param>
		/// <param name="failIfNotFound">If set to <c>true</c> return -1 if the Substring does not actually contain stringIndex</param>
		public static int WhichSubstring(List<Substring> orderedList, int stringIndex, 
		NotInSubstringBehavior whatIfNotFound = NotInSubstringBehavior.fail){
			Substring s = new Substring { index = stringIndex, count = 0 };
			int index = orderedList.BinarySearch(s);
			if(index < 0) {
				index = ~index;
				if(index > 0 && orderedList[index - 1].Contains(stringIndex)) {
					// in case of overlaps, find the first overlapping substring
					do {
						index = index - 1;
					} while(index > 0 && orderedList[index - 1].Contains(stringIndex));
				} else if(whatIfNotFound == NotInSubstringBehavior.fail) {
					index = -1;
				} else if(whatIfNotFound == NotInSubstringBehavior.previousValid) {
					if(index > 0) index--;
				}
			}
			return index;
		}
		public static int NotifyListOfInsertion(List<Substring> orderedList, int index, int count, bool modifySizeOfSubstring = true) {
			int i = WhichSubstring(orderedList, index, NotInSubstringBehavior.previousValid);
			int whichIndexStartedBeingModified = -1;
			for(; i < orderedList.Count; ++i) {
				Substring s = orderedList[i];
				if(modifySizeOfSubstring && s.index < index && s.Limit >= index) {
					if(whichIndexStartedBeingModified < 0) whichIndexStartedBeingModified = i;
					s.count += count; orderedList[i] = s;
				} else if(s.index >= index){
					if(whichIndexStartedBeingModified < 0) whichIndexStartedBeingModified = i;
					s.index += count; orderedList[i] = s;
				}
			}
			return whichIndexStartedBeingModified;
		}
		public static void PurgeSubstringsAfter(List<Substring> list, int index) {
			for(int i = list.Count - 1; i >= 0; --i) {
				if(list[i].index < index) break;
				list.RemoveAt(i);
			}
		}

		/// <returns>Substrings from orderedList that overlap s</returns>
		/// <param name="orderedList">Ordered list.</param>
		/// <param name="s">S.</param>
		public static List<Substring> Overlap(List<Substring> orderedList, Substring s) {
			List<Substring> found = new List<Substring>();
			int index = WhichSubstring(orderedList, s.index, NotInSubstringBehavior.nextValid);
			if(index >= 0) {
				int needleLimit = s.Limit;
				for(int i = index; i < orderedList.Count; ++i) {
					Substring e = orderedList[i];
					if(e.index >= needleLimit) { break; }
					if(e.Overlaps(s)) { found.Add(e); }
				}
			}
			return found;
		}

		/// <returns>Substrings from orderedList that overlap this Substring</returns>
		public List<Substring> Overlap(List<Substring> orderedList) { return Overlap(orderedList, this); }

		private static int IndexOfPreviousNonoverlapingTag(List<Substring> list, int index){
			if(list.Count == 0) return -1;
			int lastIndex = WhichSubstring(list, index, NotInSubstringBehavior.previousValid);//list.Count - 1;
			while(lastIndex < list.Count && list[lastIndex].Limit < index) {
				lastIndex++;
			}
			if(lastIndex > 0 && (lastIndex == list.Count || list[lastIndex].Limit > index)) { // if we went too far, go back.
				lastIndex--;
			}
			return lastIndex;
		}

		public static bool ClosesTheNextOne(List<Substring> list, Substring s, string text) {
			if(list.Count == 0) return false;
			//int lastIndex = WhichSubstring(list, s.index, NotInSubstringBehavior.previousValid);//list.Count - 1;
			//while(lastIndex < list.Count && list[lastIndex].Limit < s.index) {
			//	lastIndex++;
			//}
			//if(list[lastIndex].Limit > s.index) { // if we went too far, go back.
			//	lastIndex--;
			//}
			int lastIndex = IndexOfPreviousNonoverlapingTag(list, s.index);
			//if(list[lastIndex].index < s.index && list[lastIndex].Limit > s.index) { Debug.LogError("uh oh.... overlap"); }
			// check if the previous invisible-tag ends with the opening tag that starts this new invisible-tag
			Substring prev = list[lastIndex];
			int begin = -1;//text.LastIndexOf("<", prev.Limit);
			for(begin = prev.Limit - 1; begin >= 0; --begin) { if(text[begin] == '<') { break; } }
			int end = text.IndexOf(">", begin);
			if(begin < 0 || end < 0) {
				if(begin < 0) Debug.Log("could not find beginning tag for '" + prev.Of(text) + "'");
				if(end < 0) Debug.Log("could not find ending tag for '"+prev.Of(text)+"'");
				throw new Exception("malformed invisible string");
			} // @debug
			begin++;
			if(end+1 != prev.Limit) {
				Debug.Log("begin " + begin + " " + text[begin]);
				Debug.Log(""+(end)+" "+text[end]+"      "+prev.Limit);
				throw new Exception("malformed invisible string... for '" + prev.Of(text) + "'");
			} // @debug
			int optionalEnd = text.IndexOf(" ", begin);
			if(optionalEnd >= 0 && optionalEnd < end) { end = optionalEnd; }
			string tagEndingPrev = text.Substring(begin, end-begin);
			begin = s.index + 2; // skip the '</'
			end = text.IndexOf(">", begin);
			optionalEnd = text.IndexOf(" ", begin);
			if(optionalEnd >= 0 && optionalEnd < end) { end = optionalEnd; }
			string tagBeginNext = text.Substring(begin, end-begin);
			// check if the tags are the same. if they are, a body is expected between them.
			bool tagsAreTheSame = tagEndingPrev == tagBeginNext 
			|| (tagBeginNext == "color" && tagEndingPrev.StartsWith("#"));
			//if(tagsAreTheSame) {
			//	Debug.Log("not going to merge " + tagBeginNext + " and " + tagEndingPrev);
			//} else {
			//	Debug.Log("merging " + tagBeginNext + " " + tagEndingPrev);
			//}
			return tagsAreTheSame;
		}

		public static void AddToOrderedListMerge(List<Substring> list, Substring s, bool allowMerge) {
			//int lastIndex = WhichSubstring(list, s.index, NotInSubstringBehavior.previousValid);//list.Count - 1;
			//while(lastIndex < list.Count && list[lastIndex].Limit < s.index) {
			//	lastIndex++;
			//}
			//if(lastIndex > 0 && (lastIndex == list.Count || list[lastIndex].Limit > s.index)) { // if we went too far, go back.
			//	lastIndex--;
			//}
			int lastIndex = IndexOfPreviousNonoverlapingTag(list, s.index);
			if(allowMerge && lastIndex >= 0 && list[lastIndex].Limit == s.index) { // if it needs to go to the end
				Substring prev = list[lastIndex];
				prev.count += s.count;
				list[lastIndex] = prev;
			} else {
				// where does it go
				int index = list.BinarySearch(s);
				// if it is exactly a current substring, that is probably an error...
				if(index >= 0) {
					throw new Exception("Adding a replacement? There is already a substring ["+index+"] at "+s.index);
				}
				index = ~index;
				// if it is not the last element, that's also probably an error...
				if(index < list.Count) {
					throw new Exception("Inserting into the middle of the list? [" + index + "] should be at " + s.index);
				}
				// put it at the end
				list.Add(s);
			}
		}
		public void AddToOrderedListMerge(List<Substring> list, string text) {
			AddToOrderedListMerge(list, this, ClosesTheNextOne(list, this, text));
		}
	}

	private int FirstOneNotCapped(List<Substring> list, string srcText, string start, string end) {

		for(int i = 0; i < list.Count; ++i) {
			if(!list[i].CappedBy(srcText, start, end)) { return i; }
		}
		return -1;
	}

	public bool ValidateSubstringLists(string text = null) {
		if(text == null) { text = GetAllText(); }
		int index;
		index = FirstOneNotCapped(tagSubstrings, text, "<", ">");
		if(index >= 0) { Debug.LogError("tag failed: " + tagSubstrings[index].Of(text)); return false; }
		index = FirstOneNotCapped(invisibleSubstrings, text, "<", ">");
		if(index >= 0) { Debug.LogError("invisibles failed: " + invisibleSubstrings[index].Of(text)); return false; }
		index = FirstOneNotCapped(insertedLinebreaks, text, "<", ">");
		if(index >= 0) { Debug.LogError("insertedLinebreaks failed: " + insertedLinebreaks[index].Of(text)); return false; }
		for(int i = 0; i < lineSubstrings.Count; ++i) {
			int limit = lineSubstrings[i].Limit;
			if(!(limit == text.Length || text[limit] == '\n')){
				if(limit < text.Length) { Debug.LogError("Expected newline, found [" + text[limit] + "]"); } 
				else { Debug.LogError("Line expects ("+(limit-text.Length)+") too many characters"); }
				Debug.LogError("line "+i+ " failed: " + lineSubstrings[i].Of(text));
				DebugPrintSubstrings(text);
				Debug.Log(text);
				return false;
			}
		}
		return true;
	}

	public void NotifyOfInsertedText(int index, int count, bool debugOuput = false) {
		int l = Substring.NotifyListOfInsertion(lineSubstrings, index, count);
		int t = Substring.NotifyListOfInsertion(tagSubstrings, index, count);
		int i = Substring.NotifyListOfInsertion(invisibleSubstrings, index, count, false);
		int b = Substring.NotifyListOfInsertion(insertedLinebreaks, index, count, false);
		if(debugOuput) { Debug.Log("moved: tag" + t + " invis" + i + " line" + l + " break" + b); } // @debug
	}

	private void DebugPrintSubstrings(string str = null){
		if(str == null) {
			str = GetAllText();
		}
		string outp = "{"+str.Length+"}";
		if(insertedLinebreaks.Count > 0) {
			outp += "hard linebreaks [";
			for(int j = 0; j < insertedLinebreaks.Count; j++) {
				if(j > 0) outp += ", ";
				outp += insertedLinebreaks[j].index;
			}
			outp += "]\n";
		}
		for(int i = 0; i < lineSubstrings.Count; ++i) {
			Substring s = lineSubstrings[i];
			outp += s.index + "->" + s.Limit + ": ";
			outp += "\"" + lineSubstrings[i].Of(str) + "\"";
			List<Substring> invisibles = s.Overlap(invisibleSubstrings);
			if(invisibles.Count > 0) {
				outp += "\n  invisible [";
				for(int j = 0; j < invisibles.Count; j++) {
					if(j > 0) outp += ", ";
					outp += "\""+invisibles[j].Of(str)+"\"";
				}
				outp += "]";
			}
			List<Substring> tags = s.Overlap(tagSubstrings);
			if(tags.Count > 0){
				outp += "\n  tags [";
				for(int j = 0; j < tags.Count; j++) {
					if(j > 0) outp += ", ";
					string t = "error";
					if(tags[j].IsValidFor(str)) {
						t = tags[j].Of(str);
						bool isComplete = IsCompleteTag(str, tags[j]);
						t = t.Substring(0, t.IndexOf('>') + 1);
						outp += "\"" + t + "\" "+(isComplete?"":"INCOMPLETE");
					} else {
						t += tags[j].index + "->" + tags[j].Limit + " vs " + str.Length;
						outp += t;
					}
				}
				outp += "]";
			}
			outp += "\n";
		}
		Debug.Log(outp);
	}

	// TODO GetVisibleText(), which strips out the invisible text
	// TODO ConvertRealIndexToVisibleIndex(int realIndex)
	// TODO ConvertVisibleIndexToRealIndex(int visibleIndex)
	public List<Substring> tagSubstrings = new List<Substring>();
	public List<Substring> invisibleSubstrings = new List<Substring>();
	public List<Substring> lineSubstrings = new List<Substring>();
	public List<Substring> insertedLinebreaks = new List<Substring>();

	public Vector2Int GetCursorCoordinate(int index) {
		Vector2Int coord = new Vector2Int();
		int row = Substring.WhichSubstring(lineSubstrings, index, Substring.NotInSubstringBehavior.previousValid);
		coord.y = row;
		if(row >= 0 && row < lineSubstrings.Count+1) {
			Substring line;
			string text = GetAllText();
			if(row == lineSubstrings.Count) {
				int start = 0;
				if(lineSubstrings.Count > 0) {
					start = lineSubstrings[lineSubstrings.Count - 1].index;
				}
				line = new Substring { index = start, count = text.Length - start };
			} else {
				line = lineSubstrings[row];
			}
			DEBUG_output = line.OfSafe(text);

			int cursorIndex = index - line.index;
			char cursor = '|';
			if(cursorIndex > DEBUG_output.Length) { cursor = '?'; cursorIndex = DEBUG_output.Length; }
			string a = DEBUG_output.Substring(0, cursorIndex);
			string b = DEBUG_output.Substring(cursorIndex);
			DEBUG_output = a + cursor + b;

			List<Substring> invisibles = Substring.Overlap(invisibleSubstrings, line);
			if(invisibles.Count == 0) {
				coord.x = index - line.index;
			} else {
				coord.x = 0;
				// skip to the first relevant invisible tag
				int i = line.index, invisiter = 0;
				int safeiter = 0;
				Substring invis = invisibles[invisiter];
				while(line.index > invis.Limit || invis.Contains(i)){
					i = invis.Limit;
					invisiter++;
					if(invisiter < invisibles.Count){
						invis = invisibles[invisiter];
					} else {
						invis = new Substring { index = line.Limit, count = 1 };
					}
					if(safeiter++ > 1000) { Debug.Log("~oof"); break; }
				}
				// advance until the correct index is found
				safeiter = 0;
				int delta;
				while(i < index){
					if (invis.index > index) {
						delta = index - i;
						i += delta;
					} else {
						delta = invis.index - i;
						i = invis.Limit;
						invisiter++;
						if(invisiter < invisibles.Count) {
							invis = invisibles[invisiter];
						} else {
							invis = new Substring { index = line.Limit, count = 1 };
						}
					}
					coord.x += delta;
					if(safeiter++ > 1000) { Debug.Log("~ouchie "+invisiter); break; }
				}
			}
		}
		return coord;
	}
	private const char ADDED_FOR_NULL_SPACE = '\b';
	private const char FILLED_IN_NULL_SPACE = (char)(0xA0);
	public void SetCursorPosition(int r, int c) {
		Debug.Log("setpos " + r + " " + c);
		IndexAndAdjustment indexAdjust = GetPositionIndex(r, c);
		if(indexAdjust.adjustment != 0) {
			Debug.Log("need to add " + indexAdjust.adjustment);
			StringBuilder addition = new StringBuilder();
			for(int i = 0; i < indexAdjust.adjustment; ++i) {
				addition.Append(FILLED_IN_NULL_SPACE); // nonbreaking spaces
			}
			NotifyOfInsertedText(indexAdjust.index, indexAdjust.adjustment);
			string text = GetAllText();
			string a = text.Substring(0, indexAdjust.index);
			string b = text.Substring(indexAdjust.index);
			text = a + addition + b;
			_text = text;
			setText(_text);
		}
		_indexWriteCursor = indexAdjust.index + indexAdjust.adjustment;
		_coordinateWriteCursor = new Vector2Int { x = c, y = r };
		SetCursorIndex(_indexWriteCursor);
	}
	public struct IndexAndAdjustment { public int index, adjustment; }
	public IndexAndAdjustment GetPositionIndex(int r, int c) {
		Substring line = lineSubstrings[r];
		return GetPositionIndex(line, c);
	}
	public IndexAndAdjustment GetPositionIndex(Substring line, int c) {
		IndexAndAdjustment result = new IndexAndAdjustment{ index = -1, adjustment = 0};
		List<Substring> invisibles = Substring.Overlap(invisibleSubstrings, line);
		if(invisibles.Count == 0) {
			//Debug.Log("no invisibles. this is easy (?)  headed to char "+c);
			if(line.count > c){
				result.index = line.index + c;
				result.adjustment = 0;
				//Debug.Log("no adds needed: "+result.index + " " + result.adjustment);
			} else {
				result.index = line.Limit;
				result.adjustment = c - line.count;
				//Debug.Log("add neeed: "+result.index + " " + result.adjustment);
			}
		} else {
			// skip to the first relevant invisible tag
			int i = line.index, invisiter = 0;
			int safeiter = 0;
			Substring invis = invisibles[invisiter];
			while(line.index > invis.Limit || invis.Contains(i)) {
				i = invis.Limit;
				invisiter++;
				if(invisiter < invisibles.Count) {
					invis = invisibles[invisiter];
				} else {
					invis = new Substring { index = line.Limit, count = 1 };
				}
				if(safeiter++ > 1000) { Debug.Log("oof!"); break; }
			}
			// advance until the closest invisible index is found
			safeiter = 0;
			int delta;
			int columnsNeeded = c;
			while(columnsNeeded > 0) {
				if(invis.index-i >= columnsNeeded) { //  if the next invisible column happens in more units than is needed
					//delta = index - i;
					i += columnsNeeded;
					columnsNeeded = 0;
					result.index = i;
					result.adjustment = 0;
					Debug.Log("in range.");
					break;
				} else { // there is an invisible tag in the way...
					delta = invis.index - i;
					if(delta < 0){
						Debug.Log("woah. " + delta);
						result.index = i + delta;
						result.adjustment = columnsNeeded + delta;
						break;
					}
					columnsNeeded -= delta;
					i = invis.Limit;
					invisiter++;
					if(invisiter < invisibles.Count) { // if there are more invisibles to jump over, get the next one
						invis = invisibles[invisiter];
					} else if (invisiter == invisibles.Count) { // if no more invisibles, create one marking the end-of-line
						invis = new Substring { index = line.Limit, count = 0 };
					} else { // if more columns are needed, mark the index, and the adjustment necessary
						result.index = i;
						result.adjustment = columnsNeeded;
						columnsNeeded = 0;
						break;
					}
				}
				if(safeiter++ > 1000) { Debug.Log("ouchie! " + i+ " " + columnsNeeded); break; }
			}
		}
		return result;
	}

	public int WhichInvisibleSubstring(int stringIndex) {
		return Substring.WhichSubstring(invisibleSubstrings, stringIndex);
	}

	bool IsCompleteTag(string entireText, Substring s) {
		//string text = s.Of(entireText);
		if(entireText[s.index] != '<') throw new Exception("expected tag starting substring"); // debug only
		if(entireText[s.Limit-1] != '>') return false;
		int index = entireText.IndexOf('>', s.index);
		if(index >= s.Limit) return false;
		if(index < 0) throw new Exception("expected COMPLETE tag starting substring"); // debug only
		int startOfStartTagName = s.index + 1;
		string t = entireText.Substring(startOfStartTagName, index- startOfStartTagName);
		if(Array.IndexOf(Util.singleTagsTMP, t) >= 0) return true;
		if(t.StartsWith("#")) { t = "color"; }
		int endtStart = s.Limit - 1 - (t.Length);
		if(endtStart < startOfStartTagName) return false;
		string endt = entireText.Substring(endtStart, t.Length);
		if(endt == t) return true;
		return false;
	}

	private void ForceLinebreakAt(int currentIndex, ref string str) {
		string a = str.Substring(0, currentIndex), b = str.Substring(currentIndex);
		str = a + "\n" + b;
		Substring s = new Substring() { index = currentIndex, count = 1 };
		//Debug.Log("added "+currentIndex+" [" + s.Of(str) + "]");
		Substring.NotifyListOfInsertion(tagSubstrings, currentIndex, 1);
		Substring.NotifyListOfInsertion(invisibleSubstrings, currentIndex, 1);
		Substring.NotifyListOfInsertion(tagSubstrings, currentIndex, 1);
		Substring.NotifyListOfInsertion(insertedLinebreaks, currentIndex, 1);
		insertedLinebreaks.Add(s);
	}

	private void SetCommandLineWidth(int newWidth) {
		commandLineWidth = newWidth;
		UndoLinebreaks();
		setText(_text);
		RecalculateFontSize();
	}
	private bool UndoLinebreaks() {
		if(insertedLinebreaks.Count > 0) {
			StringBuilder withoutLinebreaks = new StringBuilder();
			int index = 0;
			// TODO user input needs to obey the laws of linebreaks
			string str = GetAllText();
			for(int i = 0; i < insertedLinebreaks.Count; ++i) {
				withoutLinebreaks.Append(str.Substring(index, insertedLinebreaks[i].index-index));
				//Debug.Log("removing [" + insertedLinebreaks[i].Of(str)+"] at "+insertedLinebreaks[i].index);
				index = insertedLinebreaks[i].Limit;
			}
			withoutLinebreaks.Append(str.Substring(index, str.Length - index));
			insertedLinebreaks.Clear();
			_text = withoutLinebreaks.ToString();
			return true;
		}
		return false;
	}

	private void ProcessText(ref string str) {
		//string str = GetAllText();
		//Debug.Log("CLEARING");
		invisibleSubstrings.Clear(); // contiguous invisible tags - 
		lineSubstrings.Clear(); // line beginnings and endings
		tagSubstrings.Clear(); // open and closing of tags
		// before cleaing linebreaks, check if the ones in here are still valid...
		for(int i = insertedLinebreaks.Count-1; i >= 0; i--) {
			if(!insertedLinebreaks[i].IsValidFor(str)
			|| insertedLinebreaks[i].Of(str) != "\n") { // only remove the invalid linebreak entries
				Debug.Log("removing bad break at " + insertedLinebreaks[i].index);
				insertedLinebreaks.RemoveAt(i);
			}
		}
		int endIndex, trueTokenLength;
		int lineStart = 0, visibleColumns = 0;
		StringBuilder visibleOnes = new StringBuilder();
		int noparseTagIndex = -1;
		for(int currentIndex = 0; currentIndex < str.Length; ++currentIndex) {
			if(currentIndex >= str.Length || currentIndex < 0) {
				Debug.Log("hmmm. trying to get char "+currentIndex+" out of "+str.Length);
				break;
			}
			char c = str[currentIndex];
			bool parsedAToken = false;
			if(c == '<') {
				endIndex = str.IndexOf('>', currentIndex);
				trueTokenLength = endIndex - currentIndex + 1; // +1 includes the last '>'
				string token = null;
				if(endIndex > 0) {
					// just get the starting token, ignore properties after the first space
					int space = str.IndexOf(' ', currentIndex);
					if(space >= 0 && space < endIndex) { endIndex = space; }
					token = str.Substring(currentIndex + 1, endIndex - (currentIndex + 1));
					endIndex++;
					if(token.Trim().Length == 0) { token = null; }
				}
				if(token != null && token.Trim() == "noparse") {
					if(noparseTagIndex < 0) { // ignore double-noparse tags
						Substring s = new Substring { index = currentIndex, count = trueTokenLength };
						Substring.AddToOrderedListMerge(invisibleSubstrings, s, true);
						//Substring noparseS = new Substring { index = currentIndex, count = str.Length - currentIndex };
						tagSubstrings.Add(s);
						noparseTagIndex = tagSubstrings.Count - 1;
						parsedAToken = true;
					}
					token = null;
				}
				if(token == "/noparse") {
					if(noparseTagIndex < 0){
						Debug.LogError("WOAH! noparse ended but there is no noparse beginning!");
					}
					Substring noparseTag = tagSubstrings[noparseTagIndex];
					Substring s = new Substring { index = currentIndex, count = trueTokenLength };
					Substring.AddToOrderedListMerge(invisibleSubstrings, s, false); // don't merge noparse, leave room for a body
					noparseTag.count = endIndex - noparseTag.index;
					tagSubstrings[noparseTagIndex] = noparseTag;
					parsedAToken = true;
					token = null;
					noparseTagIndex = -1;
				}
				if(noparseTagIndex < 0 && token != null && token[0] != '#' && token[0] != '/' && Array.IndexOf(Util.tagsTMPallowed, token) < 0) {
					Debug.LogWarning("Probably erroneous tag '" + token + "' found at index " + currentIndex);
					if(Array.IndexOf(Util.tagsTMP, token) >= 0) {
						Debug.LogWarning("The developer of CmdLine is explicitly not encouraging the use of '" + token + "' in the text");
					}
				}
				if(noparseTagIndex < 0 && token != null) {
					Substring s = new Substring { index = currentIndex, count = trueTokenLength };
					Substring.AddToOrderedListMerge(invisibleSubstrings, s,
					                                Substring.ClosesTheNextOne(invisibleSubstrings, s, str));
					parsedAToken = true;
				}
				if(noparseTagIndex < 0 && token != null) {
					if(token.StartsWith("/") && tagSubstrings.Count > 0) {
						//int whichTag = tagSubstrings.LastIndexOf(token.Substring(1));
						int whichTag = -1;
						if(token == "/color") {
							//Debug.Log("found /color");
							for(int e = tagSubstrings.Count - 1; e >= 0; --e) {
								//if(tagSubstrings[e].StartsWith("#")) {
								Substring s = tagSubstrings[e];
								if(str[s.index + 1] == '#') {
									//Debug.Log("found color at " + s.index+" "+s.Of(str));
									whichTag = e; break;
								}
							}
						} else {
							string endsWhat = token.Substring(1);
							for(int j = tagSubstrings.Count - 1; j >= 0; j--) {
								Substring s = tagSubstrings[j];
								if(str.Substring(s.index + 1, endsWhat.Length) == endsWhat) {
									whichTag = j;
								}
							}
						}
						if(whichTag >= 0) {
							Substring s = tagSubstrings[whichTag];
							s.count = endIndex - s.index;
							tagSubstrings[whichTag] = s;
							token = null;
							parsedAToken = true;
						} else {
							Debug.LogWarning("Unexpected closing tag " + token + " found at index " + currentIndex);
						}
					} else if(token.EndsWith("/") || Array.IndexOf(Util.singleTagsTMP, token) >= 0) {
						if(lineSubstrings != null && token == "br") {
							Debug.LogWarning("TODO finish br tag support");
							Substring s = new Substring { index = lineStart, count = currentIndex - lineStart };
							lineSubstrings.Add(s);
							if(s.count < 0) { Debug.Log("BAD!!"); }
							lineStart = endIndex;
							visibleOnes.Append("---" + visibleColumns + '\n');
							visibleColumns = 0;
						}
						parsedAToken = true;
						token = null; // don't include single-tags in the tags list
					}
				}
				if(noparseTagIndex < 0 && token != null) {
					parsedAToken = true;
					Substring s = new Substring { index = currentIndex, count = endIndex - currentIndex + 1 };
					tagSubstrings.Add(s);
				}
				// ------
				if(parsedAToken && endIndex > 0) {
					currentIndex = endIndex-1;
				}
			}
			if(!parsedAToken) {
				bool isVisibleChar = ("\n\r\a".IndexOf(c) < 0);
				if(isVisibleChar) { visibleColumns++; } // count visible characters
				if(commandLineWidth > 0 && isVisibleChar && visibleColumns == commandLineWidth+1) { // add a line break at the 80th character
					//Debug.Log("crossed the limit on line " + lineSubstrings.Count + " with char " + c+ " at "+currentIndex+" visible "+ visibleColumns);
					ForceLinebreakAt(currentIndex, ref str);
					c = '\n';
					visibleOnes.Append("~~~" + visibleColumns + '\n');
					visibleColumns--;
					isVisibleChar = false;
				}
				if(isVisibleChar) {
					visibleOnes.Append(currentIndex+"_"+c+" ");
				}
				if(c == '\n') {
					if(lineSubstrings != null) {
						Substring s = new Substring { index = lineStart, count = currentIndex - lineStart };
						lineSubstrings.Add(s);
						if(s.count < 0) { Debug.Log("BAD!!!"); }
					}
					lineStart = currentIndex + 1;
					visibleOnes.Append("!!!"+visibleColumns+'\n');
					visibleColumns = 0;
				}
			}
		}
		lineSubstrings.Add(new Substring { index = lineStart, count = str.Length - lineStart });
		//DebugPrintSubstrings(str);
		//Debug.Log(visibleOnes.ToString());
		if(str != _text) {
			_text = str;
			SetRawText(str);
		}
	}

	// TODO find where ProcessText is necessary, and where it can be skipped. this is a potentially huge CPU cost.
	/// <param name="text">What the the output text should be (turns current user input into text output)</param>
	public void setText(string text, bool reprocessText = true) {
		// add linebreaks at line limits, and mark those as invisible tags
		_text = text;
		if(reprocessText) {
			ProcessText(ref _text);
		}
		SetRawText(_text);
		// if text is replaced during input, this refreshes tags around input
		if(inputvalidator != null) {
			inputvalidator.CheckIfUserInputTagsArePresent(text);
		}
	}
	#endregion // input validation
	#region singleton
	/// <summary>the singleton instance. One will be created if none exist.</summary>
	private static CmdLine _instance;
	public static CmdLine Instance {
		get {
			if(_instance == null && (_instance = FindObjectOfType(typeof(CmdLine)) as CmdLine) == null) {
				GameObject g = new GameObject();
				_instance = g.AddComponent<CmdLine>();
				g.name = "<" + _instance.GetType().Name + ">";
#if UNITY_EDITOR && UNKNOWN_CMDLINE_APPEARS
				_instance.whereItWasStarted = Environment.StackTrace;
#endif
			}
			return _instance;
		}
	}
#if UNITY_EDITOR && UNKNOWN_CMDLINE_APPEARS
	public string whereItWasStarted;
#endif
	#endregion // singleton
	#region static utility functions
	public static class Util {
		/// <param name="layer">what Unity layer to set the given object, and all child objects, recursive</param>
		public static void SetLayerRecursive(GameObject go, int layer) {
			go.layer = layer;
			for(int i = 0; i < go.transform.childCount; ++i) {
				Transform t = go.transform.GetChild(i);
				if(t != null) {
					SetLayerRecursive(t.gameObject, layer);
				}
			}
		}
		public static string[] singleTagsTMP = { "br", "page" };
		public static string[] tagsTMP = { "align", "alpha", "b", "br", "color", "cspace", "font", "i", "indent", "line-height", "line-indent", "link", "lowercase", "margin", "mark", "mspace", "noparse", "nobr", "page", "pos", "size", "space", "sprite", "s", "smallcaps", "style", "sub", "sup", "u", "uppercase", "voffset", "width" };
		public static string[] tagsTMPallowed = { "alpha", "b", "br", "color", "font", "i", "link", "lowercase", "mark", "noparse", "nobr", "page", "sprite", "s", "style", "u", "uppercase"};

		public static string ColorToHexCode(Color c) {
			int r = (int)(255 * c.r), g = (int)(255 * c.g), b = (int)(255 * c.b), a = (int)(255 * c.a);
			return r.ToString("X2") + g.ToString("X2") + b.ToString("X2") + ((c.a != 1) ? a.ToString("X2") : "");
		}
		public static readonly char[] QUOTES = { '\'', '\"' }, WHITESPACE = { ' ', '\t', '\n', '\b', '\r' };
		/// <returns>index of the end of the token that starts at the given index 'i'</returns>
		public static int FindEndArgumentToken(string str, int i) {
			bool isWhitespace;
			do {
				isWhitespace = System.Array.IndexOf(WHITESPACE, str[i]) >= 0;
				if(isWhitespace) { ++i; }
			} while(isWhitespace && i < str.Length);
			int index = System.Array.IndexOf(QUOTES, str[i]);
			char startQuote = (index >= 0) ? QUOTES[index] : '\0';
			if(startQuote != '\0') { ++i; }
			while(i < str.Length) {
				if(startQuote != '\0') {
					if(str[i] == '\\') {
						i++; // skip the next character for an escape sequence. just leave it there.
					} else {
						index = System.Array.IndexOf(QUOTES, str[i]);
						bool endsQuote = index >= 0 && QUOTES[index] == startQuote;
						if(endsQuote) { i++; break; }
					}
				} else {
					isWhitespace = System.Array.IndexOf(WHITESPACE, str[i]) >= 0;
					if(isWhitespace) { break; }
				}
				i++;
			}
			if(i >= str.Length) { i = str.Length; }
			return i;
		}
		/// <returns>split command-line arguments</returns>
		public static List<string> ParseArguments(string commandLineInput) {
			int index = 0;
			List<string> tokens = new List<string>();
			while(index < commandLineInput.Length) {
				int end = FindEndArgumentToken(commandLineInput, index);
				if(index != end) {
					string token = commandLineInput.Substring(index, end - index).TrimStart(WHITESPACE);
					token = Unescape(token);
					int qi = System.Array.IndexOf(QUOTES, token[0]);
					if(qi >= 0 && token[token.Length - 1] == QUOTES[qi]) {
						token = token.Substring(1, token.Length - 2);
					}
					tokens.Add(token);
				}
				index = end;
			}
			return tokens;
		}
		/* https://msdn.microsoft.com/en-us/library/aa691087(v=vs.71).aspx */
		private static readonly SortedDictionary<char, char> EscapeMap = new SortedDictionary<char, char> {
		{ '0','\0' }, { 'a','\a' }, { 'b','\b' }, { 'f','\f' }, { 'n','\n' }, { 'r','\r' }, { 't','\t' }, { 'v','\v' } };
		/// <summary>convenience method to un-escape standard escape sequence strings</summary>
		/// <param name="escaped">Escaped.</param>
		public static string Unescape(string escaped) {
			if(escaped == null) { return escaped; }
			StringBuilder sb = new StringBuilder();
			bool inEscape = false;
			int startIndex = 0;
			for(int i = 0; i < escaped.Length; i++) {
				if(!inEscape) {
					inEscape = escaped[i] == '\\';
				} else {
					char c;
					if(!EscapeMap.TryGetValue(escaped[i], out c)) {
						c = escaped[i]; // unknown escape sequences are literals
					}
					sb.Append(escaped.Substring(startIndex, i - startIndex - 1));
					sb.Append(c);
					startIndex = i + 1;
					inEscape = false;
				}
			}
			sb.Append(escaped.Substring(startIndex));
			return sb.ToString();
		}
	}
	#endregion // static utility functions
	#region public API
	/// <summary>if delegates are here, calls this code instead of executing a known a command</summary>
	private event DoAfterStringIsRead waitingToReadLine;
	/// <summary>If this is set, ignore the native command line functionality, and just do this</summary>
	public DoAfterStringIsRead onInput;

	/// <summary>what to do after a string is read.</summary>
	public delegate void DoAfterStringIsRead(string readFromUser);
	public delegate void DoAfterVisiblityChange();
	public static void SetText(string text) { Instance.setText(text); }
	/// <returns>The all text, including user input</returns>
	public string GetAllText() { return (_tmpInputField) ? GetRawText() : _text; }

	/// <param name="text">Text to add as output, also turning current user input into text output</param>
	public void AddText(string text, bool toEnd = false) {
		string str = GetAllText();

		if(WriteCursor == str.Length || toEnd) {
			if(indexWherePromptWasPrintedRecently >= 0) {
				str = str.Substring(0, indexWherePromptWasPrintedRecently);
				indexWherePromptWasPrintedRecently = -1;
			}
			setText(str + text);
			WriteCursor = GetAllText().Length;
		} else {
			Debug.Log("NEED TO MAKE IT HAPPEN! "+WriteCursor); // TODO
			// break apart the text to write into lines, including if the line would need to wrap a line (mark that)
			List<string> lines = new List<string>();
			int i = 0;
			while(i < text.Length) {
				string nextLine = "";
				int eol = text.IndexOf('\n');
				if(eol == -1) {
					eol = text.Length;
				}
				nextLine = text.Substring(i, eol - i);
				//int visibleChars = VisibleCharactersInLine(nextLine, 0, nextLine.Length);
				int indexOfBreakingChar = VisibleCharactersInLine(nextLine, 0, nextLine.Length, CommandLineWidth);
				if(indexOfBreakingChar >= 0) {
					nextLine = text.Substring(i, indexOfBreakingChar) + "\n"; // TODO if the line has a newline at the end, that means it was added as a hard break. Add the hard break to the substrings list
					i += indexOfBreakingChar - i;
				} else {
					int len = eol - i;
					nextLine = text.Substring(i, len);
					i += len;
					if(i < text.Length) i++;// skip the newline character too
				}
				lines.Add(nextLine);
			}
			string o = "Need to add:\n";
			for(int e = 0; e < lines.Count; ++e){
				o += "'" + lines[e] + "'\n";
			}
			Debug.Log(o);
				// find out how much text needs to be printed out in a line
				// find out how much room there is in the line that the write cursor is in
				// if there is enough room, do some text replacement. be careful of formatting tags! some tags may need to be pushed, some may need to be split.
				// if there is not enough room, find out how much room is needed, then do text replacement
				// go to the next line.
				// setText
		}
	}

	/// <summary>Visibles the characters in line.</summary>
	/// <returns>The characters in line if maxCount is less than 0. Otherwise, the index where the maxCount visible character was printed, or -1 if there arent enough characters.</returns>
	/// <param name="text">Text.</param>
	/// <param name="start">Start.</param>
	/// <param name="end">End.</param>
	/// <param name="maxCount">Max count.</param>
	public int VisibleCharactersInLine(string text, int start, int end, int maxCount = -1) {
		int count = 0;
		for(int i = start; i < end; ++i) {
			char c = text[i];
			switch(c) {
			case '\0': case '\a': case '\b': case '\t': case '\n': case '\r': // @debug
				throw new Exception("VisibleCharactersInLine should not have to deal with char "+(int)c+".");
			case '<':
				int tend = text.IndexOf('>', i+1);
				int tendalt = text.IndexOf(' ', i + 1);
				if(tendalt < 0) tendalt = text.Length;
				int tokenEnd = Math.Min(tend, tendalt);
				string tagname = text.Substring(i + 1, (i + 1) - tokenEnd);
				if(tagname.StartsWith("#") || tagname.StartsWith("/") || Array.IndexOf(Util.tagsTMP,tagname) >= 0) {
					i = tend;
				}
				break;
			default:
				count++;
				break;
			}
			if(maxCount >= 0 && count > maxCount) {
				return i;
			}
		}
		if(maxCount >= 0) {
			return -1;
		}
		return count;
	}

	/// <param name="line">line to add as output, also turning current user input into text output</param>
	public void println(string line) {
		AddText(line + "\n");
	}
	public void readLineAsync(DoAfterStringIsRead stringCallback) {
		if(!IsInteractive() && _tmpInputField != null) { SetInteractive(true); }
		waitingToReadLine += stringCallback;
	}
	public void getInputAsync(DoAfterStringIsRead stringCallback) { readLineAsync(stringCallback); }
	public static void GetInputAsync(DoAfterStringIsRead stringCallback) { Instance.readLineAsync(stringCallback); }
	public static void ReadLine(DoAfterStringIsRead stringCallback) { Instance.readLineAsync(stringCallback); }
	/// <summary>Instance.println(line)</summary>
	public static void Log(string line) { Instance.println(line); }
	public void log(string line) { println(line); }
	public void readLine(DoAfterStringIsRead stringCallback) { readLineAsync(stringCallback); }
	public string GetRawText() { return _tmpInputField.text; }
	// don't use this, use setText instead, if possible
	private void SetRawText(string s) {
		if(_tmpInputField != null) {
			_tmpInputField.text = s;
		}
	}
	public int GetCursorIndex() { return _tmpInputField.stringPosition; }
	public void SetCursorIndex(int pos) { _tmpInputField.stringPosition = pos; }
	public void SetCursorBackToInput() {
		SetCursorIndex(GetCursorBackToInput());
	}
	public int GetCursorBackToInput() {
		return GetCursorBackToInput(GetCursorIndex());
	}
	public int GetCursorBackToInput(int pos) {
		if(indexWhereUserInputStarts < 0 || indexWhereUserInputEnded < 0) {
			return WriteCursor;
		} else if(pos < indexWhereUserInputStarts) {
			return indexWhereUserInputStarts;
		} else if(pos > indexWhereUserInputEnded) {
			return indexWhereUserInputEnded;
		}
		return pos;
	}
	#endregion // pubilc API
	#region Unity Editor interaction
#if UNITY_EDITOR
	private static Mesh _editorMesh = null; // one variable to enable better UI in the editor

	public List<Action> thingsToDoWhileEditorIsRunning = new List<Action>();
	void OnValidate() {
		thingsToDoWhileEditorIsRunning.Add(() => {
			Interactivity = interactivity;
			InterceptDebugLog = interceptDebugLog;
			TextMeshProFont = textMeshProFont;
			CommandLineWidth = commandLineWidth;
		});
	}

	void OnDrawGizmos() {
		if(_editorMesh == null) {
			_editorMesh = new Mesh();
			_editorMesh.vertices = new Vector3[] { new Vector3(-.5f, .5f), new Vector3(.5f, .5f), new Vector3(-.5f, -.5f), new Vector3(.5f, -.5f) };
			_editorMesh.triangles = new int[] { 0, 1, 2, 3, 2, 1 };
			_editorMesh.RecalculateNormals();
			_editorMesh.RecalculateBounds();
		}
		Vector3 s = this.WorldSpaceSettings.screenSize;
		if(s == Vector3.zero) { s = new Vector3(Screen.width, Screen.height, 1); }
		s.Scale(transform.lossyScale);
		s *= WorldSpaceSettings.textScale;
		Color c = ColorSet.Background;
		Gizmos.color = c;
		if(!UnityEditor.EditorApplication.isPlaying) {
			Gizmos.DrawMesh(_editorMesh, transform.position, transform.rotation, s);
		}
		Transform t = transform;
		// calculate extents
		Vector3[] points = {(t.up*s.y/2 + t.right*s.x/-2),(t.up*s.y/2 + t.right*s.x/2),
			(t.up*s.y/-2 + t.right*s.x/2),(t.up*s.y/-2 + t.right*s.x/-2)};
		for(int i = 0; i < points.Length; ++i) { points[i] += t.position; }
		c.a = 1;
		Gizmos.color = c;
		for(int i = 0; i < points.Length; ++i) {
			Gizmos.DrawLine(points[i], points[(i + 1) % points.Length]);
		}
	}
#endif
	#endregion // Unity Editor interaction
	#region Enable/Disable
	[System.Serializable]
	public struct Callbacks {
		[Tooltip("When the command line goes into active editing. This may be useful to refresh info for the command line, or disable a 3D FPS controller.")]
		public UnityEngine.Events.UnityEvent whenThisActivates;
		[Tooltip("When the command line leaves active editing. This may be useful to re-enable a 3D FPS controller.")]
		public UnityEngine.Events.UnityEvent whenThisDeactivates;
		[Tooltip("When a command is executed. Check <code>RecentInstruction</code>")]
		public UnityEngine.Events.UnityEvent whenCommandRuns;
		public bool ignoreCallbacks;
	}
	[Tooltip("Recommended scripts to pair with the CmdLine: pastebin.com/FaT6i5yF\nwhenThisActivates:    StopPhysics.enablePhysics()\nwhenThisDeactivates: StopPhysics.disablePhysics()")]
	public Callbacks callbacks = new Callbacks();
	#endregion // Enable/Disable
	#region MonoBehaviour
	void Start() {
		WriteCursor = 0;
		if(_instance == null) { _instance = this; }
		showBottomWhenTextIsAdded = true;
		NeedToRefreshUserPrompt = true;
		// test code
		PopulateWithBasicCommands();
		SetInteractive(ActiveOnStart);
	}
	void Update() {
#if UNITY_EDITOR
		if(thingsToDoWhileEditorIsRunning.Count > 0) {
			thingsToDoWhileEditorIsRunning.ForEach(a => a());
			thingsToDoWhileEditorIsRunning.Clear();
		}
#endif
		if(Interactivity != InteractivityEnum.Disabled) {
			// toggle visibility based on key presses
			bool toggle = Input.GetKeyDown(IsInteractive() ? KeyToDeactivate : KeyToActivate);
			// or toggle visibility when 5 fingers touch
			if(Input.touches.Length == 5) {
				if(!_togglingVisiblityWithMultitouch) {
					toggle = true;
					_togglingVisiblityWithMultitouch = true;
				}
			} else {
				_togglingVisiblityWithMultitouch = false;
			}
			if(toggle) {
				if(!IsInteractive()) {
					// check to see how clearly the user is looking at this CmdLine
					if(_mainView.renderMode == RenderMode.ScreenSpaceOverlay) {
						this.viewscore = 1;
					} else {
						Transform cameraTransform = Camera.main.transform;
						Vector3 lookPosition = cameraTransform.position;
						Vector3 gaze = cameraTransform.forward;
						Vector3 delta = transform.position - lookPosition;
						float distFromCam = delta.magnitude;
						float viewAlignment = Vector3.Dot(gaze, delta / distFromCam);
						if(viewAlignment < 0) {
							this.viewscore = -1;
						} else {
							this.viewscore = (1 - viewAlignment) * distFromCam;
						}
					}
					if(currentlyActiveCmdLine == null
						|| (currentlyActiveCmdLine != null && (currentlyActiveCmdLine.viewscore < 0
							|| (this.viewscore >= 0 && this.viewscore <= currentlyActiveCmdLine.viewscore)))) {
						SetInteractive(true);
					}
				} else {
					SetInteractive(false);
					this.viewscore = -1;
				}
			}
			// stop trying to show the bottom if the user wants to scroll
			if(Input.GetAxis("Mouse ScrollWheel") != 0) {
				showBottomWhenTextIsAdded = _tmpInputField.verticalScrollbar.value == 1;
			}
			if(showBottomWhenTextIsAdded) {
				_tmpInputField.verticalScrollbar.value = 1;
			}
		}
		Instruction instruction = PopInstruction();
#if CONNECT_TO_REAL_COMMAND_LINE_TERMINAL
		if(bash == null) { bash = new BASH(); }
		if(bash.IsInitialized() && AllowSystemAccess
		&& (instruction == null || instruction.IsUser(UserRawInput) || instruction.user == bash)) {
			bash.Update(instruction, this); // always update, since this also pushes the pipeline
		} else {
#endif
			// run any queued-up commands
			if(instruction != null) {
				Dispatch(instruction);
				NeedToRefreshUserPrompt = true;
				if(!callbacks.ignoreCallbacks && callbacks.whenCommandRuns != null) callbacks.whenCommandRuns.Invoke();
			}
#if CONNECT_TO_REAL_COMMAND_LINE_TERMINAL
		}
#endif

		if(Interactivity != InteractivityEnum.Disabled) {
			// make sure the cursor skips over invisible areas
			int lastCursorPos = CursorPosition;
			CursorPosition = GetCursorIndex();
			if(selectEnd != CursorPosition && selectBegin != CursorPosition) {
				selectEnd = selectBegin = -1;
			}
			if(lastCursorPos != CursorPosition) {
				CursorCoordinate = GetCursorCoordinate(CursorPosition);
			}
			if(lastCursorPos != CursorPosition && CursorPosition != indexWhereUserInputEnded) {
				int index = WhichInvisibleSubstring(GetCursorIndex());
				if(index >= 0) {
					Substring sb = invisibleSubstrings[index];
					if(CursorPosition >= sb.Middle) {
						CursorPosition = sb.index;
						if(CursorPosition > 0) CursorPosition--;
					} else {
						CursorPosition = sb.Limit;
					}
					SetCursorIndex(CursorPosition);
				}
				string o = "";
				string text = GetAllText();
				for(int i = 0; i < invisibleSubstrings.Count; ++i){
					o += invisibleSubstrings[i].Of(text) + "\n";
				}
				DEBUG_INVISIBLES = o;
			}
			if(NeedToRefreshUserPrompt && onInput == null
#if CONNECT_TO_REAL_COMMAND_LINE_TERMINAL
			&& bash.IsProbablyIdle()
#endif
			&& (waitingToReadLine == null || waitingToReadLine.GetInvocationList().Length == 0)) {
				// in case of keyboard mashing...
				if(GetUserInputLength() > 0) {
					string userInput = GetUserInput();
					setText(_text); GetInputValidator().EndUserInput(true);
					PrintPrompt(); GetInputValidator().AddUserInput(userInput);
					_text = _tmpInputField.text.Substring(0, _tmpInputField.text.Length - userInput.Length);
				} else { PrintPrompt(); }
				NeedToRefreshUserPrompt = false;
			}
		}
		// if this is the active command line and it has not yet disabled user controls. done in update to stop many onStart and onStop calls from being invoked in series
		if(currentlyActiveCmdLine == this && disabledUserControls != this) {
			// if another command line disabled user controls
			if(disabledUserControls != null) {
				// tell it to re-enable controls
				if(!callbacks.ignoreCallbacks && callbacks.whenThisDeactivates != null) callbacks.whenThisDeactivates.Invoke();
			}
			disabledUserControls = this;
			if(!callbacks.ignoreCallbacks && callbacks.whenThisActivates != null) callbacks.whenThisActivates.Invoke();
		}
	}
	#endregion // MonoBehaviour
}