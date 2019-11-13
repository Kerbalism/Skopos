using System;
using System.Collections.Generic;
using UnityEngine;


namespace Kerbalism.Contracts
{
	public static class Settings
	{
		public static bool enable_radiation_belt_discovery { get; internal set; }
		public static bool enable_sun_observations { get; internal set; }

		public static void Parse()
		{
			var cfg = GameDatabase.Instance.GetConfigNode("KerbalismContracts") ?? new ConfigNode();

			enable_radiation_belt_discovery = Lib.ConfigValue(cfg, "enable_radiation_belt_discovery", false);
			enable_sun_observations = Lib.ConfigValue(cfg, "enable_sun_observations", false);
		}
	}


} // KERBALISM
