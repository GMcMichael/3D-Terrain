using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Marching))]
public class MarchingEditor : Editor
{
    public override void OnInspectorGUI() {
        Marching marching = (Marching)target;
        DrawDefaultInspector();

        if(marching.autoUpdate)
            if(marching.ValuesChanged()) marching.DisplayMeshes();

        if(GUILayout.Button("Generate"))
            marching.DisplayMeshes();
    }
}
