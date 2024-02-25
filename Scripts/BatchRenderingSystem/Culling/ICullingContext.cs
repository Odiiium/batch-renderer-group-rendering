using UnityEngine;

public interface ICullingContext
{
	public Camera Camera { get; set; }
	public ComputeShader ComputeShader { get; set; }
	public string KernelName { get; set; }
	public int GroupSize { get; set; }
	public bool UseSquaredComputing { get; set; }
}