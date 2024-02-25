using Sirenix.OdinInspector;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class BRGRenderersProvider : MonoBehaviour, IDisposable
{
	[FoldoutGroup("Rendering settings")]
	[SerializeField] private List<BatchRenderingContext> _batchContexts;
	[SerializeField] private List<CullingContext> _cullContexts;
	[SerializeField] private TerrainGenerator _terrainGenerator;
	[SerializeField] private float _randomizePositionMultiplier;

	[FoldoutGroup("Grass Settings")]
	[MinMaxSlider(0, 2)][SerializeField] private Vector2 _minMaxHeight;
	[SerializeField] private bool _allowCulling;

	private List<Vector3> _grassPositions;

	private BatchRendererSystemFactory _factory;

	private List<IBatchRendererSystem> _renderSystems;
	private List<ICullingSystem> _cullingSystems;

	private void Awake()
	{
		_factory = new BatchRendererSystemFactory();
		_grassPositions = new List<Vector3>();
		_renderSystems = new List<IBatchRendererSystem>();
		_cullingSystems = new List<ICullingSystem>();

		_terrainGenerator.onTerrainGenerated += SetupRenderers;
	}

	private void OnDestroy()
	{
		Dispose();
	}

	public void SetupRenderers()
	{
		Dispose();
		SetupGrassRenderer();
	}

	private unsafe void SetupGrassRenderer()
	{
		_grassPositions = _terrainGenerator.
			GetChunks().
			SelectMany(x => x.MeshFilter.mesh.vertices).
			ToList();

		_batchContexts[0].NumInstances = _grassPositions.Count();

		Matrix4x4[] matrices = FillMatricesByPositions(_grassPositions, _batchContexts[0].NumInstances);

		PackedMatrix[] objectToWorldPackedMatrices = GetPackedMatrices(matrices);

		PackedMatrix[] worldToObjectPackedMatrices = GetPackedMatrices(matrices.Select(x => x.inverse).ToArray()).ToArray();

		float[] heights = Enumerable.Range(0, _batchContexts[0].NumInstances).Select(x => Random.Range(_minMaxHeight.x, _minMaxHeight.y)).ToArray();

		BRGGrassRenderingSystem grassRenderingSystem = _factory.CreateBatchRendererSystem<BRGGrassRenderingSystem>(_batchContexts[0]);

		ICullingSystem rendererSystem = grassRenderingSystem.
			RegisterCullingProvider<uint>(_cullContexts[0], _grassPositions.ToArray(), Vector3.one, _batchContexts[0].NumInstances);

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
			Vector2 random = Random.insideUnitCircle * _randomizePositionMultiplier;
			point = new Vector3(positions[i].x + random.x, positions[i].y, positions[i].z + random.y);
			resultMatrices[i] = Matrix4x4.Translate(point);
		}

		return resultMatrices;
	}

	public void Dispose()
	{
		_renderSystems.ForEach(x => x.Dispose());
	}
}