using System;
using System.Collections.Generic;
using UnityEngine;
using KERBALISM;
using KSP.Localization;

namespace KerbalismContracts
{
	public enum EquipmentState
	{
		off, nominal, no_ec, no_bandwidth
	}

	public class EquipmentData : ModuleData<ModuleKsmContractEquipment, EquipmentData>
	{
		public bool isRunning;  // true/false, if process controller is turned on or not
		public string equipmentId;
		
		public EquipmentState state = EquipmentState.off;

		public override void OnFirstInstantiate(ProtoPartModuleSnapshot protoModule, ProtoPartSnapshot protoPart)
		{
			equipmentId = modulePrefab.id;
		}

		public override void OnLoad(ConfigNode node)
		{
			isRunning = Lib.ConfigValue(node, "isRunning", true);
			equipmentId = Lib.ConfigValue(node, "equipmentId", "");
		}

		public override void OnSave(ConfigNode node)
		{
			node.AddValue("isRunning", isRunning);
			node.AddValue("equipmentId", equipmentId);
		}

		public static string StatusInfo(EquipmentState status)
		{
			switch (status)
			{
				case EquipmentState.off: return Lib.Color(Local.Generic_OFF, Lib.Kolor.Yellow);
				case EquipmentState.nominal: return Lib.Color(Local.Generic_ON, Lib.Kolor.Green);
				case EquipmentState.no_ec: return Lib.Color("No EC", Lib.Kolor.Red);
				case EquipmentState.no_bandwidth: return Lib.Color("Low Bandwidth", Lib.Kolor.Red);
				default: return string.Empty;
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
		[KSPField] public string uiGroupName;
		[KSPField] public string animationName = string.Empty;
		[KSPField] public bool animReverse = false;

		public Animator deployAnimator;

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
			deployAnimator = new Animator(part, animationName, animReverse);
			deployAnimator.Still(moduleData.isRunning ? 1f : 0f);

			if (uiGroup != null)
			{
				Events["ToggleEvent"].group = new BasePAWGroup(uiGroupName ?? uiGroup, uiGroup, false);
			}
		}

		[KSPEvent(active = true, guiActive = true, guiActiveEditor = true, requireFullControl = true, guiName = "_")]
		public void ToggleEvent()
		{
			Toggle(moduleData, true);
		}

		public static void Toggle(EquipmentData equipmentData, bool isLoaded)
		{
			equipmentData.isRunning = !equipmentData.isRunning;

			if (isLoaded)
			{
				equipmentData.loadedModule.deployAnimator.Play(!equipmentData.isRunning, false, speed: Lib.IsEditor ? 5f : 1f);

				// refresh VAB/SPH ui
				if (Lib.IsEditor)
				{
					equipmentData.state = equipmentData.isRunning ? EquipmentState.nominal : EquipmentState.off;
					GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
				}
			}
		}

		public virtual void Update()
		{
			Events["ToggleEvent"].guiName = Lib.StatusToggle(Lib.Ellipsis(title, 25), EquipmentData.StatusInfo(moduleData.state));
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

			bool running = ed.state == EquipmentState.nominal;
			if (running)
				vd.ResHandler.ElectricCharge.Consume(prefab.RequiredEC * elapsed_s, EquipmentBroker);

			KerbalismContracts.EquipmentState.Update(v, prefab.id, ed.state);
		}

		private static EquipmentState GetState(Vessel v, VesselData vd, EquipmentData ed, ModuleKsmContractEquipment prefab)
		{
			if (!ed.isRunning)
				return EquipmentState.off;

			if (vd.ResHandler.ElectricCharge.AvailabilityFactor == 0.0)
				return EquipmentState.no_ec;

			else if (API.VesselConnectionRate(v) < prefab.RequiredBandwidth)
				return EquipmentState.no_bandwidth;

			return EquipmentState.nominal;
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
			if(moduleData.isRunning)
			{
				resHandler.ElectricCharge.Consume(RequiredEC, EquipmentBroker);
			}
		}
	}
}
