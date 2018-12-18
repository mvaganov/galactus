#define CONNECT_TO_REAL_COMMAND_LINE_TERMINAL
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

/// <summary>A Command Line emulation for Unity3D
/// <description>Public Domain - This code is free, don't bother me about it!</description>
/// <author email="mvaganov@hotmail.com">Michael Vaganov</author>
public class CmdLine : MonoBehaviour {
	#region commands
	/// <summary>watching for commands *about to execute*.</summary>
	public event CommandHandler onCommand;
	/// <summary>known commands</summary>
	private Dictionary<string, Command> commands = new Dictionary<string, Command>();
	/// <summary>queue of instructions that this command line needs to execute.</summary>
	private List<string> instructionList = new List<string>();

	/// <summary>example of how to populate the command-line with commands</summary>
	public void PopulateWithBasicCommands() {
		//When adding commands, you must add a call below to registerCommand() with its name, implementation method, and help text.
		addCommand("help", (args) => {
			log(" - - - -\n" + CommandHelpString() + "\n - - - -");
		}, "prints this help.");
		addCommand("load", (args) => {
			if(args.Length > 1) {
				if(args[1] == ".") { args[1] = SceneManager.GetActiveScene().name; }
				SceneManager.LoadScene(args[1]);
			} else {
				log("to reload current scene, type <#" + ColorSet.SpecialTextHex + ">load " +
		   SceneManager.GetActiveScene().name + "</color>");
			}
		}, "loads given scene. use: load <scene name>");
		addCommand("pref", (args) => {
			for(int i = 1; i < args.Length; ++i) {
				string output = null;
				try { output = "<#" + ColorSet.SpecialTextHex + ">" + PlayerPrefs.GetString(args[i]) + "</color>"; } catch(System.Exception e) { output = "<#" + ColorSet.ErrorTextHex + ">" + e.ToString() + "</color>"; }
				if(output == null) { output = "<#" + ColorSet.ErrorTextHex + ">null</color>"; }
				log(args[i] + ":" + output);
			}
		}, "shows player prefs value. use: pref [variableName, ...]");
		addCommand("prefsave", (args) => {
			if(args.Length > 1) {
				PlayerPrefs.SetString(args[1], (args.Length > 2) ? args[2] : null);
				PlayerPrefs.Save();
			} else {
				log("missing arguments");
			}
		}, "save player prefs value. use: pref variableName variableValue]");
		addCommand("prefreset", (args) => {
			PlayerPrefs.DeleteAll();
			PlayerPrefs.Save();
		}, "clears player prefs.");
		addCommand("echo", (args) => {
			println(string.Join(" ", args, 1, args.Length - 1));
		}, "repeat given arguments as output");
		addCommand("exit", (args) => {
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
		addCommand ("cmd", (args) => {
			if(allowSystemAccess) {
				bash.CMD(string.Join(" ", args, 1, args.Length - 1), null, this);
			} else {
				HandleLog("Access Denied", "", LogType.Warning);
			}
		}, "access the true system's command-line terminal");
#endif
	}
#if CONNECT_TO_REAL_COMMAND_LINE_TERMINAL
	public class BASH {
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

		public void CMD(string s, DoAfterStringIsRead cb = null, CmdLine cmd = null) {
			if(activeDir == null) { activeDir = PWD(); }
			if(thread == null) {
				currentCommand = s.Trim();
				currentCommand_callback = cb;
				log = new List<string>();
				err = new List<string>();
				thread = new System.Threading.Thread(delegate () {
					system_process = new System.Diagnostics.Process {
						StartInfo = new System.Diagnostics.ProcessStartInfo {
#if UNITY_EDITOR_WIN32 || UNITY_STANDALONE_WIN32 || PLATFORM_STANDALONE_WIN32
							FileName = "cmd.exe",
#else
							FileName = "/bin/bash",
#endif
							Arguments = "",
							UseShellExecute = false,
							RedirectStandardOutput = true,
							RedirectStandardInput = true,
							RedirectStandardError = true,
							CreateNoWindow = false,
							WorkingDirectory = activeDir
						}
					};
					system_process.Start();
					system_process.OutputDataReceived += delegate (object sender, System.Diagnostics.DataReceivedEventArgs e) {
						if(currentCommand_callback == null) {
							log.Add(e.Data);
							probablyFinishedCommand = true;
						} else {
							currentCommand_callback(e.Data);
							currentCommand_callback = null;
						}
					};
					system_process.BeginOutputReadLine();
					bool ignoreNextError = true;
					system_process.ErrorDataReceived += delegate (object sender, System.Diagnostics.DataReceivedEventArgs e) {
						if(ignoreNextError) { ignoreNextError = false; return; }
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
							promptNeedsRedraw = true;
							lastCommand = currentCommand;
							if(currentCommand == "exit") {
								break;
							}
							system_process.StandardInput.WriteLine(currentCommand);
							system_process.StandardInput.Flush();
							probablyFinishedCommand = false;
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
					system_process.WaitForExit();
					system_process.Close();
					isInitialized = false;
					system_process = null;
					System.Threading.Thread t = thread;
					thread = null;
					if(cmd != null) {
						cmd.NeedToRefreshUserPrompt = true;
						cmd.EnqueueRun("echo exiting");
					}
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

		public string COMMAND_LINE_GETTER(string call) {
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

		public bool IsProbablyIdle(){
			return (thread == null ||
			(string.IsNullOrEmpty(currentCommand) && probablyFinishedCommand));
		}

		public bool IsInitialized() { return isInitialized; }

		public string MachineName { get { return system_process.MachineName; } }

		public void Update(string s, CmdLine cmd) {
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
			CMD(s);
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
	private BASH bash = new BASH();
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
	public delegate void CommandHandler(string[] args);
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
	/// <param name="commandWithArguments">Command string, with arguments.</param>
	public void EnqueueRun(string commandWithArguments, bool userInitiated = false) {
		instructionList.Add(commandWithArguments);
		if(userInitiated) {
			indexWherePromptWasPrintedRecently = -1; // make sure this command stays visible
		}
	}
	public void Run(string commandWithArguments) {
		if(waitingToReadLine != null) {
			waitingToReadLine(commandWithArguments);
			waitingToReadLine = null;
		} else if(onInput != null) {
			onInput(commandWithArguments);
		} else {
			if(string.IsNullOrEmpty(commandWithArguments)) { return; }
			string s = commandWithArguments.Trim(WHITESPACE); // cut leading & trailing whitespace
			string[] args = ParseArguments(s).ToArray();
			if(args.Length < 1) { return; }
			if(onCommand != null) { onCommand(args); }
			Run(args[0].ToLower(), args);
		}
	}
	/// <param name="command">Command.</param>
	/// <param name="args">Arguments. [0] is the name of the command, with [1] and beyond being the arguments</param>
	public void Run(string command, string[] args) {
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
				cmd.handler(args);
			} else {
				log("Null command '" + command + "'");
			}
		}
	}
	#endregion // commands
	#region user interface
	[FormerlySerializedAs("promptArtifact")] public string PromptArtifact = "$ ";
	[Tooltip("the main viewable UI component")]
	private Canvas _mainView;
	public enum InteractivityEnum { Disabled, ScreenOverlayOnly, WorldSpaceOnly, ActiveScreenAndInactiveWorld };
	[FormerlySerializedAs("interactivity")] public InteractivityEnum Interactivity = InteractivityEnum.ActiveScreenAndInactiveWorld;
	[FormerlySerializedAs("keyToActivate")]
	[Tooltip("Which key shows the terminal")]
	public KeyCode KeyToActivate = KeyCode.BackQuote;
	[FormerlySerializedAs("keyToDeactivate")]
	[Tooltip("Which key hides the terminal")]
	public KeyCode KeyToDeactivate = KeyCode.Escape;
	[Tooltip("used to size the console Rect Transform on creation as a UI overlay")]
	public RectTransformSettings ScreenOverlaySettings;
	[Tooltip("fill this out to set the console in the world someplace")]
	public PutItInWorldSpace WorldSpaceSettings = new PutItInWorldSpace(0.005f, Vector2.zero);
	[Tooltip("used to color the console on creation")]
	public InitialColorSettings ColorSet = new InitialColorSettings();
	private TMPro.TMP_InputField _tmpInputField;
	/// <summary>used to prevent multiple simultaneous toggles of visibility</summary>
	private bool _togglingVisiblityWithMultitouch = false;
	[Tooltip("If true, will show up and take user input immediately")]
	public bool activeOnStart = true;
#if CONNECT_TO_REAL_COMMAND_LINE_TERMINAL
	public bool allowSystemAccess = true;
#endif
	public bool NeedToRefreshUserPrompt { get; set; }
	/// used to smartly (perhaps overly-smartly) over-write the prompt when printing things out-of-sync
	private int indexWherePromptWasPrintedRecently = -1;
	[Tooltip("The TextMeshPro font used. If null, built-in-font should be used.")]
	public TMP_FontAsset textMeshProFont;
	/// <summary>which command line is currently active, and disabling user controls</summary>
	private static CmdLine currentlyActiveCmdLine, disabledUserControls;
	/// <summary>used to check which command line is the best one for the user controlling the main camera</summary>
	private float viewscore;

	[System.Serializable]
	public class InitialColorSettings {
		public Color Background = new Color(0, 0, 0, 0.5f);
		public Color Text = new Color(1, 1, 1);
		public Color ErrorText = new Color(1, 0, 0);
		public Color SpecialText = new Color(1, .75f, 0);
		public Color Scrollbar = new Color(1, 1, 1, 0.5f);
		public Color UserInput = new Color(.5f, 1, .75f);
		public Color UserSelection = new Color(1, .75f, .75f, .75f);
		public string UserInputHex { get { return CmdLine.ColorToHexCode(UserInput); } }
		public string ErrorTextHex { get { return CmdLine.ColorToHexCode(ErrorText); } }
		public string SpecialTextHex { get { return CmdLine.ColorToHexCode(SpecialText); } }
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
			r.localScale = new Vector3(textScale, textScale, textScale);
		}
	}
	public void ShowPrompt() {
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
	}
	public bool IsInOverlayMode() {
		return _mainView.renderMode == RenderMode.ScreenSpaceOverlay;
	}
	public void PositionInWorld(Vector3 center, Vector2 size = default(Vector2), float scale = 0.005f) {
		if (size == Vector2.zero) size = new Vector2 (Screen.width, Screen.height);
		PutItInWorldSpace ws = new PutItInWorldSpace(scale, size);
		if (_mainView == null) {
			WorldSpaceSettings = ws;
		} else {
			ws.ApplySettingsTo (_mainView);
		}
	}
	public void SetOverlayModeInsteadOfWorld(bool useOverlay) {
		if (useOverlay && _mainView.renderMode != RenderMode.ScreenSpaceOverlay) {
			_mainView.renderMode = RenderMode.ScreenSpaceOverlay;
		} else if(!useOverlay) {
			_mainView.renderMode = RenderMode.WorldSpace;
			WorldSpaceSettings.ApplySettingsTo (_mainView);
		}
	}
	private Canvas CreateUI () {
		_mainView = FindComponentUpHierarchy<Canvas> (transform);
		if (!_mainView) {
			_mainView = (new GameObject ("canvas")).AddComponent<Canvas> (); // so that the UI can be drawn at all
			_mainView.renderMode = RenderMode.ScreenSpaceOverlay;
			if (!_mainView.GetComponent<CanvasScaler> ()) {
				_mainView.gameObject.AddComponent<CanvasScaler> (); // so that text is pretty when zoomed
			}
			if (!_mainView.GetComponent<GraphicRaycaster> ()) {
				_mainView.gameObject.AddComponent<GraphicRaycaster> (); // so that mouse can select input area
			}
			_mainView.transform.SetParent (transform);
		}
		GameObject tmpGo = new GameObject ("TextMeshPro - InputField");
		tmpGo.transform.SetParent (_mainView.transform);
		Image img = tmpGo.AddComponent<Image> ();
		img.color = ColorSet.Background;
		if (ScreenOverlaySettings == null) {
			MaximizeRectTransform (tmpGo.transform);
		} else {
			RectTransform r = tmpGo.GetComponent<RectTransform> ();
			r.anchorMin = ScreenOverlaySettings.AnchorMin;
			r.anchorMax = ScreenOverlaySettings.AnchorMax;
			r.offsetMin = ScreenOverlaySettings.OffsetMin;
			r.offsetMax = ScreenOverlaySettings.OffsetMax;
		}
		_tmpInputField = tmpGo.AddComponent<TMP_InputField> ();
		_tmpInputField.lineType = TMP_InputField.LineType.MultiLineNewline;
		_tmpInputField.textViewport = _tmpInputField.GetComponent<RectTransform> ();
		TextMeshProUGUI tmpText;
#if UNITY_EDITOR
		try {
#endif
			tmpText = (new GameObject ("Text")).AddComponent<TextMeshProUGUI> ();
#if UNITY_EDITOR
		} catch(System.Exception) {
			throw new System.Exception("Could not create a TextMeshProUGUI object. Did you get default fonts into TextMeshPro? Window -> TextMeshPro -> Import TMP Essential Resources");
		}
#endif
		if (textMeshProFont != null) {
			tmpText.font = textMeshProFont;
		}
		tmpText.fontSize = 14;
		tmpText.transform.SetParent (tmpGo.transform);
		_tmpInputField.textComponent = tmpText;
		_tmpInputField.fontAsset = tmpText.font;
		_tmpInputField.pointSize = tmpText.fontSize;
		MaximizeRectTransform (tmpText.transform);
		tmpGo.AddComponent<RectMask2D> ();
		_tmpInputField.onFocusSelectAll = false;
		tmpText.color = ColorSet.Text;
		_tmpInputField.selectionColor = ColorSet.UserSelection;
		_tmpInputField.customCaretColor = true;
		_tmpInputField.caretColor = ColorSet.UserInput;
		_tmpInputField.caretWidth = 5;
		_tmpInputField.ActivateInputField ();
		_tmpInputField.onValueChanged.AddListener (listener_OnValueChanged);
		_tmpInputField.characterValidation = TMP_InputField.CharacterValidation.CustomValidator;
		_tmpInputField.inputValidator = GetInputValidator();
		if (_tmpInputField.verticalScrollbar == null) {
			GameObject scrollbar = new GameObject ("scrollbar vertical");
			scrollbar.transform.SetParent (_tmpInputField.transform);
			scrollbar.AddComponent<RectTransform> ();
			_tmpInputField.verticalScrollbar = scrollbar.AddComponent<Scrollbar> ();
			_tmpInputField.verticalScrollbar.direction = Scrollbar.Direction.TopToBottom;
			RectTransform r = scrollbar.GetComponent<RectTransform> ();
			r.anchorMin = new Vector2 (1, 0);
			r.anchorMax = Vector2.one;
			r.offsetMax = Vector3.zero;
			r.offsetMin = new Vector2 (-16, 0);
		}
		if (_tmpInputField.verticalScrollbar.handleRect == null) {
			GameObject slideArea = new GameObject ("sliding area");
			slideArea.transform.SetParent (_tmpInputField.verticalScrollbar.transform);
			RectTransform r = slideArea.AddComponent<RectTransform> ();
			MaximizeRectTransform (slideArea.transform);
			r.offsetMin = new Vector2 (10, 10);
			r.offsetMax = new Vector2 (-10, -10);
			GameObject handle = new GameObject ("handle");
			Image bimg = handle.AddComponent<Image> ();
			bimg.color = ColorSet.Scrollbar;
			handle.transform.SetParent (slideArea.transform);
			r = handle.GetComponent<RectTransform> ();
			r.anchorMin = r.anchorMax = Vector2.zero;
			r.offsetMax = new Vector2 (5, 5);
			r.offsetMin = new Vector2 (-5, -5);
			r.pivot = Vector2.one;
			_tmpInputField.verticalScrollbar.handleRect = r;
			_tmpInputField.verticalScrollbar.targetGraphic = img;
		}
		// an event system is required... if there isn't one, make one
		StandaloneInputModule input = FindObjectOfType (typeof(StandaloneInputModule)) as StandaloneInputModule;
		if (input == null) {
			input = (new GameObject ("<EventSystem>")).AddComponent<StandaloneInputModule> ();
		}
		// put all UI in the UI layer
		SetLayerRecursive (_mainView.gameObject, LayerMask.NameToLayer ("UI"));
		// turn it off and then back on again... that fixes some things.
		tmpGo.SetActive (false); tmpGo.SetActive (true);
		// put it in the world (if needed)
		if (Interactivity == InteractivityEnum.WorldSpaceOnly
			|| Interactivity == InteractivityEnum.ActiveScreenAndInactiveWorld) {
			WorldSpaceSettings.ApplySettingsTo (_mainView);
		}
		return _mainView;
	}
	private static RectTransform MaximizeRectTransform (Transform t) {
		return MaximizeRectTransform (t.GetComponent<RectTransform> ());
	}
	private static RectTransform MaximizeRectTransform (RectTransform r) {
		r.anchorMax = Vector2.one;
		r.anchorMin = r.offsetMin = r.offsetMax = Vector2.zero;
		return r;
	}
	public bool IsVisible () {
		return _mainView != null && _mainView.gameObject.activeInHierarchy;
	}
	/// <summary>shows (true) or hides (false).</summary>
	public void SetVisibility (bool visible) {
		if (_mainView == null) {
			activeOnStart = visible;
		} else {
			_mainView.gameObject.SetActive (visible);
		}
	}
	/// <param name="enabled">If <c>true</c>, reads from keybaord. if <c>false</c>, stops reading from keyboard</param>
	public void SetInputActive(bool enabled) {
		if (enabled) {
			_tmpInputField.ActivateInputField ();
		} else {
			_tmpInputField.DeactivateInputField ();
		}
	}
	/// <param name="enableInteractive"><c>true</c> to turn this on (and turn the previous CmdLine off)</param>
	public void SetInteractive(bool enableInteractive) {
		bool activityWhenStarted = _tmpInputField.interactable;
		if (enableInteractive && currentlyActiveCmdLine != null) {
			currentlyActiveCmdLine.SetInteractive (false);
		}
		_tmpInputField.interactable = enableInteractive; // makes focus possible
		switch (Interactivity) {
		case InteractivityEnum.ScreenOverlayOnly:
			if (!IsInOverlayMode ()) {
				SetOverlayModeInsteadOfWorld (true);
			}
			SetVisibility (enableInteractive);
			break;
		case InteractivityEnum.WorldSpaceOnly:
			if (!IsVisible ()) {
				SetVisibility (true);
			}
			if (enableInteractive)
				SetOverlayModeInsteadOfWorld (false);
			break;
		case InteractivityEnum.ActiveScreenAndInactiveWorld:
			//Debug.Log("switching "+ enableInteractive);
			if (!IsVisible ()) {
				SetVisibility (true);
			}
			SetOverlayModeInsteadOfWorld (enableInteractive);
			break;
		}
		_tmpInputField.verticalScrollbar.value = 1; // scroll to the bottom
		MoveCaretToEnd (); // move caret focus to end
		SetInputActive (_tmpInputField.interactable); // request/deny focus
		if (enableInteractive) {
			currentlyActiveCmdLine = this;
		} else if (currentlyActiveCmdLine == this) {
			// if this command line has disabled the user
			if (disabledUserControls == currentlyActiveCmdLine) {
				// tell it to re-enable controls
				//if(disabledUserControls.onDeactivateInteraction != null) disabledUserControls.onDeactivateInteraction.Invoke ();
				if(!callbacks.ignoreCallbacks && callbacks.whenThisDeactivates != null) callbacks.whenThisDeactivates.Invoke();
				disabledUserControls = null;
			}
			currentlyActiveCmdLine = null;
		}
	}
	public bool IsInteractive() { return _tmpInputField != null && _tmpInputField.interactable; }
	/// <summary>Moves the caret to the end, clearing all selections in the process</summary>
	public void MoveCaretToEnd () {
		int lastPoint = GetRawText ().Length;
		SetCaretPosition (lastPoint);
	}
#endregion // user interface
#region input validation
	/// <summary>console data that should not be modifiable as user input</summary>
	private string nonUserInput = "";
	private CmdLineValidator inputvalidator;
	/// <summary>keeps track of user selection so that the text field can be fixed if selected text is removed</summary>
	private int selectBegin, selectEnd;
	/// <summary>what replaces an attempt to un-escape the TextMeshPro noparse boundary in the command line</summary>
	public const string NOPARSE_REPLACEMENT = ">NOPARSE<";
	/// <summary>flag to move text view to the bottom when content is added</summary>
	private bool showBottomWhenTextIsAdded = false;
	/// <summary>if text is being modified to refresh it after the user did something naughty</summary>
	private bool addingOnChanged = false;
	[Tooltip ("Maximum number of lines to retain.")]
	public int maxLines = 99;
	[Tooltip ("lines with more characters than this will count as more than one line.")]
	public int maxColumnsPerLine = 120;

	private CmdLineValidator GetInputValidator() {
		if (inputvalidator == null) {
			inputvalidator = ScriptableObject.CreateInstance<CmdLineValidator> ();
			inputvalidator.cmd = this;
		}
		return inputvalidator;
	}
	private void listener_OnTextSelectionChange (string str, int start, int end) {
		selectBegin = Math.Min (start, end);
		selectEnd = Math.Max (start, end);
	}
	/// <summary>the class that tries to keep the user from wrecking the command line terminal</summary>
	private class CmdLineValidator : TMP_InputValidator {
		public CmdLine cmd;
		public bool isUserEnteringInput = false;
		public void AddUserInput(string userInput) {
			string s = cmd._tmpInputField.text;
			int cursor = cmd._tmpInputField.caretPosition; // should this be caretPosition?
			for(int i = 0; i < userInput.Length; ++i) {
				char c = userInput [i];
				Validate(ref s, ref cursor, c);
			}
			cmd._tmpInputField.text = s;
			cmd._tmpInputField.caretPosition = cursor;
		}
		int AddUserInput(ref string text, char letter) {
			int added = 0;
			if(!isUserEnteringInput) {
				isUserEnteringInput = true;
				string headr = BEGIN_USER_INPUT();
				text += headr;
				added += headr.Length;
				cmd.nonUserInput = text;
			}
			text += letter;	added += 1;
			return added;
		}
		public int EndUserInput(bool forced) {
			if (forced)
				isUserEnteringInput = true;
			string s = cmd._tmpInputField.text;
			int returned = EndUserInput (ref s);
			cmd._tmpInputField.text = s;
			return returned;
		}
		public bool HasProperInputTags(string text) {
			List<string> tags = CmdLine.CalculateTextMeshProTags(text, false);
			if (tags.Count == 0 || !tags.Contains ("noparse"))
				return false;
			string colorTag = "#" + cmd.ColorSet.UserInputHex;
			return tags.Contains(colorTag);
		}
		public bool CheckIfUserInputTagsArePresent(string text) {
			// check if the new text has the input tags opened
			return isUserEnteringInput = HasProperInputTags(text);
		}
		public string BEGIN_USER_INPUT() {
			return "<#" + cmd.ColorSet.UserInputHex + "><noparse>";
		}
		public string END_USER_INPUT() { return "</noparse></color>"; }
		private int EndUserInput(ref string text) {
			int added = 0;
			if (isUserEnteringInput) {
				isUserEnteringInput = false;
				string expectedheadr = BEGIN_USER_INPUT ();
				if(text.EndsWith(expectedheadr)) {
					text = text.Substring(0, text.Length-expectedheadr.Length);
					added -= expectedheadr.Length;
				} else {
					string footr = END_USER_INPUT();
					text += footr;
					added += footr.Length;
				}
				cmd.nonUserInput = text;
			}
			return added;
		}
		public override char Validate(ref string text, ref int i, char c) {
			if (!cmd.IsInteractive ()) return '\0';
			char letter = '\0';
			if (i < text.Length) {
				letter = text [i];
			}
			if (i < cmd.nonUserInput.Length) {
				i = cmd.GetRawText ().Length;
			}
			i += AddUserInput (ref text, c);
			// if the user is attempting to break out of noparse...
			if (c == '>') {
				// check if a tag is being ended
				int startOfTag = text.LastIndexOf ('<');
				int endOfTag = text.LastIndexOf ('>', text.Length - 2);
				if (startOfTag >= 0 && startOfTag > endOfTag) {
					string possibleTag = text.Substring (startOfTag).ToLower ();
					// unescape, incase the user is being trixie with unescape sequences...
					possibleTag = CmdLine.Unescape (possibleTag);
					// and if they are, just don't let them.
					if (possibleTag.Contains ("noparse")) {
						text = text.Substring (0, startOfTag) + NOPARSE_REPLACEMENT;
					}
				}
			}
			// if the user wants to execute (because they pressed enter)
			else if (c =='\n') {
				string inpt = cmd.GetUserInput ();
				int start = 0, end = -1;
				do {
					end = inpt.IndexOf("\n", start);
					if(end >= start && start < inpt.Length) {
						int len = end-start;
						if(len > 0) {
							cmd.EnqueueRun(inpt.Substring(start, len), true);
						}
						start = end+1;
					}
				} while(end > 0);
				if (start < inpt.Length) {
					cmd.EnqueueRun(inpt.Substring(start), true);
				}
				EndUserInput (ref text);
			}
			return '\0';
		}
	}
	private void listener_OnValueChanged (string str) {
		if (addingOnChanged) return;
		addingOnChanged = true;
		string newAddition = Input.inputString;
		// don't allow output text to be modified.
		if (GetCaretPosition () < nonUserInput.Length) {
			int offset = selectBegin - selectEnd;
			string alreadyTyped = GetUserInput (offset);
			setText (nonUserInput+alreadyTyped);
			MoveCaretToEnd ();
		}
		addingOnChanged = false;
	}
	private void EndUserInputIfNeeded() {
		if (GetInputValidator().isUserEnteringInput) {
			inputvalidator.isUserEnteringInput = false;
			setText (GetAllText () + inputvalidator.END_USER_INPUT());
		}
	}
	/// <summary>if the given text is a tag, returns the tag with noparse around it.</summary>
	private string NoparseFilterAroundTag(string text) {
		if (text.IndexOf ('<') < 0) return text;
		return "<noparse>" + text + "</noparse>";
	}
	private int CutoffIndexToEnsureLineCount (String s, int maxLines) {
		int lineCount = 0, columnCount = 0, index;
		for (index = s.Length; index > 0; --index) {
			if (s [index - 1] == '\n' || columnCount++ >= maxColumnsPerLine) {
				lineCount++;
				columnCount = 0;
				if (lineCount >= maxLines) { break; }
			}
		}
		return index;
	}
	public int GetUserInputLength(int offset = 0) {
		string s = GetRawText ();
		return s.Length - (nonUserInput.Length+offset);
	}
	/// <returns>The user input, which is text that the user has entered (at the bottom)</returns>
	private string GetUserInput (int offset = 0) {
		string s = GetRawText ();
		int len = s.Length - (nonUserInput.Length+offset);
		return (len > 0)?s.Substring (nonUserInput.Length+offset, len):"";
	}
	/// <param name="text">What the the output text should be (turns current user input into text output)</param>
	public void setText (string text) {
		int cutIndex = CutoffIndexToEnsureLineCount (text, maxLines);
		List<string> tags = null;
		if (cutIndex != 0) {
			tags = CalculateTextMeshProTags (text.Substring (0, cutIndex), false);
			text = text.Substring (cutIndex);
			if (tags != null && tags.Count > 0) {
				string openingTags = "";
				for (int i = 0; i < tags.Count; ++i) {
					openingTags += "<" + tags [i] + ">";
				}
				text = openingTags + text;
			}
			indexWherePromptWasPrintedRecently -= cutIndex;
		}
		nonUserInput = text;
		SetRawText (nonUserInput);
		// if text is replaced during input, this refreshes tags around input
		if (inputvalidator != null) {
			inputvalidator.CheckIfUserInputTagsArePresent (text);
		}
	}
#endregion // input validation
#region singleton
	/// <summary>the singleton instance. One will be created if none exist.</summary>
	private static CmdLine _instance;
	public static CmdLine Instance {
		get {
			if (_instance == null && (_instance = FindObjectOfType (typeof(CmdLine)) as CmdLine) == null) {
				GameObject g = new GameObject ();
				_instance = g.AddComponent<CmdLine> ();
				g.name = "<" + _instance.GetType ().Name + ">";
			}
			return _instance;
		}
	}
#endregion // singleton
#region static utility functions
	/// <summary>Convenience method. Finds the component here, or in a parent object.</summary>
	public static T FindComponentUpHierarchy<T> (Transform t) where T : Component {
		T found = null;
		while (t != null && found == null) {
			found = t.GetComponent<T> ();
			t = t.parent;
		}
		return found;
	}
	/// <param name="layer">what Unity layer to set the given object, and all child objects, recursive</param>
	public static void SetLayerRecursive (GameObject go, int layer) {
		go.layer = layer;
		for (int i = 0; i < go.transform.childCount; ++i) {
			Transform t = go.transform.GetChild (i);
			if (t != null) {
				SetLayerRecursive (t.gameObject, layer);
			}
		}
	}
	/// <returns>A list of the open/close tags in the given strings</returns>
	/// <param name="str">where to look for tags</param>
	/// <param name="keepClosedTags">If <c>false</c>, remove correctly closed tags</param>
	public static List<string> CalculateTextMeshProTags(string str, bool keepClosedTags = true) {
		List<string> tags = new List<string> ();
		bool noparse = false;
		for (int i = 0; i < str.Length; ++i) {
			char c = str [i];
			if (c == '<') {
				int end = str.IndexOf ('>', i);
				string token = null;
				if (end > 0) {
					// just get the starting token, ignore properties after the first space
					int space = str.IndexOf (' ', i);
					if (space >= 0 && space < end) { end = space; }
					token = str.Substring (i+1, end-(i+1));
				}
				// if noparse is one of the tags, ignore all other tags till noparse is closed.
				if (noparse) {
					if (token != null && token.Trim () == "/noparse") {
						noparse = false;
					} else {
						token = null;
					}
				}
				if (!noparse && token.Trim () == "noparse") {
					noparse = true;
				}
				if (!keepClosedTags && token != null) {
					if (token.StartsWith ("/") && tags.Count > 0) {
						int whichTag = tags.IndexOf (token.Substring (1));
						if (token == "/color") {
							for(int e = tags.Count-1; e >= 0; --e) {
								if (tags [e].StartsWith ("#")) {
									whichTag = e; break;
								}
							}
						}
						if (whichTag >= 0) {
							tags.RemoveAt (whichTag);
							token = null;
						}
					} else if (token.EndsWith ("/")) { token = null; }
				}
				if (token != null) { tags.Add (token); }
			}
		}
		return tags;
	}
	public static string ColorToHexCode(Color c) {
		int r = (int)(255 * c.r),  g = (int)(255 * c.g), b = (int)(255 * c.b), a = (int)(255 * c.a);
		return r.ToString("X2")+g.ToString("X2")+b.ToString("X2")+((c.a!=1)?a.ToString("X2"):"");
	}
	private static readonly char[] QUOTES = new char[] { '\'', '\"' },
	WHITESPACE = new char[] { ' ', '\t', '\n', '\b', '\r' };
	/// <returns>index of the end of the token that starts at the given index 'i'</returns>
	public static int FindEndArgumentToken (string str, int i) {
		bool isWhitespace;
		do {
			isWhitespace = System.Array.IndexOf (WHITESPACE, str [i]) >= 0;
			if (isWhitespace) { ++i; }
		} while (isWhitespace && i < str.Length);
		int index = System.Array.IndexOf (QUOTES, str [i]);
		char startQuote = (index >= 0) ? QUOTES [index] : '\0';
		if (startQuote != '\0') { ++i; }
		while (i < str.Length) {
			if (startQuote != '\0') {
				if (str [i] == '\\') {
					i++; // skip the next character for an escape sequence. just leave it there.
				} else {
					index = System.Array.IndexOf (QUOTES, str [i]);
					bool endsQuote = index >= 0 && QUOTES [index] == startQuote;
					if (endsQuote) { i++; break; }
				}
			} else {
				isWhitespace = System.Array.IndexOf (WHITESPACE, str [i]) >= 0;
				if (isWhitespace) { break; }
			}
			i++;
		}
		if (i >= str.Length) { i = str.Length; }
		return i;
	}
	/// <returns>split command-line arguments</returns>
	public static List<string> ParseArguments (string commandLineInput) {
		int index = 0;
		List<string> tokens = new List<string> ();
		while (index < commandLineInput.Length) {
			int end = FindEndArgumentToken (commandLineInput, index);
			if (index != end) {
				string token = commandLineInput.Substring (index, end - index).TrimStart (WHITESPACE);
				token = Unescape (token);
				int qi = System.Array.IndexOf (QUOTES, token [0]);
				if (qi >= 0 && token [token.Length - 1] == QUOTES [qi]) {
					token = token.Substring (1, token.Length - 2);
				}
				tokens.Add (token);
			}
			index = end;
		}
		return tokens;
	}
	/* https://msdn.microsoft.com/en-us/library/aa691087(v=vs.71).aspx */
	private static readonly SortedDictionary<char, char> EscapeMap = new SortedDictionary<char, char> {
		{ '0','\0' }, { 'a','\a' }, { 'b','\b' }, { 'f','\f' }, 
		{ 'n','\n' }, { 'r','\r' }, { 't','\t' }, { 'v','\v' },
	};
	/// <summary>convenience method to un-escape standard escape sequence strings</summary>
	/// <param name="escaped">Escaped.</param>
	public static string Unescape (string escaped) {
		if (escaped == null) { return escaped; }
		StringBuilder sb = new StringBuilder ();
		bool inEscape = false;
		int startIndex = 0;
		for (int i = 0; i < escaped.Length; i++) {
			if (!inEscape) {
				inEscape = escaped [i] == '\\';
			} else {
				char c;
				if (!EscapeMap.TryGetValue (escaped [i], out c)) {
					c = escaped [i]; // unknown escape sequences are literals
				}
				sb.Append (escaped.Substring (startIndex, i - startIndex - 1));
				sb.Append (c);
				startIndex = i + 1;
				inEscape = false;
			}
		}
		sb.Append (escaped.Substring (startIndex));
		return sb.ToString ();
	}

#endregion // static utility functions
#region public API
	/// <summary>if delegates are here, calls this code instead of executing a known a command</summary>
	private event DoAfterStringIsRead waitingToReadLine;
	/// <summary>If this is set, ignore the native command line functionality, and just do this</summary>
	public DoAfterStringIsRead onInput;

	/// <summary>what to do after a string is read.</summary>
	public delegate void DoAfterStringIsRead (string readFromUser);
	public delegate void DoAfterVisiblityChange ();
	public static void SetText(string text) { Instance.setText (text); }
	/// <returns>The all text, including user input</returns>
	public string GetAllText () { return (_tmpInputField) ? GetRawText () : nonUserInput; }
	/// <param name="text">Text to add as output, also turning current user input into text output</param>
	public void AddText (string text) {
		if(indexWherePromptWasPrintedRecently >= 0) {
			if(GetRawText().Length >= 0) {
				//Debug.Log(indexWherePromptWasPrintedRecently+" vs "+GetRawText().Length);
				if(indexWherePromptWasPrintedRecently < GetRawText().Length) {
					//Debug.Log("Removing [" + GetRawText().Substring(indexWherePromptWasPrintedRecently) + "]" + sysproc_currentCommand);
					//SetRawText(GetRawText().Substring(0, indexWherePromptWasPrintedRecently));
					EndUserInputIfNeeded();
					setText(GetAllText().Substring(0, indexWherePromptWasPrintedRecently) + text);
					//Debug.Log("Added [" + text + "]");
					//Debug.Log("Did I really remove? {" + GetRawText().Substring(indexWherePromptWasPrintedRecently) + "}");
				} else {
					EndUserInputIfNeeded();
					setText(GetAllText() + text);
				}
			} else {
				setText(text);
			}
		} else {
			EndUserInputIfNeeded();
			setText(GetAllText() + text);
		}
		indexWherePromptWasPrintedRecently = -1;
	}
	/// <param name="text">line to add as output, also turning current user input into text output</param>
	public void println (string line) {
		// TODO if printing a line and the only thing on this line is the prompt, write-over the prompt.
		AddText (line + "\n");
	}
	public void readLineAsync (DoAfterStringIsRead stringCallback) {
		if (!IsInteractive () && _tmpInputField != null) { SetInteractive (true); }
		waitingToReadLine += stringCallback;
	}
	public void getInputAsync(DoAfterStringIsRead stringCallback) { readLineAsync (stringCallback); }
	public static void GetInputAsync(DoAfterStringIsRead stringCallback) { Instance.readLineAsync (stringCallback); }
	public static void ReadLine (DoAfterStringIsRead stringCallback) { Instance.readLineAsync (stringCallback); }
	/// <summary>Instance.println(line)</summary>
	public static void Log (string line) { Instance.println (line); }
	public void log (string line) { println (line); }
	public void readLine (DoAfterStringIsRead stringCallback) { readLineAsync (stringCallback); }
	public string GetRawText () { return _tmpInputField.text; }
	public void SetRawText (string s) { if(_tmpInputField != null){ _tmpInputField.text = s; } }
	public int GetCaretPosition () { return _tmpInputField.stringPosition; }
	public void SetCaretPosition (int pos) { _tmpInputField.stringPosition = pos; }
#endregion // pubilc API
#region Debug.Log intercept
	[SerializeField, Tooltip("If true, all Debug.Log messages will be intercepted and duplicated here.")]
	public bool interceptDebugLog = false;//true;
	/// <summary>if this object was intercepting Debug.Logs, this will ensure that it un-intercepts as needed</summary>
	private bool dbgIntercepted = false;

	public void EnableDebugLogIntercept () { SetDebugLogIntercept (interceptDebugLog); }
	public void DisableDebugLogIntercept () { SetDebugLogIntercept (false); }
	public void SetDebugLogIntercept (bool intercept) {
		if (intercept && !dbgIntercepted) {
			Application.logMessageReceived += HandleLog;
			dbgIntercepted = true;
		} else if (!intercept && dbgIntercepted) {
			Application.logMessageReceived -= HandleLog;
			dbgIntercepted = false;
		}
	}
	private void HandleLog(string logString, string stackTrace = "", LogType type = LogType.Log) {
		switch (type) {
		case LogType.Error:
			AddText ("<#"+ColorToHexCode(Color.Lerp(ColorSet.Text, Color.red, 0.5f))+">"+logString+"</color>\n");
			break;
		case LogType.Exception:
			string c = "<#"+ColorToHexCode(Color.Lerp(ColorSet.Text, Color.magenta, 0.5f))+">";
			AddText (c+logString+"</color>\n");
			AddText (c+stackTrace+"</color>\n");
			break;
		case LogType.Warning:
			AddText ("<#"+ColorToHexCode(Color.Lerp(ColorSet.Text, Color.yellow, 0.5f))+">"+logString+"</color>\n");
			break;
		default:
			log (logString);
			break;
		}
	}
#endregion // Debug.Log intercept
#region Unity Editor interaction
	private static Mesh _editorMesh = null; // one variable to enable better UI in the editor

	void OnDrawGizmos() {
		if (_editorMesh == null) {
			_editorMesh = new Mesh ();
			_editorMesh.vertices = new Vector3[]{ new Vector3(-.5f, .5f),new Vector3( .5f, .5f), new Vector3(-.5f,-.5f),new Vector3( .5f,-.5f)};
			_editorMesh.triangles = new int[]{ 0,1,2,  3,2,1 };
			_editorMesh.RecalculateNormals ();
			_editorMesh.RecalculateBounds ();
		}
		Vector3 s = this.WorldSpaceSettings.screenSize;
		if (s == Vector3.zero) {
			s = new Vector3 (
				Screen.width*transform.lossyScale.x*WorldSpaceSettings.textScale,
				Screen.height*transform.lossyScale.y*WorldSpaceSettings.textScale,
				1*transform.lossyScale.z*WorldSpaceSettings.textScale);
		}
		Gizmos.DrawMesh(_editorMesh, transform.position, transform.rotation, s);
		Transform t = transform;
		// calculate extents
		Vector3[] points = new Vector3[]{(t.up*s.y/2 + t.right*s.x/-2),(t.up*s.y/2 + t.right*s.x/2),
			(t.up*s.y/-2 + t.right*s.x/2),(t.up*s.y/-2 + t.right*s.x/-2)};
		for (int i = 0; i < points.Length; ++i) { points[i] += t.position; }
		Gizmos.color = ColorSet.Background;
		for (int i = 0; i < points.Length; ++i) {
			Gizmos.DrawLine (points [i], points [(i + 1) % points.Length]);
		}
	}
#endregion // Unity Editor interaction
#region Enable/Disable
	[System.Serializable]
	public struct Callbacks {
		[Tooltip("When the command line goes into active editing. This may be useful to refresh info for the command line, or disable a 3D FPS controller.")]
		public UnityEngine.Events.UnityEvent whenThisActivates;
		[Tooltip("When the command line leaves active editing. This may be useful to re-enable a 3D FPS controller.")]
		public UnityEngine.Events.UnityEvent whenThisDeactivates;
		public bool ignoreCallbacks;
	}
	[Tooltip("Recommended scripts to pair with the CmdLine: pastebin.com/FaT6i5yF\nwhenThisActivates:    StopPhysics.enablePhysics()\nwhenThisDeactivates: StopPhysics.disablePhysics()")]
	public Callbacks callbacks = new Callbacks();
#endregion // Enable/Disable
#region MonoBehaviour
	void Start () {
		CreateUI ();
		showBottomWhenTextIsAdded = true;
		NeedToRefreshUserPrompt = true;
		// test code
		PopulateWithBasicCommands();
		if (nonUserInput.Length == 0) {
			log (Application.productName + ", v" + Application.version);
		} else {
			setText (nonUserInput);
		}
		SetInteractive (activeOnStart);
	}
	void Update () {
		// toggle visibility based on key presses
		bool toggle = Input.GetKeyDown(IsInteractive () ? KeyToDeactivate : KeyToActivate );
		// or toggle visibility when 5 fingers touch
		if (Input.touches.Length == 5) {
			if (!_togglingVisiblityWithMultitouch) {
				toggle = true;
				_togglingVisiblityWithMultitouch = true;
			}
		} else {
			_togglingVisiblityWithMultitouch = false;
		}
		if (toggle) {
			if (!IsInteractive ()) {
				// check to see how clearly the user is looking at this CmdLine
				if (_mainView.renderMode == RenderMode.ScreenSpaceOverlay) {
					this.viewscore = 1;
				} else {
					Transform cameraTransform = Camera.main.transform;
					Vector3 lookPosition = cameraTransform.position;
					Vector3 gaze = cameraTransform.forward;
					Vector3 delta = transform.position - lookPosition;
					float distFromCam = delta.magnitude;
					float viewAlignment = Vector3.Dot (gaze, delta / distFromCam);
					if (viewAlignment < 0) {
						this.viewscore = -1;
					} else {
						this.viewscore = (1 - viewAlignment) * distFromCam;
					}
				}
				if (currentlyActiveCmdLine == null
					|| (currentlyActiveCmdLine != null && (currentlyActiveCmdLine.viewscore < 0
						|| (this.viewscore >= 0 && this.viewscore <= currentlyActiveCmdLine.viewscore)))) {
					SetInteractive (true);
				}
			} else {
				SetInteractive (false);
				this.viewscore = -1;
			}
		}
		// stop trying to show the bottom if the user wants to scroll
		if (Input.GetAxis ("Mouse ScrollWheel") != 0) {
			showBottomWhenTextIsAdded = _tmpInputField.verticalScrollbar.value == 1;
		}
		if (showBottomWhenTextIsAdded) {
			_tmpInputField.verticalScrollbar.value = 1;
		}

#if CONNECT_TO_REAL_COMMAND_LINE_TERMINAL
		if(bash.IsInitialized() && allowSystemAccess) {
			string input = "";
			if(instructionList.Count > 0) {
				input = instructionList[0];
				instructionList.RemoveAt(0);
			}
			bash.Update(input, this);
		} else {
#endif
			// run any queued-up commands
			if (instructionList.Count > 0) {
				Run (instructionList[0]);
				instructionList.RemoveAt (0);
				NeedToRefreshUserPrompt = true;
			}
#if CONNECT_TO_REAL_COMMAND_LINE_TERMINAL
		}
#endif

		if (NeedToRefreshUserPrompt && onInput == null
#if CONNECT_TO_REAL_COMMAND_LINE_TERMINAL
		&& bash.IsProbablyIdle()
#endif
		&& (waitingToReadLine == null || waitingToReadLine.GetInvocationList ().Length == 0)) {
			// in case of keyboard mashing...
			if (GetUserInputLength () > 0) {
				string userInput = GetUserInput ();
				SetText (nonUserInput);  GetInputValidator().EndUserInput (true);
				ShowPrompt(); GetInputValidator ().AddUserInput (userInput);
				nonUserInput = _tmpInputField.text.Substring (0, _tmpInputField.text.Length - userInput.Length);
			} else { ShowPrompt(); }
			NeedToRefreshUserPrompt = false;
		}

		// if this is the active command line and it has not yet disabled user controls. done in update to stop many onStart and onStop calls from being invoked in series
		if (currentlyActiveCmdLine == this && disabledUserControls != this) {
			// if another command line disabled user controls
			if (disabledUserControls != null) {
				// tell it to re-enable controls
				if(!callbacks.ignoreCallbacks && callbacks.whenThisDeactivates != null) callbacks.whenThisDeactivates.Invoke();
			}
			disabledUserControls = this;
			if(!callbacks.ignoreCallbacks && callbacks.whenThisActivates != null) callbacks.whenThisActivates.Invoke();
		}
	}
#endregion // MonoBehaviour
}