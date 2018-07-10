using UnityEngine;
using System.Collections;

namespace Spatial {
	public class Capsule : Area {
		public bool Contains(Vector3 point) {
			return false;
		}
		
		public Vector3 GetClosestPointTo(Vector3 point) {
			return point;
		}
		
		public Vector3 GetClosestPointOnSurface(Vector3 point) {
			return point;
		}

		public bool CollidesWith(Area area) {
			return false;
		}

		public Vector3 GetLocation() {
			return Vector3.zero;
		}
	}
}