using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereNoise : BaseNoise
{
    [HideInInspector]
    public float radius = 10;
    public override ComputeBuffer Generate(ComputeBuffer pointsBuffer, int numPoints, int numPointsPerAxis, float spacing, Vector3 noiseOffset, Vector3 worldCoords, Vector3 worldSize)
    {
        computeNoise.SetFloat("radius", radius);

        return base.Generate(pointsBuffer, numPoints, numPointsPerAxis, spacing, noiseOffset, worldCoords, worldSize);
    }
}
