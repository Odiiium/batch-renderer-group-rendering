﻿using UnityEngine;

public interface ICullingSystem
{
	public ICullingSystem RegisterCullingProvider<T>(ICullingContext context, Vector3[] positionsToCull, Vector3 meshBounds, int instancesCount) where T : unmanaged;
	public T[] CalculateData<T>(T[] data);
	public ICullingProvider CullingProvider { get; }

	public bool IsCullingInitialized { get; set; }
}