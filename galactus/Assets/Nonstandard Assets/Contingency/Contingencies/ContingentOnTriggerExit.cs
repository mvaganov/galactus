using UnityEngine;
namespace NS.Contingency {
	[RequireComponent(typeof(Collider))]
	public class ContingentOnTriggerExit : _NS.Contingency.ContingencyCollide {
		void OnTriggerExit (Collider col) {
			DoActivateTrigger (col);
		}
	}
}