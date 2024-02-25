using System;
using UnityEngine;

public interface IBatchRendererSystem : IDisposable
{
	public unsafe IBatchRendererSystem AddShaderData<T>(string propertyName, uint metadataValue, T[] array) where T : unmanaged;
	public IBatchRendererSystem Execute();

	public void ReleaseData();
	public void RegisterContext(BatchRenderingContext context);

	public BatchRenderingContext Context { get; set; }

	public bool IsSystemInitialized { get; set; }
}