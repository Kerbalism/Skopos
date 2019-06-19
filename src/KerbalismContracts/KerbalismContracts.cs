using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace Kerbalism.Contracts
{
	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class KerbalismContractsMain : MonoBehaviour
	{
		public static bool initialized = false;

		public void Start()
		{			
		}
	}

	[KSPScenario(ScenarioCreationOptions.AddToAllGames, new[] { GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.FLIGHT, GameScenes.EDITOR })]
	public sealed class KerbalismContracts : ScenarioModule
	{
		// permit global access
		public static KerbalismContracts Fetch { get; private set; } = null;

		//  constructor
		public KerbalismContracts()
		{
			// enable global access
			Fetch = this;
		}

		private void OnDestroy()
		{
			Fetch = null;
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
			InitializeVisiblity();
		}

		public BodyData BodyData(CelestialBody body) {
			if(!bodyData.ContainsKey(body.bodyName)) {
				bodyData.Add(body.bodyName, new BodyData());
			}
			return bodyData[body.bodyName];
		}

		public static void SetInnerBeltVisible(CelestialBody body, bool visible = true)
		{
			Fetch.BodyData(body).inner_visible = visible;
			KERBALISM.API.SetInnerBeltVisible(body, visible);
		}

		public static void SetOuterBeltVisible(CelestialBody body, bool visible = true)
		{
			Fetch.BodyData(body).outer_visible = visible;
			KERBALISM.API.SetOuterBeltVisible(body, visible);
		}

		public static void SetMagnetopauseVisible(CelestialBody body, bool visible = true)
		{
			Fetch.BodyData(body).pause_visible = visible;
			KERBALISM.API.SetMagnetopauseVisible(body, visible);
		}

		public void InitializeVisiblity()
		{
			bool isSandboxGame = HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX;
			foreach (var body in FlightGlobals.Bodies)
			{
				var bd = BodyData(body);

				Lib.Log("Setting magnetic field visibility for " + body.bodyName + " to " + isSandboxGame);
				KERBALISM.API.SetInnerBeltVisible(body, isSandboxGame || bd.inner_visible);
				KERBALISM.API.SetOuterBeltVisible(body, isSandboxGame || bd.outer_visible);
				KERBALISM.API.SetMagnetopauseVisible(body, isSandboxGame || bd.pause_visible);
			}
		}

		public override void OnLoad(ConfigNode node)
		{
			bodyData.Clear();
			if (node.HasNode("BodyData"))
			{
				foreach (var body_node in node.GetNode("BodyData").GetNodes())
				{
					bodyData.Add(Lib.From_safe_key(body_node.name), new BodyData(body_node));
				}
			}

			if (FlightGlobals.ready)
				InitializeVisiblity();
		}

		public override void OnSave(ConfigNode node)
		{
			var bodies_node = node.AddNode("BodyData");
			foreach (var p in bodyData)
			{
				p.Value.Save(bodies_node.AddNode(Lib.To_safe_key(p.Key)));
			}
		}

		void FixedUpdate()
		{
			
		}


		private readonly Dictionary<string, BodyData> bodyData = new Dictionary<string, BodyData>();
	}

	public class BodyData {
		public bool inner_visible = false;
		public bool outer_visible = false;
		public bool pause_visible = false;

		public BodyData() {} // empty default constructor

		public BodyData(ConfigNode node) {
			inner_visible = Lib.ConfigValue(node, "inner_visible", false);	
			outer_visible = Lib.ConfigValue(node, "outer_visible", false);	
			pause_visible = Lib.ConfigValue(node, "pause_visible", false);	
		}

		public void Save(ConfigNode node) {
			node.SetValue("inner_visible", inner_visible);
			node.SetValue("outer_visible", outer_visible);
			node.SetValue("pause_visible", pause_visible);
		}
	}

}
