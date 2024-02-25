using System;
using UnityEngine;

[Serializable]
public struct CullingContext : ICullingContext
{
	public Camera Camera { get => _camera; set => _camera = value; }
	[SerializeField] private Camera _camera;
	public ComputeShader ComputeShader { get => _shader; set => _shader = value; }
	[SerializeField] private ComputeShader _shader;
	public string KernelName { get => _kernelName; set => _kernelName = value; }
	[SerializeField] private string _kernelName;
	public int GroupSize { get => _groupSize; set => _groupSize = value; }
	[SerializeField] private int _groupSize;
	public bool UseSquaredComputing { get => _useSquaredComputing; set => _useSquaredComputing = value; }
	[SerializeField] private bool _useSquaredComputing;
}