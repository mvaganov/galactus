using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace NS.Contingency {
	public class ContingentOnKeyDown : _NS.Contingency.Contingentable {
		public enum OnInputType { none, keyDown, keyUp, key}

		[System.Serializable]
		public struct KeyBind {
			public KeyCode key;
			public OnInputType act;
			public EditorGUIObjectReference bound;// = new EditorGUIObjectReference();
			public bool IsActive() {
				switch (act) {
				case OnInputType.keyDown: return (Input.GetKeyDown (key));
				case OnInputType.keyUp:   return (Input.GetKeyUp (key));
				case OnInputType.key:     return (Input.GetKey (key));
				}
				return false;
			}
			public void Act() {
				if (IsActive ()) {
					NS.F.DoActivate (bound, this, this, true);
				}
			}
			public KeyBind(KeyCode key, OnInputType act, UnityEngine.Object obj) {
				this.key=key;this.act=act;this.bound.data=obj;
			}
		}

		public List<KeyBind> keyBindings = new List<KeyBind> ();

		void Reset() {
			if (keyBindings.Count == 0) {
				keyBindings.Add (new KeyBind(KeyCode.Escape, 
					OnInputType.keyDown, 
					NS.Contingency.Response.ScriptableString.Create("Hello") ) );
			}
		}

		public override bool IsContingencyFor(UnityEngine.Object whatToActivate) {
			for (int i = 0; i < keyBindings.Count; ++i) {
				if (keyBindings [i].bound.data == whatToActivate) {
					return true;
				}
			}
			return false;
		}

		void Start() { }

		void FixedUpdate() {
			for (int i = 0; i < keyBindings.Count; ++i) {
				keyBindings [i].Act();
			}
		}
	}
}
