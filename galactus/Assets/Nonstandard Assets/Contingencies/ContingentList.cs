using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NS.Contingency {
	public class ContingentList : _NS.Contingency.Contingentable {
		public List<EditorGUIObjectReference> whatToActivate = new List<EditorGUIObjectReference>();

		public override bool IsContingencyFor (Object whatToActivate) { 
			for (int i = 0; i < this.whatToActivate.Count; ++i) {
				if (this.whatToActivate [i].data == whatToActivate)
					return true;
			}
			return false;
		}

		public virtual void DoActivateTrigger () { DoActivateTrigger(null); }
		public virtual void DoActivateTrigger (object causedActivate) {
			for (int i = 0; i < whatToActivate.Count; ++i) {
				NS.F.DoActivate (whatToActivate[i].data, causedActivate, this, true);
			}
		}
	}
}