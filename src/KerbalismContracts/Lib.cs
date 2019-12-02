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

		///<summary>used by ModulePrefab function, to support multiple modules of the same type in a part</summary>
		public sealed class Module_prefab_data
		{
			public int index;                         // index of current module of this type
			public List<PartModule> prefabs;          // set of module prefabs of this type
		}

		///<summary>
		/// get module prefab
		///  This function is used to solve the problem of obtaining a specific module prefab,
		/// and support the case where there are multiple modules of the same type in the part.
		/// </summary>
		public static PartModule ModulePrefab(List<PartModule> module_prefabs, string module_name, Dictionary<string, Module_prefab_data> PD)
		{
			// get data related to this module type, or create it
			Module_prefab_data data;
			if (!PD.TryGetValue(module_name, out data))
			{
				data = new Module_prefab_data
				{
					prefabs = module_prefabs.FindAll(k => k.moduleName == module_name)
				};
				PD.Add(module_name, data);
			}

			// return the module prefab, and increment module-specific index
			// note: if something messed up the prefab, or module were added dynamically,
			// then we have no chances of finding the module prefab so we return null
			return data.index < data.prefabs.Count ? data.prefabs[data.index++] : null;
		}

		public static bool HasExperiment(Vessel v, string experiment_id)
		{
			if (!v.loaded) return HasExperiment(v.protoVessel, experiment_id);

			foreach(var part in v.parts)
			{
				foreach(var pm in part.Modules)
				{
					if(pm.isEnabled && pm.moduleName == "Experiment")
					{
						var id = ReflectionValue<string>(pm, "experiment_id");
						if (id == experiment_id) return true;
					}
				}
			}
			return false;
		}

		private static bool HasExperiment(ProtoVessel v, string experiment_id)
		{
			// store data required to support multiple modules of same type in a part
			var PD = new Dictionary<string, Module_prefab_data>();

			// for each part
			foreach (ProtoPartSnapshot p in v.protoPartSnapshots)
			{
				// get part prefab (required for module properties)
				Part part_prefab = PartLoader.getPartInfoByName(p.partName).partPrefab;

				// get all module prefabs
				var module_prefabs = part_prefab.FindModulesImplementing<PartModule>();

				// clear module indexes
				PD.Clear();

				// for each module
				foreach (ProtoPartModuleSnapshot m in p.modules)
				{
					// get the module prefab
					// if the prefab doesn't contain this module, skip it
					PartModule module_prefab = ModulePrefab(module_prefabs, m.moduleName, PD);
					if (!module_prefab) continue;

					// if the module is disabled, skip it
					// note: this must be done after ModulePrefab is called, so that indexes are right
					if (!ProtoGetBool(m, "isEnabled")) continue;

					if(m.moduleName == "Experiment")
					{
						var id = ReflectionValue<string>(module_prefab, "experiment_id");
						if (id == experiment_id) return true;
					}
				}
			}
			return false;
		}

		public static bool ProtoGetBool(ProtoPartModuleSnapshot m, string name, bool def_value = false)
		{
			bool v;
			string s = m.moduleValues.GetValue(name);
			return s != null && bool.TryParse(s, out v) ? v : def_value;
		}

		public static string ProtoGetString(ProtoPartModuleSnapshot m, string name, string def_value = "")
		{
			string s = m.moduleValues.GetValue(name);
			Lib.Log("ProtoGetString " + name + " = " + s);
			return s ?? def_value;
		}

		private static readonly BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

		///<summary>
		/// return a value from a module using reflection
		/// note: useful when the module is from another assembly, unknown at build time
		/// note: useful when the value isn't persistent
		/// note: this function break hard when external API change, by design
		/// </summary>
		public static T ReflectionValue<T>(PartModule m, string value_name)
		{
			return (T)m.GetType().GetField(value_name, flags).GetValue(m);
		}

		// compose a set of strings together, without creating temporary objects
		// note: the objective here is to minimize number of temporary variables for GC
		// note: okay to call recursively, as long as all individual concatenation is atomic
		static StringBuilder sb = new StringBuilder(256);

		public static string BuildString(string a, string b)
		{
			sb.Length = 0;
			sb.Append(a);
			sb.Append(b);
			return sb.ToString();
		}

		public static string BuildString(string a, string b, string c)
		{
			sb.Length = 0;
			sb.Append(a);
			sb.Append(b);
			sb.Append(c);
			return sb.ToString();
		}

		public static string BuildString(string a, string b, string c, string d)
		{
			sb.Length = 0;
			sb.Append(a);
			sb.Append(b);
			sb.Append(c);
			sb.Append(d);
			return sb.ToString();
		}

		public static string BuildString(string a, string b, string c, string d, string e)
		{
			sb.Length = 0;
			sb.Append(a);
			sb.Append(b);
			sb.Append(c);
			sb.Append(d);
			sb.Append(e);
			return sb.ToString();
		}
	}
}
