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
			public _NS.Contingency.ObjectPtr bound; //= new ObjectPtr(null);
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
					NS.ActivateAnything.DoActivate (bound, this, this, true);
				}
			}
			public KeyBind(KeyCode key, OnInputType act, UnityEngine.Object obj) {
				this.key=key;this.act=act;this.bound = new _NS.Contingency.ObjectPtr { Data = obj };
			}
		}

		public List<KeyBind> keyBindings = new List<KeyBind> ();

		void Reset() {
			if (keyBindings.Count == 0) {
				NS.Contingency.Response.DoString d = gameObject.AddComponent<NS.Contingency.Response.DoString>();
				d.Text = "Hello";
				keyBindings.Add (new KeyBind(KeyCode.Escape, OnInputType.keyDown, d) );
			}
		}

		public override int GetChildContingencyCount() {return keyBindings.Count;}
		public override UnityEngine.Object GetChildContingency(int index) { return keyBindings [index].bound.Data; }
		void Start() { }

		void FixedUpdate() {
			for (int i = 0; i < keyBindings.Count; ++i) {
				keyBindings [i].Act();
			}
		}
	}
}
