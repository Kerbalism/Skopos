using System;
using KERBALISM;
using Contracts;
using KSP.Localization;

namespace KerbalismContracts
{
	public class Powered : SubRequirement
	{
		private string description;

		public Powered(string type, KerbalismContractRequirement requirement, ConfigNode node) : base(type, requirement)
		{
			description = Lib.ConfigValue<string>(node, "description", null);
		}

		public override string GetTitle(RequirementContext context)
		{
			if (!string.IsNullOrEmpty(description))
				return description;

			return "Has electricity";
		}

		internal override bool CouldBeCandiate(Vessel vessel, RequirementContext context)
		{
			return API.ResourceAmount(vessel, "ElectricCharge") > 0;
		}
	}
}
