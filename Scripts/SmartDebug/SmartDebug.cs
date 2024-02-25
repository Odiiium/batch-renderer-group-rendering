using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class SmartDebug 
{
	private static StringBuilder _builder;

	static SmartDebug()
	{
		_builder = new StringBuilder();
	}

	public static void Log(object message)
	{
		Debug.LogError($"[LOG] {message}");
	}

	public static void LogBase(object message)
	{
		Debug.Log($"[LOG] [BASE] {message}");
	}

	public static void ForEachToStringDebug<T>(IEnumerable<T> objects, string beforeEach = "", string afterEach = "")
	{
		if (objects == null || objects.Count() == 0)
			Log("Collection is null or empty");

		_builder.Clear();
		objects.ForEach(x => _builder.AppendLine($"{beforeEach} {x.ToString()} {afterEach}"));
		Log(_builder.ToString());
	}

	public static void ForEachDebug<T>(IEnumerable<T> objects, Func<object, string> forEachAction, string beforeEach = "", string afterEach = "")
	{
		if (objects == null || objects.Count() == 0)
			Log("Collection is null or empty");

		_builder.Clear();
		objects.ForEach(x => _builder.Append($"{beforeEach} {forEachAction.Invoke(x)} {afterEach}"));
		Log(_builder.ToString());
	}
}