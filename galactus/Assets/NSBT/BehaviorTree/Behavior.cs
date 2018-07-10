using UnityEngine;
using System.Collections;

// http://www.gamasutra.com/blogs/ChrisSimpson/20140717/221339/Behavior_trees_for_AI_How_they_work.php

namespace BT {
	/// <summary>
	/// Parent class for Behavior-Tree Behaviors. 
	/// override public bool HasState(){return false;} if the behavior has no internal state to remember.
	/// </summary>
	[System.Serializable]
	public abstract class Behavior : Behavable {

		public enum Status {uninitialized, success, failure, running, error};

		/// <returns>What text to potentially float above an AI's head when they are behaving this way</returns>
		public string GetDescription() { return description; }
		/// <summary>used by <see cref="Behavior.GetDescription()"/></summary>
		public string description;

		// Update is called once per frame
		public abstract Status Execute (BTOwner who);

		// called exactly once, when a behavior starts
		public virtual void Init (BTOwner who){}

		// called exactly once, when a behavior stops
		public virtual void Release (BTOwner who, Status state){}

		/// <returns>variable prefix, which identifies variables this node uses on behalf of it's owner</returns>
		public string VAR() { return "$"+this.GetHashCode(); }

		// /// <example>override public bool HasState(){return false;}</example><summary>
		// /// Return false if the behavior does not keep track of internal state. If the variable modifies no variables
		// /// internally, no clone needs to be created (better for memory). True by default, for safety's sake.</summary>
		// /// <returns><c>true</c>, if clone is required, <c>false</c> otherwise.</returns>
		// public virtual bool HasState(){return true;}

		public Status Behave(BTOwner who) {
			// if this behavior is not yet on the stack (it'sexecuting for thefirst time)
			if(who.GetCurrentBehavior() != this) {
//Debug.Log (Multiply("  ",who.behaviorStack.Count)+"Execute " + this.GetType ().ToString()+" \""+description+"\"");
				// if(HasState()) {
				// 	// make a copy of it (so that complex state changes will not alter the original)
				// 	Behavior clone = this.MemberwiseClone() as Behavior; // make a shallow copy. all we want is state.
				// 	clone.global = this; // make a note that this is a clone, in case globals need to be edited
				// 	// this clone is the one that will actually be executed
				// 	clone.Init(who);
				// 	who.behaviorStack.Push(clone);
				// 	return Status.running;//clone.Behave(who);
				// } else {
					Init (who);
					who.behaviorStack.Push(this);
				// }
			}
			// prepare for the worst behavior during execution
			Status result = Status.error;
			try{
				result = Execute (who);
			} catch(System.Exception e){
				BT.Behavior current = who.GetCurrentBehavior();
				Debug.LogWarning("error reported by: \""+current+"\": \""+((current!=null)?current.description:"NULL!")+"\"");
				Debug.LogError("ERROR IN BEHAVIOR:"+e.Message+"\n"+e.StackTrace);
			}
			// if the process ended (in success, failure, or error)
			if(result != Status.running) {
				Release (who, result);
//Debug.Log(Multiply("  ",who.behaviorStack.Count)+"next to execute: " +who.GetCurrentBehavior().description);
				if(who.GetCurrentBehavior() == this) // this may not be on the stack of the behavior modified the stack.
					who.behaviorStack.Pop(); // remove clone from the stack
			}
			return result;
		}
	}
}