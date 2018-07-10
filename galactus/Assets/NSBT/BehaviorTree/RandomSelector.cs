using UnityEngine;
using System.Collections;

namespace BT {
	/// <summary>picks one of these options at random and does it. Will continue doing the same one if it is still running</summary>
	public class RandomSelector : Composite {

		const int UNSET = -1;
		int randomIndex = UNSET;

		override public void Init(BTOwner whoExecutes) {
			if(randomIndex == UNSET) {
				randomIndex = Random.Range(0, GetChildCount());
			}
		}

		override public void Release(BTOwner whoExecutes, Status state) {
			if(state != Status.running)
				randomIndex = UNSET;
		}
		
		override public Status Execute (BTOwner whoExecutes) {
			return children[randomIndex].Behave(whoExecutes);
		}
	}
}