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
		public override Object DoGUI(Rect _position, Object obj, _NS.Contingency.Contingentable self, PropertyDrawer_EditorGUIObjectReference p) {
			float originalWidth = _position.width;
			float w = PropertyDrawer_EditorGUIObjectReference.defaultOptionWidth;
			float h = PropertyDrawer_EditorGUIObjectReference.unitHeight;
			_position.width = originalWidth - w;
			DoActivateUnityEvent sl = obj as DoActivateUnityEvent;

			SerializedObject childObj = new SerializedObject (sl);
			SerializedProperty prop = childObj.FindProperty("howToActivate");
			// float vpadding = 2, originalHeight = _position.height;
			_position.height = h;
			_position.width = originalWidth - w;


			EditorGUI.PropertyField (_position, prop);

			// TODO also check for recursion...
//			Debug.Log(prop.objectReferenceValue);
//			Debug.Log(prop.serializedObject);
			// if( prop.objectReferenceValue == this) {
			// 	prop.objectReferenceValue = null;
			// }
			// _position.height = originalHeight;
			// _position.width = originalWidth;
			// if (EditorGUI.GetPropertyHeight (prop) > 16) {
			// 	float originalY = _position.y;
			// 	float cursor = _position.y + h + vpadding;
			// 	float indent = 32;// + EditorGUI.indentLevel;
			// 	Rect r = new Rect (indent, cursor, _position.width + _position.x - indent, h);
			// 	int count = EditorGUI.IntField(r, sl.elements.Count);
			// 	if (count != sl.elements.Count) {
			// 		if (count < sl.elements.Count) {
			// 			for (int i = sl.elements.Count - 1; i >= count; --i) {
			// 				sl.elements.RemoveAt (i);
			// 			}
			// 		} else {
			// 			for (int i = sl.elements.Count; i < count; ++i) {
			// 				if (i > 0) {
			// 					sl.elements.Add (sl.elements [i - 1]);
			// 				} else {
			// 					sl.elements.Add (new global::EditorGUIObjectReference (null));
			// 				}
			// 			}
			// 		}
			// 	}
			// 	r.y += h + vpadding;
			// 	//EditorGUI.indentLevel += 1;
			// 	for (int i = 0; i < sl.elements.Count; ++i) {
			// 		sl.elements [i] = new global::EditorGUIObjectReference (
			// 			p.EditorGUIObjectReference (r, sl.elements [i].data, self));
			// 		SerializedProperty prop2 = null;
			// 		if(sl.elements [i].data != null) {
			// 			SerializedObject childObj2 = new SerializedObject (sl.elements [i].data);
			// 			prop2 = childObj2.FindProperty("elements");
			// 		}
			// 		float expectedHeight = (prop2!=null)?EditorGUI.GetPropertyHeight (prop2):h;
			// 		r.y += expectedHeight + vpadding;
			// 	}
			// 	//EditorGUI.indentLevel -= 1;
			// 	_position.y = originalY;
			// }

//				childObj.ApplyModifiedProperties ();

			p.choice = EditorGUI.Popup(new Rect(_position.x+_position.width, 
				_position.y, w, h), 0, PropertyDrawer_EditorGUIObjectReference.editChoiceOrNullify);
			if (0 != p.choice && PropertyDrawer_EditorGUIObjectReference.editChoiceOrNullify[p.choice] == "null") {
				obj = null;
				p.choice = 0;
			}
			return obj;
		}

		public override float GetPropertyHeight (SerializedProperty _property, GUIContent label, PropertyDrawer_EditorGUIObjectReference p) {
			SerializedProperty asset = _property.FindPropertyRelative("data");
			DoActivateUnityEvent cl = asset.objectReferenceValue as DoActivateUnityEvent;
			SerializedObject childObj = new SerializedObject (cl);
			SerializedProperty prop = childObj.FindProperty("howToActivate");
			float h = EditorGUI.GetPropertyHeight (prop);
			return h;
		}
		#endif
	}
}