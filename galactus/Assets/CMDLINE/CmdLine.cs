using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;
// make sure you have "TextMesh Pro" downloaded and imported from the Unity Asset Store. "Examples" unnecessary
using TMPro;

/// <summary>A Command Line emulation for Unity3D v5.6+. Just use 'CmdLine.Log()'
/// Latest version at: https://pastebin.com/nphdEi1z added prompt artifact</summary>
/// <description>MIT License - TL;DR - This code is free, don't bother me about it!</description>
/// <author email="mvaganov@hotmail.com">Michael Vaganov</author>
public class CmdLine : MonoBehaviour {
	#region commands
	/// <summary>watching for commands *about to execute*.</summary>
	public event CommandHandler onCommand;
	/// <summary>known commands</summary>
	private Dictionary<string, Command> commands = new Dictionary<string, Command> ();
	/// <summary>queue of instructions that this command line needs to execute.</summary>
	private List<string> instructionList = new List<string> ();

	/// <summary>example of how to populate the command-line with commands</summary>
	public void PopulateWithBasicCommands () {
		//When adding commands, you must add a call below to registerCommand() with its name, implementation method, and help text.
		addCommand ("help", (args) => {
			log (" - - - -\n" + CommandHelpString () + "\n - - - -");
		}, "prints this help.");
		addCommand ("load", (args) => {
			if (args.Length > 1) {
				if(args[1] == ".") { args[1] = SceneManager.GetActiveScene ().name; }
				SceneManager.LoadScene (args[1]);
			} else { log("to reload current scene, type <#"+colorSet.special+">load "+
				SceneManager.GetActiveScene ().name+"</color>"); }
		}, "loads given scene. use: load <scene name>");
		addCommand ("pref", (args) => {
			for (int i = 1; i < args.Length; ++i) {
				string output = null;
				try{ output = "<#"+colorSet.special+">"+PlayerPrefs.GetString (args [i])+"</color>"; } 
				catch(System.Exception e){ output = "<#"+colorSet.error+">"+e.ToString()+"</color>"; }
				if(output == null) { output =  "<#"+colorSet.error+">null</color>"; }
				log (args [i] + ":" + output);
			}
		}, "shows player prefs value. use: pref [variableName, ...]");
		addCommand ("prefsave", (args) => {
			if (args.Length > 1) {
				PlayerPrefs.SetString (args [1], (args.Length > 2) ? args [2] : null);
				PlayerPrefs.Save ();
			} else {
				log ("missing arguments");
			}
		}, "save player prefs value. use: pref variableName variableValue]");
		addCommand ("prefreset", (args) => {
			PlayerPrefs.DeleteAll ();
			PlayerPrefs.Save ();
		}, "clears player prefs.");
	}
	/// <param name="command">name of the command to add (case insensitive)</param>
	/// <param name="handler">code to execute with this command, think standard main</param>
	/// <param name="help">reference information, think concise man-pages. Make help <c>"\b"</c> for hidden commands</param>
	public void addCommand (string command, CommandHandler handler, string help) {
		commands.Add (command.ToLower (), new Command (command, handler, help));
	}
	/// <param name="command">name of the command to add (case insensitive)</param>
	/// <param name="handler">code to execute with this command, think standard main</param>
	/// <param name="help">reference information, think concise man-pages. Make help <c>"\b"</c> for hidden commands</param>
	public static void AddCommand (string command, CommandHandler handler, string help) {
		Instance.addCommand (command, handler, help);
	}
	/// <param name="commands">dictionary of commands to begin using, replacing old commands</param>
	public void SetCommands (Dictionary<string, Command> commands) { this.commands = commands; }
	/// <summary>replace current commands with no commands</summary>
	public void ClearCommands () { commands.Clear (); }
	/// <summary>command-line handler. think "standard main" from Java or C/C++.
	/// args[0] is the command, args[1] and beyond are the arguments.</summary>
	public delegate void CommandHandler (string[] args);
	public class Command {
		public string command { get; private set; }
		public CommandHandler handler { get; private set; }
		public string help { get; private set; }
		public Command (string command, CommandHandler handler, string helpReferenceText) {
			this.command = command; this.handler = handler; this.help = helpReferenceText;
		}
	}
	/// <returns>a list of usable commands</returns>
	public string CommandHelpString () {
		StringBuilder sb = new StringBuilder ();
		foreach (Command cmd in commands.Values) {
			if(cmd.help != "\b") // commands with a single backspace as help text are hidden commands 
				sb.Append (((sb.Length > 0) ? "\n" : "") + cmd.command + ": " + cmd.help);
		}
		return sb.ToString ();
	}
	/// <summary>Enqueues a command to run, which will be run during the next Update</summary>
	/// <param name="commandWithArguments">Command string, with arguments.</param>
	public void EnqueueRun(string commandWithArguments) {
		instructionList.Add(commandWithArguments);
	}
	public void Run (string commandWithArguments) {
		if (waitingToReadLine != null) {
			waitingToReadLine (commandWithArguments);
			waitingToReadLine = null;
		} else if (onInput != null) {
			onInput(commandWithArguments);
		} else {
			if (commandWithArguments == null || commandWithArguments.Length == 0) { return; }
			string s = commandWithArguments.Trim (whitespace); // cut leading & trailing whitespace
			string[] args = ParseArguments (s).ToArray();
			if (args.Length < 1) { return; }
			if (onCommand != null) { onCommand (args); }
			Run (args [0].ToLower (), args);
		}
	}
	/// <param name="command">Command.</param>
	/// <param name="args">Arguments. [0] is the name of the command, with [1] and beyond being the arguments</param>
	public void Run (string command, string[] args) {
		Command cmd = null;
		// try to find the given command. or the default command. if we can't find either...
		if (!commands.TryGetValue (command, out cmd) && !commands.TryGetValue ("", out cmd)) {
			// error!
			string error = "Unknown command '" + NoparseFilterAroundTag(command) + "'";
			if (args.Length > 1) { error += " with arguments "; }
			for (int i = 1; i < args.Length; ++i) {
				if (i > 1) { error += ", "; }
				error += "'" + NoparseFilterAroundTag(args [i]) + "'";
			}
			log (error);
		}
		// if we have a command
		if (cmd != null) {
			// execute it if it has valid code
			if (cmd.handler != null) {
				cmd.handler (args);
			} else {
				log ("Null command '" + command + "'");
			}
		}
	}
	#endregion // commands
	#region user interface
	public string promptArtifact = "$ ";
	[Tooltip ("the main viewable UI component")]
	private Canvas mainview;
	public enum Interactivity { disabled, screenOverlayOnly, worldSpaceOnly, activeScreenInactiveWorld };
	public Interactivity interactivity = Interactivity.activeScreenInactiveWorld;
	[Tooltip ("Which key shows the terminal")]
	public KeyCode keyToActivate = KeyCode.BackQuote;
	[Tooltip ("Which key hides the terminal")]
	public KeyCode keyToDeactivate = KeyCode.Escape;
	[Tooltip ("used to size the console Rect Transform on creation as a UI overlay")]
	public RectTransformSettings screenOverlaySettings;
	[Tooltip ("fill this out to set the console in the world someplace")]
	public PutItInWorldSpace worldSpaceSettings = new PutItInWorldSpace(0.005f, Vector2.zero);
	[Tooltip ("used to color the console on creation")]
	public InitialColorSettings colorSet = new InitialColorSettings();
	private TMPro.TMP_InputField tmpInputField;
	/// <summary>used to prevent multiple simultaneous toggles of visibility</summary>
	private bool togglingVisiblityWithMultitouch = false;
	[Tooltip ("If true, will show up and take user input immediately")]
	public bool activeOnStart = true;
	private bool needToShowUserPrompt = true;
	[Tooltip ("The TextMeshPro font used. If null, built-in-font should be used.")]
	public TMP_FontAsset textMeshProFont;
	/// <summary>which command line is currently active, and disabling user controls</summary>
	private static CmdLine currentlyActiveCmdLine, disabledUserControls;
	/// <summary>used to check which command line is the best one for the user controlling the main camera</summary>
	private float viewscore;

