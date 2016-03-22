using UnityEngine;
using System.Collections;

public class MouseLook : MonoBehaviour {
	public float xSensitivity = 5, ySensitivity = 5;
	public bool invertY = false;
	public enum Controlled { player, randomWalk, wallAvoid, randomWalkWallAvoid, getMostCenteredResource }
	public Controlled controlledBy = Controlled.player;

    public GameObject target;
    float timer = 0;
    GameObject view;
    GameObject targetCircle;

    GameObject v, up, rgt;

    // TODO look up seek code from other projects...
	void Update () {
		Vector2 move = Vector2.zero;
		switch(controlledBy) {
		    case Controlled.player:     move = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));   break;
		    case Controlled.randomWalk: move = new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)); break;
		    case Controlled.wallAvoid:  move = WallAvoid();                                                       break;
		    case Controlled.randomWalkWallAvoid:
			    move = new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)) + WallAvoid();;
			    break;
            case Controlled.getMostCenteredResource:
                timer -= Time.deltaTime;
                PlayerForce ml = GetComponent<PlayerForce>();
                Rigidbody rb = GetComponent<Rigidbody>();
                //float speed = rb.velocity.magnitude;
                ml.fore = 1;
                if (!target || timer <= 0)
                {
                    Vector3 start = transform.position + transform.forward * 20;
                    Vector3 end = transform.forward * 100 + Random.insideUnitSphere * 100;
//                    Lines.Make(ref view, Color.yellow, start, end, 20f, 20f);
                    RaycastHit hit = new RaycastHit();
                    if(Physics.CapsuleCast(start, end, 20, transform.forward, out hit))
                    {
                        ResourceNode n = hit.collider.gameObject.GetComponent<ResourceNode>();
                        if (n)
                        {
                            timer = 5;
                            target = hit.collider.gameObject;
//                            Lines.MakeCircle(ref targetCircle, Color.yellow, target.transform.position, 40, 1);
                        }
                    }
                    else
                    {
                        timer = Random.Range(10.0f, 200.0f);
                        Vector3 randomLoc = World.GetRandomLocation();
//                        Lines.MakeCircle(ref targetCircle, Color.yellow, randomLoc, 3, 1);
//                        Lines.Make(ref view, Color.yellow, randomLoc, randomLoc, 0, 0);
                        Vector3 delta = randomLoc - transform.position;
                        move.y = Vector3.Dot(delta, transform.up);
                        move.x = Vector3.Dot(delta, transform.right);
                        move.x += Random.Range(-xSensitivity, xSensitivity);
                        move.y += Random.Range(-ySensitivity, ySensitivity);
                        float dist = move.magnitude;
                        move /= dist;
                        move *= 10 * Time.deltaTime;
                    }
                }
                if (target)
                {
                    //Lines.Make(ref view, Color.yellow, transform.position, target.transform.position, .1f, .1f);
                    //Vector3 steerForce = Steering.Arrive(transform.position, rb.velocity, ml.maxSpeed, ml.accelerationForce, target.transform.position);
                    Vector3 steerForce = Steering.Seek(transform.position, rb.velocity, ml.maxSpeed, target.transform.position);
                    transform.LookAt(transform.position + steerForce);
                    move = Vector2.zero;
                    move.x += Random.Range(-2, 2);
                    move.y += Random.Range(-2, 2);
                    /*
                    Vector3 delta = target.transform.position - transform.position;
                    float distInFront = Vector3.Dot(delta, transform.forward);
                    move.y = Vector3.Dot(delta, transform.up);
                    move.x = Vector3.Dot(delta, transform.right);

                    Vector3 p = transform.position + transform.forward * distInFront;
                    Lines.Make(ref v, Color.blue, transform.position, p, .1f, .1f);
//                    Lines.Make(ref up, Color.green, p, p + transform.up * move.y, .1f, .1f);
                    p += transform.up * move.y;
//                    Lines.Make(ref rgt, Color.red, p, p + transform.right * move.x, .1f, .1f);

                    //move *= Time.deltaTime;
                    float dist = move.magnitude;
                    if (distInFront < Mathf.Abs(move.x) || distInFront < Mathf.Abs(move.y)) {
                        ml.fore = 0;
                    }
                    move /= dist;
                    move *= 10 * Time.deltaTime;
                    //                    move.x *= xSensitivity;
                    //                    move.y *= ySensitivity;
                    */
                }
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
