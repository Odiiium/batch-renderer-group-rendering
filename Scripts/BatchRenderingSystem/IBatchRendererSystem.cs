using System;

public interface IBatchRendererSystem : IDisposable
{
	public unsafe IBatchRendererSystem AddShaderData<T>(string propertyName, uint metadataValue, T[] array) where T : unmanaged;
	public IBatchRendererSystem Execute();

	public void ReleaseBuffer();
	public void RegisterContext(BatchRenderingContext context);

	public BatchRenderingContext Context { get; set; }
}