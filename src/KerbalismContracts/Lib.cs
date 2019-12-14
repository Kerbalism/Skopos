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

		public static bool HasModule(Vessel v, string moduleName, string id_name, string id_value)
		{
			if (!v.loaded) return HasExperiment(v.protoVessel, moduleName, id_name, id_value);

			foreach(var part in v.parts)
			{
				foreach(var pm in part.Modules)
				{
					if(pm.isEnabled && pm.moduleName == moduleName)
					{
						var id = ReflectionValue<string>(pm, id_name);
						if (id == id_value) return true;
					}
				}
			}
			return false;
		}

		private static bool HasExperiment(ProtoVessel v, string moduleName, string id_name, string id_value)
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
					if (!Proto.GetBool(m, "isEnabled")) continue;

					if(m.moduleName == moduleName)
					{
						var id = ReflectionValue<string>(module_prefab, id_name);
						if (id == id_value) return true;
					}
				}
			}
			return false;
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

		///<summary>return vessel position</summary>
		public static Vector3d VesselPosition(Vessel v)
		{
			// the issue
			//   - GetWorldPos3D() return mainBody position for a few ticks after scene changes
			//   - we can detect that, and fall back to evaluating position from the orbit
			//   - orbit is not valid if the vessel is landed, and for a tick on prelaunch/staging/decoupling
			//   - evaluating position from latitude/longitude work in all cases, but is probably the slowest method

			// get vessel position
			Vector3d pos = v.GetWorldPos3D();

			// during scene changes, it will return mainBody position
			if (Vector3d.SqrMagnitude(pos - v.mainBody.position) < 1.0)
			{
				// try to get it from orbit
				pos = v.orbit.getPositionAtUT(Planetarium.GetUniversalTime());

				// if the orbit is invalid (landed, or 1 tick after prelaunch/staging/decoupling)
				if (double.IsNaN(pos.x))
				{
					// get it from lat/long (work even if it isn't landed)
					pos = v.mainBody.GetWorldSurfacePosition(v.latitude, v.longitude, v.altitude);
				}
			}

			// victory
			return pos;
		}

		private static double hoursInDay = -1.0;
		///<summary>return hours in a day</summary>
		public static double HoursInDay
		{
			get
			{
				if (hoursInDay == -1.0)
				{
					if (FlightGlobals.ready || HighLogic.LoadedSceneIsEditor)
					{
						var homeBody = FlightGlobals.GetHomeBody();
						hoursInDay = Math.Round(homeBody.rotationPeriod / 3600, 0);
					}
					else
					{
						return GameSettings.KERBIN_TIME ? 6.0 : 24.0;
					}

				}
				return hoursInDay;
			}
		}

		private static double daysInYear = -1.0;
		///<summary>return year length</summary>
		public static double DaysInYear
		{
			get
			{
				if (daysInYear == -1.0)
				{
					if (FlightGlobals.ready || HighLogic.LoadedSceneIsEditor)
					{
						var homeBody = FlightGlobals.GetHomeBody();
						daysInYear = Math.Floor(homeBody.orbit.period / (HoursInDay * 60.0 * 60.0));
					}
					else
					{
						return GameSettings.KERBIN_TIME ? 426.0 : 365.0;
					}
				}
				return daysInYear;
			}
		}

		///<summary> Pretty-print a range (range is in meters) </summary>
		public static string HumanReadableDistance(double distance)
		{
			if (distance == 0.0) return "none";
			if (distance < 0.0) return Lib.BuildString("-", HumanReadableDistance(-distance));
			if (distance < 1000.0) return BuildString(distance.ToString("F1"), " m");
			distance /= 1000.0;
			if (distance < 1000.0) return BuildString(distance.ToString("F1"), " Km");
			distance /= 1000.0;
			if (distance < 1000.0) return BuildString(distance.ToString("F2"), " Mm");
			distance /= 1000.0;
			if (distance < 1000.0) return BuildString(distance.ToString("F2"), " Gm");
			distance /= 1000.0;
			if (distance < 1000.0) return BuildString(distance.ToString("F3"), " Tm");
			distance /= 1000.0;
			if (distance < 1000.0) return BuildString(distance.ToString("F3"), " Pm");
			distance /= 1000.0;
			return BuildString(distance.ToString("F3"), " Em");
		}

		///<summary> Format data size, the size parameter is in MB (megabytes) </summary>
		public static string HumanReadableDataSize(double size)
		{
			var bitsPerMB = 1024.0 * 1024.0 * 8.0;

			size *= bitsPerMB; //< bits
			if (size < 0.01) return "none";
			if (size <= 32.0) return BuildString(size.ToString("F0"), " b");
			size /= 8; //< to bytes
			if (size < 1024.0) return BuildString(size.ToString("F0"), " B");
			size /= 1024.0;
			if (size < 1024.0) return BuildString(size.ToString("F2"), " kB");
			size /= 1024.0;
			if (size < 1024.0) return BuildString(size.ToString("F2"), " MB");
			size /= 1024.0;
			if (size < 1024.0) return BuildString(size.ToString("F2"), " GB");
			size /= 1024.0;
			return BuildString(size.ToString("F2"), " TB");
		}

		///<summary> Format data rate, the rate parameter is in Mb/s </summary>
		public static string HumanReadableDataRate(double rate)
		{
			// say "none" for rates < 0.5 bits per second
			var bitsPerMB = 1024.0 * 1024.0 * 8.0;
			return rate < 1 / bitsPerMB / 2.0 ? "none" : BuildString(HumanReadableDataSize(rate), "/s");
		}

		///<summary> Pretty-print a resource rate (rate is per second). Return an absolute value if a negative one is provided</summary>
		public static string HumanReadableRate(double rate, string precision = "F3")
		{
			if (rate == 0.0) return "none";
			rate = Math.Abs(rate);
			if (rate >= 0.01) return BuildString(rate.ToString(precision), "/s");
			rate *= 60.0; // per-minute
			if (rate >= 0.01) return BuildString(rate.ToString(precision), "/m");
			rate *= 60.0; // per-hour
			if (rate >= 0.01) return BuildString(rate.ToString(precision), "/h");
			rate *= HoursInDay;  // per-day
			if (rate >= 0.01) return BuildString(rate.ToString(precision), "/d");
			return BuildString((rate * DaysInYear).ToString(precision), "/y");
		}
	}
}
