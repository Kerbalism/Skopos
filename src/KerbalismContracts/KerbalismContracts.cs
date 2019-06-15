using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP.UI.Screens;


namespace Kerbalism.Contracts
{


	/// <summary> Main class, instantiated during Main menu scene.</summary>
	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class KerbalismContractsMain : MonoBehaviour
	{
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

		public override void OnLoad(ConfigNode node)
		{
			bool isSandboxGame = HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX;
			foreach (var body in FlightGlobals.Bodies)
			{
				Lib.Log("Setting magnetic field visibility for " + body.bodyName + " to " + isSandboxGame);
				KERBALISM.API.SetInnerBeltVisible(body, isSandboxGame);
				KERBALISM.API.SetOuterBeltVisible(body, isSandboxGame);
				KERBALISM.API.SetMagnetopauseVisible(body, isSandboxGame);
			}
		}

		public override void OnSave(ConfigNode node)
		{
			
		}

		void FixedUpdate()
		{
			
		}
	}

}
