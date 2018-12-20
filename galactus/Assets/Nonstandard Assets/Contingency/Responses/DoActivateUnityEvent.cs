using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NS.Contingency.Response {
	public class DoActivateUnityEvent : _NS.Contingency.Response.DoActivateBasedOnContingency {
		public UnityEngine.Events.UnityEvent howToActivate;
		public void DoActivateTrigger () { DoActivateTrigger(null, null); }
		public void DoActivateTrigger (object whatTriggeredThis, object whatIsDoingTheWork) {
			NS.ActivateAnything.DoActivate (howToActivate, whatTriggeredThis, whatIsDoingTheWork, true);
		}
		public void DoDeactivateTrigger (object whatTriggeredThis, object whatIsDoingTheWork) {
			NS.ActivateAnything.DoActivate (howToActivate, whatTriggeredThis, whatIsDoingTheWork, false);
		}

		#if UNITY_EDITOR
		public override Object DoGUI(Rect _position, Object obj, Component self, PropertyDrawer_ObjectPtr p) {
			float originalWidth = _position.width;
			float w = PropertyDrawer_ObjectPtr.defaultOptionWidth;
			float h = PropertyDrawer_ObjectPtr.unitHeight;
			_position.width = originalWidth - w;
			DoActivateUnityEvent sl = obj as DoActivateUnityEvent;
			if(sl == null) { return obj; }
			SerializedObject childObj = new SerializedObject (sl);
			SerializedProperty prop = childObj.FindProperty("howToActivate");
			// float vpadding = 2, originalHeight = _position.height;
			_position.height = h;
			_position.width = originalWidth - w;

			EditorGUI.PropertyField (_position, prop);

			return p.ShowObjectPtrChoicesPopup(new Rect(_position.x + _position.width - w, _position.y, w, h), obj, self, true);
		}

		public override float CalcPropertyHeight (PropertyDrawer_ObjectPtr p) {
			SerializedObject childObj = new SerializedObject (this);
			SerializedProperty prop = childObj.FindProperty("howToActivate");
			return EditorGUI.GetPropertyHeight (prop);
		}
		#endif
	}
}