using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>Pull a MovingEntity GameObject toward a Collider. 
/// Will work with a MeshCollider, but a SphereCollider or BoxCollider is optimal
public class GravObject : GravSource {
	#region public API
	[Tooltip("If true, will base gravity on this object's collider normals. Can be set to false for spheres with (0,0,0) center offset, since gravity is always at transform-center.")]
	public bool useColliderGravity = true;

	public override Vector3 CalculateGravityDirectionFrom(Vector3 point) {
		if(!useColliderGravity) { return (transform.position - point).normalized; }
		Vector3 dir = Vector3.zero;
		switch(colliderType) {
		case ColliderType.box: {
				Vector3 p = NearestPointToBox(myCollider as BoxCollider, point);
				Vector3 delta = p - point;
				dir = delta.normalized;
			}
			break;
		case ColliderType.mesh: {
				Vector3 normal;
				Vector3 p = nearestPointOnMeshCalculationObject.NearestPointTo(point, out normal);
				dir = (p - point).normalized;
			}
			break;
		case ColliderType.sphere: {
				SphereCollider sc = myCollider as SphereCollider;
				Vector3 p = transform.position + sc.center;
				dir = (p - point).normalized;
			}
			break;
		default:
			dir = (transform.position - point).normalized;
			break;
		}
		return dir;
	}
	public static Vector3 NearestPointToBox(BoxCollider box, Vector3 point) {
		Transform t = box.transform;
		Quaternion rot = t.rotation;
		Vector3 delta = point - t.position;
		Vector3 rotatedPoint = (Quaternion.Inverse(rot) * delta) + t.position;
		t.rotation = Quaternion.identity;
		Vector3 nearestPoint = box.ClosestPointOnBounds(rotatedPoint);
		t.rotation = rot;
		delta = nearestPoint - t.position;
		delta = rot * delta;
		nearestPoint = t.position + delta;
		return nearestPoint;
	}
	#endregion
	#region Calculate near-points
	private enum ColliderType { none, mesh, box, sphere };
	private ColliderType colliderType = ColliderType.none;
	private Collider myCollider;
	NearestPointOnMeshCalculationObject nearestPointOnMeshCalculationObject = null;

	public class NearestPointOnMeshCalculationObject {
		Mesh mesh;
		Vector3[] verts;
		Vector3[] norms;
		VertTriList vt;
		KDTree kd;
		Transform transform;

