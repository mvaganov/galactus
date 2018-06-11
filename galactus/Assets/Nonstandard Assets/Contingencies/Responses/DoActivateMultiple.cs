using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NS.Contingency.Response {
	public class DoActivateMultiple : _NS.Contingency.Response.DoActivateBasedOnContingency {
		public List<EditorGUIObjectReference> whatToActivate = new List<EditorGUIObjectReference>();
		public void DoActivateTrigger () { DoActivateTrigger(null, this); }
		public void DoActivateTrigger (object whatTriggeredThis, object whatIsBeingTriggerd) {
			whatToActivate.ForEach(
				o => NS.F.DoActivate(o, whatTriggeredThis, whatIsBeingTriggerd, true)
			);
		}
		public void DoDeactivateTrigger (object whatTriggeredThis, object whatIsBeingTriggerd) {
			whatToActivate.ForEach(
				o => NS.F.DoActivate(o, whatTriggeredThis, whatIsBeingTriggerd, false)
			);
		}
	}
}