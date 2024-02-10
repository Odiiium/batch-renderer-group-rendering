using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TerrainGenerator : MonoBehaviour
{
	[SerializeField] private int _terrainLength;
	[SerializeField] private float _maxHeight;
	[SerializeField] private float _scaleModifier;
	[SerializeField] private float _seedScaleModifier;

	[Header("Mesh")]
	[SerializeField] private Material _material;

	public MeshFilter MeshFilter { get => _filter ??= GetComponent<MeshFilter>(); }
	private MeshFilter _filter;
	public MeshRenderer Renderer { get => _renderer ??= GetComponent<MeshRenderer>(); }	
	private MeshRenderer _renderer;

	public static Mesh CurrentMesh { get; private set; }

	public event Action onTerrainGenerated;
	public event Action onTerrainRegenerated;

	private const float UNIVERSAL_SEED = 235.142f;

	private void Start()
	{
		GenerateTerrain();
	}

	[ContextMenu("Regenerate")]
	public void Regenerate()
	{
		onTerrainRegenerated?.Invoke();
		GenerateTerrain();
	}

	public void GenerateTerrain()
	{
		Vector3[] vertices = CalculateVertices();
		int[] triangles = CalculateTriangles();

		Mesh mesh = new Mesh
		{
			vertices = vertices,
			triangles = triangles
		};

		mesh.RecalculateNormals();

		CurrentMesh = mesh;
		MeshFilter.mesh = mesh;
		Renderer.material = _material;
		onTerrainGenerated?.Invoke();
	}

	private Vector3[] CalculateVertices()
	{
		Vector3[] vertices = new Vector3[(_terrainLength + 1) * (_terrainLength + 1)];

		float seed = Time.timeSinceLevelLoad + UNIVERSAL_SEED;

		for (int i = 0, v = 0; i <= _terrainLength; i++)
			for (int j = 0; j <= _terrainLength; j++)
			{
				var height = Mathf.PerlinNoise(i * seed / _seedScaleModifier, j * seed / _seedScaleModifier) * _maxHeight;
				vertices[v] = new Vector3(j * _scaleModifier, height, i * _scaleModifier);
				v++;
			}
		return vertices;
	}

	private int[] CalculateTriangles()
	{
		int[] triangles = new int[_terrainLength * _terrainLength * 6];
		int tris = 0;
		int vert = 0;

		for (int j = 0; j < _terrainLength; j++)
		{
			for (int i = 0; i < _terrainLength; i++)
			{
				triangles[tris] = vert;
				triangles[tris + 1] = vert + _terrainLength + 1;
				triangles[tris + 2] = vert + 1;
				triangles[tris + 3] = vert + 1;
				triangles[tris + 4] = vert + _terrainLength + 1;
				triangles[tris + 5] = vert + _terrainLength + 2;
				vert++;
				tris += 6;
			}
			vert++;
		}

		return triangles;
	}
}
