using Sirenix.Utilities;
using System;
using System.Linq;
using UnityEngine;

public class BatchRendererPreprocessor
{
	private ChunkPartitionSettings _settings;

	private RendererChunk[] _chunks;
	private RendererChunkData[] _chunksData;

	public void SetSettings(ChunkPartitionSettings settings)
	{
		_settings = settings;
	}

	public Vector3[] GroupPositions(Vector3[] positions)
	{
		_chunks ??= ComputeChunks();

		RendererChunkData currentData = default;

		IGrouping<int, Vector3>[] positionsById = positions.GroupBy(position =>
		{
			for (int i = 0; i < _chunksData.Length; i++)
			{
				currentData = _chunksData[i];

				if (position.x >= currentData.xMin && position.x < currentData.xMax &&
					position.z >= currentData.yMin && position.z < currentData.yMax)
					return currentData.Id;
			}
			return 0;
		}).OrderBy(x => x.Key).ToArray();

		for (int i = 0; i < positionsById.Count(); i++)
		{
			if (i >= _chunks.Count()) break;
			_chunks[i].PositionsCount = checked((short)positionsById[i].Count());
		}

		return positionsById.SelectMany(x => x).ToArray();
	}

	public RendererChunk[] ComputeChunks()
	{
		if (!_chunks.IsNullOrEmpty()) return _chunks;

		RendererChunk[] chunksArray = new RendererChunk[_settings.ChunksInLineCount * _settings.ChunksInLineCount];
		RendererChunkData[] chunkData = new RendererChunkData[_settings.ChunksInLineCount * _settings.ChunksInLineCount];
		Vector2 chunkSize = _settings.TerrainSize / _settings.ChunksInLineCount;

		Vector2 offset, chunkCenter = default;
		short id = default;
		float positionErrorX = default;
		float positionErrorY = default;

		for (int y = 0; y < _settings.ChunksInLineCount; y++)
			for (int x = 0; x < _settings.ChunksInLineCount; x++)
			{
				id = checked((short)(x + y * _settings.ChunksInLineCount));
				offset.x = x * chunkSize.x;
				offset.y = y * chunkSize.y;
				chunkCenter = chunkSize * .5f + offset;

				chunksArray[id] = new RendererChunk(id, chunkCenter, 0);

				if (y == _settings.ChunksInLineCount - 1)
					positionErrorY = 1;
				if (x == _settings.ChunksInLineCount - 1)
					positionErrorX = 1;

				chunkData[id] = new RendererChunkData((short)id, offset.x, offset.y, offset.x + chunkSize.x + positionErrorX, offset.y + chunkSize.y + positionErrorY);
			}

		_chunks = chunksArray;
		_chunksData = chunkData;

		return _chunks;
	}

	public Vector3 GetChunkBounds()
	{
		return new Vector3(_settings.TerrainSize.x / _settings.ChunksInLineCount,
							_settings.ChunkAmplitude,
							_settings.TerrainSize.y / _settings.ChunksInLineCount);
	}

	private struct RendererChunkData
	{
		public RendererChunkData(short id, float xmin, float ymin, float xmax, float ymax)
		{
			Id = id;
			xMin = xmin;
			yMin = ymin;
			xMax = xmax;
			yMax = ymax;
		}

		public short Id;
		public float xMin, yMin, xMax, yMax;
	}

}