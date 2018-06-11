using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NS.Contingency.Response {
	public class DoActivateDebugLog : _NS.Contingency.Response.DoActivateBasedOnContingency {
		public string toPrint;
		public void DoActivateTrigger (object whatTriggeredThis) {
			Debug.Log(toPrint);
		}
		public void DoDeactivateTrigger (object whatTriggeredThis) {
			Debug.Log(toPrint);
		}
	}
}