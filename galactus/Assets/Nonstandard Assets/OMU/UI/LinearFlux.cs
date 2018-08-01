using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TYPE = System.Single;

[System.Serializable]
/// TODO make the position and rate a templated type, so this can be used for non-linear paths in 3D
/// this class is used for mostly-linear systems, where every now-and-then, the linear rate changes. used for semi-uniform user-interface height/y-position calculations
public class LinearFlux {
	[System.Serializable]
	public struct DataPoint : System.IComparable {
		public float t;
		public TYPE position; // position
		public TYPE rate;
		public void Add(TYPE offset){
			position+=offset;
		}
		public void Scale(float scalar){position=position*scalar;rate=rate*scalar;}
		public DataPoint(float t, TYPE rate=default(TYPE), TYPE position=default(TYPE)){
			this.t=t;
			this.rate=rate;
			this.position=position;}
		public int CompareTo(object o) { if(o is DataPoint) { return t.CompareTo(((DataPoint)o).t); } return 0; }
        public override string ToString() { return t + "@" + position + ":" + rate; }
		public class PositionCompare:IComparer<DataPoint>{
			public int Compare(DataPoint a,DataPoint b){return (a).position.CompareTo((b).position);}}
		public static PositionCompare positionCompare = new PositionCompare();
		public bool Equals(DataPoint o){return t == o.t && position == o.position && rate == o.rate;}
        public TYPE GetPosition(float t){return (t - this.t) * rate + position;}
        public TYPE GetRate() { return rate; }
	}
	private List<DataPoint> flux = new List<DataPoint>();
    public List<DataPoint> GetData() { return flux; }
    public LinearFlux() { Reset(1, 0); }
	public LinearFlux(TYPE rate, TYPE position) {
        Reset(rate, position);
	}
    public void Reset(TYPE rate, TYPE position) {
		flux.Clear();
        flux.Add(new DataPoint(0, rate, position));
	}
    public int GetInternalFluxIndex(float t) {
        DataPoint e = new DataPoint(t);
        int kvpIndex = flux.BinarySearch(e);
        if (kvpIndex < 0) { kvpIndex = ~kvpIndex; } else { return kvpIndex; }
        if (kvpIndex == 0) {
            if (flux.Count == 0) throw new System.Exception("cannot get rate without data");
            kvpIndex = 1;
        }
        return kvpIndex - 1;
    }
	// public TYPE GetPosition(float t, DataPoint from) { return from.rate * (t-from.t) + from.position;}
    public TYPE GetRate(float t) {
        return flux[GetInternalFluxIndex(t)].GetRate();
    }
	public TYPE GetPosition(float t) {
        return flux[GetInternalFluxIndex(t)].GetPosition(t);
	}
	public float GetT(TYPE position) {
		DataPoint e = new DataPoint(0,default(TYPE),position);
		int kvpIndex = flux.BinarySearch(e, DataPoint.positionCompare);
		if(kvpIndex < 0) { kvpIndex = ~kvpIndex; } else { return flux[kvpIndex].t; }
		if(kvpIndex == 0) {
			if(flux.Count == 0) throw new System.Exception("cannot get t without data");
			kvpIndex=1;
		}
		e = flux[kvpIndex-1];
		return ((position - e.position) / e.rate) + e.t;
	}

