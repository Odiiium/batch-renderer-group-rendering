using System;
using UnityEngine;

public interface ICullingProvider : IDisposable
{
	public unsafe ICullingProvider FillChunksBuffer(RendererChunk[] positionsToCull);
	//public unsafe ICullingProvider FillStatesBuffer<T>(int instancesCount) where T : unmanaged;
	public ICullingProvider SetChunkBounds(Vector3 meshBounds);
	public RendererChunk[] GetChunks(RendererChunk[] chunks);
	public void ExecuteData();
	public void PerformDispatch();
}
