using UnityEngine;
using System.Collections;

namespace BT {
	/// <summary>keep doing everything in here until failure or error</summary>
	public class Repeater : Sequence {
		override public Status Execute(BTOwner whoExecutes) {
	//		ExecuteLine1 (whoExecutes);
			Status state = Status.running;
			int currentTask = GetCurrentIndex (whoExecutes);
	//		for(; currentTask < children.Count; ++currentTask)
			{
				state = children[currentTask].Behave(whoExecutes);
				if(state == Status.running) {
	//				break;
				}
				Debug.LogError("code unfinished");
	//			if(!ContinueTask(whoExecutes, state)) {
	//				currentTask = 0;
	//				break;
	//			}
				if(currentTask >= GetChildCount())
					currentTask = 0;
			}
			return state;
		}
	}
}