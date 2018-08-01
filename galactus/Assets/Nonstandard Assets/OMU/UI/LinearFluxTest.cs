using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinearFluxTest : MonoBehaviour
{

    public GameObject cube;

    public LinearFlux flux = new LinearFlux(1, 0);
    public float defaultSize = 0.5f;
    public List<GameObject> cubes;

    // Use this for initialization
    void Start()
    {
        //flux.AddInconsistency(0, defaultSize);
        flux.MakeRateChangeAt(0, -1, defaultSize);
        for (int i = 0; i < cubes.Count; ++i) {
            cubes[i] = Instantiate(cube) as GameObject;
            Expandycube ec = cubes[i].AddComponent<Expandycube>();
            ec.index = i;
            ec.defaultSize = defaultSize;
            ec.transform.localScale += Vector3.right * i;
            ec.parent = this;
            cubes[i].transform.position = transform.position + Vector3.down * defaultSize * i;
            //flux.AddInconsistency(i, ec.defaultSize);
        }
        Refresh();
    }

    // Update is called once per frame
    void Update()
    {

    }

    GameObject line;

    public void Refresh() {
        for (int i = 0; i < cubes.Count; ++i)
        {
            cubes[i].transform.position = transform.position + Vector3.down * flux.GetPosition(i);
        }
        List<Vector3> lp = new List<Vector3>();
        Vector3 p = transform.position - Vector3.up * 5 - Vector3.right * 5;
        float bigtmax = 30;
        float maxT = bigtmax;
        List<LinearFlux.DataPoint> data = flux.GetData();
        for (int i = 0; i < data.Count; ++i) {
            LinearFlux.DataPoint dp = data[i];
            Vector3 np = p - new Vector3(dp.t, -dp.position) * 0.5f;
            lp.Add(np);
            lp.Add(np);
            lp.Add(np + Vector3.up);
            lp.Add(np - Vector3.up);
            lp.Add(np);
            lp.Add(np);
            if(dp.t > maxT) {
                maxT = dp.t;
            }
        }
        if(maxT <= bigtmax){
            lp.Add(p - new Vector3(maxT, -flux.GetPosition(maxT)) * 0.5f);
        }
        NS.Lines.Make(ref line, lp.ToArray(), lp.Count, Color.red);
    }

    public void RateChange(float t, float deltat, float rate) {
    }

    public class Expandycube : MonoBehaviour {
        public Color normal = Color.white, expanded = Color.red;
        public bool isExpanded = false;
        public float defaultSize = 1, expandSize = 2;
        public LinearFluxTest parent;
        public int index = -1;

		private void OnMouseDown() {
            isExpanded = !isExpanded;
            GetComponent<Renderer>().material.color = isExpanded ? expanded : normal;
            //if (true) {
                parent.flux.MakeRateChangeAt(index, 1, isExpanded ? expandSize : defaultSize);
                //parent.flux.AssertConsistency();
                parent.Refresh();
            //} else {
            //    float prevRate = parent.flux.GetRate(index);
            //    float delta = 0;
            //    float whereTheCascadeStarts = index;
            //    if (isExpanded) {
            //        Debug.Log("expand "+parent.flux);
            //        delta = expandSize - prevRate;
            //        parent.flux.AddInconsistency(index, expandSize);
            //        Debug.Log(prevRate);
            //        LinearFlux.DataPoint theNextOne = parent.flux.flux[parent.flux.GetInternalFluxIndex(index+1)];
            //        // if there is no next
            //        int findex;
            //        if (theNextOne.t != index + 1) {
            //            parent.flux.AddInconsistency(index + 1, prevRate);
            //            findex = parent.flux.GetInternalFluxIndex(index + 1);
            //        } else {
            //            findex = parent.flux.GetInternalFluxIndex(index);
            //        }
            //        for (int i = findex + 1; i < parent.flux.flux.Count; ++i) {
            //            LinearFlux.DataPoint dp = parent.flux.flux[i];
            //            dp.position += delta;
            //            parent.flux.flux[i] = dp;
            //        }
            //        parent.flux.AssertConsistency();
            //    } else {
            //        delta = defaultSize - prevRate;
            //        parent.flux.AddInconsistency(index, defaultSize);
            //        float rate = parent.flux.GetRate(index + 1);
            //        Debug.Log("contract "+rate);
            //        parent.flux.AddInconsistency(index + 1, rate, parent.flux.GetPosition(index + 1) + delta);

            //        int findex = parent.flux.GetInternalFluxIndex(index+1);
            //        for (int i = findex + 1; i < parent.flux.flux.Count; ++i) {
            //            LinearFlux.DataPoint dp = parent.flux.flux[i];
            //            dp.position += delta;
            //            parent.flux.flux[i] = dp;
            //        }
            //        parent.flux.AssertConsistency();
            //    }
            //    Debug.Log(parent.flux);
            //    parent.Refresh();
            //}
		}
	}
}
