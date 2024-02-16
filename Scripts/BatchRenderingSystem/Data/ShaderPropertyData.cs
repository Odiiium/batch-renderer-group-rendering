using System;

public class ShaderPropertyData
{
	public Array DataArray;
	public int PropertyId;
	public uint MetadataValue;
	public int Size;
	public int DataOffset;

	public void SetArray<T>(T[] array) where T : unmanaged
	{
		DataArray = array;
	}

	public ShaderPropertyData(int id, uint metadataValue, int size)
	{
		PropertyId = id;
		MetadataValue = metadataValue;
		Size = size;
		DataOffset = 0;
	}

	public ShaderPropertyData(int id, uint metadataValue, int size, int dataOffset) : this(id, metadataValue, size)
	{
		DataOffset = dataOffset;
	}
}