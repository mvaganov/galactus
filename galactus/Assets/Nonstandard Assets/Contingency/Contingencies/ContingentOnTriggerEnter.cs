using UnityEngine;
namespace NS.Contingency {
	[RequireComponent(typeof(Collider))]
	public class ContingentOnTriggerEnter : _NS.Contingency.ContingencyCollide { 
		void OnTriggerEnter (Collider col) {
			DoActivateTrigger (col);
		}
	}
}