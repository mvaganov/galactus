using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NS.Contingency.Response {
	public class ContingentList : _NS.Contingency.Response.DoActivateBasedOnContingency {
		public List<EditorGUIObjectReference> elements = new List<EditorGUIObjectReference>();
		public virtual void DoActivateTrigger () { DoActivate(null, this, true); }
		public void DoActivate (object whatTriggeredThis, object whatIsBeingTriggerd, bool active) {
			elements.ForEach(
				o => NS.F.DoActivate(o, whatTriggeredThis, whatIsBeingTriggerd, active)
			);
		}
		public void DoActivateTrigger (object whatTriggeredThis, object whatIsBeingTriggerd) {
			DoActivate (whatTriggeredThis, whatIsBeingTriggerd, true);
		}
		public void DoDeactivateTrigger (object whatTriggeredThis, object whatIsBeingTriggerd) {
			DoActivate (whatTriggeredThis, whatIsBeingTriggerd, false);
		}

		public override bool IsContingencyFor (Object whatToActivate) { 
			for (int i = 0; i < this.elements.Count; ++i) {
				if (this.elements [i].data == whatToActivate)
					return true;
			}
			return false;
		}

		#if UNITY_EDITOR
		public override Object DoGUI(Rect _position, Object obj, _NS.Contingency.Contingentable self, PropertyDrawer_EditorGUIObjectReference p) {
			float originalWidth = _position.width;
			float w = PropertyDrawer_EditorGUIObjectReference.defaultOptionWidth;
			float h = PropertyDrawer_EditorGUIObjectReference.unitHeight;
			_position.width = originalWidth - w;
			ContingentList sl = obj as ContingentList;

			SerializedObject childObj = new SerializedObject (sl);
			SerializedProperty prop = childObj.FindProperty("elements");
			float vpadding = 2, originalHeight = _position.height;
			_position.height = h;
			_position.width = originalWidth - w;


			EditorGUI.PropertyField (_position, prop);

			// TODO also check for recursion...
//			Debug.Log(prop.objectReferenceValue);
//			Debug.Log(prop.serializedObject);
			// if( prop.objectReferenceValue == this) {
			// 	prop.objectReferenceValue = null;
			// }
			_position.height = originalHeight;
			_position.width = originalWidth;
			if (EditorGUI.GetPropertyHeight (prop) > 16) {
				float originalY = _position.y;
				float cursor = _position.y + h + vpadding;
				float indent = 32;// + EditorGUI.indentLevel;
				Rect r = new Rect (indent, cursor, _position.width + _position.x - indent, h);
				int count = EditorGUI.IntField(r, sl.elements.Count);
				if (count != sl.elements.Count) {
					if (count < sl.elements.Count) {
						for (int i = sl.elements.Count - 1; i >= count; --i) {
							sl.elements.RemoveAt (i);
						}
					} else {
						for (int i = sl.elements.Count; i < count; ++i) {
							if (i > 0) {
								sl.elements.Add (sl.elements [i - 1]);
							} else {
								sl.elements.Add (new global::EditorGUIObjectReference (null));
							}
						}
					}
				}
				r.y += h + vpadding;
				//EditorGUI.indentLevel += 1;
				for (int i = 0; i < sl.elements.Count; ++i) {
					sl.elements [i] = new global::EditorGUIObjectReference (
						p.EditorGUIObjectReference (r, sl.elements [i].data, self));
					// SerializedProperty prop2 = null;
					// if(sl.elements [i].data != null) {
					// 	SerializedObject childObj2 = new SerializedObject (sl.elements [i].data);
					// 	prop2 = childObj2.FindProperty("elements");
					// }
					// float expectedHeight = (prop2!=null)?EditorGUI.GetPropertyHeight (prop2):h;
					float expectedHeight = CalculateElementHeight(p, sl.elements [i].data, h);
					r.y += expectedHeight + vpadding;
				}
				//EditorGUI.indentLevel -= 1;
				_position.y = originalY;
			}

//				childObj.ApplyModifiedProperties ();

			p.choice = EditorGUI.Popup(new Rect(_position.x+_position.width-w, 
				_position.y, w, h), 0, PropertyDrawer_EditorGUIObjectReference.editChoiceOrNullify);
			if (0 != p.choice && PropertyDrawer_EditorGUIObjectReference.editChoiceOrNullify[p.choice] == "null") {
				obj = null;
				p.choice = 0;
			}
			return obj;
		}

		// really kludgy function... should be using GetPropertyHeight of the Contingentable, but only once I figure out what the seralization madness is all about
		float CalculateElementHeight(PropertyDrawer_EditorGUIObjectReference p, Object o, float defaultHeight) {
			SerializedProperty prop2 = null;
			if(o != null) {
				SerializedObject childObj2 = new SerializedObject (o);
				prop2 = childObj2.FindProperty("elements");
				if(prop2 == null) {
					prop2 = childObj2.FindProperty("howToActivate");
				}
			}
			float expectedHeight = (prop2!=null)?EditorGUI.GetPropertyHeight (prop2):defaultHeight;
			return expectedHeight;
		}
		public override float GetPropertyHeight (SerializedProperty _property, GUIContent label, PropertyDrawer_EditorGUIObjectReference p) {
			SerializedProperty asset = _property.FindPropertyRelative("data");
			ContingentList cl = asset.objectReferenceValue as ContingentList;
			SerializedObject childObj = new SerializedObject (cl);
			SerializedProperty prop = childObj.FindProperty("elements");
			return EditorGUI.GetPropertyHeight (prop);
		}
		#endif
	}
}