using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MarchingWorld))]
public class MarchingWorldEditor : Editor
{
    public override void OnInspectorGUI() {
        MarchingWorld marchingWorld = (MarchingWorld)target;
        DrawDefaultInspector();

        if(marchingWorld.autoUpdate)
            if(marchingWorld.ValuesChanged()) marchingWorld.GenerateWorld();

        if(GUILayout.Button("Generate"))
            marchingWorld.GenerateWorld();
    }
}
