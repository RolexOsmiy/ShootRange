using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MyDebug
{
	public static void Log(string logString)
	{
		if(Debug.isDebugBuild)
			Debug.Log(logString);
	}

	public static void LogRed(string logString)
	{
		#if !UNITY_EDITOR
		Log(logString);
		#else
		if(Debug.isDebugBuild)
			Debug.Log("<color=red>" + logString + "</color>");
		#endif
	}

	public static void LogBlue(string logString)
	{
		#if !UNITY_EDITOR
		Log(logString);
		#else
		if(Debug.isDebugBuild)
			Debug.Log("<color=lightblue>" + logString + "</color>");
		#endif
	}

	public static void LogGreen(string logString)
	{
		#if !UNITY_EDITOR
		Log(logString);
		#else
		if(Debug.isDebugBuild)
			Debug.Log("<color=green>" + logString + "</color>");
		#endif
	}

	public static void LogWhite(string logString)
	{
		#if !UNITY_EDITOR
		Log(logString);
		#else
		if(Debug.isDebugBuild)
			Debug.Log("<color=white>" + logString + "</color>");
		#endif
	}

	public static void LogGray(string logString)
	{
		#if !UNITY_EDITOR
		Log(logString);
		#else
		if(Debug.isDebugBuild)
			Debug.Log("<color=gray>" + logString + "</color>");
		#endif
	}
	
	public static void LogYellow(string logString)
	{
#if !UNITY_EDITOR
		Log(logString);
#else
		if(Debug.isDebugBuild)
			Debug.Log("<color=yellow>" + logString + "</color>");
#endif
	}
}
