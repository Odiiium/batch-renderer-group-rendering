using System;
using UnityEngine;

[Serializable]
public class ChunkPartitionSettings
{
	public ChunkPartitionSettings(int chunksCount, Vector2 terrainSize, float chunkAmplitude)
	{
		ChunksInLineCount = chunksCount;
		TerrainSize = terrainSize;
		ChunkAmplitude = chunkAmplitude;
	}

	public int ChunksInLineCount;
	public Vector2 TerrainSize;
	public float ChunkAmplitude;
}
