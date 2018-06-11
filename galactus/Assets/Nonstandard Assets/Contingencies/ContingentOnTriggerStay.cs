using UnityEngine;
namespace NS.Contingency {
	[RequireComponent(typeof(Collider))]
	public class ContingentOnTriggerStay : _NS.Contingency.ContingencyCollide {
		void OnTriggerStay (Collider col) {
			DoActivateTrigger (col);
		}
	}
}