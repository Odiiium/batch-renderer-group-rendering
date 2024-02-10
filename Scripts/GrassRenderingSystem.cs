using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

using Random = UnityEngine.Random;

public class GrassRenderingSystem : MonoBehaviour, IDisposable
{
	#region Inspector

	[SerializeField] private Mesh _grassMesh;
	[SerializeField] private Material _grassMaterial;
	[SerializeField] private TerrainGenerator _terrainGenerator;
	[SerializeField] private int _numInstances;
	[SerializeField] private float _randomizePositionMultiplier;

	[FoldoutGroup("Grass Settings")]
	[MinMaxSlider(0, 2)][SerializeField] private Vector2 minMaxHeight;

	private BatchRendererGroup _batchRenderGroup;
	private GraphicsBuffer _instanceBuffer;

	private BatchMeshID _meshId;
	private BatchMaterialID _materialId;
	private BatchID _batchId;

	private List<Vector3> _rendererPoints;
	private List<Vector3> _rendererNormals;
	private List<ShaderDataByProperty> _propertiesMetadata;

	private int _planeSize = 100;

	private const int _sizeOfMatrix = 64;
	private const int _sizeOfPackedMatrix = 48;
	private const int _sizeOfFloat = 4 ;
	private const int _bytesPerInstance = _sizeOfPackedMatrix  * 2	+ _sizeOfFloat;
	private const int _extraBytes = 1024;

	#endregion Inspector


	private void Start()
	{
		_rendererPoints = new List<Vector3>();
		_rendererNormals = new List<Vector3>();

		_terrainGenerator.onTerrainGenerated += RenderGrass;
		_terrainGenerator.onTerrainRegenerated += Dispose;
	}

	private void OnDisable()
	{
		Dispose();
	}

	private void OnDestroy()
	{
		_instanceBuffer.Release();
		_instanceBuffer.Dispose();
		_terrainGenerator.onTerrainGenerated -= RenderGrass;
		_terrainGenerator.onTerrainRegenerated -= Dispose;
	}


	[ContextMenu("Force dispose")]
	public void Dispose()
	{
		_batchRenderGroup.Dispose();
		_instanceBuffer.Dispose();
		_rendererPoints.Clear();
	}

	private void RenderGrass()
	{
		_propertiesMetadata = SetupShaderProperties();

		SetupRendererGroup();

		_rendererPoints = _terrainGenerator.MeshFilter.mesh.vertices.ToList();
		_rendererNormals = _terrainGenerator.MeshFilter.mesh.normals.ToList();

		PopulateInstanceDataBuffer(_batchRenderGroup, _instanceBuffer, _propertiesMetadata, _numInstances);
	}

	private void SetupRendererGroup()
	{
		_batchRenderGroup = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
		_meshId = _batchRenderGroup.RegisterMesh(_grassMesh);
		_materialId = _batchRenderGroup.RegisterMaterial(_grassMaterial);

		AllocateInstanceDataBuffer(_propertiesMetadata);
	}

