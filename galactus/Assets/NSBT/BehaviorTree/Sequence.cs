using UnityEngine;
using System.Collections;

namespace BT {
	public class Sequence : Composite {

		override public Status Execute (BTOwner whoExecutes) {
			int currentTask = GetCurrentIndex (whoExecutes);
//Debug.Log ("current index: " + currentTask);
			Status childstatus = children[currentTask].Behave(whoExecutes);
			Status status = childstatus;
			if(childstatus == Status.success) {
				status = Status.running;
			}
			++currentTask;
			if(currentTask >= GetChildCount()) {
				currentTask = 0;
				if(childstatus == Status.success) {
					status = Status.success;
				}
			}
//Debug.Log ("next index: " + currentTask);
			SetCurrentIndex (whoExecutes, currentTask);
			return status;
		}
	}
}