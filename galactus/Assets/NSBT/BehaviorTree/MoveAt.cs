using UnityEngine;
using System.Collections;

namespace BT {
	public class MoveAt : Behavior {
		public string targetVariableName;

		override public Status Execute (BTOwner whoExecutes) {
			object o;
			bool found = whoExecutes.variables.TryGetValue (targetVariableName, out o);
			if (found && o != null) {
				Spatial.Locatable loc = whoExecutes.GetLocation(targetVariableName);
				// AgentController ac = whoExecutes.GetComponent<AgentController>();
				// if(ac) {
				// 	ac.ai.SetSeekPosition(loc.GetLocation());
				// }
				MovingEntity_AI meai = whoExecutes.GetComponent<MovingEntity_AI>();
				meai.SetSeekLocation(loc.GetLocation());
				// throw new System.Exception("Need to find an AI component of "+whoExecutes+" and tell it to seek "+loc);
			} else {
				// AgentController ac = whoExecutes.GetComponent<AgentController>();
				// if(ac) {
				// 	ac.ai.ClearSeekPosition();
				// }
				// return Status.failure;
				MovingEntity_AI meai = whoExecutes.GetComponent<MovingEntity_AI>();
				meai.ClearSeekLocation();
				// throw new System.Exception("Need to find an AI component of "+whoExecutes+" and tell it to stop seeking");
			}
			return Status.success;
		}
		//override public bool HasState(){return false;}
	}
}