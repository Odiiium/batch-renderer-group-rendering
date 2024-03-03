using UnityEngine;

public interface ICullingSystem
{
	public ICullingSystem RegisterCullingProvider(ICullingContext context, RendererChunk[] chunksToCull, Vector3 meshBounds, int instancesCount);
	public RendererChunk[] CalculateData(RendererChunk[] data);
	public ICullingProvider CullingProvider { get; }

	public bool IsCullingInitialized { get; set; }
}