		public NearestPointOnMeshCalculationObject(GameObject go) {
			transform = go.transform;
			mesh = go.GetComponent<MeshFilter>().mesh;
			verts = mesh.vertices;
			norms = mesh.normals;
			vt = new VertTriList(mesh);
			kd = KDTree.MakeFromPoints(verts);
		}
		public Vector3 NearestVertexTo(Vector3 point) {
			// convert point to local space
			point = transform.InverseTransformPoint(point);
			float minDistanceSqr = Mathf.Infinity;
			Vector3 nearestVertex = Vector3.zero;
			// scan all vertices to find nearest
			foreach(Vector3 vertex in verts) {
				Vector3 diff = point-vertex;
				float distSqr = diff.sqrMagnitude;
				if(distSqr < minDistanceSqr) {
					minDistanceSqr = distSqr;
					nearestVertex = vertex;
				}
			}
			// convert nearest vertex back to world space
			return transform.TransformPoint(nearestVertex);
		}
		public Vector3 NearestPointTo(Vector3 point, out Vector3 ptNormal) {
			Vector3 objSpacePt = transform.InverseTransformPoint(point);
			Vector3 meshPt = NearestPointOnMesh(objSpacePt, verts, kd, mesh.triangles, vt, out ptNormal);
			Vector3 closest = transform.TransformPoint(meshPt);
			ptNormal = transform.TransformVector(ptNormal).normalized;
			return closest;
		}
		public Vector3 NearestPointTo(Vector3 point) {
			Vector3 objSpacePt = transform.InverseTransformPoint(point);
			Vector3 meshPt = NearestPointOnMesh(objSpacePt, verts, mesh.triangles, vt);
			Vector3 closest = transform.TransformPoint(meshPt);
			return closest;
		}
		public static float GetDistPointToLine(Vector3 origin, Vector3 direction, Vector3 point) {
			Vector3 point2origin = origin - point;
			Vector3 point2closestPointOnLine = point2origin - Vector3.Dot(point2origin, direction) * direction;
			return point2closestPointOnLine.magnitude;
		}
		Vector3 NearestPointOnMesh(Vector3 pt, Vector3[] verts, KDTree vertProx, int[] tri, VertTriList vt, out Vector3 pointNormal) {
			//	find nearest vertex (point must be on triangle useing this vertex if the mesh is convex)
			int nearest = vertProx.FindNearest(pt);
			pointNormal = Vector3.zero;
			//	Get the list of triangles in which the nearest vert "participates".
			int[] nearTris = vt[nearest];
			Vector3 nearestPt = Vector3.zero;
			float nearestSqDist = float.PositiveInfinity;
			TriangleSection ts;
			for(int i = 0; i < nearTris.Length; i++) {
				int triOff = nearTris[i] * 3;
				Vector3 a = verts[tri[triOff]], b = verts[tri[triOff + 1]], c = verts[tri[triOff + 2]];
				Vector3 possNearestPt = NearestPointOnTri(pt, a, b, c, out ts);
				float possNearestSqDist = (pt - possNearestPt).sqrMagnitude;
				if(possNearestSqDist < nearestSqDist) {
					nearestPt = possNearestPt;
					nearestSqDist = possNearestSqDist;
					switch(ts) {
					case TriangleSection.side_ab: {
							Vector3 norm1 = norms[tri[triOff+0]], norm2 = norms[tri[triOff+1]];
							pointNormal = (norm1+norm2)/2;
						}
						break;
					case TriangleSection.side_bc: {
							Vector3 norm1 = norms[tri[triOff+1]], norm2 = norms[tri[triOff+2]];
							pointNormal = (norm1+norm2)/2;
						}
						break;
					case TriangleSection.side_ca: {
							Vector3 norm1 = norms[tri[triOff+2]], norm2 = norms[tri[triOff+0]];
							pointNormal = (norm1+norm2)/2;
						}
						break;
					case TriangleSection.surface: {
							Vector3 AB = b - a, AC = c - a;
							pointNormal = Vector3.Normalize(Vector3.Cross(AB, AC));
						}
						break;
					}
				}
			}
			return nearestPt;
		}
		/// <summary>easier if we don't need to get the normal!</summary>
		Vector3 NearestPointOnMesh(Vector3 pt, Vector3[] verts, int[] tri, VertTriList vt) {
			//	find nearest vertex (point must be on triangle useing this vertex if the mesh is convex)
			int nearest = -1;
			float nearestSqDist = float.PositiveInfinity;
			for(int i = 0; i < verts.Length; i++) {
				float sqDist = (verts[i] - pt).sqrMagnitude;
				if(sqDist < nearestSqDist) {
					nearest = i;
					nearestSqDist = sqDist;
				}
			}
			//	Get the list of triangles in which the nearest vert "participates".
			int[] nearTris = vt[nearest];
			Vector3 nearestPt = Vector3.zero;
			nearestSqDist = float.PositiveInfinity;
			TriangleSection ts;
			for(int i = 0; i < nearTris.Length; i++) {
				int triOff = nearTris[i] * 3;
				Vector3 a = verts[tri[triOff]];
				Vector3 b = verts[tri[triOff + 1]];
				Vector3 c = verts[tri[triOff + 2]];
				Vector3 possNearestPt = NearestPointOnTri(pt, a, b, c, out ts);
				float possNearestSqDist = (pt - possNearestPt).sqrMagnitude;
				if(possNearestSqDist < nearestSqDist) {
					nearestPt = possNearestPt;
					nearestSqDist = possNearestSqDist;
				}
			}
			return nearestPt;
		}

