using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using CommNet;
using KSP.Localization;

namespace Kerbalism.Contracts
{
	public static class Lib
	{
		public static void Log(string msg, params object[] param)
		{
			UnityEngine.Debug.Log(string.Format("{0}: {1}", "[KerbalismContracts] ", string.Format(msg, param)));
		}

		[Conditional("DEBUG")]
		public static void DebugLog(string msg, params object[] param)
		{
			UnityEngine.Debug.Log(string.Format("{0}: {1}", "[KerbalismContracts] ", string.Format(msg, param)));
		}

		private static CelestialBody homeSun = null;
		public static CelestialBody GetHomeSun()
		{
			if(homeSun == null)
			{
				homeSun = FlightGlobals.GetHomeBody();
				do
				{
					if (homeSun.GetTemperature(0) > 1000)
						break;
					if (homeSun.referenceBody != null)
						homeSun = homeSun.referenceBody;
				} while (homeSun.referenceBody != null);
			}
			return homeSun;
		}
	}
}
