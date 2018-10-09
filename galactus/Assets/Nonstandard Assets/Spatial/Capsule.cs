using UnityEngine;
using System.Collections;

namespace Spatial {

	// TODO FIXME finish this class plz.
	public class Capsule : ConcreteArea {
        public Vector3 start, end;
        public float startRadius, endRadius;

		public override void Translate (Vector3 delta) { start += delta; end += delta; }
		public override void Rotate (Quaternion q) { start = q * start; end = q * end; }
		public override void Scale (Vector3 coefficient) {
			start.Scale(coefficient); end.Scale(coefficient);
			float s = (coefficient.x + coefficient.y + coefficient.z) / 3;
			startRadius *= s; endRadius *= s;
		}

		public override bool Contains(Vector3 point) {
			return false;
		}
		
		public override Vector3 GetClosestPointTo(Vector3 point) {
			return point;
		}
		
		public override Vector3 GetClosestPointOnSurface(Vector3 point, out Vector3 surfaceNormal) {
            surfaceNormal = Vector3.zero;
			return point;
		}

		public override bool CollidesWith(Area area) {
			if(area.GetType() == typeof(Line)) { return CollidesWith (area as Line); }
			return base.CollidesWith (area);
		}

		public override Vector3 GetLocation() {
			return Vector3.zero;
		}

		public override bool Raycast(Ray r, out RaycastHit hit) {
            hit = new RaycastHit();
            return false;
        }

		public override int Wireframe (Vector3[] out_wireframeVertices)
		{
			throw new System.NotImplementedException ();
//			return -1;
		}
    }
}