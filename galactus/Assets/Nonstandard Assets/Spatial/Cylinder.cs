using UnityEngine;
using System.Collections;

namespace Spatial {
	// TODO FIXME finish this class plz.
	public class Cylinder : Area {
		public bool Contains(Vector3 point) {
			return false;
		}
		
		public Vector3 GetClosestPointTo(Vector3 point) {
			return point;
		}
		
		public Vector3 GetClosestPointOnSurface(Vector3 point, out Vector3 surfaceNormal) {
            surfaceNormal = Vector3.zero;
            return point;
		}
		
		public bool CollidesWith(Area area) {
			return false;
		}
		
		public Vector3 GetLocation() {
			return Vector3.zero;
		}

        public bool Raycast(Ray r, out RaycastHit hit)
        {
            hit = new RaycastHit();
            return false;
        }
    }
}