using UnityEngine;
using System.Collections;

namespace Spatial {
	public class Sphere : ConcreteArea {
		public Vector3 center;
		public float radius;

		public override void Translate (Vector3 delta) { center += delta; }
		public override void Rotate (Quaternion q) { }
		public override void Scale (Vector3 coefficient) { 
			center.Scale(coefficient);
			radius *= (coefficient.x+coefficient.y+coefficient.z)/3;
		}

		public Sphere() { }

		public Sphere(Vector3 center, float radius) { this.center = center; this.radius = radius; }

		public override bool Contains(Vector3 p) {
			return Vector3.Distance (center, p) <= radius;
		}
		public override Vector3 GetClosestPointTo(Vector3 point) {
			return GetClosestPointTo (point, center, radius);
		}
		static public Vector3 GetClosestPointTo(Vector3 point, Vector3 center, float radius) {
			Vector3 delta = point - center;
			if(delta.sqrMagnitude < radius*radius)
				return point;
			return center + (delta.normalized * radius);
		}
		public override Vector3 GetClosestPointOnSurface(Vector3 point, out Vector3 surfaceNormal) {
            Vector3 delta = point - center;
            surfaceNormal = delta.normalized;
            return center + (surfaceNormal * radius);
		}
		static public Vector3 GetClosestPointOnSurface(Vector3 point, Vector3 center, float radius) {
			Vector3 delta = point - center;
			return center + (delta.normalized * radius);
		}
		public static bool CollidesWith(Vector3 Acenter, float Aradius, Vector3 Bcenter, float Bradius) {
			return (Acenter - Bcenter).sqrMagnitude < (Aradius+Bradius);
		}
        /// <summary>find the distance along the ray till the sphere is intersected</summary>
        /// <param name="ray_o"></param>
        /// <param name="ray_dir"></param>
		/// <param name="ray_dist">the distance along the ray till the sphere is intersected</param>
		/// <returns></returns>
		bool Raycast(Vector3 rayOrigin, Vector3 rayDirection, out float out_rayDistance) {
            float b = Vector3.Dot(rayDirection, rayOrigin - center);
            Vector3 delta = rayOrigin - center;
            float c = delta.sqrMagnitude - (radius * radius);
            float d = b * b - c;
			out_rayDistance = -b - Mathf.Sqrt (d);
			if (out_rayDistance < 0) {
				rayDirection *= -1;
				b = Vector3.Dot(rayDirection, rayOrigin - center);
				d = b * b - c;
				out_rayDistance = b + Mathf.Sqrt (d);
			}
            if (d < 0.0f) {
                return false;
            }
            return true;
        }

		public override bool Raycast(Ray r, out RaycastHit hit) { // TODO make sure that the RaycastHit still calculates a point if the ray hits it backwards...
            hit = new RaycastHit();
			float d;
			bool doesHit = Raycast(r.origin, r.direction, out d);
			hit.distance = d;
			hit.point = r.direction * hit.distance + r.origin;
			hit.normal = (hit.point - center).normalized;
			return doesHit;
        }

		public bool CollidesWithSphere(Sphere s) {
			return CollidesWith (center, radius, s.center, s.radius);
		}

		public override bool CollidesWith(Area area) {
			if(area.GetType() == typeof(Sphere)) {      return CollidesWithSphere(area as Sphere);
			} else if(area.GetType() == typeof(AABB)) { return (area as AABB).CollidesWithSphere(this);
			} else if(area.GetType() == typeof(Box)) {  return (area as Box).CollidesWithSphere(this);
			}
			return base.CollidesWith (area);
		}
		public override Vector3 GetLocation() { return center; }
		public override int Wireframe (Vector3[] out_wireframeVertices) {
			Vector3[] verts = NS.Lines.CreateSpiralSphere (center, radius, Vector3.up, Vector3.right, 12, 4);
			for (int i = 0; i < verts.Length && i < out_wireframeVertices.Length; ++i) {
				out_wireframeVertices [i] = verts [i];
			}
			return Mathf.Min(verts.Length, out_wireframeVertices.Length);
		}
	}
}