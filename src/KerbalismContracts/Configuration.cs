using System;
using System.Collections.Generic;
using UnityEngine;
using KERBALISM;
using System.Collections;

namespace KerbalismContracts
{

	public static class Configuration
	{
		private static readonly Dictionary<string, KerbalismContractRequirement> Requirements = new Dictionary<string, KerbalismContractRequirement>();

		public static bool HideRadiationBelts { get; private set; }
		public static String SunObservationEquipment { get; private set; }
		public static double MinSunObservationAngle { get; private set; }

		public static KerbalismContractRequirement Requirement(string id)
		{
			if (!Requirements.ContainsKey(id))
			{
				Utils.LogStack($"Requirement {id} does not exist", LogLevel.Error);
				return null;
			}
			return Requirements[id];
		}

		public static void Load()
		{
			var cfg = GameDatabase.Instance.GetConfigNode("KerbalismContracts") ?? new ConfigNode();

			HideRadiationBelts = Lib.ConfigValue(cfg, "hideRadiationBelts", true);
			SunObservationEquipment = Lib.ConfigValue(cfg, "sunObservationEquipment", "uvcs");
			MinSunObservationAngle = Lib.ConfigValue(cfg, "minSunObservationAngle", 2.0);

			foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("KerbalismContractRequirement"))
			{
				var requirement = new KerbalismContractRequirement(node);
				if (!string.IsNullOrEmpty(requirement.name))
					Requirements.Add(requirement.name, requirement);
			}
		}
	}


} // KERBALISM
