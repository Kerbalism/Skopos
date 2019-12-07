using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kerbalism.Contracts
{
	public class KerbalismResourceHandler
	{
		public PartModule partModule { get; private set; }
		public List<PartResourceDefinition> consumedResources { get; private set; }
		public List<ModuleResource> inputResources { get; private set; }

		private bool KerbalismPresent = true;
		private string title;
		private double lastFixedUpdate;

		internal KerbalismResourceHandler(PartModule partModule, string title = null)
		{
			this.partModule = partModule;
			this.title = title;

			if (string.IsNullOrEmpty(title))
				this.title = partModule.moduleName;
		}

		/// <summary>
		/// Add an input resource to be consumed by the part module.
		/// </summary>
		/// <param name="resourceName"></param>
		/// <param name="resourceRate"></param>
		internal void AddInputResource(string resourceName, double resourceRate)
		{
			if (KerbalismPresent && inputResources == null) inputResources = new List<ModuleResource>();
			List<ModuleResource> list = KerbalismPresent ? inputResources : partModule.resHandler.inputResources;

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

		internal void OnAwake()
		{
			if (consumedResources == null)
				consumedResources = new List<PartResourceDefinition>();
			else
				consumedResources.Clear();

			if (KerbalismPresent)
				return;

			if (inputResources == null) inputResources = new List<ModuleResource>();

			var inResources = partModule.resHandler.inputResources;

			int i = 0;
			for (int count = inResources.Count; i < count; i++)
				consumedResources.Add(PartResourceLibrary.Instance.GetDefinition(inResources[i].name));
		}

		internal double FixedUpdate(ref string status)
		{
			if(!KerbalismPresent)
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

		internal List<PartResourceDefinition> GetConsumedResources()
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
			resources.Add(new RI(resource_name, -rate));
			return this;
		}

		/// <summary>
		/// Register a resource for consumption
		/// </summary>
		public KerbalismResourceBroker Consume(string resource_name, double rate)
		{
			resources.Add(new RI(resource_name, rate));
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

			// 1st pass: calculate max. available rate
			foreach (var r in resources)
			{
				if (r.rate <= 0) continue;
				double requestedAmount = r.rate * elapsed_s;
				double available = KerbalismAPI.ResourceAvailable(vessel, r.name);

				available = Math.Min(requestedAmount, available);
				rate = Math.Min(rate, available / requestedAmount);
			}

			// 2nd pass: consume resources
			foreach (var r in resources)
			{
				double requestedAmount = r.rate * elapsed_s * rate;
				if (requestedAmount > 0)
					KerbalismAPI.ConsumeResource(vessel, r.name, requestedAmount, title);
				else
					KerbalismAPI.ProduceResource(vessel, r.name, requestedAmount, title);
			}

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
		public static double ResourceAvailable(Vessel vessel, string resource_name)
		{
			return KERBALISM.API.ResourceAmount(vessel, resource_name);
		}

		public static void ConsumeResource(Vessel vessel, string resource_name, double quantity, string title)
		{
			KERBALISM.API.ConsumeResource(vessel, resource_name, Math.Abs(quantity), title);
		}

		public static void ProduceResource(Vessel vessel, string resource_name, double quantity, string title)
		{
			KERBALISM.API.ProduceResource(vessel, resource_name, Math.Abs(quantity), title);
		}
	}

	public static class Proto
	{
		public static bool GetBool(ProtoPartModuleSnapshot m, string name, bool def_value = false)
		{
			bool v;
			string s = m.moduleValues.GetValue(name);
			return s != null && bool.TryParse(s, out v) ? v : def_value;
		}

		public static string GetString(ProtoPartModuleSnapshot m, string name, string def_value = "")
		{
			string s = m.moduleValues.GetValue(name);
			return s ?? def_value;
		}

		public static double GetDouble(ProtoPartModuleSnapshot m, string name, double def_value = 0.0)
		{
			string s = m.moduleValues.GetValue(name);
			return s == null ? def_value : double.Parse(s);
		}
	}
}
