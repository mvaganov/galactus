using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BT {
	public abstract class Composite : Behavior {
		/// <summary>made public for script access</summary>
		public Behavable[] children;
//		public List<Behavior> children = new List<Behavior>();

		override public void Init(BTOwner who) {
			SetCurrentIndex (who, 0);
		}
	//	override public void Release(BTOwner who) {
	//		SetCurrentIndex (who, 0);
	//	}

		private string IndexName() { return this.VAR (); }
		public int GetCurrentIndex(BTOwner who) { return OMU.Value.GetInt(who.variables, IndexName()); }// (int)who.variables[IndexName()]; }
		public void SetCurrentIndex(BTOwner who, int index) { who.variables[IndexName()] = index; }

		public void AddChild(Behavior behavior) {
			int count = ((children != null) ? children.Length : 0);
			Behavable[] c = new Behavior[count + 1];
			if(children != null) {
				for(int i = 0; i < children.Length; ++i) {
					c[i] = children[i];
				}
			}
			c [count] = behavior;
			children = c;
			//children.Add(behavior);
		}
		public int GetChildCount() { return children.Length; }
		public Behavable GetChild(int i) { return children[i]; }
	}
}