		public enum TriangleSection { unknown, surface, side_ab, side_bc, side_ca };
		/// <param name="ptNormal">Point normal. If on the triangle surface, returns the surface normal</param>
		public Vector3 NearestPointOnTri(Vector3 pt, Vector3 a, Vector3 b, Vector3 c, out TriangleSection pointDetail) {
			pointDetail = TriangleSection.unknown;
			Vector3 edge1 = b - a;
			Vector3 edge2 = c - a;
			Vector3 edge3 = c - b;
			float edge1Len = edge1.magnitude;
			float edge2Len = edge2.magnitude;
			float edge3Len = edge3.magnitude;
			Vector3 ptLineA = pt - a;
			Vector3 ptLineB = pt - b;
			Vector3 ptLineC = pt - c;
			Vector3 xAxis = edge1 / edge1Len;
			Vector3 zAxis = Vector3.Cross(edge1, edge2).normalized;
			Vector3 yAxis = Vector3.Cross(zAxis, xAxis);
			Vector3 edge1Cross = Vector3.Cross(edge1, ptLineA);
			Vector3 edge2Cross = Vector3.Cross(edge2, -ptLineC);
			Vector3 edge3Cross = Vector3.Cross(edge3, ptLineB);
			bool edge1On = Vector3.Dot(edge1Cross, zAxis) > 0f;
			bool edge2On = Vector3.Dot(edge2Cross, zAxis) > 0f;
			bool edge3On = Vector3.Dot(edge3Cross, zAxis) > 0f;
			//	If the point is inside the triangle then return its coordinate.
			if(edge1On && edge2On && edge3On) {
				pointDetail = TriangleSection.surface;
				float xExtent = Vector3.Dot(ptLineA, xAxis);
				float yExtent = Vector3.Dot(ptLineA, yAxis);
				return a + xAxis * xExtent + yAxis * yExtent;
			}
			//	Otherwise, the nearest point is somewhere along one of the edges.
			Vector3 edge1Norm = xAxis;
			Vector3 edge2Norm = edge2.normalized;
			Vector3 edge3Norm = edge3.normalized;
			float edge1Ext = Mathf.Clamp(Vector3.Dot(edge1Norm, ptLineA), 0f, edge1Len);
			float edge2Ext = Mathf.Clamp(Vector3.Dot(edge2Norm, ptLineA), 0f, edge2Len);
			float edge3Ext = Mathf.Clamp(Vector3.Dot(edge3Norm, ptLineB), 0f, edge3Len);
			Vector3 edge1Pt = a + edge1Ext * edge1Norm;
			Vector3 edge2Pt = a + edge2Ext * edge2Norm;
			Vector3 edge3Pt = b + edge3Ext * edge3Norm;
			float sqDist1 = (pt - edge1Pt).sqrMagnitude;
			float sqDist2 = (pt - edge2Pt).sqrMagnitude;
			float sqDist3 = (pt - edge3Pt).sqrMagnitude;
			if(sqDist1 < sqDist2) {
				if(sqDist1 < sqDist3) {
					pointDetail = TriangleSection.side_ab;
					return edge1Pt;
				} else {
					pointDetail = TriangleSection.side_bc;
					return edge3Pt;
				}
			} else if(sqDist2 < sqDist3) {
				pointDetail = TriangleSection.side_ca;
				return edge2Pt;
			} else {
				pointDetail = TriangleSection.side_bc;
				return edge3Pt;
			}
		}
	}
	#endregion // Calculate near-points
	#region VertTriList
	//	lookup table for a mesh identifying which vertexes reference which triangles
	public class VertTriList {
		public int[][] list;
		public int[] aliases;

