using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SebastianMarching))]
public class SebMarchEditor : Editor
{
    public override void OnInspectorGUI() {
        SebastianMarching sebMarching = (SebastianMarching)target;
        DrawDefaultInspector();

        if(GUILayout.Button("Generate"))
            sebMarching.GenerateFixedMap();
    }
}
