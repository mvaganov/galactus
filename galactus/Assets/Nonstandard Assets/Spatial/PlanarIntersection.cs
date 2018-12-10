using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spatial
{
    public class PlanarIntersection : Planar
    {
        public Planar[] intersections;
        public PlanarIntersection() { }
        public PlanarIntersection(Vector3 point, Vector3 normal, Planar[] intersections)
        {
            this.point = point; this.normal = normal; this.intersections = intersections;
        }

        public override void Translate(Vector3 delta) { 
            base.Translate(delta);
            System.Array.ForEach(intersections, (p) => { p.Translate(delta); });
        }
        public override void Rotate(Quaternion q) {
            base.Rotate(q);
            System.Array.ForEach(intersections, (p) => { p.Rotate(q); });
        }
        public override void Scale(Vector3 coefficient) {
            base.Scale(coefficient);
            System.Array.ForEach(intersections, (p) => { p.Scale(coefficient); });
        }

        public override bool Contains(Vector3 point)
        {
            bool onPlane = base.Contains(point);
            System.Array.ForEach(intersections, (p) => {
                if (!p.IsFacing(point))
                    onPlane = false;
            });
            return onPlane;
        }
        public override Vector3 GetClosestPointTo(Vector3 point) {
            // TODO
            // if the point is not facing a plane
                // get the ray intersecting the planes
                // get the closest point on that ray to point
                // championship-belt that point
            // if there is no closest-point (from the championship-belt algorithm)
                // just return what is in the plane
            return GetClosestPointTo(point, this.point, this.normal);
        }
        public override Vector3 GetClosestPointOnSurface(Vector3 point, out Vector3 surfaceNormal)
        {
            return GetClosestPointOnSurface(point, out surfaceNormal, point, normal, intersections);
        }

        public static Vector3 GetClosestPointOnSurface(Vector3 point, out Vector3 surfaceNormal, Vector3 planePoint, Vector3 planeNormal, Planar[] confiningPlanes)
        {
            // TODO testme
            surfaceNormal = IsFacing(point, planePoint, planeNormal) ? planeNormal : -planeNormal;
            return GetClosestPointTo(point, planePoint, planeNormal);
        }

        // TODO the collision deteciton suite, so that the confined plane correctly tests all the things...

        // TODO take into account the confines
        public static bool Raycast(Ray r, out RaycastHit hit, Vector3 planePoint, Vector3 planeNormal, Planar[] confines)
        {
            hit = new RaycastHit();
            Plane p = new Plane(planeNormal, planePoint);
            float d;
            bool actualHit = p.Raycast(r, out d);
            hit.point = r.origin + r.direction * d;
            hit.normal = IsFacing(r.origin, planePoint, planeNormal) ? planeNormal : -planeNormal;
            return actualHit;
        }

    }
}