		//	Indexable - use "vertTri[i]" to get the list of triangles for vertex i.
		public int[] this[int index] {
			get { return list[aliases[index]]; }
		}
		public VertTriList(int[] tri, Vector3[] verts) { Init(tri, verts); }
		public VertTriList(Mesh mesh) { Init(mesh.triangles, mesh.vertices); }
		public void Init(int[] tri, Vector3[] verts) {
			int numVerts = verts.Length;
			// find duplicate verts, since many meshes are created with dups for rendering purposes
			List<int>[] duplicates = new List<int>[verts.Length];
			// create an alias table, so duplicate vertexes can point to the same triangles
			aliases = new int[verts.Length];
			for(int i = 0; i < duplicates.Length; ++i) { duplicates[i] = new List<int>(); }
			for(int i = 0; i < verts.Length; ++i) {
				for(int j = i + 1; j < verts.Length; ++j) {
					if(verts[i] == verts[j]) {
						if(!duplicates[i].Contains(j)) duplicates[i].Add(j);
						if(!duplicates[j].Contains(i)) duplicates[j].Add(i);
					}
				}
			}
			// sort duplicates, so the earliest verts are first, and add that as an aliases
			for(int i = 0; i < duplicates.Length; ++i) {
				duplicates[i].Sort();
				aliases[i] = (duplicates[i].Count>0) ? Mathf.Min(i, duplicates[i][0]) : i;
			}
			// go through the triangles, keeping a count of how many times each vert is used
			int[] counts = new int[numVerts];
			for(int i = 0; i < tri.Length; i++) { counts[tri[i]]++; }
			// merge aliases into earliest counts
			for(int i = 0; i < aliases.Length; ++i) {
				if(aliases[i] < i) {
					int earliestIndex = aliases[i];
					counts[earliestIndex] += counts[i];
					counts[i] = 0;
				}
			}
			// initialise empty jagged array with appropriate number of elements for each vert.
			list = new int[numVerts][];
			for(int i = 0; i < counts.Length; i++) {
				if(counts[i] > 0) { list[i] = new int[counts[i]]; }
			}
			// assign appropriate triangle number each time given vert is encountered in triangles list
			for(int i = 0; i < tri.Length; i++) {
				int vert = tri[i];
				vert = aliases[vert]; // take aliases into account
				list[vert][--counts[vert]] = i / 3;
			}
		}
	}
	#endregion // VertTriList
	#region KDTree
	// KDTree.cs - A Stark, September 2009.
	//	This class implements a data structure that stores a list of points in space.
	//	A common task in game programming is to take a supplied point and discover which
	//	of a stored set of points is nearest to it. For example, in path-plotting, it is often
	//	useful to know which waypoint is nearest to the player's current
	//	position. The kd-tree allows this "nearest neighbour" search to be carried out quickly,
	//	or at least much more quickly than a simple linear search through the list.
	//	At present, the class only allows for construction (using the MakeFromPoints static method)
	//	and nearest-neighbour searching (using FindNearest). More exotic kd-trees are possible, and
	//	this class may be extended in the future if there seems to be a need.
	//	nearest-neighbour search returns integer index
	public class KDTree {
		public KDTree[] lr;
		public Vector3 pivot;
		public int pivotIndex, axis;
		//	faster if 2, for X,Y search
		const int numDims = 3;

