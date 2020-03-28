using System;
using System.Collections.Generic;
using UnityEngine;
using KERBALISM;
/*
namespace KerbalismContracts
{
	public class KerbalismContractEquipment: PartModule, IResourceConsumer, IModuleInfo
	{	
		[KSPField] public string id;                    // id of associated experiment definition
		[KSPField] public string title = string.Empty;  // PAW compatible name
		[KSPField] public double min_bandwidth;

		[KSPField] public string resourceName = "ElectricCharge";
		[KSPField] public double resourceRate;

		[KSPField(isPersistant = true)] public bool running = false;

		private List<PartResourceDefinition> consumedResources;
		private List<ModuleResource> inputResources;

		private enum State
		{
			off, nominal, no_ec, no_bandwidth
		}
		private State state = State.nominal;

		public override void OnLoad(ConfigNode node)
		{
			if (running)
				EquipmentStateTracker.Update(vessel, id, true);

			List<ModuleResource> list = resHandler.inputResources;

			// KSP calls OnLoad twice, so double-check if we added the resource already before we add it a second time.
			foreach (var resource in list)
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

		public virtual void Update()
		{
			string statusStr = title + ": ";
			switch (state)
			{
				case State.off: statusStr += "off"; break;
				case State.nominal: statusStr += "running"; break;
				case State.no_bandwidth: statusStr += "<color=red>needs " + Lib.HumanReadableDataRate(min_bandwidth) + "</color>"; break;
				case State.no_ec: statusStr += "<color=red>no EC</color>"; break;
			}
			Events["ToggleEvent"].guiName = statusStr;
		}

		public override void OnAwake()
		{
			if (consumedResources == null)
				consumedResources = new List<PartResourceDefinition>();
			else
				consumedResources.Clear();

			if (inputResources == null) inputResources = new List<ModuleResource>();

			var inResources = resHandler.inputResources;
			int i = 0;
			for (int count = inResources.Count; i < count; i++)
				consumedResources.Add(PartResourceLibrary.Instance.GetDefinition(inResources[i].name));
		}

		private double last_state_update = 0;

		public void FixedUpdate()
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				if (Time.time - last_state_update > 0.25)
				{
					EquipmentStateTracker.Update(vessel, id, state == State.nominal);
					last_state_update = Time.time;
				}

				state = running ? State.nominal : State.off;

				bool isOn = running;
				if (min_bandwidth > 0 && isOn)
				{
					isOn &= KERBALISM.API.VesselConnectionRate(vessel) > min_bandwidth;
					if (!isOn) state = State.no_bandwidth;
				}
			}
		}

		/// <summary>
		/// We're always going to call you for resource handling.  You tell us what to produce or consume.  Here's how it'll look when your vessel is NOT loaded
		/// </summary>
		/// <param name="v">the vessel (unloaded)</param>
		/// <param name="part_snapshot">proto part snapshot (contains all non-persistant KSPFields)</param>
		/// <param name="module_snapshot">proto part module snapshot (contains all non-persistant KSPFields)</param>
		/// <param name="proto_part_module">proto part module snapshot (contains all non-persistant KSPFields)</param>
		/// <param name="proto_part">proto part snapshot (contains all non-persistant KSPFields)</param>
		/// <param name="availableResources">key-value pair containing all available resources and their currently available amount on the vessel. if the resource is not in there, it's not available</param>
		/// <param name="resourceChangeRequest">key-value pair that contains the resource names and the units per second that you want to produce/consume (produce: positive, consume: negative)</param>
		/// <param name="elapsed_s">how much time elapsed since the last time. note this can be very long, minutes and hours depending on warp speed</param>
		/// <returns>the title to be displayed in the resource tooltip</returns>
		public static string BackgroundUpdate(Vessel v,
			ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot,
			PartModule proto_part_module, Part proto_part,
			Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest,
			double elapsed_s)
		{
			KerbalismContractEquipment module = proto_part_module as KerbalismContractEquipment;

			bool running = Lib.Proto.GetBool(module_snapshot, "running", false);
			if (running)
			{
				bool isOn = true;

				if (module.min_bandwidth > 0)
					isOn &= KERBALISM.API.VesselConnectionRate(v) > module.min_bandwidth;

				double ec = 0;
				availableResources.TryGetValue("ElectricCharge", out ec);
				isOn &= ec > 0;

				if (isOn)
					resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -module.resourceRate));

				EquipmentStateTracker.Update(v, module.id, isOn);
			}

			return module.title;
		}


		/// <summary>
		/// We're also always going to call you when you're loaded.  Since you're loaded, this will be your PartModule, just like you'd expect in KSP. Will only be called while in flight, not in the editor
		/// </summary>
		/// <param name="availableResources">key-value pair containing all available resources and their currently available amount on the vessel. if the resource is not in there, it's not available</param>
		/// <param name="resourceChangeRequest">key-value pair that contains the resource names and the units per second that you want to produce/consume (produce: positive, consume: negative)</param>
		/// <returns></returns>
		public virtual string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (running)
			{
				double ec = 0;
				availableResources.TryGetValue(resourceName, out ec);
				if(ec <= 0)
					state = State.no_ec;

				resourceChangeRequest.Add(new KeyValuePair<string, double>(resourceName, -resourceRate));
			}

			return title;
		}

		/// <summary>
		/// This will be called by Kerbalism in the editor (VAB/SPH), possibly several times after a change to the vessel.
		///
		/// The Kerbalism Planner allows to select different situations and bodies, and will update the simulated environment accordingly. This simulated
		/// environment is passed into this method:
		///
		/// - body: the currently selected body
		/// - environment: a string to double dictionary, currently containing:
		///   - altitude: the altitude of the vessel above the body
		///   - orbital_period: the duration of a circular equitorial orbit at the given altitude
		///   - shadow_period: the duration of that orbit that will be in the planets shadow
		///   - albedo_flux
		///   - solar_flux
		///   - sun_dist: distance to the sun
		///   - temperature
		///   - total_flux
		/// </summary>
		/// <param name="resources">A list of resource names and production/consumption rates.
		/// Production is a positive rate, consumption is negatvie. Add all resources your module is going to produce/consume.</param>
		/// <param name="body">The currently selected body in the Kerbalism planner</param>
		/// <param name="environment">Environment variables guesstimated by Kerbalism, based on the current selection of body and vessel situation. See above.</param>
		/// <returns>The title to display in the tooltip of the planner UI.</returns>
		public string PlannerUpdate(List<KeyValuePair<string, double>> resources, CelestialBody body, Dictionary<string, double> environment)
		{
			if (running)
			{
				// consume the resource if running
				resources.Add(new KeyValuePair<string, double>(resourceName, -resourceRate));
			}
			return title;
		}

		[KSPEvent(guiActiveUnfocused = true, guiActive = true, guiActiveEditor = true, guiName = "_", active = true, groupName = "Equipment", groupDisplayName = "Equipment")]
		public void ToggleEvent()
		{
			running = !running;

			// refresh VAB/SPH ui
			if (HighLogic.LoadedSceneIsEditor)
			{
				state = running ? State.nominal : State.off;
				GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
			}
		}

		public List<PartResourceDefinition> GetConsumedResources()
		{
			return consumedResources;
		}

		public string GetModuleTitle() { return title; }
		public Callback<Rect> GetDrawModulePanelCallback() { return null; }
		public string GetPrimaryField() { return string.Empty; }

		public override string GetInfo()
		{
			Specifics specs = new Specifics();

			var res = PartResourceLibrary.Instance.GetDefinition(resourceName);

			if (resourceRate > 0) specs.Add(res.displayName, Lib.HumanReadableRate(resourceRate));
			if (min_bandwidth > 0) specs.Add("Min. data rate", Lib.HumanReadableDataRate(min_bandwidth));

			return specs.Info();
		}
	}
}
*/
