using UnityEngine;
using System.Collections;

namespace BT {
	/// <summary>will continue to execute the subtask until the timer is done or the sub task fails.
	/// When the timer finishes, trigger failure if it's a limit, and success if its just a timer</summary>
	public class Timer : Decorator {
		/// <summary>when this timer will trigger</summary>
		public long time;

		override public void Init(BTOwner who) {
			who.variables[VAR()] = time + who.GetUpTimeMS();
			//time += who.GetUpTimeMS ();
		}
		override public Status Execute(BTOwner who) {
			Status status = base.Execute (who);
			if(who.GetUpTimeMS() >= OMU.Value.GetLong(who.variables, VAR())) {
				return Status.success;
			}
			return status;
		}
		/// <summary>explicitly requires internal state</summary>
		//override public bool HasState(){return true;}
	}
}