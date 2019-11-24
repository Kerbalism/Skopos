using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;
using KSP.UI.Screens;
using System.Text;

namespace Kerbalism.Contracts
{
	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class KerbalismContractsMain :	MonoBehaviour
	{
		public static bool initialized = false;
		public static bool KerbalismInitialized = false;

		public void Start()
		{
			Settings.Parse();

			KERBALISM.API.OnRadiationFieldChanged.Add(RadiationFieldTracker.Update);
			KERBALISM.API.OnExperimentStateChanged.Add(ExperimentStateTracker.Update);
		}
	}

	[KSPScenario(ScenarioCreationOptions.AddToAllGames, new[] { GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.FLIGHT, GameScenes.EDITOR })]
	public sealed class KerbalismContracts : ScenarioModule
	{
		public static KerbalismContracts Instance { get; private set; } = null;

		private float lastUpdate = 0;

		//  constructor
		public KerbalismContracts()
		{
			// enable global access
			Instance = this;
		}

		private void OnDestroy()
		{
			Instance = null;
		}

		private void Update()
		{
			if(!KerbalismContractsMain.initialized) {
				StartCoroutine(InitializeVisiblityDeferred());
				KerbalismContractsMain.initialized = true;
			}

			var now = Time.time;
			if (lastUpdate + 5 > now)
				return;

			if(KerbalismContractsMain.KerbalismInitialized)
			{
				lastUpdate = now;

				foreach(var vessel in FlightGlobals.Vessels)
				{
					TestVesselBelts(vessel);
				}
			}
		}

		private void TestVesselBelts(Vessel vessel)
		{
			if (!RelevantVessel(vessel))
				return;

			var bd = BodyData(vessel.mainBody);

			bool skip = bd.has_inner && bd.has_outer && bd.has_pause;
			if (skip) return; // we'll get events for all belts, no custom tracking needed

			// for missing fields, test if vessel would be in a plausible fake field
			bool in_inner = bd.has_inner ? RadiationFieldTracker.InnerBelt(vessel) : InPlausibleBeltLocation(vessel, RadiationFieldType.INNER_BELT);
			bool in_outer = bd.has_outer ? RadiationFieldTracker.OuterBelt(vessel) : InPlausibleBeltLocation(vessel, RadiationFieldType.OUTER_BELT);
			bool in_pause = bd.has_pause ? RadiationFieldTracker.Magnetosphere(vessel) : InPlausibleBeltLocation(vessel, RadiationFieldType.MAGNETOPAUSE);

			RadiationFieldTracker.Update(vessel, in_inner, in_outer, in_pause);
		}

		private bool InPlausibleBeltLocation(Vessel vessel, RadiationFieldType field)
		{
			switch(field)
			{
				case RadiationFieldType.INNER_BELT:
					return vessel.altitude > vessel.mainBody.Radius * 2.8 && vessel.altitude > vessel.mainBody.Radius * 3 && Math.Abs(vessel.longitude) < 30;
				case RadiationFieldType.OUTER_BELT:
					return vessel.altitude > vessel.mainBody.Radius * 4.5 && vessel.altitude > vessel.mainBody.Radius * 5 && Math.Abs(vessel.longitude) < 65; ;
				case RadiationFieldType.MAGNETOPAUSE:
					return vessel.altitude < vessel.mainBody.Radius * 6;
			}
			return false;
		}

		internal static bool RelevantVessel(Vessel vessel)
		{
			if (vessel == null)
				return false;

			switch (vessel.vesselType)
			{
				case VesselType.Unknown:
				case VesselType.EVA:
				case VesselType.Debris:
				case VesselType.Flag:
				case VesselType.SpaceObject:
					return false;
			}
			if (vessel.Landed) return false;

			return true;
		}

		private IEnumerator InitializeVisiblityDeferred()
		{
			yield return new WaitForSeconds(5);
			InitKerbalism();
		}

		public BodyData BodyData(CelestialBody body) {
			if(!bodyData.ContainsKey(body.flightGlobalsIndex)) {
				bodyData.Add(body.flightGlobalsIndex, new BodyData());
			}
			return bodyData[body.flightGlobalsIndex];
		}

		private static void ShowMessage(CelestialBody body, bool wasVisible, bool visible, RadiationFieldType field)
		{
			if (visible && !wasVisible)
			{
				StringBuilder sb = new StringBuilder(256);
				String message = Lib.BuildString("<b>", body.bodyName, ": <color=#8BED8B>", RadiationField.Name(field), "</color> researched</b>");
				sb.Append(message);
				sb.Append("\n\n");

				var bd = Instance.BodyData(body);

				KERBALISM.API.Message(sb.ToString());

				MessageSystem.Message m = new MessageSystem.Message("Radiation Field Researched", sb.ToString(), MessageSystemButton.MessageButtonColor.GREEN, MessageSystemButton.ButtonIcons.ACHIEVE);
				MessageSystem.Instance.AddMessage(m);
			}
		}

