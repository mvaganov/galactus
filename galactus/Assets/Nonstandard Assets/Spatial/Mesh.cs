using UnityEngine;
using System.Collections;

namespace Spatial {
	public class Mesh : ConcreteArea {

		public override void Translate (Vector3 delta) { }
		public override void Rotate (Quaternion q) { }
		public override void Scale (Vector3 coefficient) { }

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
			return base.CollidesWith (area);
		}
		
		public override Vector3 GetLocation() {
			return Vector3.zero;
		}

		public override bool Raycast(Ray r, out RaycastHit hit)
        {
            hit = new RaycastHit();
            return false;
        }

		public override int Wireframe (Vector3[] out_wireframeVertices)
		{
			throw new System.NotImplementedException ();
		}
    }
}