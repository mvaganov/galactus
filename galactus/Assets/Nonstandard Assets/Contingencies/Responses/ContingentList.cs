using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NS.Contingency.Response {
	public class ContingentList : _NS.Contingency.Response.DoActivateBasedOnContingency {
		public enum KindOfList {Normal_List, Sequence_List, Priority_List};
		public KindOfList kindOfList;
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

		public override int GetChildContingencyCount() {return elements.Count;}
		public override Object GetChildContingency(int index) { return elements[index].data; }

		#if UNITY_EDITOR
		public override Object DoGUI(Rect _position, Object obj, _NS.Contingency.Contingentable self, PropertyDrawer_EditorGUIObjectReference p) {
			float originalWidth = _position.width;
			float w = PropertyDrawer_EditorGUIObjectReference.defaultOptionWidth;
			float wl = PropertyDrawer_EditorGUIObjectReference.defaultLabelWidth;
			float h = PropertyDrawer_EditorGUIObjectReference.unitHeight;
			float indent = 32;

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
				Rect r = new Rect (indent, _position.y + h, _position.width + _position.x - indent, h);
				// label the type-of-list, and the number of elements
				Rect labelr = r;
				labelr.width = wl;
				kindOfList = PropertyDrawer_EditorGUIObjectReference.EditorGUI_EnumPopup<KindOfList>(labelr, kindOfList);
				labelr.width = r.width - wl;
				labelr.x += wl;
				int count = EditorGUI.IntField(labelr, sl.elements.Count);
				// update the list with the new size, adding null
				if (count != sl.elements.Count) {
					if (count < sl.elements.Count) {
						for (int i = sl.elements.Count - 1; i >= count; --i) { sl.elements.RemoveAt (i); }
					} else {
						for (int i = sl.elements.Count; i < count; ++i) { sl.elements.Add (new global::EditorGUIObjectReference (null)); }
					}
				}
				r.y += h;
				// draw the elements below
				for (int i = 0; i < sl.elements.Count; ++i) {
					Object prevObj = sl.elements [i].data;
					Object eobj = p.EditorGUIObjectReference (r, sl.elements [i].data, self);
					sl.elements [i] = new global::EditorGUIObjectReference (eobj);

					if(eobj != prevObj && ContingencyRecursionCheck() != null) {
						Debug.LogWarning("Disallowing recursion of "+eobj);
						sl.elements [i] = new global::EditorGUIObjectReference (prevObj);
					}

					_NS.Contingency.Contingentable c = sl.elements [i].data as _NS.Contingency.Contingentable;
					if(c != null) {
						if(c.gameObject == null) {
							sl.elements [i] = new global::EditorGUIObjectReference (null);
						}
					}
					float expectedHeight = CalculateElementHeight(p, sl.elements [i].data, h);
					r.y += expectedHeight + vpadding;
				}
			}
			PropertyDrawer_EditorGUIObjectReference.StandardOptionPopup(new Rect(_position.x+_position.width-w, _position.y, w, h), ref obj);
			return obj;
		}

		// really kludgy function... should be using GetPropertyHeight of the Contingentable, but only once I figure out what the seralization madness is all about
		float CalculateElementHeight(PropertyDrawer_EditorGUIObjectReference p, Object o, float defaultHeight) {
			SerializedProperty prop2 = null;
			if(o != null) {
				_NS.Contingency.Contingentable childObj2 = o as _NS.Contingency.Contingentable;
				if(childObj2 != null) {
					return childObj2.CalcPropertyHeight(p);
				}
			}
			float expectedHeight = (prop2!=null)?EditorGUI.GetPropertyHeight (prop2):defaultHeight;
			return expectedHeight;
		}
		public override float CalcPropertyHeight (PropertyDrawer_EditorGUIObjectReference p) {
			SerializedObject childObj = new SerializedObject (this);
			SerializedProperty prop = childObj.FindProperty("elements");
			return EditorGUI.GetPropertyHeight (prop);
		}
		#endif
	}
}