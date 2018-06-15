using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NS.Contingency.Response {
	public class DoActivateDebugLog : _NS.Contingency.Response.DoActivateBasedOnContingency {
		[SerializeField]
		protected string text;
		public string Text { set { this.text = value; } get { return this.text; } }
		public void DoActivateTrigger (object whatTriggeredThis) { DoActivateTrigger(); }
		public void DoDeactivateTrigger (object whatTriggeredThis) { DoActivateTrigger(); }

		public void DoActivateTrigger() {
			switch(typeOfLog) {
				case DebugLogTypes.Log:		Debug.Log(text);		break;
				case DebugLogTypes.Warning:	Debug.LogWarning(text);	break;
				case DebugLogTypes.Error:	Debug.LogError(text);	break;
			}
		}

		public enum DebugLogTypes {Log, Warning, Error};
		public DebugLogTypes typeOfLog = DebugLogTypes.Log;
		#if UNITY_EDITOR
		public override Object DoGUI(Rect _position, Object obj, _NS.Contingency.Contingentable self, PropertyDrawer_EditorGUIObjectReference p) {
			string t = Text;
			obj = PropertyDrawer_EditorGUIObjectReference.DoGUIEnumLabeledString<DebugLogTypes>(
				_position, obj, self, p, ref typeOfLog, ref t);
			Text = t;
			return obj;
		}
		#endif
	}
}