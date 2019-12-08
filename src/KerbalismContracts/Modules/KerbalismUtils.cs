using System;
using System.Collections.Generic;
using System.Reflection;

namespace Kerbalism.Contracts
{
	/// <summary>
	/// Wrapper class that either handles resources using the stock KSP resource handler,
	/// or uses the Kerbalsim resource system. Detects if Kerbalism is loaded and will
	/// call the Kerbalism API if it's there.
	///
	/// For the stock resource consumption to work, your part module must implement IResourceConsumer
	/// (return GetConsumedResources from this class). It also must call this FixedUpdate from its own FixedUpdate.
	/// </summary>
	public class KerbalismResourceHandler
	{
		public PartModule partModule { get; private set; }
		public List<PartResourceDefinition> consumedResources { get; private set; }
		public List<ModuleResource> inputResources { get; private set; }

		private string title;
		private double lastFixedUpdate;

		/// <summary>
		/// Instantiate this in your part module. title will appear in a tool tip window
		/// when you hover over the Kerbalism resource consumption displays.
		/// </summary>
		/// <param name="partModule"></param>
		/// <param name="title"></param>
		public KerbalismResourceHandler(PartModule partModule, string title = null)
		{
			this.partModule = partModule;
			this.title = title;

			if (string.IsNullOrEmpty(title))
				this.title = partModule.moduleName;
		}

		/// <summary>
		/// Call this from your part modules OnLoad.
		/// Add an input resource to be consumed by the part module.
		/// Safe to call multiple times with the same values.
		/// </summary>
		/// <param name="resourceName"></param>
		/// <param name="resourceRate"></param>
		internal void AddInputResource(string resourceName, double resourceRate)
		{
			if (KerbalismAPI.Available && inputResources == null) inputResources = new List<ModuleResource>();
			List<ModuleResource> list = KerbalismAPI.Available ? inputResources : partModule.resHandler.inputResources;

			// KSP calls OnLoad twice, so double-check if we added the resource already before we add it a second time.
			foreach(var resource in list)
			{
				if (resource.name == resourceName)
				{
					resource.rate = resourceRate;
					return;
				}
			}

			ModuleResource moduleResource = new ModuleResource();
			moduleResource.name = resourceName;
			moduleResource.title = KSPUtil.PrintModuleName(resourceName);
			moduleResource.id = resourceName.GetHashCode();
			moduleResource.rate = resourceRate;

			list.Add(moduleResource);
		}

		/// <summary>
		/// Call this from your part modules OnAwake.
		/// </summary>
		public void OnAwake()
		{
			if (consumedResources == null)
				consumedResources = new List<PartResourceDefinition>();
			else
				consumedResources.Clear();

			if (KerbalismAPI.Available)
				return;

			if (inputResources == null) inputResources = new List<ModuleResource>();

			var inResources = partModule.resHandler.inputResources;

			int i = 0;
			for (int count = inResources.Count; i < count; i++)
				consumedResources.Add(PartResourceLibrary.Instance.GetDefinition(inResources[i].name));
		}

		/// <summary>
		/// Call this from your PartModules static FixedUpdate method.
		/// </summary>
		/// <param name="status"></param>
		/// <returns></returns>
		public double FixedUpdate(ref string status)
		{
			if(!KerbalismAPI.Available)
				return partModule.resHandler.UpdateModuleResourceInputs(ref status, 1.0, 0.99, false, false, true);

			if(lastFixedUpdate == 0)
			{
				lastFixedUpdate = Planetarium.GetUniversalTime();
				return 1.0;
			}

			double elapsed_s = Planetarium.GetUniversalTime() - lastFixedUpdate;

			KerbalismResourceBroker broker = new KerbalismResourceBroker();
			foreach(var inputResource in inputResources)
				broker.Consume(inputResource.name, inputResource.rate);

			double rate = broker.Execute(partModule.vessel, title, elapsed_s);

			lastFixedUpdate = Planetarium.GetUniversalTime();
			return rate;
		}

		/// <summary>
		/// Call this from your PartModules GetConsumedResources
		/// </summary>
		/// <returns></returns>
		public List<PartResourceDefinition> GetConsumedResources()
		{
			return consumedResources;
		}
	}

	/// <summary>
	/// Interface class to the kerbalism resource system.
	/// Use this to produce / consume resources from your part module.
	/// </summary>
	public class KerbalismResourceBroker
	{
		/// <summary>
		/// Register a resource for consumption
		/// </summary>
		public KerbalismResourceBroker Produce(string resource_name, double rate)
		{
			resources.Add(new RI(resource_name, Math.Abs(rate)));
			return this;
		}

		/// <summary>
		/// Register a resource for consumption
		/// </summary>
		public KerbalismResourceBroker Consume(string resource_name, double rate)
		{
			resources.Add(new RI(resource_name, -Math.Abs(rate)));
			return this;
		}

		/// <summary>
		/// Produce / Consume all resources that were previously registered.
		/// Returns the rate (0..1) of the execution. A return value less than
		/// 1 means that there was a lack of input resources.
		/// </summary>
		public double Execute(Vessel vessel, string title, double elapsed_s)
		{
			double rate = 1.0;

			List<string> resource_names = new List<string>(resources.Count);
			foreach (var r in resources)
				resource_names.Add(r.name);

			List<double> resource_amounts = KerbalismAPI.ResourceAmounts(vessel, resource_names);

			// 1st pass: calculate max. available rate
			for(int i = 0; i < resources.Count; i++)
			{
				RI r = resources[i];

				// productions have a positive rate. here we look at consumption only
				if (r.rate > 0)
					continue;

				double requestedAmount = r.rate * elapsed_s;
				double available = resource_amounts[i];

				available = Math.Min(requestedAmount, available);
				rate = Math.Min(rate, available / requestedAmount);
				i++;
			}

			// 2nd pass: consume resources
			List<KeyValuePair<string, double>> resourceUpdates = new List<KeyValuePair<string, double>>(resources.Count);
			for (int i = 0; i < resources.Count; i++)
			{
				RI r = resources[i];
				double requestedAmount = r.rate * elapsed_s * rate;
				resourceUpdates.Add(new KeyValuePair<string, double>(r.name, requestedAmount));
			}
			KerbalismAPI.ProcessResources(vessel, resourceUpdates, title);

			return rate;
		}

		private class RI
		{
			internal string name;
			internal double rate;

			internal RI(string name, double rate)
			{
				this.name = name;
				this.rate = rate;
			}
		}

		private List<RI> resources = new List<RI>();
	}

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
