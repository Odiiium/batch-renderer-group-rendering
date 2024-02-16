﻿using System.Collections.Generic;
using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Rendering;
using UnityEngine;
using System.Linq;

public abstract class BatchRendererSystem : IBatchRendererSystem
{
	protected BatchRenderingContext _context;

	protected BatchRendererGroup _batchRenderGroup;
	protected GraphicsBuffer _instanceBuffer;

	protected BatchMeshID _meshId;
	protected BatchMaterialID _materialId;
	protected BatchID _batchId;

	protected List<ShaderPropertyData> _properties;
	protected List<int> _addresses;

	public BatchRenderingContext Context { get => _context; set => _context = value; }

	public BatchRendererSystem() { }

	public virtual IBatchRendererSystem Execute()
	{
		AllocateBuffer();
		CreateBatch();
		return this;
	}

	public void RegisterContext(BatchRenderingContext context)
	{
		Context = context;
		RegisterData();
	}

	private void RegisterData()
	{
		_batchRenderGroup = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
		_meshId = _batchRenderGroup.RegisterMesh(_context.Mesh);
		_materialId = _batchRenderGroup.RegisterMaterial(_context.Material);
	}

	protected virtual void AllocateBuffer()
	{
		int size = BufferCountForInstances(_properties.Sum(x => x.Size), _context.NumInstances, _context.BufferStride, _context.BufferFreeSpace);
		_instanceBuffer = new GraphicsBuffer(_context.BufferTarget, size, _context.BufferStride);
	}

	protected virtual void CreateBatch()
	{
		List<int> bufferAdresses = GetShadersAddresses(_context.BufferFreeSpace).ToList();
		PopulateBuffer(bufferAdresses);
		NativeArray<MetadataValue> metadata = CreateMetadata(_properties, bufferAdresses);
		_batchId = _batchRenderGroup.AddBatch(metadata, _instanceBuffer.bufferHandle);

	}

	protected virtual List<int> GetShadersAddresses(int freeSpace = 96)
	{
		int lastAddress = freeSpace;
		return _properties.Select((x, i) => i == 0 ? lastAddress : lastAddress += _properties[i - 1].Size * _context.NumInstances).ToList();
	}

	protected virtual void PopulateBuffer(List<int> bufferAdresses)
	{
		ShaderPropertyData shaderData = null;
		for (int i = 0; i < _properties.Count; i++)
		{
			shaderData = _properties[i];
			_instanceBuffer.SetData(shaderData.DataArray, 0, bufferAdresses[i] / shaderData.Size, _context.NumInstances);
		}
	}

	private NativeArray<MetadataValue> CreateMetadata(List<ShaderPropertyData> propertiesData, List<int> addresses)
	{
		return new NativeArray<MetadataValue>(propertiesData.
			Select((x, i) => new MetadataValue()
			{
				NameID = x.PropertyId,
				Value = x.MetadataValue | (uint)addresses[i]
			}
			).ToArray(), Allocator.Temp);
	}

	public unsafe IBatchRendererSystem AddShaderData<T>(string propertyName, uint metadataValue, T[] array) where T : unmanaged
	{
		ShaderPropertyData data = new ShaderPropertyData(Shader.PropertyToID(propertyName), metadataValue, sizeof(T), 0);
		data.SetArray(array);
		(_properties ??= new List<ShaderPropertyData>()).Add(data);
		return this;
	}

	public void Dispose()
	{
		ReleaseBuffer();
	}

	public void ReleaseBuffer()
	{
		_instanceBuffer.Release();
		_instanceBuffer.Dispose();
		_batchRenderGroup.Dispose();
	}

	protected int BufferCountForInstances(int bytesPerInstance, int numInstances, int stride, int extraBytes = 0)
	{
		int totalBytes = bytesPerInstance * numInstances + extraBytes;
		totalBytes &= -16;
		return totalBytes / stride;
	}

	public unsafe JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
	{
		int alignment = UnsafeUtility.AlignOf<long>();

		var drawCommands = (BatchCullingOutputDrawCommands*)cullingOutput.drawCommands.GetUnsafePtr();

		drawCommands->drawCommands = (BatchDrawCommand*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawCommand>(), alignment, Allocator.TempJob);
		drawCommands->drawRanges = (BatchDrawRange*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawRange>(), alignment, Allocator.TempJob);
		drawCommands->visibleInstances = (int*)UnsafeUtility.Malloc(_context.NumInstances * sizeof(float), alignment, Allocator.TempJob);
		drawCommands->drawCommandPickingInstanceIDs = null;

		drawCommands->drawCommandCount = 1;
		drawCommands->drawRangeCount = 1;
		drawCommands->visibleInstanceCount = _context.NumInstances;

		drawCommands->instanceSortingPositions = null;
		drawCommands->instanceSortingPositionFloatCount = 0;

		drawCommands->drawCommands[0].visibleOffset = 0;
		drawCommands->drawCommands[0].visibleCount = (uint)_context.NumInstances;
		drawCommands->drawCommands[0].batchID = _batchId;
		drawCommands->drawCommands[0].materialID = _materialId;
		drawCommands->drawCommands[0].meshID = _meshId;
		drawCommands->drawCommands[0].submeshIndex = 0;
		drawCommands->drawCommands[0].splitVisibilityMask = 0xff;
		drawCommands->drawCommands[0].flags = BatchDrawCommandFlags.None;
		drawCommands->drawCommands[0].sortingPosition = 0;

		drawCommands->drawRanges[0].drawCommandsBegin = 0;
		drawCommands->drawRanges[0].drawCommandsCount = 1;
		drawCommands->drawRanges[0].filterSettings = new BatchFilterSettings() { renderingLayerMask = 0xffffffff };

		for (int i = 0; i < _context.NumInstances; ++i)
			drawCommands->visibleInstances[i] = i;

		return new JobHandle();
	}
}