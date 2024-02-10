using System;

public class ShaderDataByProperty
{
	public Array DataArray;
	public int PropertyId;
	public uint MetadataValue;
	public int Size;
	public int DataOffset;

	public ShaderDataByProperty(int id, uint metadataValue, int size)
	{
		PropertyId = id;
		MetadataValue = metadataValue;
		Size = size;
		DataOffset = 0;
	}

	public ShaderDataByProperty(int id, uint metadataValue, int size, Array data, int dataOffset) : this(id, metadataValue, size)
	{
		DataOffset = dataOffset;
		DataArray = data;
	}


}