using UnityEngine;

public interface ICullingSystem
{
	public ICullingSystem RegisterCullingProvider<T>(ICullingContext context, RendererChunk[] chunksToCull, Vector3 meshBounds, int instancesCount) where T : unmanaged;
	public RendererChunk[] CalculateData(RendererChunk[] data);
	public ICullingProvider CullingProvider { get; }

	public bool IsCullingInitialized { get; set; }
}
