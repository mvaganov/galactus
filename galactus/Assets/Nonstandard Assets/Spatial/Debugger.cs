using UnityEngine;
using System.Collections.Generic;

public class Debugger : MonoBehaviour {

    public Spatial.Area area;

    //public enum AreaType { AABB, Sphere, Box, Cylinder, Capsule, Mesh };
    //public AreaType areaType;

    private string geometry =
        //"Spatial.AABB{min:[-1,-1,-1] max:[3,3,3]}"
        //"Spatial.Sphere{c*:[8,0,0]r*:3}"
        //"Spatial.Box{s*:[1,3,5],c*:[0,5,0],r*:[0,45,45]}"
        //"Spatial.Planar{p*[3,3,3]n*[1,1,1]}"
        //"Spatial.Line{s*[3,3,3]e*[-3,-3,-5]}"
        //"Spatial.Circle{c*[3,1,3]r* 5}"

        //"Spatial.Triangle{a[1,1,1]b[3,3,3]c[-2,2,2]}"
        // TODO next!
        "Spatial.ConvexPolygon{p*[[1,1,1][3,2,1][2,1.5,3][1.5,1.5,3]]}"
        //"Spatial.Cone{s*[-1,1,1]e*[3,3,3]rS* 1,rE* 3}"
        //"Spatial.Capsule{s*[1,1,1]e*[3,3,3]rS* 1,rE* 3}"
        ;

    private GameObject lines;
    private GameObject[] line_surfaces, line_rays, line_views;
    private GameObject planeTest;

    private Vector3 location;

    private GameObject testCollision = null;

	private GameObject lineP1, lineP2, rayp1, rayp2, rayCross, rayCross2, THELINE;

	public Ray PlanarCollision(Spatial.Planar p1, Spatial.Planar p2) {
		//Ray result = new Ray ();
		RaycastHit p1rh, p2rh, crossrh, crossrh2;
		p2.Raycast (new Ray (p1.point, p1.normal), out p1rh);
		NS.Lines.Make (ref rayp1, p1.point, p1rh.point, Color.magenta);
		p2.Raycast (new Ray (p1.point, p2.normal), out p2rh);
		NS.Lines.Make (ref rayp2, p1.point, p2rh.point, Color.blue);

		Vector3 crossDir = Vector3.Cross (p1.normal, p2.normal).normalized;
		Debug.Log (crossDir+"        "+crossDir.magnitude);
		Debug.Log ("   dat: "+Vector3.Dot(p1.normal, p2.normal)+" ");
		bool doesCross = p2.Raycast (new Ray (p1.point, -crossDir), out crossrh);
		Debug.Log (doesCross+"    "+p1.point+" "+crossrh.point);
		//Lines.Make (ref rayCross, Color.green, p1.point, p1.point+crossDir*10, 0.1f, 0.1f);
		NS.Lines.Make (ref rayCross, p1.point, crossrh.point, Color.green);

		Vector3 perpCrossDir = Vector3.Cross (crossDir, p1.normal).normalized;
		p2.Raycast (new Ray (p1.point, perpCrossDir), out crossrh2);
		NS.Lines.Make (ref rayCross2, p1.point, crossrh2.point, Color.yellow);

		Vector3 delta = crossrh.point - crossrh2.point;
		Vector3 crossRayDir = delta.normalized;
		return new Ray (crossrh2.point, crossRayDir);
	}
	GameObject pA, pB, pC;
	// Use this for initialization
	void Start () {
		// testing planar intersection
//		Vector3 off = new Vector3 (-10, 2, 10);
//		Spatial.Planar p1 = new Spatial.Planar (new Vector3 (-3, 2, 3)+off, -new Vector3 (1, 1, 1).normalized);
//		Spatial.Planar p2 = new Spatial.Planar (new Vector3 (5, 2, -3)+off, new Vector3 (-0.25f, -1, 1).normalized);
//		Debug.Log (p1.point);
//		Vector3[] lineVerts = new Vector3[200];
//		p1.WireModel (lineVerts); Lines.Make (ref lineP1, Color.red, lineVerts, lineVerts.Length, .1f, .1f);
//		p2.WireModel (lineVerts); Lines.Make (ref lineP2, Color.cyan, lineVerts, lineVerts.Length, .1f, .1f);
//
//		Ray r;
//		Spatial.Planar.CollidesWith (p1, p2, out r);
//		Lines.Make (ref THELINE, Color.black, r.origin, r.origin+r.direction*3, 0.5f, 0.0f);

        //print(areaType.ToString());
        object o = null;
        try
        {
            o = OMU.Parser.Compile("geometry input", geometry);
        } catch(System.Exception e){
            o = null;
            Debug.Log(e);
        }
        print(o);
        area = o as Spatial.Area;

        if (area is Spatial.ConcreteArea) {
			Spatial.ConcreteArea a = area as Spatial.ConcreteArea;
			a.FixGeometryProblems ();
			a.Outline (ref lines, Color.white);
            lines.transform.SetParent (transform);
        }
        closestMarkers = new Transform[test.Length];
        line_surfaces = new GameObject[test.Length];
        line_rays = new GameObject[test.Length];
        line_views = new GameObject[test.Length];
        for (int i = 0; i < closestMarkers.Length; ++i)
        {
            closestMarkers[i] = (Instantiate(closestMarkerPrefab) as GameObject).transform;
        }
        
    }

