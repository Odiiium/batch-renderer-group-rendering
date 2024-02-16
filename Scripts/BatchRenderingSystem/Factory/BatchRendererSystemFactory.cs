public class BatchRendererSystemFactory
{
	public T CreateBatchRendererSystem<T>(BatchRenderingContext context) where T : IBatchRendererSystem, new()
	{
		T batchRendererSystem = new T();
		batchRendererSystem.RegisterContext(context);
		return batchRendererSystem;
	}
}