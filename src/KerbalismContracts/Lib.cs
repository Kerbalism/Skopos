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

		public static string To_safe_key(string key) { return key.Replace(" ", "___"); }
		public static string From_safe_key(string key) { return key.Replace("___", " "); }

		// get a value from config
		public static T ConfigValue<T>(ConfigNode cfg, string key, T def_value)
		{
			try
			{
				return cfg.HasValue(key) ? (T)Convert.ChangeType(cfg.GetValue(key), typeof(T)) : def_value;
			}
			catch (Exception e)
			{
				Lib.Log("error while trying to parse '" + key + "' from " + cfg.name + " (" + e.Message + ")");
				return def_value;
			}
		}

		// get an enum from config
		public static T ConfigEnum<T>(ConfigNode cfg, string key, T def_value)
		{
			try
			{
				return cfg.HasValue(key) ? (T)Enum.Parse(typeof(T), cfg.GetValue(key)) : def_value;
			}
			catch (Exception e)
			{
				Lib.Log("invalid enum in '" + key + "' from " + cfg.name + " (" + e.Message + ")");
				return def_value;
			}
		}


		public static bool HasRadiationSensor(ProtoVessel v)
		{
			foreach (var p in v.protoPartSnapshots)
			{
				foreach(var pm in p.modules)
				{
					if(pm.moduleName == "Sensor")
					{
						var type = ConfigValue(pm.moduleValues, "type", string.Empty);
						if (type == "radiation") return true;
					}
				}
			}

			return false;
		}
	}
}
