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

			foreach(ConfigNode node in GameDatabase.Instance.GetConfigNodes("KerbalismContractRequirement"))
			{
				var requirement = new KerbalismContractRequirement(node);
				if (!string.IsNullOrEmpty(requirement.name))
					Requirements.Add(requirement.name, requirement);
			}
		}
	}


} // KERBALISM