    public void MakeRateChangeAt(float t, float deltaT, TYPE rate) {
        float position;// = GetPosition(0);

        // empty flux list
        if (flux == null || flux.Count == 0) {
            if (flux == null) flux = new List<DataPoint>();
            flux.Add(new DataPoint(t, rate, 0));
            //Debug.Log("new list");
            return;
        }
        // t is before element 0
        if (t < flux[0].t) {
            //float oldrate = flux[0].rate;
            float delta = flux[0].t - t;
            flux.Insert(0, new DataPoint(t, rate, flux[0].position - rate * delta));
            if(deltaT > 0 && t+deltaT < flux[1].t) {
                flux.Insert(0, new DataPoint(t+deltaT, 0, 0));
            }
            //Debug.Log("new 0th element");
            return;
        }
        // t is after last element
        DataPoint lastone = flux[flux.Count - 1];
        if (t > lastone.t) {
            float delta = t - lastone.t;
            DataPoint thisone = new DataPoint(t, rate, lastone.position + lastone.rate * delta);
            flux.Add(thisone);
            if(deltaT > 0) {
                DataPoint endcap = new DataPoint(t + deltaT, lastone.rate, thisone.position + thisone.rate * deltaT);
                flux.Add(endcap);
            }
            //Debug.Log("new last element");
            return;
        }
        bool valueAdded = false;
        // find if it's an existing data point that must be reset
        for (int i = 0; i < flux.Count; ++i) {
            if(flux[i].t == t) {
                // change it
                float oldrate = flux[i].rate;

                //flux[i].SetRate(rate);
                DataPoint thisone = flux[i];
                thisone.rate = rate;
                flux[i] = thisone;

                if (deltaT > 0 && ((i < flux.Count-1 && flux[i+1].t > t+deltaT) || i == flux.Count-1)) {
                    // check the next one (the cap)
//                    DataPoint cap = flux[i + 1];
                    //if(cap.t > t+deltaT) {
                    //    throw new System.Exception("deltaT too large. "+cap.t+" > "+(t + deltaT));
                    //}
                    // if there is no cap, add one
//                    if(cap.t < t+deltaT) {
                        DataPoint cap = new DataPoint(t + deltaT, oldrate, flux[i].position + deltaT * rate);
                        flux.Insert(i + 1, cap);
//                    }
                }
                //Debug.Log("replacing old value");
                valueAdded = true;
                break;
            }
        }
        if(!valueAdded) {
            // find if it's supposed to be between existing data points
            for (int i = 0; i < flux.Count-1; i++) {
                if(t > flux[i].t && t <= flux[i+1].t) {
                    float oldrate = flux[i].rate;

                    float delta = t - flux[i].t;
                    DataPoint thisone = new DataPoint(t, rate, flux[i].position + flux[i].rate * delta);
                    flux.Insert(i + 1, thisone);
                    //Debug.Log("inserting into the middle");

                    if(deltaT > 0) {
                        // check the next one (the cap)
                        DataPoint cap = flux[i + 2];
                        //if (cap.t > t + deltaT) {
                        //    throw new System.Exception("deltaT too large. " + cap.t + " > " + (t + deltaT));
                        //}
                        // if there is no cap, add one
                        if (cap.t > t + deltaT) {
                            cap = new DataPoint(t + deltaT, oldrate, flux[i].position + deltaT * rate);
                            flux.Insert(i + 2, cap);
                            //Debug.Log("added cap");
                        } else {
                            //Debug.Log("ignored cap "+cap.t+" "+(t+deltaT));
                        }
                    }
                    break;
                }
            }
        }

        // validate 't' values are in order, with no duplicates
        for (int i = 1; i < flux.Count-1; ++i) {
            if(flux[i].t >= flux[i+1].t) {
                throw new System.Exception("Element " + (i) + " is out of order! " +
                                           flux[i].t + " should be less than the next element, " + flux[i + 1].t);
            }
        }
        // collapse data points where possible
        for (int i = 1; i < flux.Count; ++i) {
            if (flux[i].rate == flux[i - 1].rate) {
                flux.RemoveAt(i);
                i--;
                //Debug.Log("removing collapsable value");
            }
        }

        // enforce positions line up with rates
        for (int i = 0; i < flux.Count - 1; ++i) {
            DataPoint dp = flux[i];
            DataPoint np = flux[i + 1];
            float dt = np.t - dp.t;
            position = dp.position + dp.rate * dt;
            if (position != np.position) {
                np.position = position;
                flux[i+1] = np;
                //throw new System.Exception("inconsistent. index " + i +
                                           //" suggests t:" + np.t + " should be "
                                           //+ position + ", but index " + (i + 1)
                                           //+ " says " + np.position);
                //Debug.Log("fixing position "+i);
            }
        }

    }
    //public void AddInconsistency(float t, TYPE rate)
    //{
    //    TYPE valueAtT = GetPosition(t);
    //    AddInconsistency(t, rate, valueAtT);
    //}
    public void AssertConsistency() {
        float position = GetPosition(0);
        for (int i = 0; i < flux.Count-1; ++i) {
            DataPoint dp = flux[i];
            DataPoint np = flux[i + 1];
            float deltaT = np.t - dp.t;
            position = dp.position + dp.rate * deltaT;
            if(position != np.position) {
                np.position = position;
                flux[i+1] = np;
                //throw new System.Exception("inconsistent. index " + i + 
                                           //" suggests t:" + np.t + " should be " 
                                           //+ position + ", but index " + (i + 1)
                                           //+ " says " + np.position);
            }
        }
    }

	public override string ToString()
	{
        string s = "";
        for (int i = 0; i < flux.Count; ++i) {
            if (i > 0) s += ", ";
            s += flux[i];
        }
		return s;
	}
}