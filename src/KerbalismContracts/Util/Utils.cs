using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using UnityEngine;
using CommNet;
using KSP.Localization;
using KSP.UI.Screens;
using KSP.UI;
using KSP.UI.Screens.Flight;
using System.Collections;
using System.Linq.Expressions;

namespace KerbalismContracts
{
	public enum LogLevel
	{
		Message,
		Warning,
		Error
	}

	public class Utils
	{
		internal static bool RelevantVessel(Vessel vessel)
		{
			if (vessel == null)
				return false;

			if (vessel.Landed)
				return false;

			switch (vessel.vesselType)
			{
				case VesselType.Unknown:
				case VesselType.EVA:
				case VesselType.Debris:
				case VesselType.Flag:
				case VesselType.DeployedScienceController:
				case VesselType.DeployedSciencePart:
				case VesselType.SpaceObject:
					return false;
			}

			return true;
		}

		private static void Log(MethodBase method, string message, LogLevel level)
		{
			switch (level)
			{
				default:
					UnityEngine.Debug.Log(string.Format("[KerCon] {0}.{1} {2}", method.ReflectedType.Name, method.Name, message));
					return;
				case LogLevel.Warning:
					UnityEngine.Debug.LogWarning(string.Format("[KerCon] {0}.{1} {2}", method.ReflectedType.Name, method.Name, message));
					return;
				case LogLevel.Error:
					UnityEngine.Debug.LogError(string.Format("[KerCon] {0}.{1} {2}", method.ReflectedType.Name, method.Name, message));
					return;
			}
		}

		///<summary>write a message to the log</summary>
		public static void Log(string message, LogLevel level = LogLevel.Message, params object[] param)
		{
			StackTrace stackTrace = new StackTrace();
			Log(stackTrace.GetFrame(1).GetMethod(), string.Format(message, param), level);
		}

		///<summary>write a message and the call stack to the log</summary>
		public static void LogStack(string message, LogLevel level = LogLevel.Message, params object[] param)
		{
			StackTrace stackTrace = new StackTrace();
			Log(stackTrace.GetFrame(1).GetMethod(), string.Format(message, param), level);

			// KSP will already log the stacktrace if the log level is error
			if (level != LogLevel.Error)
				UnityEngine.Debug.Log(stackTrace);
		}

		///<summary>write a message to the log, only on DEBUG and DEVBUILD builds</summary>
		[Conditional("DEBUG"), Conditional("DEVBUILD")]
		public static void LogDebug(string message, LogLevel level = LogLevel.Message, params object[] param)
		{
			StackTrace stackTrace = new StackTrace();
			Log(stackTrace.GetFrame(1).GetMethod(), string.Format(message, param), level);
		}

		///<summary>write a message and the full call stack to the log, only on DEBUG and DEVBUILD builds</summary>
		[Conditional("DEBUG"), Conditional("DEVBUILD")]
		public static void LogDebugStack(string message, LogLevel level = LogLevel.Message, params object[] param)
		{
			StackTrace stackTrace = new StackTrace();
			Log(stackTrace.GetFrame(1).GetMethod(), string.Format(message, param), level);

			// KSP will already log the stacktrace if the log level is error
			if (level != LogLevel.Error)
				UnityEngine.Debug.Log(stackTrace);
		}
	}
}
