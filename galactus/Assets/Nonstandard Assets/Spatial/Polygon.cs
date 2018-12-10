using UnityEngine;
using System.Collections;

namespace Spatial {
	public class Polygon : ConcreteArea {
        public Vector3[] points;
        public Polygon() { points = new Vector3[0]; }
        public Polygon(Vector3[] points)
        {
            this.points = points;
            ForcePointsToPlane(GetAverageLocation(), SurfaceNormalExhastive());
        }
        public override void FixGeometryProblems()
        {
            ForcePointsToPlane(GetAverageLocation(), SurfaceNormalExhastive());
        }

        // TODO testme
        public bool IsConvex() { return IsConvex(points, SurfaceNormal()); }
        // TODO testme
        public static bool IsConvex(Vector3[] points, Vector3 surfaceNormal) {
            Vector3 a = points[points.Length - 2], b = points[points.Length - 1], c = points[0];
            Vector3 pnormal = Vector3.Cross((b - a).normalized, surfaceNormal);
            bool expectedFacing = Planar.IsFacing(c, a, pnormal);
            a = b; b = c;
            for (int i = 1; i < points.Length; ++i) {
                c = points[i];
                pnormal = Vector3.Cross((b - a).normalized, surfaceNormal);
                if (Planar.IsFacing(c, a, pnormal) != expectedFacing){
                    return false;
                }
                a = b; b = c;
            }
            return true;
        }

        public Polygon[] SplitIntoTriangles() {
            // TODO
            // create a list of lines for the shape, the border lines of the shape.
            // figure out which side is outside by doing raycasts out, checking if it hits a line
            // delauny triangulate the points, not-allowing edges that cross existing border lines
            // remove triangles that have edges that are outside of border lines
            // return the list of triangles
            return null;
        }

        public Polygon[] SplitConcaveIntoConvex()
        {
            // TODO
            // turn entire shape into triangle polygons using delauny triangulation
            // starting with the largest triangle
            // combine adjacent pairs of polygons if they would be convex when combined
            return null;
        }

        public void ForcePointsToPlane(Vector3 point, Vector3 normal)
        {
            if (points.Length > 3)
            {
                //Planar p = new Planar(point, normal);
                Vector3 n;
                // clamp all points onto the plane
                for (int i = 0; i < points.Length; ++i)
                {
                    points[i] = Planar.GetClosestPointOnSurface(points[i], out n, point, normal);
                }
            }
        }

        public override void Translate(Vector3 delta)
        {
            for (int i = 0; i < points.Length; ++i) { points[i] += delta; }
        }
        public override void Rotate(Quaternion q)
        {
            for (int i = 0; i < points.Length; ++i) { points[i] = q * points[i]; }
        }
        public override void Scale(Vector3 coefficient)
        {
            for (int i = 0; i < points.Length; ++i) { points[i].Scale(coefficient); }
        }

        public Vector3 SurfaceNormalExhastive()
        {
            // average the surface normals at each angle
            Vector3 a = points[points.Length - 2], b = points[points.Length - 1], c, n = Vector3.zero;
            for (int i = 0; i < points.Length; ++i)
            {
                c = points[i];
                n += Vector3.Cross(a - b, b - c).normalized;
                a = b;
                b = c;
            }
            return n.normalized;
        }

        public Vector3 SurfaceNormal()
        {
            // assume the first angle has the correct surface normal
            return Vector3.Cross(points[0] - points[1], points[1] - points[2]).normalized;
        }

        public Planar GetPlane()
        {
            return new Planar(points[0], SurfaceNormal());
        }

        public bool IsWithinBoundary(Vector3 point)
        {
            Vector3 a = points[points.Length - 1], b, surfaceNormal = SurfaceNormal(), side, planarNorm;
            for (int i = 0; i < points.Length; ++i)
            {
                b = points[i];
                side = b - a;
                planarNorm = Vector3.Cross(surfaceNormal, side).normalized;
                if (!Planar.IsFacing(point, a, planarNorm))
                {
                    return false;
                }
                a = b;
            }
            return true;
            //Vector3 sideA = b - a;
            //Vector3 sideB = c - b;
            //Vector3 sideC = a - c;
            //Vector3 normA = Vector3.Cross(Vector3.Cross(sideA, sideB), sideA).normalized;
            //Vector3 normB = Vector3.Cross(Vector3.Cross(sideB, sideC), sideB).normalized;
            //Vector3 normC = Vector3.Cross(Vector3.Cross(sideC, sideA), sideC).normalized;
            //return Planar.IsFacing(point, a, normA) && Planar.IsFacing(point, b, normB) && Planar.IsFacing(point, c, normC);
        }

