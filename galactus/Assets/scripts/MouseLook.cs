using UnityEngine;
using System.Collections;

public class MouseLook : MonoBehaviour {
	public float xSensitivity = 5, ySensitivity = 5;
	public bool invertY = false;
	public enum Controlled { player, randomWalk, wallAvoid, randomWalkWallAvoid }
	public Controlled contrlledBy = Controlled.player;
	void Update () {
		Vector2 move = Vector2.zero;
		switch(contrlledBy) {
		case Controlled.player:     move = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));   break;
		case Controlled.randomWalk: move = new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)); break;
		case Controlled.wallAvoid:  move = WallAvoid();                                                       break;
		case Controlled.randomWalkWallAvoid:
			move = new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)) + WallAvoid();;
			break;
		}
		if(move.x != 0) {
			transform.Rotate(0, move.x * xSensitivity, 0);
		}
		if(move.y != 0) {
			if(invertY) { move.y *= -1; }
			transform.Rotate(-move.y * ySensitivity, 0, 0);
		}
	}
	public Vector2 WallAvoid() {
		Vector2 move = Vector2.zero;
		Ray r = new Ray(transform.position, transform.forward);
		RaycastHit rh = new RaycastHit();
		float rayLength = 50;
		PlayerForce pf = GetComponent<PlayerForce>();
		if(pf != null) { rayLength = pf.maxSpeed + pf.accelerationForce; }
		if(Physics.Raycast(r, out rh, rayLength)) {
			// if you're running directly at a wall, just pick *any* direction!
			if(rh.normal == -transform.forward) {
				move = new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
			} else {
				// otherwise, pick the direction most aligned with the normal of the wall
				move = new Vector2(Vector3.Dot(transform.up, rh.normal), Vector3.Dot(transform.right, rh.normal));
			}
		}
		return move;
	}
}
