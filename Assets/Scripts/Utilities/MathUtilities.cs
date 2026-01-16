using System.Collections.Generic;
using UnityEngine;

public static class MathUtilities
{
    /// <summary>
    /// Calculates the shortest distance between a point and a line segment
    /// </summary>
    public static float DistanceToLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 line = lineEnd - lineStart;
        float length = line.magnitude;
        line.Normalize();

        Vector3 toPoint = point - lineStart;
        float projection = Vector3.Dot(toPoint, line);

        if (projection < 0)
            return Vector3.Distance(point, lineStart);
        else if (projection > length)
            return Vector3.Distance(point, lineEnd);
        else
            return Vector3.Distance(point, lineStart + line * projection);
    }

    /// <summary>
    /// Generates a random point within a circle
    /// </summary>
    public static Vector2 RandomPointInCircle(float radius, System.Random random = null)
    {
        if (random == null)
        {
            float angle = UnityEngine.Random.value * 2 * Mathf.PI;
            float r = Mathf.Sqrt(UnityEngine.Random.value) * radius;
            return new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
        }
        else
        {
            float angle = (float)random.NextDouble() * 2 * Mathf.PI;
            float r = Mathf.Sqrt((float)random.NextDouble()) * radius;
            return new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
        }
    }

    /// <summary>
    /// Generates a random point within a sphere
    /// </summary>
    public static Vector3 RandomPointInSphere(float radius, System.Random random = null)
    {
        if (random == null)
        {
            // Unity's random
            float u = UnityEngine.Random.value;
            float v = UnityEngine.Random.value;
            float theta = u * 2.0f * Mathf.PI;
            float phi = Mathf.Acos(2.0f * v - 1.0f);
            float r = Mathf.Pow(UnityEngine.Random.value, 1.0f / 3.0f) * radius;

            float sinPhi = Mathf.Sin(phi);
            return new Vector3(
                r * sinPhi * Mathf.Cos(theta),
                r * sinPhi * Mathf.Sin(theta),
                r * Mathf.Cos(phi)
            );
        }
        else
        {
            // System random
            float u = (float)random.NextDouble();
            float v = (float)random.NextDouble();
            float theta = u * 2.0f * Mathf.PI;
            float phi = Mathf.Acos(2.0f * v - 1.0f);
            float r = Mathf.Pow((float)random.NextDouble(), 1.0f / 3.0f) * radius;

            float sinPhi = Mathf.Sin(phi);
            return new Vector3(
                r * sinPhi * Mathf.Cos(theta),
                r * sinPhi * Mathf.Sin(theta),
                r * Mathf.Cos(phi)
            );
        }
    }

    /// <summary>
    /// Calculates the angle between two vectors in degrees
    /// </summary>
    public static float AngleBetweenVectors(Vector3 a, Vector3 b)
    {
        float dot = Vector3.Dot(a.normalized, b.normalized);
        dot = Mathf.Clamp(dot, -1f, 1f);
        return Mathf.Acos(dot) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// Rotates a point around an axis
    /// </summary>
    public static Vector3 RotatePointAroundAxis(Vector3 point, Vector3 axis, float angle)
    {
        Quaternion rotation = Quaternion.AngleAxis(angle, axis);
        return rotation * point;
    }

    /// <summary>
    /// Generates a smooth random walk path
    /// </summary>
    public static List<Vector3> GenerateRandomWalk(int steps, float stepSize, float smoothness, System.Random random = null)
    {
        List<Vector3> path = new List<Vector3>();
        Vector3 currentPosition = Vector3.zero;
        Vector3 currentDirection = Vector3.forward;

        path.Add(currentPosition);

        for (int i = 0; i < steps; i++)
        {
            // Generate random angle change
            float angleChange;
            if (random != null)
            {
                angleChange = (float)(random.NextDouble() - 0.5) * smoothness * 2;
            }
            else
            {
                angleChange = (UnityEngine.Random.value - 0.5f) * smoothness * 2;
            }

            // Rotate direction
            currentDirection = Quaternion.Euler(0, angleChange, 0) * currentDirection;

            // Move in new direction
            currentPosition += currentDirection * stepSize;
            path.Add(currentPosition);
        }

        return path;
    }

    /// <summary>
    /// Calculates the centroid of a list of points
    /// </summary>
    public static Vector3 CalculateCentroid(List<Vector3> points)
    {
        if (points.Count == 0)
            return Vector3.zero;

        Vector3 sum = Vector3.zero;
        foreach (var point in points)
        {
            sum += point;
        }

        return sum / points.Count;
    }

    /// <summary>
    /// Calculates the bounding box of a list of points
    /// </summary>
    public static Bounds CalculateBoundingBox(List<Vector3> points)
    {
        if (points.Count == 0)
            return new Bounds();

        Vector3 min = points[0];
        Vector3 max = points[0];

        foreach (var point in points)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        Vector3 center = (min + max) * 0.5f;
        Vector3 size = max - min;

        return new Bounds(center, size);
    }

    /// <summary>
    /// Performs linear interpolation between multiple points
    /// </summary>
    public static List<Vector3> InterpolatePoints(List<Vector3> points, int segmentsPerEdge)
    {
        if (points.Count < 2)
            return new List<Vector3>(points);

        List<Vector3> interpolated = new List<Vector3>();

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 start = points[i];
            Vector3 end = points[i + 1];

            for (int j = 0; j < segmentsPerEdge; j++)
            {
                float t = (float)j / (segmentsPerEdge - 1);
                Vector3 point = Vector3.Lerp(start, end, t);
                interpolated.Add(point);
            }
        }

        return interpolated;
    }

    /// <summary>
    /// Generates a Poisson disk distribution within a rectangle
    /// </summary>
    public static List<Vector2> GeneratePoissonDiskDistribution(float width, float height, float minDistance, int maxAttempts = 30, System.Random random = null)
    {
        List<Vector2> points = new List<Vector2>();
        List<Vector2> activePoints = new List<Vector2>();

        // Start with a random point
        Vector2 firstPoint;
        if (random != null)
        {
            firstPoint = new Vector2((float)random.NextDouble() * width, (float)random.NextDouble() * height);
        }
        else
        {
            firstPoint = new Vector2(UnityEngine.Random.value * width, UnityEngine.Random.value * height);
        }

        points.Add(firstPoint);
        activePoints.Add(firstPoint);

        while (activePoints.Count > 0)
        {
            // Pick a random active point
            int randomIndex;
            if (random != null)
            {
                randomIndex = random.Next(activePoints.Count);
            }
            else
            {
                randomIndex = UnityEngine.Random.Range(0, activePoints.Count);
            }

            Vector2 point = activePoints[randomIndex];
            bool found = false;

            // Try to find a valid point around it
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2 candidate = GenerateRandomPointAround(point, minDistance, minDistance * 2, random);

                if (IsValidPoissonPoint(candidate, width, height, points, minDistance))
                {
                    points.Add(candidate);
                    activePoints.Add(candidate);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                activePoints.RemoveAt(randomIndex);
            }
        }

        return points;
    }

    /// <summary>
    /// Generates a random point around a center point
    /// </summary>
    private static Vector2 GenerateRandomPointAround(Vector2 center, float minRadius, float maxRadius, System.Random random = null)
    {
        float angle, radius;

        if (random != null)
        {
            angle = (float)random.NextDouble() * 2 * Mathf.PI;
            radius = minRadius + (float)random.NextDouble() * (maxRadius - minRadius);
        }
        else
        {
            angle = UnityEngine.Random.value * 2 * Mathf.PI;
            radius = minRadius + UnityEngine.Random.value * (maxRadius - minRadius);
        }

        return center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
    }

    /// <summary>
    /// Checks if a point is valid for Poisson disk distribution
    /// </summary>
    private static bool IsValidPoissonPoint(Vector2 point, float width, float height, List<Vector2> existingPoints, float minDistance)
    {
        // Check bounds
        if (point.x < 0 || point.x >= width || point.y < 0 || point.y >= height)
            return false;

        // Check distance to existing points
        foreach (var existingPoint in existingPoints)
        {
            if (Vector2.Distance(point, existingPoint) < minDistance)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Calculates the area of a triangle defined by three points
    /// </summary>
    public static float TriangleArea(Vector3 a, Vector3 b, Vector3 c)
    {
        return Vector3.Cross(b - a, c - a).magnitude * 0.5f;
    }

    /// <summary>
    /// Performs barycentric coordinate calculation
    /// </summary>
    public static Vector3 BarycentricCoordinates(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 v0 = b - a, v1 = c - a, v2 = p - a;
        float d00 = Vector3.Dot(v0, v0);
        float d01 = Vector3.Dot(v0, v1);
        float d11 = Vector3.Dot(v1, v1);
        float d20 = Vector3.Dot(v2, v0);
        float d21 = Vector3.Dot(v2, v1);
        float denom = d00 * d11 - d01 * d01;

        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1.0f - v - w;

        return new Vector3(u, v, w);
    }

    /// <summary>
    /// Checks if a point is inside a triangle
    /// </summary>
    public static bool IsPointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 bary = BarycentricCoordinates(p, a, b, c);
        return bary.x >= 0 && bary.y >= 0 && bary.z >= 0;
    }

    /// <summary>
    /// Calculates the volume of a tetrahedron
    /// </summary>
    public static float TetrahedronVolume(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        return Mathf.Abs(Vector3.Dot(d - a, Vector3.Cross(b - a, c - a))) / 6f;
    }

    /// <summary>
    /// Performs smooth step interpolation
    /// </summary>
    public static float SmoothStep(float edge0, float edge1, float x)
    {
        x = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return x * x * (3 - 2 * x);
    }

    /// <summary>
    /// Performs smoother step interpolation
    /// </summary>
    public static float SmootherStep(float edge0, float edge1, float x)
    {
        x = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return x * x * x * (x * (x * 6 - 15) + 10);
    }

    /// <summary>
    /// Maps a value from one range to another
    /// </summary>
    public static float Map(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return (value - fromMin) * (toMax - toMin) / (fromMax - fromMin) + toMin;
    }

    /// <summary>
    /// Clamps a value between min and max with wrapping
    /// </summary>
    public static float Wrap(float value, float min, float max)
    {
        float range = max - min;
        return min + ((value - min) % range + range) % range;
    }

    /// <summary>
    /// Calculates the shortest angle between two angles
    /// </summary>
    public static float ShortestAngle(float from, float to)
    {
        float difference = to - from;
        return Wrap(difference + 180, 0, 360) - 180;
    }
}
