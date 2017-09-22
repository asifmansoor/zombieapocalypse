using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(AIWaypointNetwork))]
public class AIWaypointNetworkEditor : Editor
{
    private string lastSettings = "Connections:0:0";

    public override void OnInspectorGUI()
    {
        AIWaypointNetwork network = (AIWaypointNetwork)target;

        network.DisplayMode = (PathDisplayMode)EditorGUILayout.EnumPopup("Display Mode", network.DisplayMode);

        if (network.DisplayMode == PathDisplayMode.Paths)
        {
            network.UIStart = EditorGUILayout.IntSlider("Waypoint Start", network.UIStart, 0, network.Waypoints.Count - 1);
            network.UIEnd = EditorGUILayout.IntSlider("Waypoint End", network.UIEnd, 0, network.Waypoints.Count - 1);
        }

        base.OnInspectorGUI();
        //DrawDefaultInspector();

        string currentSettings = network.DisplayMode.ToString() + ":" + network.UIStart.ToString() + ":" + network.UIEnd.ToString();
        if (lastSettings != currentSettings)
        {
            lastSettings = currentSettings;
            SceneView.RepaintAll();
        }
    }

    void OnSceneGUI()
    {
        AIWaypointNetwork network = (AIWaypointNetwork)target;

        GUIStyleState gsstate = new GUIStyleState();
        gsstate.textColor = Color.white;

        GUIStyle gs = new GUIStyle();
        gs.fontSize = 12;
        gs.normal = gsstate;

        for (int i = 0; i < network.Waypoints.Count; i++)
        {
            if (network.Waypoints[i] != null)
            {
                Handles.Label(network.Waypoints[i].position, "Waypoint " + i, gs);
            }
        }

        if (network.DisplayMode == PathDisplayMode.Connections)
        {
            Vector3[] linePoints = new Vector3[network.Waypoints.Count + 1];

            for (int i = 0; i < network.Waypoints.Count; i++)
            {
                if (network.Waypoints[i] != null)
                {
                    linePoints[i] = network.Waypoints[i].position;
                }
                else
                {
                    linePoints[i] = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
                }
            }
            linePoints[network.Waypoints.Count] = network.Waypoints[0].position;
            Handles.color = Color.cyan;
            Handles.DrawPolyLine(linePoints);
        }
        else if (network.DisplayMode == PathDisplayMode.Paths)
        {
            UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();

            if (network.Waypoints[network.UIStart] != null && network.Waypoints[network.UIEnd] != null)
            {
                Vector3 from = network.Waypoints[network.UIStart].position;
                Vector3 to = network.Waypoints[network.UIEnd].position;

                UnityEngine.AI.NavMesh.CalculatePath(from, to, UnityEngine.AI.NavMesh.AllAreas, path);
                Handles.color = Color.yellow;
                Handles.DrawPolyLine(path.corners);
            }
        }
    }

}
