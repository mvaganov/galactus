using UnityEngine;
using System.Collections;

public class EntitySteering : MonoBehaviour {
	//public float xSensitivity = 5, ySensitivity = 5;
	public bool invertY = false;
	public enum Controlled { player, randomWalk, wallAvoid, randomWalkWallAvoid, getMostCenteredResource }
    // TODO remove, check MouseLook controlledByPlayer (or whatever)
	public Controlled controlledBy = Controlled.player;

	public Transform controllingTransform = null;

    public GameObject target;
    float timer = 0;
    GameObject view, viewS, viewE;
    public bool flee = false;

    GameObject v, up, rgt;
    GameObject specialAIBehavior;

    public void ClearTarget()
    {
        target = null;
    }

    public bool AisCloserThanB(GameObject A, GameObject B)
    {
        return Vector3.Distance(transform.position, A.transform.position) < Vector3.Distance(transform.position, B.transform.position);
    }

    public bool FollowThisTargetIfItsCloser(GameObject t)
    {
        // if there is no target, this one is it. de-facto.
        if (!target) { target = t; return true; }
        // if this newly found target is closer than the current target
        else if (AisCloserThanB(t, target))
        {
            // target the newly found target
            target = t;
            return true;
        }
        return false;
    }

    // TODO look up seek code from other projects...
	void Update () {
		Vector2 move = Vector2.zero;
		PlayerForce ml = GetComponent<PlayerForce>();
		Rigidbody rb = GetComponent<Rigidbody>();
		switch(controlledBy) {
		    case Controlled.player:     //move = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));   break;
				if(controllingTransform){
					Vector3 p = controllingTransform.position;
				Vector3 dir = Vector3.zero;
						//controllingTransform.forward
				float fore = Input.GetAxis("Vertical");
				float side = Input.GetAxis("Horizontal");
					if (fore != 0 || side != 0) { 
						if (fore != 0) { 
						dir = Steering.SeekDirectionNormal(controllingTransform.forward*ml.maxSpeed, rb.velocity, ml.accelerationForce, Time.deltaTime);
						dir *= fore; 
					}
						if (side != 0) {
							Vector3 right = Vector3.Cross (controllingTransform.up, dir);
							dir += right * side * (fore<0?-1:1);
						}
						if (fore != 0 && side != 0) {
							dir.Normalize ();
						}
					}
				ml.accelDirection = dir;
				}
				break;
		    //case Controlled.randomWalk: move = new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)); break;
		    case Controlled.wallAvoid:  move = WallAvoid();                                                       break;
		    case Controlled.randomWalkWallAvoid:
			    move = new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)) + WallAvoid();;
			    break;
            case Controlled.getMostCenteredResource:
				ResourceEater thisRe = ml.GetResourceEater();
                timer -= Time.deltaTime;
                //float speed = rb.velocity.magnitude;
                ml.fore = 1;
                if (!target || timer <= 0)
                {
                    if (target == World.GetInstance()) target = null;
                    //Vector3 start = transform.position + transform.forward * 20;
                    //Vector3 end = transform.forward * 100 + Random.insideUnitSphere * 100;

                    //Vector3 dir = (end - start).normalized;
                    //Lines.Make(ref view, Color.yellow, start, end, 20f, 20f);
                    //Lines.MakeCircle(ref viewS, Color.yellow, start, dir, 20f, 1f);
                    //Lines.MakeCircle(ref viewE, Color.yellow, end, dir, 20f, 1f);

                    //RaycastHit hit = new RaycastHit();
                    //RaycastHit[] hits = Physics.CapsuleCastAll(start, end, 20, transform.forward);
                    Ray r = new Ray(transform.position, Random.onUnitSphere);
                    RaycastHit[] hits = Physics.SphereCastAll(r, thisRe.GetRadius()+20, 100f);
                    if (hits != null && hits.Length > 0)//Physics.CapsuleCast(start, end, 20, transform.forward, out hit))
                    {
                        foreach (RaycastHit hit in hits)
                        {
                            ResourceNode n = hit.collider.GetComponent<ResourceNode>();
                            if (n)
                            {
                                if (FollowThisTargetIfItsCloser(hit.collider.gameObject))
                                {
                                    flee = false;
                                }
                                timer = Random.Range(1.0f, 2.0f);
                            }
                            else
                            {
                                ResourceEater re = hit.collider.GetComponent<ResourceEater>();
                                if (re && re != thisRe)
                                {
                                    if (re.GetMass() > thisRe.GetMass() * ResourceEater.minimumPreySize)
                                    {
                                        if (FollowThisTargetIfItsCloser(re.gameObject))
                                        {
                                            flee = true;
                                            timer = Random.Range(0.25f, 2.0f);
                                        }
                                    } else if(re.GetMass() * ResourceEater.minimumPreySize < thisRe.GetMass())
                                    {
                                        if (FollowThisTargetIfItsCloser(re.gameObject))
                                        {
                                            flee = false;
                                        }
                                    }
                                }
                            }

                        }
                    }
                    if (target == null)
                    {
                        timer = Random.Range(1.0f, 2.0f);
                        target = World.GetInstance().gameObject;
                    }
                }
                if (target)
                {
                    //ResourceEater re = target.GetComponent<ResourceEater>();
                    //if (re) Lines.Make(ref specialAIBehavior, flee?Color.yellow:Color.red, transform.position, re.transform.position, 0.1f, 0.1f);
                    Vector3 steerForce = Vector3.zero;

                    if (!flee)
                    {
                        //Lines.Make(ref view, Color.yellow, transform.position, target.transform.position, .1f, .1f);
                        steerForce = Steering.Arrive(transform.position, target.transform.position, rb.velocity, ml.maxSpeed, ml.accelerationForce, Time.deltaTime);
                        //Vector3 steerForce = Steering.Seek(transform.position, target.transform.position, rb.velocity, ml.maxSpeed, ml.accelerationForce, Time.deltaTime);
                    } else
                    {
                        steerForce = Steering.Flee(transform.position, target.transform.position, rb.velocity, ml.maxSpeed, ml.accelerationForce, Time.deltaTime);
                    }
                    transform.LookAt(transform.position + steerForce);
					ml.accelDirection = steerForce;
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
		//if(move.x != 0) {
		//	transform.Rotate(0, move.x * xSensitivity, 0);
		//}
		//if(move.y != 0) {
		//	if(invertY) { move.y *= -1; }
		//	transform.Rotate(-move.y * ySensitivity, 0, 0);
		//}
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
