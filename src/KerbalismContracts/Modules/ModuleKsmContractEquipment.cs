using System;
using System.Collections.Generic;
using UnityEngine;
using KERBALISM;
using KSP.Localization;

namespace KerbalismContracts
{
	public class EquipmentData : ModuleData<ModuleKsmContractEquipment, EquipmentData>
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

	public class ModuleKsmContractEquipment : KsmPartModule<ModuleKsmContractEquipment, EquipmentData>, IModuleInfo, IBackgroundModule, IPlannerModule
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

			KerbalismContracts.EquipmentState.Update(v, prefab.id, running);
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

		public void PlannerUpdate(VesselResHandler resHandler, VesselDataShip vesselData)
		{
			if(running)
			{
				resHandler.ElectricCharge.Consume(RequiredEC, EquipmentBroker);
			}
		}
	}
}
