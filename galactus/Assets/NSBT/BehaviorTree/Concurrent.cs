using UnityEngine;
using System.Collections;

namespace BT {
	/// <summary>Run all of these behaviors AT THE SAME TIME until one succeeds. Process heavy</summary>
	public class Concurrent : Sequence {
		private Status[] threadStatus;
		public bool failOnLimit = false;

		override public void Init (BTOwner who){
			threadStatus = new Status[GetChildCount ()];
			for(int i = 0; i < threadStatus.Length; ++i)
				threadStatus[i] = Status.uninitialized;
		}
		override public Status Execute(BTOwner who){
			Debug.LogError("this code is not done.");
			bool atLeastOneIsRunning = false;
			bool atLeastOneFailed = false;
			for(int i = 0; i < GetChildCount(); ++i) {
				switch(threadStatus[i]) {
					case Status.uninitialized:
					case Status.running:
					threadStatus [i] = children [i].Behave (who);
					if (threadStatus [i] == Status.failure) {
						atLeastOneFailed = true;
					}
					atLeastOneIsRunning = true;
					break;
				}
			}
			if (failOnLimit && atLeastOneFailed) { return Status.failure; }
			return atLeastOneIsRunning ? Status.running : Status.success;
		}
	}
}