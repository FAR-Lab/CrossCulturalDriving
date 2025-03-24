using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using System.Linq;

/// <summary>
/// The centerline is visualize via splines
/// This utility function helps both visualize the spline that was created and determine the distance from a point along the spline
/// </summary>
[RequireComponent(typeof(SplineContainer))]
[RequireComponent(typeof(LineRenderer))]
public class SplineCenterlineUtility : MonoBehaviour
{
    [Tooltip("How large the input CL should be")]
    [SerializeField]
    private int numPoints = 400;

    [Tooltip("Required: An in-scene game object used to visualize the closest point a car is to the CL. This is also used " +
        "to calculate the closest point. If you don't want to see the indicator, " +
        "disable the mesh renderer on this object")]
    [SerializeField]
    private GameObject closestPointIndicator;

    [Tooltip("Line renderer adds extra an visualization to the spline." +
    "Leave empty if you do not want to visualize the spline with a line.")]
    [SerializeField]
    private LineRenderer lineRenderer;

    [Tooltip("Reference to the spline container")]
    [SerializeField]
    private SplineContainer splines;

    [Tooltip("The positions to visualize from the CL")]
    public List<Vector3> points = new List<Vector3>();


    private void Awake()
    {
        if (splines == null)
            splines = gameObject.GetComponent<SplineContainer>();
    }

    public void LoadPointsFromCL(List<CLPoints> CLs)
    {
        if (points.Count > 1)
            points.Clear();

        foreach (CLPoints CL in CLs) {
            points.Add(new Vector3(CL.HeadPosX, 0, CL.HeadPosZ));
        }

        CreateSpline();
    }

    public void CreateSpline()
    {
        if (splines != null)
            foreach (Spline oldSpline in splines.Splines)
                splines.RemoveSpline(oldSpline);  // Delete old splines

        if (lineRenderer != null)
            lineRenderer.positionCount = 0;  //Remove old line renderer

        numPoints = points.Count;

        Spline spline = splines.AddSpline();

        for (int i = 0; i < points.Count; i++)
        {
            BezierKnot knot = new BezierKnot(points[i]);
            spline.Insert(i, knot);  // Insert points along the spline
        }
        
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = numPoints;  // Creates the line renderer

            for (int i = 0; i < numPoints; i++)  // Populate line renderer with points from the spline
            {
                float t = (float)i / (numPoints - 1);
                lineRenderer.SetPosition(i, spline.EvaluatePosition(t));
            }
        }
    }
    public static float Cross(Vector2 value1, Vector2 value2)
    {
        return value1.x * value2.y 
               - value1.y * value2.x;
    }

    /// <summary>
    /// Function to get the closest distance from a position "point" to the attached spline
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public float GetClosestDistanceToSpline(Vector3 point) {
        point = transform.InverseTransformPoint(point);
        Spline spline = splines.Splines[0];

        float minDistance = float.MaxValue;
        Vector3 indicatorPosition = closestPointIndicator.transform.localPosition;

        Vector3 closestLineSegment = Vector3.zero, lineToPoint= Vector3.zero;
        
        for (int i = 0; i < numPoints - 1; i++)
        {
            float t1 = i / (float)(numPoints - 1);
            float t2 = (i + 1) / (float)(numPoints - 1);

            Vector3 p1 = spline.EvaluatePosition(t1);
            Vector3 p2 = spline.EvaluatePosition(t2);

            // Distance to the line segment between p1 and p2
            Vector3 closestPointOnLine = GetClosestPointOnLine(point, p1, p2);
            float distanceToLine = Vector3.Distance(point, closestPointOnLine);
            if (distanceToLine < minDistance)
            {
                indicatorPosition = closestPointOnLine;
                closestLineSegment = p2 - p1;
                lineToPoint = point - closestPointOnLine;
                minDistance = distanceToLine;
            }
        }

        closestPointIndicator.transform.localPosition = indicatorPosition;
        var sign = -Mathf.Sign(Cross(new Vector2(closestLineSegment.x, closestLineSegment.z), new Vector2(lineToPoint.x, lineToPoint.z)));
        return minDistance * sign;
    }
    
    public int GetClosestPointIndex(Vector3 position)
    {
        int closestIndex = -1;
        float minDistance = float.MaxValue;
        for (int i = 0; i < points.Count; i++)
        {
            float distance = Vector3.Distance(points[i], position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }
        return closestIndex;
    }


    /// <summary>
    /// Utility function - get the closest point on a line that is between 2 points
    /// </summary>
    /// <param name="point"></param>
    /// <param name="linePoint1"></param>
    /// <param name="linePoint2"></param>
    /// <returns></returns>
    private Vector3 GetClosestPointOnLine(Vector3 point, Vector3 linePoint1, Vector3 linePoint2)
    {
        Vector3 lineDirection = linePoint2 - linePoint1;
        float projection = Vector3.Dot(point - linePoint1, lineDirection) / lineDirection.sqrMagnitude;
        projection = Mathf.Clamp01(projection);
        return linePoint1 + lineDirection * projection;
    }
    
    public Vector3 GetClosestPointOnSpline(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        Spline spline = splines.Splines[0];

        float minDistance = float.MaxValue;
        Vector3 closestPoint = Vector3.zero;

        for (int i = 0; i < numPoints - 1; i++)
        {
            float t1 = i / (float)(numPoints - 1);
            float t2 = (i + 1) / (float)(numPoints - 1);

            Vector3 p1 = spline.EvaluatePosition(t1);
            Vector3 p2 = spline.EvaluatePosition(t2);

            Vector3 closestPointOnLine = GetClosestPointOnLine(position, p1, p2);
            float distanceToLine = Vector2.Distance(
                new Vector2(position.x, position.z), 
                new Vector2(closestPointOnLine.x, closestPointOnLine.z));

            if (distanceToLine < minDistance)
            {
                closestPoint = closestPointOnLine;
                minDistance = distanceToLine;
            }
        }

        closestPoint.y = position.y;
        return transform.TransformPoint(closestPoint);
    }
    public Vector3 GetPointAtDistanceAlongSpline(Vector3 startPoint, float distance, float yCoordinate = 0f)
    {
        Vector3 localStartPoint = transform.InverseTransformPoint(startPoint);
        Spline spline = splines.Splines[0];
    
        float closestT = 0f;
        float minDistance = float.MaxValue;
    
        for (int i = 0; i < numPoints; i++)
        {
            float t = i / (float)(numPoints - 1);
            Vector3 point = spline.EvaluatePosition(t);
            float dist = Vector2.Distance(
                new Vector2(localStartPoint.x, localStartPoint.z),
                new Vector2(point.x, point.z));
            
            if (dist < minDistance)
            {
                minDistance = dist;
                closestT = t;
            }
        }
    
        float splineLength = EstimateSplineLength(spline);
        float tIncrement = distance / splineLength;
        float newT = Mathf.Clamp01(closestT + tIncrement);
    
        Vector3 newPosition = spline.EvaluatePosition(newT);
        newPosition.y = yCoordinate;
    
        return transform.TransformPoint(newPosition);
    }

    private float EstimateSplineLength(Spline spline)
    {
        float length = 0f;
        Vector3 previousPoint = spline.EvaluatePosition(0f);
    
        for (int i = 1; i <= numPoints; i++)
        {
            float t = i / (float)numPoints;
            Vector3 currentPoint = spline.EvaluatePosition(t);
            length += Vector3.Distance(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
    
        return length;
    }

}
