﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NS.Contingency.Response {
	public class ContingentList : _NS.Contingency.Response.DoActivateBasedOnContingency {
		public enum KindOfList {Normal_List, Sequence_List, Priority_List};
		public KindOfList kindOfList;
		public string description;
		public List<_NS.Contingency.ObjectPtr> elements = new List<_NS.Contingency.ObjectPtr>();
		public virtual void DoActivateTrigger () { DoActivate(null, this, true); }
		public void DoActivate (object whatTriggeredThis, object whatIsBeingTriggerd, bool active) {
			elements.ForEach(
				o => NS.ActivateAnything.DoActivate(o, whatTriggeredThis, whatIsBeingTriggerd, active)
			);
		}
		public void DoActivateTrigger (object whatTriggeredThis, object whatIsBeingTriggerd) {
			DoActivate (whatTriggeredThis, whatIsBeingTriggerd, true);
		}
		public void DoDeactivateTrigger (object whatTriggeredThis, object whatIsBeingTriggerd) {
			DoActivate (whatTriggeredThis, whatIsBeingTriggerd, false);
		}

		public override int GetChildContingencyCount() {return elements.Count;}
		public override Object GetChildContingency(int index) { return elements[index].Data; }

		#if UNITY_EDITOR
		public override Object DoGUI(Rect _position, Object obj, Component self, PropertyDrawer_ObjectPtr p) {
			float originalWidth = _position.width;
			float w = PropertyDrawer_ObjectPtr.defaultOptionWidth;
			float wl = PropertyDrawer_ObjectPtr.defaultLabelWidth;
			float h = PropertyDrawer_ObjectPtr.unitHeight;
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
				kindOfList = Reflection.EditorGUI_EnumPopup(labelr, kindOfList);
				labelr.width = r.width - wl;
				labelr.x += wl;
				int count = EditorGUI.IntField(labelr, sl.elements.Count);
				// update the list with the new size, adding null
				if (count != sl.elements.Count) {
					if (count < sl.elements.Count) {
						for (int i = sl.elements.Count - 1; i >= count; --i) { sl.elements.RemoveAt (i); }
					} else {
						for (int i = sl.elements.Count; i < count; ++i) {
							sl.elements.Add (new _NS.Contingency.ObjectPtr {Data=null});
						}
					}
				}
				r.y += h;
				// draw the elements below
				for(int i = 0; i < sl.elements.Count; ++i) {
					Object prevObj = sl.elements[i].Data;
					Object eobj = p.EditorGUIObjectReference(r, sl.elements[i].Data, self);
					sl.elements[i] = new _NS.Contingency.ObjectPtr { Data = eobj };

					// prevent recursion of Contingent objects
					if(eobj != prevObj && ContingencyRecursionCheck() != null) {
						Debug.LogWarning("Disallowing recursion of "+eobj);
						sl.elements[i] = new _NS.Contingency.ObjectPtr { Data = prevObj };
					}
					// prevent assignment of illeagal Contingency
					_NS.Contingency.Contingentable c = sl.elements [i].Data as _NS.Contingency.Contingentable;
					if(c != null && c.gameObject == null) { sl.elements [i] = new _NS.Contingency.ObjectPtr {Data=null}; }

					float expectedHeight = CalculateElementHeight(p, sl.elements [i].Data, h);
					r.y += expectedHeight + vpadding;
				}
			}
			return p.ShowObjectPtrChoicesPopup(new Rect(_position.x + _position.width - w*2, _position.y, w, h), obj, self, true);
		}

		// really kludgy function... should be using GetPropertyHeight of the Contingentable, but only once I figure out what the seralization madness is all about
		float CalculateElementHeight(PropertyDrawer_ObjectPtr p, Object o, float defaultHeight) {
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
		public override float CalcPropertyHeight (PropertyDrawer_ObjectPtr p) {
			SerializedObject childObj = new SerializedObject (this);
			SerializedProperty prop = childObj.FindProperty("elements");
			return EditorGUI.GetPropertyHeight (prop);
		}
		#endif
	}
}