using UnityEngine;
using System.Collections;

namespace Spatial {
	public class Location : Locatable {
		Transform t;
		Vector3 position;

		public Location(Vector3 v) {	position = v;	}
		public Location(Transform t) {	this.t = t;	}
		public Location(object o) {
			if(o is Transform) {
				this.t = (o as Transform);
			} else if(o is Vector3) {
				position = (Vector3)o;
			}
		}

	//	public static implicit operator Vector3(Location loc) { return loc.GetLocation (); }
	//
	//	public static implicit operator Location(Vector3 v) { return new Location(v); }
	//
	//	public static implicit operator Location(Transform t) { return new Location(t); }

		public Vector3 GetLocation() {
			if(t != null)
				return t.position;
			return position;
		}
		public Vector3 GetClosestPointTo(Vector3 start) {
			return GetLocation ();
		}
	}
}