using UnityEngine;

namespace Spatial {
	public class Location : Locatable {
		Transform t;
        Collider c;
        Vector3 position;

		public Location(Vector3 v) {	position = v;	}
		public Location(Transform t) {	this.t = t;	}
        public Location(Collider c) { this.c = c; }
		public Location(object o) {
			if(o is Transform) {
				this.t = (o as Transform);
			} else if(o is Vector3) {
				position = (Vector3)o;
			} else if(o is Collider) {
                c = (Collider)o;
            }
		}

	//	public static implicit operator Vector3(Location loc) { return loc.GetLocation (); }
	//	public static implicit operator Location(Vector3 v) { return new Location(v); }
	//	public static implicit operator Location(Transform t) { return new Location(t); }

		public Vector3 GetLocation() {
            if (t != null)
                return t.position;
            else if (c != null) return c.transform.position;
			return position;
		}
		public Vector3 GetClosestPointTo(Vector3 start) {
			return GetLocation ();
		}
	}
}