using UnityEngine.Rendering;
using UnityEngine;

[System.Serializable]
public class BatchRenderingContext
{
	public Mesh Mesh;
	public Material Material;
	public int BufferStride;
	public int NumInstances;
	public int BufferFreeSpace;

	public GraphicsBuffer.Target BufferTarget
	{ get => BatchRendererGroup.BufferTarget == BatchBufferTarget.RawBuffer ? GraphicsBuffer.Target.Raw : GraphicsBuffer.Target.Constant; }
}