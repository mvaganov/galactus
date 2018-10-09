using UnityEngine;
using System.Collections;

namespace Spatial {
	public interface Area : Locatable {
		bool Contains(Vector3 point);
		bool CollidesWith (Area area);
		Vector3 GetClosestPointOnSurface(Vector3 point, out Vector3 surfaceNormal);
		// TODO? GetClosestPointInVolume
		// TODO? GetClosestPointBetweenEdges
		// TODO? GetClosestVertex
		// TODO? GetClosestEdge
		// TODO? GetClosestSurface
        bool Raycast(Ray r, out RaycastHit hit);// Vector3 start, Vector3 direciton, out Vector3 point, out float distance);
	}
}