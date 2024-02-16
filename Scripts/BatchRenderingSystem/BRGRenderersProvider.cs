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
	[SerializeField] private TerrainGenerator _terrainGenerator;
	[SerializeField] private float _randomizePositionMultiplier;

	[FoldoutGroup("Grass Settings")]
	[MinMaxSlider(0, 2)][SerializeField] private Vector2 minMaxHeight;

	private List<Vector3> _rendererPoints;
	private List<Vector3> _rendererNormals;

	private BatchRendererSystemFactory _factory;

	private List<IBatchRendererSystem> _renderSystems;

	private void Start()
	{
		_factory = new BatchRendererSystemFactory();
		_rendererPoints = new List<Vector3>();
		_renderSystems = new List<IBatchRendererSystem>();

		_terrainGenerator.onTerrainGenerated += SetupRenderers;
	}

	private void OnDestroy()
	{
		Dispose();
	}

	public void SetupRenderers()
	{
		SetupGrassRenderer();
	}

	private unsafe void SetupGrassRenderer()
	{
		_rendererPoints = _terrainGenerator.MeshFilter.mesh.vertices.ToList();

		Matrix4x4[] matrices = FillMatricesByTerrainPositions(_batchContexts[0].NumInstances);

		PackedMatrix[] objectToWorldPackedMatrices = GetPackedMatrices(matrices);

		PackedMatrix[] worldToObjectPackedMatrices = GetPackedMatrices(matrices.Select(x => x.inverse).ToArray()).ToArray();

		float[] heights = Enumerable.Range(0, _batchContexts[0].NumInstances).Select(x => Random.Range(minMaxHeight.x, minMaxHeight.y)).ToArray();

		IBatchRendererSystem grassRenderer = _factory.
			CreateBatchRendererSystem<BRGGrassRenderingSystem>(_batchContexts[0]).
			AddShaderData<PackedMatrix>("unity_ObjectToWorld", 0x80000000, objectToWorldPackedMatrices).
			AddShaderData<PackedMatrix>("unity_WorldToObject", 0x80000000, worldToObjectPackedMatrices).
			AddShaderData<float>("_StaticGrassHeight", 0x80000000, heights).
			Execute();

		_renderSystems.Add(grassRenderer);
	}

	private PackedMatrix[] GetPackedMatrices(Matrix4x4[] matrices4x4)
	{
		return matrices4x4.Select(x => new PackedMatrix(x)).ToArray();
	}

	private Matrix4x4[] FillMatricesByTerrainPositions(int count)
	{
		Matrix4x4[] resultMatrices = new Matrix4x4[count];
		Vector3 point;

		for (int i = 0; i < count; i++)
		{
			Vector2 random = Random.insideUnitCircle * _randomizePositionMultiplier;
			point = new Vector3(_rendererPoints[i].x + random.x, _rendererPoints[i].y, _rendererPoints[i].z + random.y);
			resultMatrices[i] = Matrix4x4.Translate(point);
		}

		return resultMatrices;
	}

	public void Dispose()
	{
		_renderSystems.ForEach(x => x.Dispose());
	}
}