using UnityEngine;
namespace NS.Contingency {
	[RequireComponent(typeof(Collider))]
	public class ContingentOnControllerColliderHit : _NS.Contingency.ContingencyCollide {
		void OnControllerColliderHit(CharacterController col) {
			DoActivateTrigger (col);
		}
	}
}