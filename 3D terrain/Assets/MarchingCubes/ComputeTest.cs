using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeTest : MonoBehaviour
{
    public ComputeShader computeTest;

    ComputeBuffer Results;

    void Start()
    {
        Results = new ComputeBuffer(512, sizeof(float)*3);

        computeTest.SetBuffer(0, "Result", Results);
        computeTest.Dispatch(0, 2, 1, 2);//1, 1, 1 should just run the 8, 8, 1 times that is numthread

        Vector3[] results = new Vector3[512];
        Results.GetData(results);

        Results.Release();
        Results = null;

        for (int i = 0; i < results.Length; i++)
        {
            Debug.Log("index: " + i + ",             x: " + results[i].x + ", y: " + results[i].y + ", z: " + results[i].z);
        }
    }
}
