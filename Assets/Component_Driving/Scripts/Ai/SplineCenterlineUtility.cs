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
    [SerializeField]
    private List<Vector3> points = new List<Vector3>();


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

    /// <summary>
    /// Function to get the closest distance from a position "point" to the attached spline
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public float GetClosestDistanceToSpline(Vector3 point)
    {
        Spline spline = splines.Splines[0];

        float minDistance = float.MaxValue;
        Vector3 indicatorPosition = closestPointIndicator.transform.position;

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

        closestPointIndicator.transform.position = indicatorPosition;
        var rightSideOfSegment = new Vector2(closestLineSegment.z, -closestLineSegment.x);
        var sign = Mathf.Sign(Vector2.Dot(rightSideOfSegment, new Vector2(lineToPoint.x, lineToPoint.z)));
        return minDistance * sign;
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
}
