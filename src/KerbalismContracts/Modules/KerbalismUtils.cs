using System;
using System.Collections.Generic;
using System.Reflection;

namespace Kerbalism.Contracts
{
	/// <summary>
	/// Kerbalism API interface, using this avoids a compile-time dependency to Kerbalism.
	/// </summary>
	public static class KerbalismAPI
	{
		private static Type API;
		private static MethodInfo API_ProcessResources;
		private static MethodInfo API_ResourceAmounts;
		internal static readonly bool Available;

		static KerbalismAPI()
		{
			foreach (var a in AssemblyLoader.loadedAssemblies)
			{
				// name will be "Kerbalism" for debug builds,
				// and "Kerbalism18" or "Kerbalism16_17" for releases
				// there also is a KerbalismBootLoader, possibly a KerbalismContracts and other mods
				// that start with Kerbalism, so explicitly request equality or test for anything
				// that starts with Kerbalism1
				if (a.name.Equals("Kerbalism") || a.name.StartsWith("Kerbalism1", StringComparison.Ordinal))
				{
					API = a.assembly.GetType("KERBALISM.API");
					Lib.Log("Found KERBALISM API in " + a.name + ": " + API);
					if(API != null)
					{
						API_ResourceAmounts = API.GetMethod("ResourceAmounts");
						API_ProcessResources = API.GetMethod("ProcessResources");
					}
					Available = API != null;

					Lib.Log("Kerbalism available: " + Available);
				}
			}
		}

		public static List<double> ResourceAmounts(Vessel vessel, List<string> resource_names)
		{
			if (API == null) return new List<double>();
			return (List<double>)API_ResourceAmounts.Invoke(null, new object[] { vessel, resource_names });
		}

		public static void ProcessResources(Vessel vessel, List<KeyValuePair<string, double>> resources, string title)
		{
			if (API == null) return;
			API_ProcessResources.Invoke(null, new object[] { vessel, resources, title });
		}
	}

	/// <summary>
	/// Helper function to interact with proto modules of unloaded vessels. You will need this a lot.
	/// </summary>
	public static class Proto
	{
		public static bool GetBool(ProtoPartModuleSnapshot m, string name, bool def_value = false)
		{
			bool v;
			string s = m.moduleValues.GetValue(name);
			return s != null && bool.TryParse(s, out v) ? v : def_value;
		}

		public static uint GetUInt(ProtoPartModuleSnapshot m, string name, uint def_value = 0)
		{
			uint v;
			string s = m.moduleValues.GetValue(name);
			return s != null && uint.TryParse(s, out v) ? v : def_value;
		}

		public static int GetInt(ProtoPartModuleSnapshot m, string name, int def_value = 0)
		{
			int v;
			string s = m.moduleValues.GetValue(name);
			return s != null && int.TryParse(s, out v) ? v : def_value;
		}

		public static float GetFloat(ProtoPartModuleSnapshot m, string name, float def_value = 0.0f)
		{
			// note: we set NaN and infinity values to zero, to cover some weird inter-mod interactions
			float v;
			string s = m.moduleValues.GetValue(name);
			return s != null && float.TryParse(s, out v) && !float.IsNaN(v) && !float.IsInfinity(v) ? v : def_value;
		}

		public static double GetDouble(ProtoPartModuleSnapshot m, string name, double def_value = 0.0)
		{
			// note: we set NaN and infinity values to zero, to cover some weird inter-mod interactions
			double v;
			string s = m.moduleValues.GetValue(name);
			return s != null && double.TryParse(s, out v) && !double.IsNaN(v) && !double.IsInfinity(v) ? v : def_value;
		}

		public static string GetString(ProtoPartModuleSnapshot m, string name, string def_value = "")
		{
			string s = m.moduleValues.GetValue(name);
			return s ?? def_value;
		}

		public static T GetEnum<T>(ProtoPartModuleSnapshot m, string name, T def_value)
		{
			string s = m.moduleValues.GetValue(name);
			if (s != null && Enum.IsDefined(typeof(T), s))
			{
				T forprofiling = (T)Enum.Parse(typeof(T), s);
				UnityEngine.Profiling.Profiler.EndSample();
				return forprofiling;
			}
			return def_value;
		}

		public static T GetEnum<T>(ProtoPartModuleSnapshot m, string name)
		{
			string s = m.moduleValues.GetValue(name);
			if (s != null && Enum.IsDefined(typeof(T), s))
				return (T)Enum.Parse(typeof(T), s);
			return (T)Enum.GetValues(typeof(T)).GetValue(0);
		}

		///<summary>set a value in a proto module</summary>
		public static void Set<T>(ProtoPartModuleSnapshot module, string value_name, T value)
		{
			module.moduleValues.SetValue(value_name, value.ToString(), true);
		}
	}
}
