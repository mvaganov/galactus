using UnityEngine;
using System.Collections;

namespace BT {
	public class Selector : Composite {

		override public Status Execute (BTOwner whoExecutes) {
			int currentTask = GetCurrentIndex (whoExecutes);
			Status childstatus = children[currentTask].Behave(whoExecutes);
			Status status = childstatus;
			if(childstatus == Status.failure) {
				status = Status.running;
			}
			++currentTask;
			if(currentTask >= GetChildCount()) {
				currentTask = 0;
				if(childstatus == Status.failure) {
					status = Status.failure;
				}
			}
			SetCurrentIndex (whoExecutes, currentTask);
			return status;
		}
	}
}