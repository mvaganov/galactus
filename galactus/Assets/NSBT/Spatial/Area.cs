using UnityEngine;
using System.Collections;

namespace Spatial {
	public interface Area : Locatable {

		bool Contains(Vector3 point);
		Vector3 GetClosestPointOnSurface(Vector3 point);
		bool CollidesWith(Area area);
	}
}