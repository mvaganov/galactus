using System.Collections;
using System.Collections.Generic;
using Spatial;
using UnityEngine;

public class DelaunyTest : MonoBehaviour {
	public GameObject simpleSphere;
	public List<GameObject> points = new List<GameObject>();
	public int count = 10;
	public BoxCollider bounds;

	bool CalcCircumscriptionOK(int i, int j, int k)
	{
		float radius;
		Vector3 center, normal,
		a = points[i].transform.position,
		b = points[j].transform.position,
		c = points[k].transform.position;
		Triangle.CalculateCircumscription(a, b, c, out center, out radius, out normal);
        Collider[] hits = Physics.OverlapSphere(center, radius);
        for (int fe = 0; fe < hits.Length; fe++) {
            Transform t = hits[fe].transform;
            if (t != points[i].transform
                && t != points[j].transform
                && t != points[k].transform
                && t != transform)
            {
                return false;
            }
        }
        Triangle tri = new Triangle(a, b, c);
        GameObject lineObj = null;
        tri.Outline(ref lineObj, Color.black);
        return true;
	}

	GameObject go;
	// Use this for initialization
	void Start ()
	{
        Vector3 b = bounds.size;
		for(int i = 0; i < count; ++i) {
            Vector3 p = new Vector3(b.x*Random.value, b.y*Random.value, b.z*Random.value);
            p += bounds.center - bounds.size/2;
            //p += bounds.transform.position;
            points.Add(Instantiate(simpleSphere, p, Quaternion.identity) as GameObject);
		}
        for (int i = 0; i < count; ++i) {
            for (int j = i + 1; j < count; ++j) {
                for (int k = j + 1; k < count; ++k){
                    CalcCircumscriptionOK(i, j, k);
                }
            }
        }
	}
	
	// Update is called once per frame
	void Update ()
	{
		Vector3 p = Vector3.zero;
		p.x = (Input.mousePosition.x / Screen.width - 0.5f)*10;
		p.z = (Input.mousePosition.y / Screen.height - 0.5f)*10;
		NS.Lines.MakeCircle(ref go, p, Vector3.up, Color.white);
		
	}
}
