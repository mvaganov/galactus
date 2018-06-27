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
			NS.F.DoActivate (howToActivate, whatTriggeredThis, whatIsDoingTheWork, true);
		}
		public void DoDeactivateTrigger (object whatTriggeredThis, object whatIsDoingTheWork) {
			NS.F.DoActivate (howToActivate, whatTriggeredThis, whatIsDoingTheWork, false);
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

			//p.choice = EditorGUI.Popup(new Rect(_position.x+_position.width, 
			//	_position.y, w, h), 0, PropertyDrawer_EditorGUIObjectReference.editChoiceOrNullify);
			// if (0 != p.choice) {
			// 	if(PropertyDrawer_EditorGUIObjectReference.editChoiceOrNullify[p.choice] == PropertyDrawer_EditorGUIObjectReference.setToNull) {
			// 		obj = null;
			// 		p.choice = 0;
			// 	} else if(PropertyDrawer_EditorGUIObjectReference.editChoiceOrNullify[p.choice] == PropertyDrawer_EditorGUIObjectReference.delete) {
			// 		DestroyImmediate(obj);
			// 		obj = null;
			// 		p.choice = 0;
			// 	}

			// }
			PropertyDrawer_ObjectPtr.StandardOptionPopup(new Rect(_position.x+_position.width, _position.y, w, h), ref obj);
			return obj;
		}

		public override float CalcPropertyHeight (PropertyDrawer_ObjectPtr p) {
			SerializedObject childObj = new SerializedObject (this);
			SerializedProperty prop = childObj.FindProperty("howToActivate");
			return EditorGUI.GetPropertyHeight (prop);
		}
		#endif
	}
}