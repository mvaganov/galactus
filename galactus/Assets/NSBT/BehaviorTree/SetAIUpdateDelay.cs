using UnityEngine;
using System.Collections;

namespace BT {
	public class SetAIUpdateDelay : Behavior {
		// TODO this is really crappy. "varName" should just be "newUpdateDelay" as a string, and it should be parsed. If it's a variable, it should be parsed, otherwise, treat it like a Number.

		///[Tooltip("Which integer variable to use as the AI Update Delay")]
		public string varName;
		///[Tooltip("if the variable name above is empty or bad, use this value instead.")]
		public int timeMS = 1000;

		override public Status Execute (BTOwner who) {
			if(varName == null || varName.Length == 0
			|| !OMU.Value.TryGetInt(who.variables, varName, out who.aiTimerMS)) {
				who.aiTimerMS = timeMS;
			}
			return Status.success;
		}
	}
}
