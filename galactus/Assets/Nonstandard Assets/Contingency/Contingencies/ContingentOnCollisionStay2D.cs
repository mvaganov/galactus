using UnityEngine;
namespace NS.Contingency {
	[RequireComponent(typeof(Collider2D))]
	public class ContingentOnCollisionStay2D : _NS.Contingency.ContingencyCollide {
		void OnCollisionStay2D (Collision2D col) {
			DoActivateTrigger (col);
		}
	}
}