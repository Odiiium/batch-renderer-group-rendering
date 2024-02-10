using UnityEngine;

struct PackedMatrix
{
	public float c0x;
	public float c0y;
	public float c0z;
	public float c1x;
	public float c1y;
	public float c1z;
	public float c2x;
	public float c2y;
	public float c2z;
	public float c3x;
	public float c3y;
	public float c3z;

	public PackedMatrix(Matrix4x4 m)
	{
		c0x = m.m00;
		c0y = m.m10;
		c0z = m.m20;
		c1x = m.m01;
		c1y = m.m11;
		c1z = m.m21;
		c2x = m.m02;
		c2y = m.m12;
		c2z = m.m22;
		c3x = m.m03;
		c3y = m.m13;
		c3z = m.m23;
	}
}