	private void AllocateInstanceDataBuffer(List<ShaderDataByProperty> propertiesMetadata)
	{
		int stride = 4;

		int bytesPerInstance = propertiesMetadata.Where(x => x.MetadataValue == 0x80000000).Sum(x => x.Size);
		int bufferSize = BufferCountForInstances(bytesPerInstance, _numInstances, stride, _extraBytes);

		_instanceBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, bufferSize, stride);
	}

	private void PopulateInstanceDataBuffer(BatchRendererGroup rendererGroup, GraphicsBuffer buffer, List<ShaderDataByProperty> propertiesData, int numInstances)
	{
		List<int> bufferAdresses = CalculatePropertyAddresses(propertiesData, numInstances);
		PopulateDataBuffer(buffer, propertiesData, bufferAdresses, numInstances);

		NativeArray<MetadataValue> metadata = CreateMetadata(propertiesData, bufferAdresses);

		_batchId = rendererGroup.AddBatch(metadata, buffer.bufferHandle);
	}

	public List<ShaderDataByProperty> SetupShaderProperties()
	{
		return new List<ShaderDataByProperty>()
		{
			new ShaderDataByProperty(Shader.PropertyToID("unity_ObjectToWorld"), 0x80000000, _sizeOfPackedMatrix),
			new ShaderDataByProperty(Shader.PropertyToID("unity_WorldToObject"), 0x80000000, _sizeOfPackedMatrix),
			new ShaderDataByProperty(Shader.PropertyToID("_StaticGrassHeight"), 0x80000000, _sizeOfFloat)
		};
	}

	private void PopulateDataBuffer(GraphicsBuffer buffer, List<ShaderDataByProperty> propertiesData, List<int> bufferAdresses, int numInstances)
	{
		Matrix4x4[] matrices = FillMatricesByTerrainPositions(numInstances);

		PackedMatrix[] objectToWorldPackedMatrices = GetPackedMatrices(matrices);

		PackedMatrix[] worldToObjectPackedMatrices = GetPackedMatrices(matrices.Select(x => x.inverse).ToArray()).ToArray();

		float[] heights = Enumerable.Range(0, numInstances).Select(x => Random.Range(minMaxHeight.x, minMaxHeight.y)).ToArray();

		buffer.SetData(objectToWorldPackedMatrices, 0, bufferAdresses[0] / 48, objectToWorldPackedMatrices.Length);
		buffer.SetData(worldToObjectPackedMatrices, 0, bufferAdresses[1] / 48, worldToObjectPackedMatrices.Length);
		buffer.SetData(heights, 0, bufferAdresses[2] / 4, heights.Length);
	}

	private List<int> CalculatePropertyAddresses(List<ShaderDataByProperty> propertiesData, int numInstances)
	{
		List<int> addresses = new List<int>();

		int zeroMatrixAddress = 4 * 12 * 2;
		int lastPropertyAddress = zeroMatrixAddress;

		for (byte i = 0; i < propertiesData.Count; i++)
		{
			addresses.Add(lastPropertyAddress);
			lastPropertyAddress = lastPropertyAddress + propertiesData[i].Size * numInstances;
		}

		int id = 0;
		addresses.ForEach(x => { id++; Debug.LogError($"adress id {id}, address {x}");});

		return addresses;
	}

	private NativeArray<MetadataValue> CreateMetadata(List<ShaderDataByProperty> propertiesData, List<int> addresses)
	{
		return new NativeArray<MetadataValue>(propertiesData.
			Select((x, i) => new MetadataValue() 
			{ 
				NameID = x.PropertyId,
				Value = x.MetadataValue | (uint)addresses[i] 
			}
			).ToArray(), Allocator.Temp);
	}

	public unsafe JobHandle OnPerformCulling(BatchRendererGroup rendererGroup,
		BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
	{
		int alignment = UnsafeUtility.AlignOf<long>();

		var drawCommands = (BatchCullingOutputDrawCommands*)cullingOutput.drawCommands.GetUnsafePtr();

		drawCommands->drawCommands = (BatchDrawCommand*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawCommand>(), alignment, Allocator.TempJob);
		drawCommands->drawRanges = (BatchDrawRange*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawRange>(), alignment, Allocator.TempJob);
		drawCommands->visibleInstances = (int*)UnsafeUtility.Malloc(_numInstances * sizeof(float), alignment, Allocator.TempJob);
		drawCommands->drawCommandPickingInstanceIDs = null;

		drawCommands->drawCommandCount = 1;
		drawCommands->drawRangeCount = 1;
		drawCommands->visibleInstanceCount = _numInstances;

		drawCommands->instanceSortingPositions = null;
		drawCommands->instanceSortingPositionFloatCount = 0;

		drawCommands->drawCommands[0].visibleOffset = 0;
		drawCommands->drawCommands[0].visibleCount = (uint)_numInstances;
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

		for (int i = 0; i < _numInstances; ++i)
			drawCommands->visibleInstances[i] = i;

		return new JobHandle();
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

	private PackedMatrix[] GetPackedMatrices(Matrix4x4[] matrices4x4)
	{
		return matrices4x4.Select(x => new PackedMatrix(x)).ToArray();
	}

	
	private int BufferCountForInstances(int bytesPerInstance, int numInstances, int stride, int extraBytes = 0)
	{
		int totalBytes = bytesPerInstance * numInstances + extraBytes;
		totalBytes &= -16;
		SSDebug.Log(totalBytes);
		return totalBytes / stride;
	}
}