using UnityEngine;
namespace NS.Contingency {
	[RequireComponent(typeof(Collider2D))]
	public class ContingentOnTriggerStay2D : _NS.Contingency.ContingencyCollide {
		void OnTriggerStay2D (Collider2D col) {
			DoActivateTrigger (col);
		}
	}
}