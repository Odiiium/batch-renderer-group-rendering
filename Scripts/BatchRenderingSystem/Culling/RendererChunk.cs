using UnityEngine;

public struct RendererChunk
{
	public RendererChunk(short id, Vector2 position, short positionsCount, short isOnView = 0)
	{
		Id = id;
		Position = position;
		IsOnView = isOnView;
		PositionsCount = positionsCount;
	}

	public int Id;
	public Vector2 Position;
	public int PositionsCount;
	public int IsOnView;
}