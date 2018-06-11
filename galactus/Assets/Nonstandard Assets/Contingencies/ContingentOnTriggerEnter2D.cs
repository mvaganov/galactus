using UnityEngine;
namespace NS.Contingency {
	[RequireComponent(typeof(Collider2D))]
	public class ContingentOnTriggerEnter2D : _NS.Contingency.ContingencyCollide {
		void OnTriggerEnter2D (Collider2D col) {
			DoActivateTrigger (col);
		}
	}
}