		public KDTree() { lr = new KDTree[2]; }
		//	Make a new tree from a list of points.
		public static KDTree MakeFromPoints(params Vector3[] points) {
			int[] indices = Iota(points.Length);
			return MakeFromPointsInner(0, 0, points.Length - 1, points, indices);
		}
		//	Recursively build a tree by separating points at plane boundaries.
		static KDTree MakeFromPointsInner(int depth, int stIndex, int enIndex, Vector3[] points, int[] inds) {
			KDTree root = new KDTree();
			root.axis = depth % numDims;
			int splitPoint = FindPivotIndex(points, inds, stIndex, enIndex, root.axis);
			root.pivotIndex = inds[splitPoint];
			root.pivot = points[root.pivotIndex];
			int leftEndIndex = splitPoint - 1;
			if(leftEndIndex >= stIndex) {
				root.lr[0] = MakeFromPointsInner(depth + 1, stIndex, leftEndIndex, points, inds);
			}
			int rightStartIndex = splitPoint + 1;
			if(rightStartIndex <= enIndex) {
				root.lr[1] = MakeFromPointsInner(depth + 1, rightStartIndex, enIndex, points, inds);
			}
			return root;
		}
		static void SwapElements(int[] arr, int a, int b) {
			int temp = arr[a];
			arr[a] = arr[b];
			arr[b] = temp;
		}
		//	Simple "median of three" heuristic to find a reasonable splitting plane.
		static int FindSplitPoint(Vector3[] points, int[] inds, int stIndex, int enIndex, int axis) {
			float a = points[inds[stIndex]][axis];
			float b = points[inds[enIndex]][axis];
			int midIndex = (stIndex + enIndex) / 2;
			float m = points[inds[midIndex]][axis];
			if(a > b) {
				if(m > a) { return stIndex; }
				if(b > m) { return enIndex; }
				return midIndex;
			} else {
				if(a > m) { return stIndex; }
				if(m > b) { return enIndex; }
				return midIndex;
			}
		}
		//	a new pivot index from range by splitting points falling at either side of its plane.
		public static int FindPivotIndex(Vector3[] points, int[] inds, int stIndex, int enIndex, int axis) {
			int splitPoint = FindSplitPoint(points, inds, stIndex, enIndex, axis);
			// int splitPoint = Random.Range(stIndex, enIndex);
			Vector3 pivot = points[inds[splitPoint]];
			SwapElements(inds, stIndex, splitPoint);
			int currPt = stIndex + 1;
			int endPt = enIndex;
			while(currPt <= endPt) {
				Vector3 curr = points[inds[currPt]];
				if((curr[axis] > pivot[axis])) {
					SwapElements(inds, currPt, endPt);
					endPt--;
				} else {
					SwapElements(inds, currPt - 1, currPt);
					currPt++;
				}
			}
			return currPt - 1;
		}
		public static int[] Iota(int num) {
			int[] result = new int[num];
			for(int i = 0; i < num; i++) { result[i] = i; }
			return result;
		}
		//	Find the nearest point in the set to the supplied point.
		public int FindNearest(Vector3 pt) {
			float bestSqDist = float.PositiveInfinity;
			int bestIndex = -1;
			Search(pt, ref bestSqDist, ref bestIndex);
			return bestIndex;
		}
		//	Recursively search the tree.
		void Search(Vector3 pt, ref float bestSqSoFar, ref int bestIndex) {
			float mySqDist = (pivot - pt).sqrMagnitude;
			if(mySqDist < bestSqSoFar) {
				bestSqSoFar = mySqDist;
				bestIndex = pivotIndex;
			}
			float planeDist = pt[axis] - pivot[axis]; //DistFromSplitPlane(pt, pivot, axis);
			int selector = planeDist <= 0 ? 0 : 1;
			if(lr[selector] != null) {
				lr[selector].Search(pt, ref bestSqSoFar, ref bestIndex);
			}
			selector = (selector + 1) % 2;
			float sqPlaneDist = planeDist * planeDist;
			if((lr[selector] != null) && (bestSqSoFar > sqPlaneDist)) {
				lr[selector].Search(pt, ref bestSqSoFar, ref bestIndex);
			}
		}
		//	Get a point's distance from an axis-aligned plane.
		float DistFromSplitPlane(Vector3 pt, Vector3 planePt, int axis) {
			return pt[axis] - planePt[axis];
		}
		//	Simple output of tree structure - mainly useful for getting a rough
		//	idea of how deep the tree is (and therefore how well the splitting
		//	heuristic is performing).
		public string Dump(int level) {
			string result = pivotIndex.ToString().PadLeft(level) + "\n";
			if(lr[0] != null) { result += lr[0].Dump(level + 2); }
			if(lr[1] != null) { result += lr[1].Dump(level + 2); }
			return result;
		}
	}
	#endregion // KDTree
	#region MonoBehaviour
	void Start() {
		myCollider = GetComponent<Collider>();
		     if(myCollider is BoxCollider) { colliderType = ColliderType.box; }
		else if(myCollider is SphereCollider) { colliderType = ColliderType.sphere; }
		else if(myCollider is MeshCollider) {
			nearestPointOnMeshCalculationObject = new NearestPointOnMeshCalculationObject(gameObject);
			colliderType = ColliderType.mesh;
		}
	}
	#endregion // MonoBehaviour
}

