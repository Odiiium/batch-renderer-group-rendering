using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FuncType = NoiseOctave.FunctionType;

public class NoiseGenerator
{
	private float _amplitude;
	private float _scaleValue;
	private float _persistance;

	public static HashSet<FuncType> AllTypes { get => _types ??= new HashSet<FuncType>
		(Enum.GetNames(typeof(FuncType)).Select(x => Enum.Parse<FuncType>(x))); }
	private static HashSet<FuncType> _types;


	public NoiseGenerator(float amplitude, float scaleValue, float persistance)
	{
		_amplitude = amplitude;
		_scaleValue = scaleValue;	
		_persistance = persistance;
	}

	public float PerlinNoise(Vector2 position, float amplitude)
	{
		return Mathf.PerlinNoise(position.x / _amplitude, position.y / _amplitude) * amplitude;
	}

	public float CalculateNoiseByOctaves(Vector2 position, List<NoiseOctave> octaves)
	{
		float noiseValue = 0;
		float perlinValue = 0;
		float amplitude = 1;
		float frequency = 1;
		float octaveNumericModifier = 1;

		for (int i = 0; i < octaves.Count; i++)
		{
			perlinValue = Mathf.PerlinNoise(position.x * frequency * _scaleValue, position.y * frequency * _scaleValue);
			octaveNumericModifier = (1 << octaves[i].OctaveDegree) * _persistance;

			noiseValue += perlinValue * amplitude;

			amplitude /= octaveNumericModifier;
			frequency *= octaveNumericModifier;
		}

		return noiseValue * _amplitude;
	}	

	public IEnumerable<NoiseOctave> GenerateRandomOctaves(int count, IEnumerable<FuncType> includedFunctions = null)
	{
		float threshold = 0;
		return Enumerable.Range(0, count).Select(x =>
		{
			FuncType functionType = includedFunctions is null ? includedFunctions.GetRandom() : AllTypes.GetRandom();
			return new NoiseOctave((byte)x, functionType);
		});
	}

	private float GetOctaveValue(float value, NoiseOctave octave)
	{
		switch (octave.Function)
		{
			case FuncType.Sin:
				return Mathf.Sin((value) / _scaleValue) * _amplitude;
			case FuncType.Cos:
				return Mathf.Cos((value) / _scaleValue) * _amplitude;
			default:
				return 1;
		}
	}
}

[System.Serializable]
public struct NoiseOctave
{
	public byte OctaveDegree;
	public FunctionType Function;

	public NoiseOctave(byte octaveDegree, FunctionType func)
	{
		OctaveDegree = octaveDegree;
		Function = func;	
	}

	[System.Serializable]
	public enum FunctionType
	{
		Sin, Cos
	}	
}