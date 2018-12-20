using UnityEngine;
namespace NS.Contingency {
	[RequireComponent(typeof(Collider2D))]
	public class ContingentOnCollisionExit2D : _NS.Contingency.ContingencyCollide {
		void OnCollisionExit2D (Collision2D col) {
			DoActivateTrigger (col);
		}
	}
}