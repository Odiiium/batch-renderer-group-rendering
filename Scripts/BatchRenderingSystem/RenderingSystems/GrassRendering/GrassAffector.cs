using System.Collections;
using UnityEngine;

public class GrassAffector : MonoBehaviour
{
	[SerializeField] private Material _grassMaterial;

	private const string AFFECTOR_POSITION_SHADER_PROPERTY = "_AffectorPosition";

	public void Update()
	{
		SetGlobalPositionToShader();
	}

	private void SetGlobalPositionToShader() =>
		_grassMaterial.SetVector(AFFECTOR_POSITION_SHADER_PROPERTY, transform.position);
}
