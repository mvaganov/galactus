using UnityEngine;
using System.Collections;

namespace BT {
	/// <summary>will return the opposite of what the sub task would return. 
	/// SUCCESS becomes FAIL and vice versa. ERROR, and RUNNING remain unchanged.</summary>
	public class Inverter : Decorator {
		override public Status Execute(BTOwner whoExecutes) {
			Status status = child.Behave(whoExecutes);
			switch(status){
				case Status.failure:	return Status.success;
				case Status.success:	return Status.failure;
			}
			return status;
		}
		//override public bool HasState(){return false;}
	}
}