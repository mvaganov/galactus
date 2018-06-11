using UnityEngine;
namespace NS.Contingency {
	[RequireComponent(typeof(Collider))]
	public class ContingentOnCollisionExit : _NS.Contingency.ContingencyCollide {
		void OnCollisionExit (Collision col) {
			DoActivateTrigger (col);
		}
	}
}