		public static void SetInnerBeltVisible(CelestialBody body, bool visible = true)
		{
			Lib.Log("Setting visibility for inner belt of " + body + " to " + visible);
			bool wasVisible = Instance.BodyData(body).inner_visible;
			Instance.BodyData(body).inner_visible = visible;

			if (Settings.enable_radiation_belt_discovery)
				KERBALISM.API.SetInnerBeltVisible(body, visible);

			ShowMessage(body, wasVisible, visible, RadiationFieldType.INNER_BELT);
		}

		public static void SetOuterBeltVisible(CelestialBody body, bool visible = true)
		{
			Lib.Log("Setting visibility for outer belt of " + body + " to " + visible);
			bool wasVisible = Instance.BodyData(body).outer_visible;
			Instance.BodyData(body).outer_visible = visible;

			if (Settings.enable_radiation_belt_discovery)
				KERBALISM.API.SetOuterBeltVisible(body, visible);

			ShowMessage(body, wasVisible, visible, RadiationFieldType.OUTER_BELT);
		}

		public static void SetMagnetopauseVisible(CelestialBody body, bool visible = true)
		{
			Lib.Log("Setting visibility for magnetosphere of " + body + " to " + visible);
			bool wasVisible = Instance.BodyData(body).pause_visible;
			Instance.BodyData(body).pause_visible = visible;

			if (Settings.enable_radiation_belt_discovery)
				KERBALISM.API.SetMagnetopauseVisible(body, visible);

			ShowMessage(body, wasVisible, visible, RadiationFieldType.MAGNETOPAUSE);
		}

		public void InitKerbalism()
		{
			bool isSandboxGame = HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX;

			if(Settings.enable_radiation_belt_discovery)
			{
				foreach (var body in FlightGlobals.Bodies)
				{
					var bd = BodyData(body);

					Lib.Log("Setting magnetic field visibility for " + body.bodyName + " to " + bd.inner_visible + "/" + bd.outer_visible + "/" + bd.pause_visible);
					KERBALISM.API.SetInnerBeltVisible(body, isSandboxGame || bd.inner_visible);
					KERBALISM.API.SetOuterBeltVisible(body, isSandboxGame || bd.outer_visible);
					KERBALISM.API.SetMagnetopauseVisible(body, isSandboxGame || bd.pause_visible);

					bd.has_inner = KERBALISM.API.HasInnerBelt(body);
					bd.has_outer = KERBALISM.API.HasOuterBelt(body);
					bd.has_pause = KERBALISM.API.HasMagnetopause(body);
				}
			}

			KerbalismContractsMain.KerbalismInitialized = true;
		}

		public override void OnLoad(ConfigNode node)
		{
			KerbalismContractsMain.initialized = false;
			KerbalismContractsMain.KerbalismInitialized = false;

			bodyData.Clear();
			
			if (node.HasNode("BodyData"))
			{
				foreach (var body_node in node.GetNode("BodyData").GetNodes())
				{
					if(body_node.name.StartsWith("index_", StringComparison.Ordinal)) {
						int index = Int32.Parse(body_node.name.Substring(6));
						bodyData.Add(index, new BodyData(body_node));
					}
				}
			}
		}

		public override void OnSave(ConfigNode node)
		{
			var bodies_node = node.AddNode("BodyData");
			foreach (var p in bodyData)
			{
				p.Value.Save(bodies_node.AddNode("index_" + p.Key));
			}
		}

		private readonly Dictionary<int, BodyData> bodyData = new Dictionary<int, BodyData>();
	}

	public class BodyData {
		public bool inner_visible = false;
		public bool outer_visible = false;
		public bool pause_visible = false;
		internal bool has_inner = false;
		internal bool has_outer = false;
		internal bool has_pause = false;

		public BodyData() {} // empty default constructor

		public BodyData(ConfigNode node) {
			inner_visible = Lib.ConfigValue(node, "inner_visible", false);	
			outer_visible = Lib.ConfigValue(node, "outer_visible", false);
			pause_visible = Lib.ConfigValue(node, "pause_visible", false);
		}

		public void Save(ConfigNode node) {
			node.AddValue("inner_visible", inner_visible);
			node.AddValue("outer_visible", outer_visible);
			node.AddValue("pause_visible", pause_visible);
		}
	}
}
