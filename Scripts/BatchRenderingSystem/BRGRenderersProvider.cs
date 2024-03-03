using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class BRGRenderersProvider : MonoBehaviour, IDisposable
{
	[FoldoutGroup("Rendering settings")]
	[SerializeField] private List<BatchRenderingContext> _batchContexts;
	[FoldoutGroup("Rendering settings")]
	[SerializeField] private List<CullingContext> _cullContexts;
	[SerializeField] private TerrainGenerator _terrainGenerator;
	[SerializeField] private int _chunksInLineCount;

	[FoldoutGroup("Grass Settings")]
	[MinMaxSlider(0, 2)][SerializeField] private Vector2 _minMaxHeight;
	[SerializeField] private bool _allowCulling;

	private List<Vector3> _terrainPositions;
	private RendererChunk[] _rendererChunks;
	private Vector3 _chunkBounds;

	private BatchRendererSystemFactory _factory;
	private BatchRendererPreprocessor _renderingPreProcessor;

	private List<IBatchRendererSystem> _renderSystems;
	private List<ICullingSystem> _cullingSystems;

	private void Awake()
	{
		_factory = new BatchRendererSystemFactory();
		_terrainPositions = new List<Vector3>();
		_renderSystems = new List<IBatchRendererSystem>();
		_cullingSystems = new List<ICullingSystem>();
		_renderingPreProcessor = new BatchRendererPreprocessor();

		_terrainGenerator.onTerrainGenerated += PerformBatchRendering;
	}

	private void OnDestroy()
	{
		Dispose();
	}


	private void PerformBatchRendering()
	{
		_terrainPositions = _terrainGenerator.
			GetChunks().
			SelectMany(x => x.MeshFilter.mesh.vertices).
			ToList();

		_batchContexts[0].NumInstances = _terrainPositions.Count;

		ExecutePreprocessor();
		SetupRenderers();
	}

	private void ExecutePreprocessor()
	{
		_renderingPreProcessor.SetSettings(
			new ChunkPartitionSettings(_chunksInLineCount, _terrainGenerator.GetTerrainSize(), _terrainGenerator.GetAmplitude()));

		_terrainPositions = _renderingPreProcessor.GroupPositions(_terrainPositions.ToArray()).ToList();
		_rendererChunks = _renderingPreProcessor.ComputeChunks();
		_chunkBounds = _renderingPreProcessor.GetChunkBounds();
	}

	public void SetupRenderers()
	{
		Dispose();
		SetupGrassRenderer();
	}

	private unsafe void SetupGrassRenderer()
	{
		_batchContexts[0].NumInstances = _terrainPositions.Count();

		Matrix4x4[] matrices = FillMatricesByPositions(_terrainPositions.ToList(), _batchContexts[0].NumInstances);
		PackedMatrix[] objectToWorldPackedMatrices = GetPackedMatrices(matrices);
		PackedMatrix[] worldToObjectPackedMatrices = GetPackedMatrices(matrices.Select(x => x.inverse).ToArray()).ToArray();
		float[] heights = Enumerable.Range(0, _batchContexts[0].NumInstances).Select(x => Random.Range(_minMaxHeight.x, _minMaxHeight.y)).ToArray();

		BRGGrassRenderingSystem grassRenderingSystem = _factory.CreateBatchRendererSystem<BRGGrassRenderingSystem>(_batchContexts[0]);

		ICullingSystem rendererSystem = grassRenderingSystem.
			RegisterCullingProvider(_cullContexts[0], _rendererChunks, _chunkBounds, _batchContexts[0].NumInstances);

		IBatchRendererSystem grassRenderer = grassRenderingSystem.
			AddShaderData<PackedMatrix>("unity_ObjectToWorld", 0x80000000, objectToWorldPackedMatrices).
			AddShaderData<PackedMatrix>("unity_WorldToObject", 0x80000000, worldToObjectPackedMatrices).
			AddShaderData<float>("_StaticGrassHeight", 0x80000000, heights).
			Execute();

		rendererSystem.IsCullingInitialized = _allowCulling;
		grassRenderer.IsSystemInitialized = true;

		_cullingSystems.Add(rendererSystem);
		_renderSystems.Add(grassRenderer);
	}

	private PackedMatrix[] GetPackedMatrices(Matrix4x4[] matrices4x4)
	{
		return matrices4x4.Select(x => new PackedMatrix(x)).ToArray();
	}

	private Matrix4x4[] FillMatricesByPositions(List<Vector3> positions, int count)
	{
		Matrix4x4[] resultMatrices = new Matrix4x4[count];
		Vector3 point;

		for (int i = 0; i < count; i++)
		{
			point = new Vector3(positions[i].x, positions[i].y, positions[i].z);
			resultMatrices[i] = Matrix4x4.Translate(point);
		}

		return resultMatrices;
	}

	public void Dispose()
	{
		_renderSystems.ForEach(x => x.Dispose());
	}
}