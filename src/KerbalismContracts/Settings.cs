using System;
using System.Collections.Generic;
using UnityEngine;


namespace Kerbalism.Contracts
{
	public static class Settings
	{
		public static float discovery_base_funds { get; internal set; }
		public static float discovery_base_science { get; internal set; }
		public static float discovery_base_reputation { get; internal set; }

		public static float discovery_inner_funds_bonus { get; internal set; }
		public static float discovery_outer_funds_bonus { get; internal set; }
		public static float discovery_pause_funds_bonus { get; internal set; }

		public static float discovery_inner_science_bonus { get; internal set; }
		public static float discovery_outer_science_bonus { get; internal set; }
		public static float discovery_pause_science_bonus { get; internal set; }

		public static float discovery_inner_reputation_bonus { get; internal set; }
		public static float discovery_outer_reputation_bonus { get; internal set; }
		public static float discovery_pause_reputation_bonus { get; internal set; }

		public static int inner_discovery_crossings { get; internal set; }
		public static int outer_discovery_crossings { get; internal set; }
		public static int pause_discovery_crossings { get; internal set; }


		public static void Parse()
		{
			var cfg = GameDatabase.Instance.GetConfigNode("KerbalismContracts") ?? new ConfigNode();

			discovery_base_funds = Lib.ConfigValue(cfg, "discovery_base_funds", 1000.0f);
			discovery_base_science = Lib.ConfigValue(cfg, "discovery_base_science", 1.0f);
			discovery_base_reputation = Lib.ConfigValue(cfg, "discovery_base_reputation", 1.0f);

			discovery_inner_funds_bonus = Lib.ConfigValue(cfg, "discovery_inner_funds_bonus", 1000.0f);
			discovery_outer_funds_bonus = Lib.ConfigValue(cfg, "discovery_outer_funds_bonus", 2000.0f);
			discovery_pause_funds_bonus = Lib.ConfigValue(cfg, "discovery_pause_funds_bonus", 3000.0f);

			discovery_inner_science_bonus = Lib.ConfigValue(cfg, "discovery_inner_science_bonus", 1.0f);
			discovery_outer_science_bonus = Lib.ConfigValue(cfg, "discovery_outer_science_bonus", 1.0f);
			discovery_pause_science_bonus = Lib.ConfigValue(cfg, "discovery_pause_science_bonus", 1.0f);

			discovery_inner_reputation_bonus = Lib.ConfigValue(cfg, "discovery_inner_reputation_bonus", 1.0f);
			discovery_outer_reputation_bonus = Lib.ConfigValue(cfg, "discovery_outer_reputation_bonus", 1.0f);
			discovery_pause_reputation_bonus = Lib.ConfigValue(cfg, "discovery_pause_reputation_bonus", 1.0f);

			inner_discovery_crossings = Lib.ConfigValue(cfg, "inner_discovery_crossings", 5);
			outer_discovery_crossings = Lib.ConfigValue(cfg, "outer_discovery_crossings", 6);
			pause_discovery_crossings = Lib.ConfigValue(cfg, "pause_discovery_crossings", 4);
		}
	}


} // KERBALISM
