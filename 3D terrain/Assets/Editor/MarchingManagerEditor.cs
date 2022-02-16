using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MarchingManager))]
public class MarchingManagerEditor : Editor
{
    public override void OnInspectorGUI() {
        MarchingManager marchingManager = (MarchingManager)target;
        DrawDefaultInspector();

        if(marchingManager.autoUpdate && marchingManager.settingsChanged)
            marchingManager.Run();

        if(GUILayout.Button("GenerateFixedMap"))
            marchingManager.GenerateFixedMap();
    }
}
