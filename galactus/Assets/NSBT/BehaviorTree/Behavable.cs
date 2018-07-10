using UnityEngine;
using System.Collections;

namespace BT {
	/// <summary>Any AI that can be given to an agent</summary>
	public interface Behavable {

		/// <summary>Behave the specified who.</summary>
		/// <param name="who">Who needs to behave in the way that this class knows how to behave.</param>
		/// <returns>how the behavior is going (or has gone)</returns>
		Behavior.Status Behave(BTOwner who);

		/// <returns>What text to potentially float above an AI's head when they are behaving this way</returns>
		string GetDescription();
	}
}