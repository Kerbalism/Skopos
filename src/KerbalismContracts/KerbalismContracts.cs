using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;

namespace Kerbalism.Contracts
{
	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class KerbalismContractsMain : MonoBehaviour
	{
		public static bool initialized = false;

		public void Start()
		{
			KERBALISM.API.OnRadiationFieldChanged.Add(RadiationFieldTracker.Update);
		}
	}


	[KSPScenario(ScenarioCreationOptions.AddToAllGames, new[] { GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.FLIGHT, GameScenes.EDITOR })]
	public sealed class KerbalismContracts : ScenarioModule
	{
		// permit global access
		public static KerbalismContracts Instance { get; private set; } = null;

		//public static float SunObservationL1 { get; private set; } = 0.05f;
		//public static float SunObservationL2 { get; private set; } = 0.20f;
		//public static float SunObservationL3 { get; private set; } = 0.35f;

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

		public static void SetInnerBeltVisible(CelestialBody body, bool visible = true)
		{
			Lib.Log("Setting visibility for inner belt of " + body + " to " + visible);
			Instance.BodyData(body).inner_visible = visible;
			KERBALISM.API.SetInnerBeltVisible(body, visible);
		}

		public static void SetOuterBeltVisible(CelestialBody body, bool visible = true)
		{
			Lib.Log("Setting visibility for outer belt of " + body + " to " + visible);
			Instance.BodyData(body).outer_visible = visible;
			KERBALISM.API.SetOuterBeltVisible(body, visible);
		}

		public static void SetMagnetopauseVisible(CelestialBody body, bool visible = true)
		{
			Lib.Log("Setting visibility for magnetosphere of " + body + " to " + visible);
			Instance.BodyData(body).pause_visible = visible;
			KERBALISM.API.SetMagnetopauseVisible(body, visible);
		}

		public void InitKerbalism()
		{
			bool isSandboxGame = HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX;
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

			UpdateStormObservationQuality();
		}

		public void UpdateStormObservationQuality()
		{
			/*
			float q = SunObservationL3;

			// basic observation quality from the tracking station
			if(ScenarioUpgradeableFacilities.Instance != null && HighLogic.CurrentGame.Mode != Game.Modes.SANDBOX)
			{
				q = SunObservationL1;

				var dsnLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.TrackingStation);
				if (dsnLevel > 0.5f) q = SunObservationL3;
				else if (dsnLevel > 0.1f) q = SunObservationL2;
			}


			// add sun observing satellites values here
			// TODO

			Lib.Log("Setting sun observation quality to " + q);

			KERBALISM.API.SetStormObservationQuality(q);
			*/
		}

		public override void OnLoad(ConfigNode node)
		{
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
