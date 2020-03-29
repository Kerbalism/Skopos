using System;
using System.Collections.Generic;
using UnityEngine;
using KERBALISM;
using KSP.Localization;

namespace KerbalismContracts
{
	public class EquipmentData: ModuleData<ModuleKsmContractEquipment, EquipmentData>
	{
		public bool isRunning;  // true/false, if process controller is turned on or not
		public bool isBroken;   // true if process controller is broken
		public string equipmentId;

		public enum State
		{
			off, nominal, no_ec, no_bandwidth
		}
		public State state = State.nominal;

		public override void OnFirstInstantiate(ProtoPartModuleSnapshot protoModule, ProtoPartSnapshot protoPart)
		{
			isRunning = modulePrefab.running;
			isBroken = modulePrefab.broken;
			equipmentId = modulePrefab.id;
		}

		public override void OnLoad(ConfigNode node)
		{
			isRunning = Lib.ConfigValue(node, "isRunning", true);
			isBroken = Lib.ConfigValue(node, "isBroken", false);
			equipmentId = Lib.ConfigValue(node, "equipmentId", "");
		}

		public override void OnSave(ConfigNode node)
		{
			node.AddValue("isRunning", isRunning);
			node.AddValue("isBroken", isBroken);
			node.AddValue("equipmentId", equipmentId);
		}

		public override void OnVesselDataUpdate(VesselDataBase vd)
		{
			if (moduleIsEnabled && !isBroken)
			{
				Utils.LogDebug($"{equipmentId} enabled {moduleIsEnabled} running {isRunning} broken {isBroken}");
			}
		}
	}

	public class ModuleKsmContractEquipment: KsmPartModule<ModuleKsmContractEquipment, EquipmentData>, IModuleInfo, IBackgroundModule
	{	
		[KSPField] public string id;
		[KSPField] public string title = string.Empty;
		[KSPField] public double RequiredBandwidth;
		[KSPField] public double RequiredEC;
		[KSPField] public string uiGroup;

		[KSPField]
		[UI_Toggle(scene = UI_Scene.All, affectSymCounterparts = UI_Scene.None)]
		public bool running;

		// internal state
		internal BaseField runningField;
		internal bool broken;

		static KERBALISM.ResourceBroker EquipmentBroker = KERBALISM.ResourceBroker.GetOrCreate("ksmEquipment", KERBALISM.ResourceBroker.BrokerCategory.VesselSystem, "Equipment");

		// parsing configs at prefab compilation
		public override void OnLoad(ConfigNode node)
		{
			if (HighLogic.LoadedScene == GameScenes.LOADING)
			{
				EquipmentData prefabData = new EquipmentData();
				prefabData.SetPartModuleReferences(this, this);
				prefabData.OnFirstInstantiate(null, null);
				moduleData = prefabData;
			}
		}

		public override void OnStart(StartState state)
		{
			runningField = Fields["running"];
			runningField.guiName = title;
			runningField.guiActive = runningField.guiActiveEditor = true;
			runningField.OnValueModified += (field) => Toggle(moduleData, true);

			((UI_Toggle)runningField.uiControlFlight).enabledText = Lib.Color(Local.Generic_ENABLED.ToLower(), Lib.Kolor.Green);
			((UI_Toggle)runningField.uiControlFlight).disabledText = Lib.Color(Local.Generic_DISABLED.ToLower(), Lib.Kolor.Yellow);
			((UI_Toggle)runningField.uiControlEditor).enabledText = Lib.Color(Local.Generic_ENABLED.ToLower(), Lib.Kolor.Green);
			((UI_Toggle)runningField.uiControlEditor).disabledText = Lib.Color(Local.Generic_DISABLED.ToLower(), Lib.Kolor.Yellow);

			if (uiGroup != null)
				runningField.group = new BasePAWGroup(uiGroup, uiGroup, false);
		}

		public static void Toggle(EquipmentData equipmentData, bool isLoaded)
		{
			if (equipmentData.isBroken)
				return;

			equipmentData.isRunning = !equipmentData.isRunning;

			if (isLoaded)
			{
				equipmentData.loadedModule.running = equipmentData.isRunning;

				// refresh VAB/SPH ui
				if (Lib.IsEditor)
					GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
			}
		}

		public virtual void FixedUpdate()
		{
			// basic sanity checks
			if (Lib.IsEditor)
				return;
			if (!vessel.TryGetVesselData(out VesselData vd))
				return;

			RunningUpdate(vessel, vd, moduleData, this, Kerbalism.elapsed_s);
		}

		public void BackgroundUpdate(VesselData vd, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule, double elapsed_s)
		{
			if (!ModuleData.TryGetModuleData<ModuleKsmContractEquipment, EquipmentData>(protoModule, out EquipmentData experimentData))
				return;

			RunningUpdate(vd.Vessel, vd, experimentData, this, elapsed_s);
		}

		private static void RunningUpdate(Vessel v, VesselData vd, EquipmentData ed, ModuleKsmContractEquipment prefab, double elapsed_s)
		{
			ed.state = GetState(v, vd, ed, prefab);

			bool running = ed.state == EquipmentData.State.nominal;
			if (running)
				vd.ResHandler.ElectricCharge.Consume(prefab.RequiredEC * elapsed_s, EquipmentBroker);

			KerbalismContracts.EquipmentStateTracker.Update(v, prefab.id, running);
		}

		private static EquipmentData.State GetState(Vessel v, VesselData vd, EquipmentData ed, ModuleKsmContractEquipment prefab)
		{
			if (!ed.isRunning)
				return EquipmentData.State.off;

			if (vd.ResHandler.ElectricCharge.AvailabilityFactor == 0.0)
				return EquipmentData.State.no_ec;

			else if (API.VesselConnectionRate(v) < prefab.RequiredBandwidth)
				return EquipmentData.State.no_bandwidth;

			return EquipmentData.State.nominal;
		}

		/*
		public override void OnLoad(ConfigNode node)
		{
			EquipmentStateTracker.Update(vessel, id, running);
		}

		public virtual void Update()
		{
			string statusStr = title + ": ";
			switch (state)
			{
				case State.off: statusStr += Lib.Color(Local.Generic_OFF, Lib.Kolor.Yellow); break;
				case State.nominal: statusStr += Lib.Color(Local.Generic_RUNNING, Lib.Kolor.Green); break;
				case State.no_bandwidth: statusStr += Lib.Color(Localizer.Format("needs <<1>>", Lib.HumanReadableDataRate(RequiredBandwidth)), Lib.Kolor.Red); break;
				case State.no_ec: statusStr += Lib.Color(Local.ElectricalCharge, Lib.Kolor.Red); break;
			}
			Events["ToggleEvent"].guiName = statusStr;
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
				if (RequiredBandwidth > 0 && isOn)
				{
					isOn &= API.VesselConnectionRate(vessel) > RequiredBandwidth;
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
		*/

		public string GetModuleTitle() { return title; }
		public Callback<Rect> GetDrawModulePanelCallback() { return null; }
		public string GetPrimaryField() { return string.Empty; }

		public override string GetInfo()
		{
			Specifics specs = new Specifics();

			var res = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");
			if (RequiredEC > 0) specs.Add(res.displayName, Lib.HumanReadableRate(RequiredEC));
			if (RequiredBandwidth > 0) specs.Add("Min. data rate", Lib.HumanReadableDataRate(RequiredBandwidth));

			return specs.Info();
		}
	}
}