    public Transform[] test;
    public Transform[] closestMarkers;
    public GameObject closestMarkerPrefab;

	GameObject shortLine, longline;

    // Update is called once per frame
    void Update () {
		//Spatial.Line output = new Spatial.Line();
		//Vector3 pX = test [0].position, dX = test [0].forward;
		//Spatial.Line.GetShortestLineBetweenLines (
		//	new Spatial.Line (new Vector3 (3, 3, 3), new Vector3 (-3, -3, -5)), 
		//	new Spatial.Line (pX, pX + dX * 10), output);
		//NS.Lines.Make (ref shortLine, output.start, output.end, Color.blue);
	    //NS.Lines.Make (ref longline, pX, pX + dX * 10, Color.red);

		if (area != null && test != null && test.Length > 0) {
            for (int i = 0; i < test.Length; ++i) {
                Transform t = test[i];
                Vector3 closest = area.GetClosestPointTo(t.position);
                closestMarkers[i].position = closest;

                Vector3 s;
                Vector3 p = area.GetClosestPointOnSurface(t.position, out s);
                GameObject line_surface = line_surfaces[i];
                GameObject line_ray = line_rays[i];
                GameObject line_view = line_views[i];
	            NS.Lines.Make(ref line_surface, p, p + s, Color.yellow);
                line_surface.transform.SetParent(transform);
                Ray r = new Ray(t.position, t.forward);
                RaycastHit hit;
                if (area.Raycast(r, out hit)) {
	                NS.Lines.Make(ref line_ray, hit.point, hit.point + hit.normal, Color.red);
                    if (line_ray) line_ray.SetActive(true);
	                NS.Lines.Make(ref line_view, r.origin, hit.point, (hit.distance >= 0)?Color.magenta:Color.green);
                } else
                {
                    if (line_ray) line_ray.SetActive(false);
	                NS.Lines.Make(ref line_view, r.origin, r.origin + r.direction, Color.magenta);
                }
                if (line_ray) line_ray.transform.SetParent(transform);
                line_view.transform.SetParent(transform);

                line_surfaces[i] = line_surface;
                line_rays[i] = line_ray;
                line_views[i] = line_view;
            }
        }
        if (testCollision)
        {
            // TODO create Spatial.Sphere, and test collision against it
            //Debugger other = testCollision.GetComponent<Debugger>();
        }
	}
}