        public override bool Contains(Vector3 point)
        {
            return System.Array.IndexOf(points, point) >= 0 || (GetPlane().Contains(point) && IsWithinBoundary(point));
        }

        public override Vector3 GetClosestPointTo(Vector3 point)
        {
            Vector3 e;
            return GetClosestPointOnSurface(point, out e);
        }

        public override Vector3 GetClosestPointOnSurface(Vector3 point, out Vector3 surfaceNormal)
        {
            Line closestLine = null;
            float howClose = -1, dist;
            Vector3 a = points[points.Length - 1], b, side, testPlaneNormal,
                p = Vector3.zero, closestPoint = p;
            surfaceNormal = SurfaceNormal();
            for (int i = 0; i < points.Length; ++i)
            {
                b = points[i];
                side = b - a;
                testPlaneNormal = Vector3.Cross(surfaceNormal, side).normalized;
                if (!Planar.IsFacing(point, a, testPlaneNormal))
                {
                    Line line = new Line(a, b);
                    p = line.GetClosestPointTo(point);
                    // if there is no closest line, or this line is closer to the point than the closest line
                    dist = (p - point).sqrMagnitude;
                    if (closestLine == null || dist < howClose)
                    {
                        // this is now the newest closest line
                        howClose = dist;
                        closestLine = line;
                        closestPoint = p;
                    }
                }
                a = b;
            }
            // if the point should be in the plane proper (not on an outer edge)
            if (closestLine == null)
            {
                closestPoint = Planar.GetClosestPointOnSurface(point, out surfaceNormal, points[0], surfaceNormal);
            }
            else
            {
                surfaceNormal = (point - closestPoint).normalized;
            }
            return closestPoint;
        }

        public override bool Raycast(Ray r, out RaycastHit hit)
        {
            Vector3 n = SurfaceNormal();
            bool hits = Planar.Raycast(r, out hit, points[0], n);
            if (hits && hit.distance >= 0 && IsWithinBoundary(hit.point))
            {
                return true;
            }
            return false;
        }

        public override int Wireframe(Vector3[] out_points)
        {
            if (points != null && points.Length > 0)
            {
                for (int i = 0; i < points.Length; ++i)
                {
                    out_points[i] = points[i];
                }
                out_points[points.Length] = points[0];
                return points.Length + 1;
            }
            else
            {
                return 0;
            }
        }

        // TODO FIXME
        public override bool CollidesWith(Area area)
        {
            if (area.GetType() == typeof(Sphere))
            {
                // 
            }
            return base.CollidesWith(area);
        }

        // TODO test me
        public Sphere BoundingSphere()
        {
            // calculate the 2 extremes
            int a = 0, b = 1, c = 2;
            float longest = -1, len;
            for (int i = 0; i < points.Length; ++i)
            {
                for (int j = i + 1; j < points.Length; ++j)
                {
                    len = Vector3.Distance(points[i], points[j]);
                    if (longest < 0 || len > longest)
                    {
                        a = i; b = j; longest = len;
                    }
                }
            }
            // calculate the sphere for the given extremes
            Vector3 center = (points[a] + points[b]) / 2;
            float radius = longest / 2;
            longest = radius;
            // find the point furthest outside the bounding sphere (if there is one)
            for (int i = 0; i < points.Length; ++i)
            {
                if (i != a && i != b)
                {
                    len = Vector3.Distance(center, points[i]);
                    if (len > longest)
                    {
                        c = i; longest = len;
                    }
                }
            }
            // to Triangle.Circumscription
            if (longest > radius)
            {
                Vector3 upNormal;
                Triangle.CalculateCircumscription(
                    points[a], points[b], points[c],
                    out center, out radius, out upNormal);
            }
            return new Sphere(center, radius);
        }

        public Sphere InscribedSphere()
        {
            // find the minimum distance 3 points
            return new Sphere(Vector3.zero, 1); // TODO Circular-inscription
        }

        public override Vector3 GetLocation()
        {
            return GetAverageLocation();
        }
        public Vector3 GetAverageLocation()
        {
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < points.Length; ++i) { sum += points[i]; }
            return sum / points.Length;
        }
    }
}
