using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kerbalism.Contracts
{
	public class KerbalismContractEquipment: PartModule, IResourceConsumer
	{
		[KSPField] public string id;                    // id of associated experiment definition
		[KSPField] public string title = string.Empty;  // PAW compatible name
		[KSPField] public double data_rate;             // min. data_rate to operate

		[KSPField] public string resourceName = "ElectricCharge";
		[KSPField] public double resourceRate;

		[KSPField(isPersistant = true)] public bool running = false;

		private KerbalismResourceHandler resourceHandler;

		public override void OnLoad(ConfigNode node)
		{
			if(resourceHandler == null)
				resourceHandler = new KerbalismResourceHandler(this, title);
			
			resourceHandler.AddInputResource(resourceName, resourceRate);

			if (running)
				EquipmentStateTracker.Update(vessel, id, true);
		}

		public virtual void Update()
		{
			Events["ToggleEvent"].guiName = title + ": " + (running ? "running" : "disabled");
		}

		public override void OnAwake()
		{
			if (resourceHandler == null)
				resourceHandler = new KerbalismResourceHandler(this, title);

			resourceHandler.OnAwake();
		}

		private double last_update = 0;
		public void FixedUpdate()
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				bool isOn = running;
				if(running)
				{
					string status = string.Empty;
					var result = resourceHandler.FixedUpdate(ref status);
					isOn &= result >= 0.99;
				}

				if(Time.time - last_update > 0.25)
				{
					EquipmentStateTracker.Update(vessel, id, isOn);
					last_update = Time.time;
				}
			}
		}

		public static void BackgroundUpdate(Vessel vessel, ProtoPartSnapshot proto_part, ProtoPartModuleSnapshot proto_module, PartModule pm, Part p, double elapsed_s)
		{
			KerbalismContractEquipment module = pm as KerbalismContractEquipment;

			bool running = Proto.GetBool(proto_module, "running", false);
			if(running)
			{
				double rate = new KerbalismResourceBroker()
					.Consume(module.resourceName, module.resourceRate)
					.Execute(vessel, module.title, elapsed_s);

				EquipmentStateTracker.Update(vessel, module.id, rate > 0.99);
			}
		}


#if KSP15_16
		[KSPEvent(guiActiveUnfocused = true, guiActive = true, guiActiveEditor = true, guiName = "_", active = true)]
#else
		[KSPEvent(guiActiveUnfocused = true, guiActive = true, guiActiveEditor = true, guiName = "_", active = true, groupName = "Equipment", groupDisplayName = "Equipment")]
#endif
		public void ToggleEvent()
		{
			running = !running;
		}

		public List<PartResourceDefinition> GetConsumedResources()
		{
			return resourceHandler.GetConsumedResources();
		}
	}
}
