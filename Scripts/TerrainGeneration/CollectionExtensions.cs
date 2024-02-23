using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CollectionExtensions
{
	public static T GetRandom<T>(this IEnumerable<T> list)
	{
		return list.ElementAt(Random.Range(0, list.Count() - 1));
	}
}