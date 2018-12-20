using UnityEngine;
namespace NS.Contingency {
	[RequireComponent(typeof(Collider))]
	public class ContingentOnCollisionStay : _NS.Contingency.ContingencyCollide {
		void OnCollisionStay (Collision col) {
			DoActivateTrigger (col);
		}
	}
}