using UnityEngine;
using System.Collections;

namespace Spatial {
	public abstract class ConcreteArea : Area {
		public const float SMALL_NUM = 0.00000001f;
		//public const float SMALL_NUM = 1.0f/(2 << 20);

		public virtual bool CollidesWithLine(Line line) { return line.CollidesWith (this); }
		public virtual bool CollidesWith(Area area) {
			if(area.GetType() == typeof(Line)) { return CollidesWithLine(area as Line); }
			throw new System.Exception("don't know how to collide with "+area.GetType());
		}
		public abstract bool Contains(Vector3 p);
		// TODO? rename to GetClosestPointInVolume?
		public abstract Vector3 GetClosestPointTo (Vector3 point);
		public abstract Vector3 GetClosestPointOnSurface (Vector3 point, out Vector3 surfaceNormal);
		public abstract Vector3 GetLocation ();
		public abstract bool Raycast (Ray r, out RaycastHit hit);

		public abstract void Translate (Vector3 delta);
		public abstract void Rotate (Quaternion euler);
		public abstract void Scale (Vector3 coefficient);
		public virtual void Scale (float coefficient) { Scale(new Vector3(coefficient,coefficient,coefficient)); }

		/// <summary>Fixes the geometry problems using some generalized algorithm specific to each geometry type.</summary>
		public virtual void FixGeometryProblems() {}

		/// <param name="out_wireframeVertices">buffer to output wireframe vertices.</param>
		/// <returns>how many points were written into the buffer</returns>
		public abstract int Wireframe (Vector3[] out_wireframeVertices);

		public virtual LineRenderer Outline(ref GameObject obj, Color c = default(Color)) {
			Vector3[] verts = new Vector3[60];
			int numVerts = Wireframe (verts);
			LineRenderer lr = NS.Lines.Make (ref obj, verts, numVerts, c);
			return lr;
		}
	}
}
