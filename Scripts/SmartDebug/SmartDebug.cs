using UnityEngine;

public static class SmartDebug 
{
	public static void Log(object message)
	{
		Debug.LogError($"[LOG] {message}");
	}

	public static void LogBase(object message)
	{
		Debug.Log($"[LOG] [BASE] {message}");
	}
}