using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TerrainGenerator : MonoBehaviour
{
	[Header("Mesh")]
	[SerializeField] private Material _material;

	[Header("Chunk settings")]
	[SerializeField] private int _chunkCountX, _chunkCountY;
	[SerializeField] private float _chunkSize;
	[SerializeField][Range(1, 256)] private int _lineVerticesCount;

	[Header("Noise generation")]
	[SerializeField][Range(.01f, 1000)] private float _amplitude;
	[SerializeField][Range(.001f, 5)] private float _scaleValue;
	[SerializeField] private float _persistance;
	[SerializeField] private int _octavesCount;

	[Header("Data")]
	[SerializeField] private List<NoiseOctave.FunctionType> noiseFunctions;
	[ShowInInspector] private List<NoiseOctave> _noiseOctaves;
	[ShowInInspector] private List<Chunk> _chunks;
	[ShowInInspector] private List<GameObject> _planes;

	private NoiseGenerator _noiseGenerator;

	public MeshFilter MeshFilter { get => _filter ??= GetComponent<MeshFilter>(); }
	private MeshFilter _filter;
	public MeshRenderer Renderer { get => _renderer ??= GetComponent<MeshRenderer>(); }	
	private MeshRenderer _renderer;

	public static Mesh CurrentMesh { get; private set; }

	public event Action onTerrainGenerated;
	public event Action onTerrainRegenerated;

	private void Start()
	{
		GenerateTerrain();
	}

	[ContextMenu(nameof(Regenerate))]
	public void Regenerate()
	{
		_chunks.ForEach(x => Destroy(x.MeshFilter.gameObject));
		_chunks.Clear();
		GenerateTerrain();
		onTerrainRegenerated?.Invoke();
	}

	[ContextMenu(nameof(GenerateTerrain))]
	public void GenerateTerrain()
	{
		_chunks ??= new List<Chunk>();
		_noiseGenerator = new NoiseGenerator(_amplitude, _scaleValue, _persistance);
		_noiseOctaves = _noiseGenerator.GenerateRandomOctaves(_octavesCount, noiseFunctions).ToList();

		for (int y = 0; y < _chunkCountY; y++)
		{
			for (int x = 0; x < _chunkCountX; x++)
			{
				Chunk chunk = GenerateChunk(x + y * _chunkCountX, x, y);
				_chunks.Add(chunk);
			}
		}

		onTerrainGenerated?.Invoke();
	}

	private Chunk GenerateChunk(int id, int x, int y)
	{
		GameObject chunkObject = new GameObject();
		chunkObject.name = $"Chunk_{id}";

		Chunk chunk = new Chunk()
		{
			Id = id,
			Object = chunkObject,
			MeshFilter = chunkObject.AddComponent<MeshFilter>(),
			Renderer = chunkObject.AddComponent<MeshRenderer>()
		};

		chunkObject.transform.parent = this.transform;

		Vector3[] chunkVertices = GetChunkVertices(x, y);
		int[] triangles = GetTriangles();
		FillChunkMesh(ref chunk, chunkVertices, triangles, _material);

		return chunk;
	}

	private Vector3[] GetChunkVertices(int chunkX, int chunkY)
	{
		if ((_lineVerticesCount - 1) * (_lineVerticesCount - 1) > ushort.MaxValue)
			throw new Exception($"Mesh with vertices count = {_lineVerticesCount * _lineVerticesCount} is greater than limit and cannot be created");

		Vector3[] vertices = new Vector3[_lineVerticesCount * _lineVerticesCount];

		float vertexDistance = _chunkSize / (_lineVerticesCount - 1);
		float height, xPos, yPos = 0;
		Vector2 posVector = Vector2.zero;

		for (int i = 0, v = 0; i < _lineVerticesCount; i++)
			for (int j = 0; j < _lineVerticesCount; j++)
			{
				xPos = chunkX * _chunkSize + j * vertexDistance;
				yPos = chunkY * _chunkSize + i * vertexDistance;
				posVector.x = xPos;
				posVector.y = yPos;

				height = _noiseGenerator.CalculateNoiseByOctaves(posVector, _noiseOctaves);

				vertices[v] = new Vector3(xPos, height, yPos);
				v++;
			}
		return vertices;
	}

	private int[] GetTriangles()
	{
		int edgesInRow = _lineVerticesCount - 1;
		int[] triangles = new int[edgesInRow * edgesInRow * 6];
		int tris = 0;
		int vert = 0;
		
		for (int j = 0; j < edgesInRow; j++)
		{
			for (int i = 0; i < edgesInRow; i++)
			{
				triangles[tris] = vert;
				triangles[tris + 1] = vert + edgesInRow + 1;
				triangles[tris + 2] = vert + 1;
				triangles[tris + 3] = vert + 1;
				triangles[tris + 4] = vert + edgesInRow + 1;
				triangles[tris + 5] = vert + edgesInRow + 2;
				vert++;
				tris += 6;
			}
			vert++;
		}
		
		return triangles;
	}

	private void FillChunkMesh(ref Chunk chunk, Vector3[] vertices, int[] triangles, Material material)
	{
		chunk.Renderer.material = material;
		chunk.MeshFilter.mesh = new Mesh()
		{
			vertices = vertices,
			triangles = triangles
		};
		chunk.MeshFilter.mesh.RecalculateNormals();
	}

	public List<Chunk> GetChunks() => _chunks;
}

[System.Serializable]
public struct Chunk
{
	public int Id;
	public GameObject Object;
	public MeshRenderer Renderer;
	public MeshFilter MeshFilter;
}
