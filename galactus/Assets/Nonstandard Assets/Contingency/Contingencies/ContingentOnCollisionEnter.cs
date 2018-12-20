using UnityEngine;
namespace NS.Contingency {
	[RequireComponent(typeof(Collider))]
	public class ContingentOnCollisionEnter : _NS.Contingency.ContingencyCollide {
		void OnCollisionEnter (Collision col) {
			DoActivateTrigger (col);
		}
	}
}