#region Simple gravity field
public class GravField : GravSource {
    public Vector3 gravityDirection;
    public override Vector3 CalculateGravityDirectionFrom(Vector3 point) {
        return gravityDirection;
    }
}
#endregion

#region Base GravitySource
public class GravSource : MonoBehaviour {
	[Tooltip("apply gravity if player hits this collider")]
	public bool entangleOnCollision = true;
	[Tooltip("apply gravity if player enters this collider trigger")]
	public bool entangleOnTrigger = true;
	[Tooltip("apply gravity power on player, not just gravity direction")]
	public bool forceGravitypower = false;
	[Tooltip("How much acceleration to apply to the PlayerControl, if gravity power is forced")]
	public float power = 9.81f;

	public virtual Vector3 CalculateGravityDirectionFrom(Vector3 point) {
		Vector3 delta = transform.position - point;
		return delta.normalized;
	}
	public void OnCollisionEnter(Collision col) {
		if(!enabled) return;
		if(entangleOnCollision) { EntanglePlayer(col.gameObject.GetComponent<MovingEntity>()); }
	}
	public void OnTriggerEnter(Collider col) {
		if(!enabled) return;
		if(entangleOnTrigger) { EntanglePlayer(col.GetComponent<MovingEntity>()); }
	}
	public void EntanglePlayer(MovingEntity p) {
		if(p && p.gravity.application != MovingEntity.GravityState.none) {
			GravPuller gp = p.gameObject.GetComponent<GravPuller>();
			if(!gp) { gp = p.gameObject.AddComponent<GravPuller>(); }
			if(gp.gravitySource != this) { gp.Init(this); } else { gp.Refresh(); }
		}
	}
}
#endregion // Base GravitySource

#region Gravity Puller (additional script for MovingEntity)
public class GravPuller : MonoBehaviour {
	public GravSource gravitySource;
	private GravSource lastGravitySource;
	private float timeOfLastSource;
	private MovingEntity p;
	[Tooltip("If true, will re-orient velocity to match any changes to gravity. useful for making tight video-gamey motion on small gravity wells.")]
	public bool velocityFollowsGravity = true;

	public void Init(GravSource gs) {
		if(gs != this && (gs != lastGravitySource || Time.time > timeOfLastSource+1)) {
			timeOfLastSource = Time.time;
			lastGravitySource = gravitySource;
			gravitySource = gs;
			p = GetComponent<MovingEntity>();
			Refresh();
		}
	}
	public void Refresh() {
		if(p.gravity.application == MovingEntity.GravityState.none) return;
		Vector3 nextDir = gravitySource.CalculateGravityDirectionFrom(transform.position);
		Rigidbody rb = p.rb;
		if(velocityFollowsGravity && nextDir != p.gravity.dir && rb != null) {
			Vector3 localVelocity = p.transform.InverseTransformDirection(rb.velocity);
			float angle = Vector3.Angle(nextDir, p.gravity.dir);
			Vector3 axis = Vector3.Cross(p.gravity.dir, nextDir).normalized;
			Quaternion q = Quaternion.AngleAxis(angle, axis);
			p.transform.rotation = Quaternion.LookRotation(q*transform.forward, q*transform.up);
			rb.velocity = p.transform.TransformDirection (localVelocity);
		}
		p.gravity.dir = nextDir;
		if (gravitySource.forceGravitypower) {
			p.gravity.power = gravitySource.power;
		}
	}
	void FixedUpdate() { Refresh(); }
	public Vector3 PullAsIfFrom(Vector3 alternateLocation) {
		return gravitySource.CalculateGravityDirectionFrom(alternateLocation);
	}
}
#endregion // Gravity Puller (additional script for MovingEntity)

#if UNITY_EDITOR
[CustomEditor(typeof(GravSource), true)]
public class CustomEditor_GravSource : CustomEditor_TYPE_ADJUSTABLE<GravSource> { }
#endif