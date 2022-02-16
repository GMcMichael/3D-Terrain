using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestingNoise : BaseNoise
{
    [HideInInspector]
    public float testNum = 1;
    public override ComputeBuffer Generate(ComputeBuffer pointsBuffer, int numPoints, int numPointsPerAxis, float spacing, Vector3 noiseOffset, Vector3 worldCoords, Vector3 worldSize)
    {
        computeNoise.SetFloat("testNum", testNum);
        return base.Generate(pointsBuffer, numPoints, numPointsPerAxis, spacing, noiseOffset, worldCoords, worldSize);
    }
}
