using UnityEngine;
namespace NS.Contingency {
	[RequireComponent(typeof(Collider2D))]
	public class ContingentOnTriggerExit2D : _NS.Contingency.ContingencyCollide {
		void OnTriggerExit2D (Collider2D col) {
			DoActivateTrigger (col);
		}
	}
}