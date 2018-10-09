using UnityEngine;
using System.Collections;

namespace Spatial {
	public class Box : ConcreteArea {
		public Vector3 center, size;
		public Quaternion rotation;

		public override void Translate (Vector3 delta) { center += delta; }
		public override void Rotate (Quaternion q) { rotation = rotation * q; }
		public override void Scale (Vector3 coefficient) { center.Scale(coefficient); size.Scale(coefficient); }

		public Box(){}
		public Box(Vector3 center, Vector3 size, Quaternion rotation) {
			this.center = center; this.size = size; this.rotation = rotation;
		}

		static public Vector3 UnRotate(Quaternion rotation, Vector3 position) {
			return Quaternion.Inverse (rotation) * position;
		}

		public override bool Contains(Vector3 point) {
			Vector3 half = size / 2;
			return AABB.Contains (UnRotate(rotation, (center - point)), -1*half, half);
		}

		public override Vector3 GetClosestPointTo(Vector3 point) {
			Vector3 half = size / 2;
			Vector3 p = AABB.GetClosestPointTo (UnRotate(rotation, (center - point)), -1*half, half);
            return (rotation * p) + center;
        }

		public override Vector3 GetClosestPointOnSurface(Vector3 point, out Vector3 surfaceNormal) {
			Vector3 half = size / 2;
			Vector3 p = AABB.GetClosestPointOnSurface (UnRotate(rotation, (point - center)), -1*half, half, out surfaceNormal);
            surfaceNormal = rotation * surfaceNormal;
            return (rotation * p) + center;
		}

		public override bool Raycast(Ray r, out RaycastHit hit) {
            Vector3 half = size / 2;
            Ray translated = new Ray(UnRotate(rotation, (r.origin - center)), Quaternion.Inverse(rotation) * r.direction);
            bool actualHit = AABB.Raycast(translated, out hit, -half, half);
            hit.point = (rotation * hit.point) + center;
            hit.normal = rotation * hit.normal;
            return actualHit;
        }

		public override int Wireframe(Vector3[] out_wireframeVertices) {
            Vector3 half = size / 2;
			int writtenPoints = AABB.Wireframe(out_wireframeVertices, -half, half);
			for(int i = 0; i < writtenPoints; ++i) {
				out_wireframeVertices[i] = (rotation * out_wireframeVertices[i]) + center;
            }
			return writtenPoints;
        }

		private static Vector3[] faceDirections = { Vector3.left, Vector3.right, Vector3.down, Vector3.up, Vector3.back, Vector3.forward };

		public bool CollidesWithAABB(AABB aabb) { return CollidesWithBox (aabb.ToBox()); }

		public bool CollidesWithBox(Box b) {
			Sphere s1 = BoundingSphere ();
			Sphere s2 = b.BoundingSphere ();
			if (s1.CollidesWithSphere (s2)) {
				Vector3[] theseSides = new Vector3[faceDirections.Length];
				Vector3[] thoseSides = new Vector3[faceDirections.Length];
				Planar[] hisPlanes = new Planar [thoseSides.Length];
				Vector3 hisHalf = b.rotation * (b.size / 2);
				for (int i = 0; i < faceDirections.Length; ++i) {
					theseSides[i] =   rotation * faceDirections [i];
					thoseSides[i] = b.rotation * faceDirections [i];
					hisPlanes[i] = new Planar (b.center + (((i&2)==1) ? -hisHalf : hisHalf), b.rotation * faceDirections [i]);
				}
				// test planar collision of each side against each other side
				Planar myP;
				Vector3 myHalf = this.rotation * (this.size/2);
				Ray r = new Ray ();
				for (int i = 0; i < faceDirections.Length; ++i) {
					myP = new Planar (center + (((i&2)==1) ? -myHalf : myHalf), theseSides [i]);
					for (int j = 0; j < faceDirections.Length; ++j) {
						// check each ray of collision.
						if (myP.CollidesWith (hisPlanes [i], out r)) {
							float d0, d1;
							// return true if the ray of collision crosses both bounding spheres
							if (hisPlanes [i].Raycast (r, out d1) && myP.Raycast (r, out d0)) {
								return true; // TODO test plz!
							}
						}
					}
				}
			}
			return false;
		}

		public bool CollidesWithPlane(Planar p) {
			// check collision against each of the 6 sides
			return false;
		}

		public override bool CollidesWith(Area area) {
			       if(area.GetType() == typeof(Sphere)) { return CollidesWithSphere (area as Sphere);
			} else if(area.GetType() == typeof(Box)) {    return CollidesWithBox(area as Box);
				// do sphere-collision estimate
				// check each quad
			}
			return base.CollidesWith (area);
		}

		public bool CollidesWithSphere(Sphere s) {
			Vector3 half = size / 2;
			return AABB.CollidesWithSphere(-half, half, UnRotate(rotation, (s.center - center)), s.radius);
		}

		public Sphere BoundingSphere() {
			return new Sphere (center, size.magnitude/2);
		}

		public override Vector3 GetLocation() { return center; }
	}
}
