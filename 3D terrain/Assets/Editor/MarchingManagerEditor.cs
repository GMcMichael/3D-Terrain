using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MarchingManager))]
public class MarchingManagerEditor : Editor
{
    public override void OnInspectorGUI() {
        MarchingManager marchingManager = (MarchingManager)target;
        DrawDefaultInspector();

        if(GUILayout.Button("Generate"))
            marchingManager.CreateFixedMap();
    }
}
