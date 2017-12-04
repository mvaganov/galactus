using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TYPE = System.Single;

[System.Serializable]
/// TODO make the position and rate a templated type, so this can be used for non-linear paths in 3D
public class LinearFlux {
	[System.Serializable]
	public struct DataPoint : System.IComparable {
		public float t;
		public TYPE position; // position
		public TYPE rate;
		public void Add(TYPE offset){
			// System.Reflection.MethodInfo mi = typeof(TYPE).GetMethod("op_Addition",
    		// System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public );
			// position = (TYPE)mi.Invoke(null,new object[]{position,offset});
			position+=offset;
		}
		public void Scale(float scalar){position=position*scalar;rate=rate*scalar;}
		public DataPoint(float index, TYPE rate=default(TYPE), TYPE position=default(TYPE)){
			this.t=index;
			this.rate=rate;
			this.position=position;}
		public int CompareTo(object o) { if(o is DataPoint) { return t.CompareTo(((DataPoint)o).t); } return 0; }
		public override string ToString() { return t+"@"+position; }
		public class PositionCompare:IComparer<DataPoint>{
			public int Compare(DataPoint a,DataPoint b){return (a).position.CompareTo((b).position);}}
		public static PositionCompare positionCompare = new PositionCompare();
		public bool Equals(DataPoint o){return t==o.t&&position==o.position&&rate==o.rate;}
		public TYPE GetPosition(float t){return rate * (t-this.t) + position;}
	}
	public List<DataPoint> flux = new List<DataPoint>();
	public LinearFlux(){}
	public LinearFlux(TYPE coefficient, TYPE initial) {
		Reset(coefficient,initial);
		// Debug.Log("generally, y = "+coefficient+"x + "+initial);
	}
	public void Reset(TYPE coefficient, TYPE initial){
		flux.Clear();
		flux.Add(new DataPoint(0,coefficient,initial));
	}
	// public TYPE GetPosition(float t, DataPoint from) { return from.rate * (t-from.t) + from.position;}
	public TYPE GetPosition(float t) {
		DataPoint e = new DataPoint(t);
		int kvpIndex = flux.BinarySearch(e);
		if(kvpIndex < 0) { kvpIndex = ~kvpIndex; } else { return flux[kvpIndex].position; }
		if(kvpIndex == 0) {
			if(flux.Count == 0) throw new System.Exception("cannot get position without data");
			kvpIndex=1;
		}
		e = flux[kvpIndex-1];
		return e.GetPosition(t);
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
	public void AddInconsistency(float t, TYPE rate, TYPE position, bool adjustLaterInconsistencies = true) {
		DataPoint e = new DataPoint(t,rate,position);
		int indexToPlace = flux.BinarySearch(e);
		bool needToReplace = false, newInconsistencyFound = false, newInitialValue = false;
		TYPE expectedPosition=0;
		if(indexToPlace >= 0) {
			// duplicate! ignored.
			if(e.Equals(flux[indexToPlace])) {
				// Debug.Log("ignoring duplicate");
				return;
			}
			expectedPosition = flux[indexToPlace].position;
			needToReplace = true;
		} else {
			indexToPlace = ~indexToPlace;
			// not-duplicate, also not-inconsistent...
			if(indexToPlace > 0) {
				DataPoint prevFluxPoint = flux[indexToPlace-1];
				expectedPosition = prevFluxPoint.GetPosition(t);
				if(e.position == expectedPosition && e.rate == prevFluxPoint.rate){
					Debug.Log("ignoring consistent");
					return;
				}
				newInconsistencyFound = true;
			} else {
				expectedPosition = GetPosition(t);
				newInitialValue = true;
			}
		}
		TYPE delta = e.position - expectedPosition;
		if(needToReplace) {
			// if instead of replacing, it could be removed...
			bool removeInstead = false;
			if(indexToPlace>0) {
				DataPoint prevFluxPoint = flux[indexToPlace-1];
				expectedPosition = prevFluxPoint.GetPosition(t);
				if(e.position == expectedPosition && e.rate == prevFluxPoint.rate){
					Debug.Log("removing consistent "+indexToPlace+" t:"+e);
					flux.RemoveAt(indexToPlace);
					if(delta == default(TYPE)) return;
					indexToPlace -= 1;
					removeInstead = true;
				}
			}
			if(!removeInstead){flux[indexToPlace]=e;}
		} else if(newInconsistencyFound || newInitialValue) {
			flux.Insert(indexToPlace, e);
		}
		Add(delta, indexToPlace);
	}
	// public void ScaleFrom(float scalar, int indexToStart = 0) { for(int i=indexToStart;i<flux.Count;++i){flux[i].Scale(scalar);} }
	public void Add(TYPE delta, int startIndex = -1) {
		// string s="";for(int i=0;i<flux.Count;++i){s+=flux[i].ToString()+", ";}Debug.Log(s);
		if(startIndex < flux.Count) {
			// s="["+e+":";
			// Debug.Log("adjusting "+delta+" after "+flux[startIndex].t);
			if(delta!=default(TYPE)) {
				for(int i=startIndex+1;i<flux.Count;++i) {
					flux[i].Add(delta);
					// DataPoint d = flux[i]; flux[i] = new DataPoint(d.t, d.rate, d.position + delta);
				}
				// for(int i=0;i<flux.Count;++i){s+=flux[i].ToString()+", ";}Debug.Log(s+"]");
			}// else { throw new System.Exception("delta 0? shouldn't this have been caught earlier?"); }
		} else { if(startIndex > flux.Count) throw new System.Exception("how did this happen? BinarySearch returned out of bounds?"); }
	}
}