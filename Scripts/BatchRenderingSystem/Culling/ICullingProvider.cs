using System;
using UnityEngine;

public interface ICullingProvider : IDisposable
{
	public unsafe ICullingProvider FillPositionsBuffer(Vector3[] positionsToCull);
	public unsafe ICullingProvider FillStatesBuffer<T>(int instancesCount) where T : unmanaged;
	public ICullingProvider SetMeshBounds(Vector3 meshBounds);
	public T[] GetStates<T>(T[] states);
	public void ExecuteData();
	public void PerformDispatch();
}
