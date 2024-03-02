using System;
using System.Linq;
using UnityEngine;

public class CullingProvider : ICullingProvider
{
	private ComputeBuffer _chunkBuffer;

	private readonly Camera _mainCamera;
	private readonly ComputeShader _shader;
	private readonly int _cullKernelIndex;
	private readonly int _groupSize;
	private readonly bool _useSquaredComputing;
	private int _instancesCount;

	private const string CAMERA_VP_MATRIX_NAME = "CameraViewProjection";
	private const string CAMERA_POSITION_NAME = "CameraPosition";
	private const string CHUNK_BOUNDS_NAME = "ChunkBounds";
	private const string CHUNK_BUFFER_NAME = "Chunks";
	private const string SCREEN_ALLOWED_SIZE_NAME = "SCREEN_ALLOWED_SIZE";
	private const string Z_ALLOWED_SIZE_NAME = "Z_ALLOWED_SIZE";

	private const float SCREEN_ALLOWED_SIZE = 1.25f;
	private const float Z_ALLOWED_SIZE = -1f;

	public CullingProvider(ICullingContext context)
	{
		_mainCamera = context.Camera;
		_shader = context.ComputeShader;
		_cullKernelIndex = context.ComputeShader.FindKernel(context.KernelName);
		_groupSize = context.GroupSize;
		_useSquaredComputing = context.UseSquaredComputing;
	}

	public unsafe ICullingProvider FillChunksBuffer(RendererChunk[] chunksToCull)
	{
		_instancesCount = chunksToCull.Count();
		_chunkBuffer = new ComputeBuffer(chunksToCull.Length, sizeof(RendererChunk));
		_chunkBuffer.SetData(chunksToCull);
		return this;
	}

	public ICullingProvider SetChunkBounds(Vector3 chunkBounds)
	{
		_shader.SetVector(CHUNK_BOUNDS_NAME, chunkBounds);
		return this;
	}

	public void ExecuteData()
	{
		_shader.SetBuffer(_cullKernelIndex, CHUNK_BUFFER_NAME, _chunkBuffer);
		_shader.SetMatrix(CAMERA_VP_MATRIX_NAME, _mainCamera.projectionMatrix * _mainCamera.transform.worldToLocalMatrix);
		_shader.SetVector(CAMERA_POSITION_NAME, _mainCamera.transform.position);
		_shader.SetFloat(SCREEN_ALLOWED_SIZE_NAME, SCREEN_ALLOWED_SIZE);
		_shader.SetFloat(Z_ALLOWED_SIZE_NAME, Z_ALLOWED_SIZE);
	}

	public RendererChunk[] GetChunks(RendererChunk[] chunks)
	{
		_chunkBuffer.GetData(chunks);
		return chunks;
	}

	public void PerformDispatch()
	{
		_shader.SetMatrix(CAMERA_VP_MATRIX_NAME, _mainCamera.projectionMatrix * _mainCamera.transform.worldToLocalMatrix);
		_shader.SetVector(CAMERA_POSITION_NAME, _mainCamera.transform.position);
		_shader.Dispatch(_cullKernelIndex, _instancesCount / _groupSize, _useSquaredComputing ? _instancesCount / _groupSize : 1, 1);
	}

	public void Dispose()
	{
		_chunkBuffer.Dispose();
	}
}