	[System.Serializable]
	public class InitialColorSettings {
		public Color background = new Color (0, 0, 0, 0.5f);
		public Color text = new Color (1, 1, 1);
		public Color errorText = new Color (1, 0, 0);
		public Color specialText = new Color (1, .75f, 0);
		public Color scrollbar = new Color (1, 1, 1, 0.5f);
		public Color userInput = new Color(.5f,1,.75f);
		public Color userSelection = new Color(1,.75f,.75f,.75f);
		public string input { get{ return CmdLine.ColorToHexCode (userInput); } }
		public string error { get{ return CmdLine.ColorToHexCode (errorText); } }
		public string special { get{ return CmdLine.ColorToHexCode (specialText); } }
	}
	[System.Serializable]
	public class RectTransformSettings {
		public Vector2 anchorMin = Vector2.zero, anchorMax = Vector2.one,
		offsetMin = Vector2.zero, offsetMax = Vector2.zero;
	}
	[System.Serializable]
	public class PutItInWorldSpace {
		[Tooltip("If zero, will automatically set to current Screen's pixel size")]
		public Vector2 screenSize = new Vector2(0,0);
		public float textScale = 0.005f;
		public PutItInWorldSpace(float scale, Vector2 size) {
			this.textScale = scale;
			this.screenSize = size;
		}
		public void ApplySettingsTo(Canvas c) {
			if (screenSize == Vector2.zero) { screenSize = new Vector2 (Screen.width, Screen.height); }
			c.renderMode = RenderMode.WorldSpace;
			RectTransform r = c.GetComponent<RectTransform> ();
			r.sizeDelta = screenSize;
			c.transform.localPosition = Vector3.zero;
			c.transform.localRotation = Quaternion.identity;
			r.anchoredPosition = Vector2.zero;
			r.localScale = new Vector3 (textScale, textScale, textScale);
		}
	}
	public bool IsInOverlayMode() {
		return mainview.renderMode == RenderMode.ScreenSpaceOverlay;
	}
	public void PositionInWorld(Vector3 center, Vector2 size = default(Vector2), float scale = 0.005f) {
		if (size == Vector2.zero) size = new Vector2 (Screen.width, Screen.height);
		PutItInWorldSpace ws = new PutItInWorldSpace(scale, size);
		if (mainview == null) {
			worldSpaceSettings = ws;
		} else {
			ws.ApplySettingsTo (mainview);
		}
	}
	public void SetOverlayModeInsteadOfWorld(bool useOverlay) {
		if (useOverlay && mainview.renderMode != RenderMode.ScreenSpaceOverlay) {
			mainview.renderMode = RenderMode.ScreenSpaceOverlay;
		} else if(!useOverlay) {
			worldSpaceSettings.ApplySettingsTo (mainview);
		}
	}
	private Canvas CreateUI () {
		mainview = FindComponentUpHierarchy<Canvas> (transform);
		if (!mainview) {
			mainview = (new GameObject ("canvas")).AddComponent<Canvas> (); // so that the UI can be drawn at all
			mainview.renderMode = RenderMode.ScreenSpaceOverlay;
			if (!mainview.GetComponent<CanvasScaler> ()) {
				mainview.gameObject.AddComponent<CanvasScaler> (); // so that text is pretty when zoomed
			}
			if (!mainview.GetComponent<GraphicRaycaster> ()) {
				mainview.gameObject.AddComponent<GraphicRaycaster> (); // so that mouse can select input area
			}
			mainview.transform.SetParent (transform);
		}
		GameObject tmpGo = new GameObject ("TextMeshPro - InputField");
		tmpGo.transform.SetParent (mainview.transform);
		Image img = tmpGo.AddComponent<Image> ();
		img.color = colorSet.background;
		if (screenOverlaySettings == null) {
			MaximizeRectTransform (tmpGo.transform);
		} else {
			RectTransform r = tmpGo.GetComponent<RectTransform> ();
			r.anchorMin = screenOverlaySettings.anchorMin;
			r.anchorMax = screenOverlaySettings.anchorMax;
			r.offsetMin = screenOverlaySettings.offsetMin;
			r.offsetMax = screenOverlaySettings.offsetMax;
		}
		tmpInputField = tmpGo.AddComponent<TMP_InputField> ();
		tmpInputField.lineType = TMP_InputField.LineType.MultiLineNewline;
		tmpInputField.textViewport = tmpInputField.GetComponent<RectTransform> ();
		TextMeshProUGUI tmpText = (new GameObject ("Text")).AddComponent<TextMeshProUGUI> ();
		if (textMeshProFont != null) {
			tmpText.font = textMeshProFont;
		}
		tmpText.fontSize = 14;
		tmpText.transform.SetParent (tmpGo.transform);
		tmpInputField.textComponent = tmpText;
		tmpInputField.fontAsset = tmpText.font;
		tmpInputField.pointSize = tmpText.fontSize;
		MaximizeRectTransform (tmpText.transform);
		tmpGo.AddComponent<RectMask2D> ();
		tmpInputField.onFocusSelectAll = false;
		tmpText.color = colorSet.text;
		tmpInputField.selectionColor = colorSet.userSelection;
		tmpInputField.customCaretColor = true;
		tmpInputField.caretColor = colorSet.userInput;
		tmpInputField.caretWidth = 5;
		tmpInputField.ActivateInputField ();
		tmpInputField.onValueChanged.AddListener (listener_OnValueChanged);
		tmpInputField.characterValidation = TMP_InputField.CharacterValidation.CustomValidator;
		tmpInputField.inputValidator = GetInputValidator();
		if (tmpInputField.verticalScrollbar == null) {
			GameObject scrollbar = new GameObject ("scrollbar vertical");
			scrollbar.transform.SetParent (tmpInputField.transform);
			scrollbar.AddComponent<RectTransform> ();
			tmpInputField.verticalScrollbar = scrollbar.AddComponent<Scrollbar> ();
			tmpInputField.verticalScrollbar.direction = Scrollbar.Direction.TopToBottom;
			RectTransform r = scrollbar.GetComponent<RectTransform> ();
			r.anchorMin = new Vector2 (1, 0);
			r.anchorMax = Vector2.one;
			r.offsetMax = Vector3.zero;
			r.offsetMin = new Vector2 (-16, 0);
		}
		if (tmpInputField.verticalScrollbar.handleRect == null) {
			GameObject slideArea = new GameObject ("sliding area");
			slideArea.transform.SetParent (tmpInputField.verticalScrollbar.transform);
			RectTransform r = slideArea.AddComponent<RectTransform> ();
			MaximizeRectTransform (slideArea.transform);
			r.offsetMin = new Vector2 (10, 10);
			r.offsetMax = new Vector2 (-10, -10);
			GameObject handle = new GameObject ("handle");
			Image bimg = handle.AddComponent<Image> ();
			bimg.color = colorSet.scrollbar;
			handle.transform.SetParent (slideArea.transform);
			r = handle.GetComponent<RectTransform> ();
			r.anchorMin = r.anchorMax = Vector2.zero;
			r.offsetMax = new Vector2 (5, 5);
			r.offsetMin = new Vector2 (-5, -5);
			r.pivot = Vector2.one;
			tmpInputField.verticalScrollbar.handleRect = r;
			tmpInputField.verticalScrollbar.targetGraphic = img;
		}
		// an event system is required... if there isn't one, make one
		StandaloneInputModule input = FindObjectOfType (typeof(StandaloneInputModule)) as StandaloneInputModule;
		if (input == null) {
			input = (new GameObject ("<EventSystem>")).AddComponent<StandaloneInputModule> ();
		}
		// put all UI in the UI layer
		SetLayerRecursive (mainview.gameObject, LayerMask.NameToLayer ("UI"));
		// turn it off and then back on again... that fixes some things.
		tmpGo.SetActive (false); tmpGo.SetActive (true);
		// put it in the world (if needed)
		if (interactivity == Interactivity.worldSpaceOnly
			|| interactivity == Interactivity.activeScreenInactiveWorld) {
			worldSpaceSettings.ApplySettingsTo (mainview);
		}
		return mainview;
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
		return mainview != null && mainview.gameObject.activeInHierarchy;
	}
	/// <summary>shows (true) or hides (false).</summary>
	public void SetVisibility (bool visible) {
		if (mainview == null) {
			activeOnStart = visible;
		} else {
			mainview.gameObject.SetActive (visible);
		}
	}
	/// <param name="enabled">If <c>true</c>, reads from keybaord. if <c>false</c>, stops reading from keyboard</param>
	public void SetInputActive(bool enabled) {
		if (enabled) {
			tmpInputField.ActivateInputField ();
		} else {
			tmpInputField.DeactivateInputField ();
		}
	}
	/// <param name="enableInteractive"><c>true</c> to turn this on (and turn the previous CmdLine off)</param>
	public void SetInteractive(bool enableInteractive) {
		bool activityWhenStarted = tmpInputField.interactable;
		if (enableInteractive && currentlyActiveCmdLine != null) {
			currentlyActiveCmdLine.SetInteractive (false);
		}
		tmpInputField.interactable = enableInteractive; // makes focus possible
		switch (interactivity) {
		case Interactivity.screenOverlayOnly:
			if (!IsInOverlayMode ()) {
				SetOverlayModeInsteadOfWorld (true);
			}
			SetVisibility (enableInteractive);
			break;
		case Interactivity.worldSpaceOnly:
			if (!IsVisible ()) {
				SetVisibility (true);
			}
			if (enableInteractive)
				SetOverlayModeInsteadOfWorld (false);
			break;
		case Interactivity.activeScreenInactiveWorld:
			if (!IsVisible ()) {
				SetVisibility (true);
			}
			SetOverlayModeInsteadOfWorld (enableInteractive);
			break;
		}
		tmpInputField.verticalScrollbar.value = 1; // scroll to the bottom
		MoveCaretToEnd (); // move caret focus to end
		SetInputActive (tmpInputField.interactable); // request/deny focus
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
	public bool IsInteractive() { return tmpInputField != null && tmpInputField.interactable; }
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
			string s = cmd.tmpInputField.text;
			int cursor = cmd.tmpInputField.caretPosition; // should this be caretPosition?
			for(int i = 0; i < userInput.Length; ++i) {
				char c = userInput [i];
				Validate(ref s, ref cursor, c);
			}
			cmd.tmpInputField.text = s;
			cmd.tmpInputField.caretPosition = cursor;
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
			string s = cmd.tmpInputField.text;
			int returned = EndUserInput (ref s);
			cmd.tmpInputField.text = s;
			return returned;
		}
		public bool HasProperInputTags(string text) {
			List<string> tags = CmdLine.CalculateTextMeshProTags(text, false);
			if (tags.Count == 0 || !tags.Contains ("noparse"))
				return false;
			string colorTag = "#" + cmd.colorSet.input;
			return tags.Contains(colorTag);
		}
		public bool CheckIfUserInputTagsArePresent(string text) {
			// check if the new text has the input tags opened
			return isUserEnteringInput = HasProperInputTags(text);
		}
		public string BEGIN_USER_INPUT() {
			return "<#" + cmd.colorSet.input + "><noparse>";
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
							cmd.EnqueueRun(inpt.Substring(start, len));
						}
						start = end+1;
					}
				} while(end > 0);
				if (start < inpt.Length) {
					cmd.EnqueueRun(inpt.Substring(start));
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
	private static CmdLine instance;
	public static CmdLine Instance {
		get {
			if (instance == null) {
				if ((instance = FindObjectOfType (typeof(CmdLine)) as CmdLine) == null) {
					GameObject g = new GameObject ();
					instance = g.AddComponent<CmdLine> ();
					g.name = "<" + instance.GetType ().Name + ">";
				}
			}
			return instance;
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
	private static readonly char[] quotes = new char[] { '\'', '\"' },
	whitespace = new char[] { ' ', '\t', '\n', '\b', '\r' };
	/// <returns>index of the end of the token that starts at the given index 'i'</returns>
	public static int FindEndArgumentToken (string str, int i) {
		bool isWhitespace;
		do {
			isWhitespace = System.Array.IndexOf (whitespace, str [i]) >= 0;
			if (isWhitespace) { ++i; }
		} while (isWhitespace && i < str.Length);
		int index = System.Array.IndexOf (quotes, str [i]);
		char startQuote = (index >= 0) ? quotes [index] : '\0';
		if (startQuote != '\0') { ++i; }
		while (i < str.Length) {
			if (startQuote != '\0') {
				if (str [i] == '\\') {
					i++; // skip the next character for an escape sequence. just leave it there.
				} else {
					index = System.Array.IndexOf (quotes, str [i]);
					bool endsQuote = index >= 0 && quotes [index] == startQuote;
					if (endsQuote) { i++; break; }
				}
			} else {
				isWhitespace = System.Array.IndexOf (whitespace, str [i]) >= 0;
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
		string token;
		List<string> tokens = new List<string> ();
		while (index < commandLineInput.Length) {
			int end = FindEndArgumentToken (commandLineInput, index);
			if (index != end) {
				token = commandLineInput.Substring (index, end - index).TrimStart (whitespace);
				token = Unescape (token);
				int qi = System.Array.IndexOf (quotes, token [0]);
				if (qi >= 0 && token [token.Length - 1] == quotes [qi]) {
					token = token.Substring (1, token.Length - 2);
				}
				tokens.Add (token);
			}
			index = end;
		}
		return tokens;
	}
	/* https://msdn.microsoft.com/en-us/library/aa691087(v=vs.71).aspx */
	private readonly static SortedDictionary<char, char> EscapeMap = new SortedDictionary<char, char> {
		{ '0','\0' }, { 'a','\a' }, { 'b','\b' }, { 'f','\f' }, 
		{ 'n','\n' }, { 'r','\r' }, { 't','\t' }, { 'v','\v' },
	};
	/// <summary>convenience method to unescape standard escape sequence strings</summary>
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
	public string GetAllText () { return (tmpInputField) ? GetRawText () : nonUserInput; }
	/// <param name="text">Text to add as output, also turning current user input into text output</param>
	public void AddText (string text) {
		EndUserInputIfNeeded ();
		setText (GetAllText () + text);
	}
	/// <param name="text">line to add as output, also turning current user input into text output</param>
	public void println (string line) { AddText (line + "\n"); }
	public void readLineAsync (DoAfterStringIsRead stringCallback) {
		if (!IsInteractive () && tmpInputField != null) { SetInteractive (true); }
		waitingToReadLine += stringCallback;
	}
	public void getInputAsync(DoAfterStringIsRead stringCallback) { readLineAsync (stringCallback); }
	public static void GetInputAsync(DoAfterStringIsRead stringCallback) { Instance.readLineAsync (stringCallback); }
	public static void ReadLine (DoAfterStringIsRead stringCallback) { Instance.readLineAsync (stringCallback); }
	/// <summary>Instance.println(line)</summary>
	public static void Log (string line) { Instance.println (line); }
	public void log (string line) { println (line); }
	public void readLine (DoAfterStringIsRead stringCallback) { readLineAsync (stringCallback); }
	public string GetRawText () { return tmpInputField.text; }
	public void SetRawText (string s) { if(tmpInputField != null){ tmpInputField.text = s; } }
	public int GetCaretPosition () { return tmpInputField.stringPosition; }
	public void SetCaretPosition (int pos) { tmpInputField.stringPosition = pos; }
	#endregion // pubilc API
	#region Debug.Log intercept
	[SerializeField, Tooltip ("If true, all Debug.Log messages will be intercepted and duplicated here.")]
	private bool interceptDebugLog = true;
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
	private void HandleLog (string logString, string stackTrace, LogType type) { log (logString); }
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
		Vector3 s = this.worldSpaceSettings.screenSize;
		if (s == Vector3.zero) {
			s = new Vector3 (
				Screen.width*transform.lossyScale.x*worldSpaceSettings.textScale,
				Screen.height*transform.lossyScale.y*worldSpaceSettings.textScale,
				1*transform.lossyScale.z*worldSpaceSettings.textScale);
		}
		Gizmos.DrawMesh(_editorMesh, transform.position, transform.rotation, s);
		Transform t = transform;
		// calculate extents
		Vector3[] points = new Vector3[]{(t.up*s.y/2 + t.right*s.x/-2),(t.up*s.y/2 + t.right*s.x/2),
			(t.up*s.y/-2 + t.right*s.x/2),(t.up*s.y/-2 + t.right*s.x/-2)};
		for (int i = 0; i < points.Length; ++i) { points[i] += t.position; }
		Gizmos.color = colorSet.background;
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
		// test code
		PopulateWithBasicCommands ();
		if (nonUserInput.Length == 0) {
			log (Application.productName + ", v" + Application.version);
		} else {
			setText (nonUserInput);
		}
		SetInteractive (activeOnStart);
	}
	void Update () {
		// toggle visibility based on key presses
		bool toggle = Input.GetKeyDown(IsInteractive () ? keyToDeactivate : keyToActivate );
		// or toggle visibility when 5 fingers touch
		if (Input.touches.Length == 5) {
			if (!togglingVisiblityWithMultitouch) {
				toggle = true;
				togglingVisiblityWithMultitouch = true;
			}
		} else {
			togglingVisiblityWithMultitouch = false;
		}
		if (toggle) {
			if (!IsInteractive ()) {
				// check to see how clearly the user is looking at this CmdLine
				if (mainview.renderMode == RenderMode.ScreenSpaceOverlay) {
					this.viewscore = 1;
				} else {
					Vector3 lookPosition = Camera.main.transform.position;
					Vector3 gaze = Camera.main.transform.forward;
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
			showBottomWhenTextIsAdded = tmpInputField.verticalScrollbar.value == 1;
		}
		if (showBottomWhenTextIsAdded) {
			tmpInputField.verticalScrollbar.value = 1;
		}
		if (needToShowUserPrompt && onInput == null && (waitingToReadLine == null || waitingToReadLine.GetInvocationList ().Length == 0)) {
			// in case of keyboard mashing...
			if (GetUserInputLength () > 0) {
				string userInput = GetUserInput ();
				SetText (nonUserInput);  GetInputValidator().EndUserInput (true);
				AddText (promptArtifact); GetInputValidator ().AddUserInput (userInput);
				nonUserInput = tmpInputField.text.Substring (0, tmpInputField.text.Length - userInput.Length);
			} else { AddText (promptArtifact); }
			needToShowUserPrompt = false;
		}
		// run any queued-up commands
		while (instructionList.Count > 0) {
			Run (instructionList[0]);
			instructionList.RemoveAt (0);
			needToShowUserPrompt = true;
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