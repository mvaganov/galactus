using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NS.Contingency.Response {
	public class DoString : _NS.Contingency.Response.DoActivateBasedOnContingency {
		[SerializeField]
		protected string text;
		public enum Purpose {Log, Warning, Error, JustString};
		public Purpose purpose = Purpose.Log;
		public string Text { set { this.text = value; } get { return this.text; } }
		public void DoActivateTrigger (object whatTriggeredThis) { DoActivateTrigger(); }
		public void DoDeactivateTrigger (object whatTriggeredThis) { DoActivateTrigger(); }

		public void DoActivateTrigger() {
			switch(purpose) {
				case Purpose.Log:		Debug.Log(text);		break;
				case Purpose.Warning:	Debug.LogWarning(text);	break;
				case Purpose.Error:	Debug.LogError(text);	break;
			}
		}

		#if UNITY_EDITOR
		public override Object DoGUI(Rect _position, Object obj, Component self, PropertyDrawer_ObjectPtr p) {
			string t = Text;
			obj = p.DoGUIEnumLabeledString(_position, obj, self, ref purpose, ref t);
			Text = t;
			return obj;
		}
		#endif
	}
}