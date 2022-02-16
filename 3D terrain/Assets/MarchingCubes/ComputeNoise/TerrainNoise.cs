using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainNoise : BaseNoise
{
    [HideInInspector]
    public bool hasFloor;
    [HideInInspector]
    public float floorLevel;
    [HideInInspector]
    public float noiseAmplitude;
    [HideInInspector]
    public float noiseFrequency;
    [HideInInspector]
    public int noiseOctaves;
    public override ComputeBuffer Generate(ComputeBuffer pointsBuffer, int numPoints, int numPointsPerAxis, float spacing, Vector3 noiseOffset, Vector3 worldCoords, Vector3 worldSize)
    {
        computeNoise.SetBool("hasFloor", hasFloor);
        computeNoise.SetFloat("floorLevel", floorLevel);
        computeNoise.SetFloat("frequency", noiseFrequency);
        computeNoise.SetFloat("amplitude", noiseAmplitude);
        computeNoise.SetInt("octaves", noiseOctaves);
        return base.Generate(pointsBuffer, numPoints, numPointsPerAxis, spacing, noiseOffset, worldCoords, worldSize);
    }

    public void SetValues(bool _hasFloor, float _floorLevel, float _noiseFrequency, float _noiseAmplitude, int _noiseOctaves) {
        hasFloor = _hasFloor;
        floorLevel = _floorLevel;
        noiseAmplitude = _noiseAmplitude;
        noiseFrequency = _noiseFrequency;
        noiseOctaves = _noiseOctaves;
    }
}
