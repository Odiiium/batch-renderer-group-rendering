using System;
using System.Linq;
using UnityEngine;

public class CullingProvider : ICullingProvider
{
	private ComputeBuffer _positionsBuffer;
	private ComputeBuffer _statesBuffer;

	private readonly Camera _mainCamera;
	private readonly ComputeShader _shader;
	private readonly int _cullKernelIndex;
	private readonly int _groupSize;
	private readonly bool _useSquaredComputing;
	private int _instancesCount;

	private const string CAMERA_VP_MATRIX_NAME = "CameraViewProjection";
	private const string CAMERA_POSITION_NAME = "CameraPosition";
	private const string MESH_BOUNDS_NAME = "MeshBounds";
	private const string POSITIONS_BUFFER_NAME = "Positions";
	private const string STATES_BUFFER_NAME = "States";
	private const string SCREEN_ALLOWED_SIZE_NAME = "SCREEN_ALLOWED_SIZE";
	private const string Z_ALLOWED_SIZE_NAME = "Z_ALLOWED_SIZE";

	private const float SCREEN_ALLOWED_SIZE = 1.3f;
	private const float Z_ALLOWED_SIZE = -1f;

	public CullingProvider(ICullingContext context)
	{
		_mainCamera = context.Camera;
		_shader = context.ComputeShader;
		_cullKernelIndex = context.ComputeShader.FindKernel(context.KernelName);
		_groupSize = context.GroupSize;
		_useSquaredComputing = context.UseSquaredComputing;
	}

	public unsafe ICullingProvider FillPositionsBuffer(Vector3[] positionsToCull)
	{
		_positionsBuffer = new ComputeBuffer(positionsToCull.Length, sizeof(Vector3));
		_positionsBuffer.SetData(positionsToCull);
		return this;
	}

	public unsafe ICullingProvider FillStatesBuffer<T>(int instancesCount) where T : unmanaged
	{
		this._instancesCount = instancesCount;
		_statesBuffer = new ComputeBuffer(instancesCount, sizeof(T));
		T[] states = new T[instancesCount];
		Array.Fill(states, default(T));
		_statesBuffer.SetData(states);
		return this;
	}

	public ICullingProvider SetMeshBounds(Vector3 meshBounds)
	{
		_shader.SetVector(MESH_BOUNDS_NAME, meshBounds);
		return this;
	}

	public void ExecuteData()
	{
		_shader.SetBuffer(_cullKernelIndex, POSITIONS_BUFFER_NAME, _positionsBuffer);
		_shader.SetBuffer(_cullKernelIndex, STATES_BUFFER_NAME, _statesBuffer);
		_shader.SetMatrix(CAMERA_VP_MATRIX_NAME, _mainCamera.projectionMatrix * _mainCamera.transform.worldToLocalMatrix);
		_shader.SetVector(CAMERA_POSITION_NAME, _mainCamera.transform.position);
		_shader.SetFloat(SCREEN_ALLOWED_SIZE_NAME, SCREEN_ALLOWED_SIZE);
		_shader.SetFloat(Z_ALLOWED_SIZE_NAME, Z_ALLOWED_SIZE);
	}


	public T[] GetStates<T>(T[] states)
	{
		_statesBuffer.GetData(states);
		return states;
	}

	public void PerformDispatch()
	{
		_shader.SetMatrix(CAMERA_VP_MATRIX_NAME, _mainCamera.projectionMatrix * _mainCamera.transform.worldToLocalMatrix);
		_shader.SetVector(CAMERA_POSITION_NAME, _mainCamera.transform.position);
		_shader.Dispatch(_cullKernelIndex, _instancesCount / _groupSize, _useSquaredComputing ? _instancesCount / _groupSize : 1, 1);
	}

	public void Dispose()
	{
		_positionsBuffer.Dispose();
		_statesBuffer.Dispose();
	}
}