using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FSMGraphWindow : EditorWindow
{
    private SO_NodeContainer nodeContainer;

    private Vector2 nodeSize = new Vector2(120, 60);
    private Color nodeColor = Color.gray;
    private Color lineColor = Color.white;
    private int fontSize = 18;
    private Vector2 nodeSpacing = new Vector2(300, 100);

    private float labelOffsetDistance = 15f;

    private Dictionary<SO_FSMNode, Vector2> nodePositions = new Dictionary<SO_FSMNode, Vector2>();
    private Vector2 scrollPosition;

    public static void ShowWindow(SO_NodeContainer nodeContainer)
    {
        FSMGraphWindow window = GetWindow<FSMGraphWindow>("FSM Graph");
        window.nodeContainer = nodeContainer;
    }

    private void OnGUI()
    {
        if (nodeContainer == null)
        {
            EditorGUILayout.LabelField("No Node Container selected.");
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        BeginWindows();

        nodePositions.Clear();

        Vector2 startPosition = new Vector2(10, position.height / 2);

        HashSet<SO_FSMNode> visitedNodes = new HashSet<SO_FSMNode>();

        DrawNode(nodeContainer.startNode, startPosition, visitedNodes);

        EndWindows();

        EditorGUILayout.EndScrollView();
    }

    private void DrawNode(SO_FSMNode node, Vector2 position, HashSet<SO_FSMNode> visitedNodes)
    {
        if (node == null || visitedNodes.Contains(node))
            return;

        visitedNodes.Add(node);
        nodePositions[node] = position;

        GUIStyle nodeStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = Texture2D.whiteTexture, textColor = Color.black },
            fontSize = fontSize,
            alignment = TextAnchor.MiddleCenter
        };

        Color oldColor = GUI.color;
        GUI.color = nodeColor;
        GUILayout.BeginArea(new Rect(position, nodeSize), nodeStyle);
        GUILayout.Label(node.name, nodeStyle);
        GUILayout.EndArea();
        GUI.color = oldColor;

        if (node.transitionConditions != null)
        {
            int transitionCount = node.transitionConditions.Length;

            if (transitionCount == 1)
            {
                SO_TransitionCondition condition = node.transitionConditions[0];
                if (condition == null || condition.targetNode == null)
                    return;

                Vector2 targetPosition = position + new Vector2(nodeSpacing.x, 0);

                DrawConnection(position, targetPosition, nodeSize, condition);

                DrawNode(condition.targetNode, targetPosition, visitedNodes);
            }
            else if (transitionCount == 2)
            {
                for (int i = 0; i < transitionCount; i++)
                {
                    SO_TransitionCondition condition = node.transitionConditions[i];
                    if (condition == null || condition.targetNode == null)
                        continue;

                    float angle = (i == 0) ? 45f : -45f;
                    float radians = angle * Mathf.Deg2Rad;
                    Vector2 offset = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * nodeSpacing.x;

                    Vector2 targetPosition = position + offset;

                    DrawConnection(position, targetPosition, nodeSize, condition);

                    DrawNode(condition.targetNode, targetPosition, visitedNodes);
                }
            }
            else
            {
                for (int i = 0; i < transitionCount; i++)
                {
                    SO_TransitionCondition condition = node.transitionConditions[i];
                    if (condition == null || condition.targetNode == null)
                        continue;

                    float angle = 90f - (180f / (transitionCount - 1)) * i;
                    float radians = angle * Mathf.Deg2Rad;
                    Vector2 offset = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * nodeSpacing.x;

                    Vector2 targetPosition = position + offset;

                    DrawConnection(position, targetPosition, nodeSize, condition);

                    DrawNode(condition.targetNode, targetPosition, visitedNodes);
                }
            }
        }
    }

    private void DrawConnection(Vector2 fromPosition, Vector2 toPosition, Vector2 nodeSize, SO_TransitionCondition condition)
    {
        Handles.BeginGUI();
        Handles.color = lineColor;

        Vector3 startPos = new Vector3(fromPosition.x + nodeSize.x, fromPosition.y + nodeSize.y / 2, 0);
        Vector3 endPos = new Vector3(toPosition.x, toPosition.y + nodeSize.y / 2, 0);

        Handles.DrawLine(startPos, endPos);
        Handles.EndGUI();

        Vector2 direction = endPos - startPos;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        Vector3 labelPos = (startPos + endPos) / 2;

        Vector2 perpDirection = Vector2.Perpendicular(direction.normalized);

        Vector2 labelOffset = perpDirection.normalized * labelOffsetDistance;

        if (direction.y < 0) 
        {
            labelOffset = -labelOffset;
        }

        labelPos += (Vector3)labelOffset;

        Matrix4x4 savedMatrix = GUI.matrix;

        GUIUtility.RotateAroundPivot(angle, labelPos);

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            normal = { textColor = Color.white },
            alignment = TextAnchor.MiddleCenter,
            fontSize = fontSize
        };

        Vector2 labelSize = new Vector2(200, 20); // Width and height of the label area
        Rect labelRect = new Rect(labelPos.x - labelSize.x / 2, labelPos.y - labelSize.y / 2, labelSize.x, labelSize.y);

        GUI.Label(labelRect, condition.name, labelStyle);

        GUI.matrix = savedMatrix;
    }
}
