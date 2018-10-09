using UnityEngine;
using System.Collections;

namespace Spatial {
	public interface Locatable {
		Vector3 GetLocation();
		Vector3 GetClosestPointTo(Vector3 start);
	}
}