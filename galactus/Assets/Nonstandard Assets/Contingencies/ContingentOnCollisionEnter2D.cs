using UnityEngine;
namespace NS.Contingency {
	[RequireComponent(typeof(Collider2D))]
	public class ContingentOnCollisionEnter2D : _NS.Contingency.ContingencyCollide { 
		void OnCollisionEnter2D (Collision2D col) {
			DoActivateTrigger (col);
		}
	}
}