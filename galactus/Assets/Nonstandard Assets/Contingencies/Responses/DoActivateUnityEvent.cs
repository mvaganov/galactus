using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
	}
}