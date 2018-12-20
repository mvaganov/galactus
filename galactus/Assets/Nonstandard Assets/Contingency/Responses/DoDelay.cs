using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NS.Contingency.Response {
	public class DoDelay : _NS.Contingency.Response.DoActivateBasedOnContingency {
		[SerializeField]
		protected float secondsDelay;
		public bool activate = true;
		
		public _NS.Contingency.ObjectPtr next;

		public override int GetChildContingencyCount() { return 1; }
		public override Object GetChildContingency(int index) { return next.Data; }

		public float Seconds { set { this.secondsDelay = value; } get { return this.secondsDelay; } }
		public void DoActivate (object whatTriggeredThis, object whatIsBeingTriggerd, bool activate) {
			 NS.ActivateAnything.DoActivate(next, whatTriggeredThis, whatIsBeingTriggerd, activate, secondsDelay);
		}
		public void DoActivateTrigger (object whatTriggeredThis, object whatIsBeingTriggerd) { DoActivate(whatTriggeredThis, whatIsBeingTriggerd, activate); }
		public void DoDeactivateTrigger (object whatTriggeredThis, object whatIsBeingTriggerd) { DoActivate(whatTriggeredThis, whatIsBeingTriggerd, !activate); }

		private static string[] activeOptions = new string[]{"sec,  do", "sec,undo"};

		#if UNITY_EDITOR
		public override Object DoGUI(Rect _position, Object obj, Component self, PropertyDrawer_ObjectPtr p) {
			if(this == null) return null;
			//[seconds] "sec, do" [thing]
			Rect r = _position;
			float w = PropertyDrawer_ObjectPtr.defaultOptionWidth, 
			wl = PropertyDrawer_ObjectPtr.defaultLabelWidth;
			r.width = wl * 2 / 5;
			secondsDelay = EditorGUI.FloatField(r, secondsDelay);
			r.x += r.width;
			r.width = wl * 3 / 5;
			int choice = EditorGUI.Popup(r, activate?0:1, activeOptions);
			activate = (choice==0);
			r.x += r.width;
			r.width = _position.width - w - wl;

			PropertyDrawer_ObjectPtr.showLabel = false;

			Object prevObj = next.Data;
			next = new _NS.Contingency.ObjectPtr { Data = p.EditorGUIObjectReference(r, prevObj, self) };
			next = new _NS.Contingency.ObjectPtr { Data = PropertyDrawer_NS_Contingency_ObjectPtr.FilterDirectReferenceAdjustment_(next.Data, prevObj, self) };

			PropertyDrawer_ObjectPtr.showLabel = true;

			obj = p.ShowObjectPtrChoicesPopup(r, obj, self, true);
			r.x += r.width;
			r.width = w;
			return obj;
		}

		public override float CalcPropertyHeight (PropertyDrawer_ObjectPtr p) {
			SerializedObject childObj = new SerializedObject (this);
			SerializedProperty prop = childObj.FindProperty("next");
			return EditorGUI.GetPropertyHeight (prop);
		}
		#endif
	}
}