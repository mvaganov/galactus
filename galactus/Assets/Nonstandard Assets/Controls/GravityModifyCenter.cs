using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NS {
	public class GravityModifyCenter : MonoBehaviour {

		public const float SECONDS_TILL_PREVIOUS_GRAVITY_SOURCE_CAN_BE_REVISITED = 1;

		protected virtual GravityModifierBase SetGravityControl(MoveControls mc) {
			GravityModifierCenter gct = mc.gameObject.AddComponent<GravityModifierCenter> ();
			gct.center = transform;
			return gct;
		}

		public void DoGravity(GameObject target) {
			MoveControls mc = target.GetComponent<MoveControls> ();
			if (mc != null) {
				GravityModifierBase gcb = mc.gameObject.GetComponent<GravityModifierBase> ();
				if (gcb == null || (gcb.src != gameObject && gcb.prevSrc != gameObject)) {
					GameObject prevSource = (gcb != null) ? gcb.src : null;
					Destroy (gcb);
					gcb = SetGravityControl (mc);
					gcb.SetSources(gameObject, prevSource);
					NS.Timer.setTimeout (() => {
						gcb.prevSrc = null; // remove previous source on timer, to allow return
					}, (long)(SECONDS_TILL_PREVIOUS_GRAVITY_SOURCE_CAN_BE_REVISITED*1000));
				}
			}
		}
		void OnCollisionEnter(Collision col) {
			DoGravity (col.gameObject);
		}
		void OnTriggerEnter(Collider col) {
			DoGravity (col.gameObject);
		}
	}

	public class GravityModifierBase : MonoBehaviour {
		public MoveControls body;
		public CameraControls camCon;

		[HideInInspector]
		public GameObject src = null, prevSrc = null;

		public void SetSources(GameObject source, GameObject previousSource){
			src = source;
			prevSrc = previousSource;
		}

		void FindCameraControls() {
			if (camCon == null) { 
				if (Camera.main != null) {
					camCon = Camera.main.GetComponent<CameraControls> (); 
				}
			}
		}

		void Start () {
			if (body == null)   { body = GetComponent<MoveControls> (); }
			FindCameraControls ();
		}
		public void ApplyGravityDirection(Vector3 dir) {
			if (body.gravityDirection != dir) {
				body.gravityDirection = dir;
				if (camCon == null) {
					FindCameraControls ();
				}
				if (camCon != null) {
					camCon.SetStandDirectionTarget (-dir);
				}
			}
		}
		public void ApplyGravityCenteredOn(Vector3 gravityCenter){
			Vector3 delta = gravityCenter - body.transform.position;
			Vector3 dir = delta.normalized;
			ApplyGravityDirection (dir);
		}
		public static GravityModifierBase[] DestroyAllGravityControls(GameObject player){
			GravityModifierBase[] gc = player.GetComponents<GravityModifierBase> ();
			for (int i = gc.Length - 1; i >= 0; --i) {
				Destroy (gc [i]);
			}
			return gc;
		}
	}

	public class GravityModifierCenter : GravityModifierBase {
		public Transform center;
		void Update () {
			ApplyGravityCenteredOn (center.position);
		}
	}
}