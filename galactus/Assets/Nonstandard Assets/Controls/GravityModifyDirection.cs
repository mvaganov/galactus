using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NS {
	public class GravityModifyDirection : GravityModifyCenter {
		public Vector3 direction = Vector3.down;
		protected override GravityModifierBase SetGravityControl(MoveControls mc) {
			GravityModifierDirection gcd = mc.gameObject.AddComponent<GravityModifierDirection> ();
			gcd.direction = direction;
			return gcd;
		}
		void Start(){
			direction.Normalize ();
		}
	}

	public class GravityModifierDirection : GravityModifierBase {
		public Vector3 direction;
		void Update () {
			ApplyGravityDirection (direction);
		}
	}
}