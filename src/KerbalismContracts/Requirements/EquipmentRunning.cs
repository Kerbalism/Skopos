using System;
using KERBALISM;
using Contracts;

namespace KerbalismContracts
{
	public class EquipmentRunning : SubRequirement
	{
		private string equipment;
		private string description;
		private string shortDescription;

		public EquipmentRunning(string type, KerbalismContractRequirement requirement, ConfigNode node) : base(type, requirement)
		{
			equipment = Lib.ConfigValue(node, "equipment", "");
			description = Lib.ConfigValue<string>(node, "description", null);
			shortDescription = Lib.ConfigValue<string>(node, "shortDescription", null);
		}

		public override string GetTitle(RequirementContext context)
		{
			return description ?? shortDescription ?? equipment;
		}

		internal override bool CouldBeCandiate(Vessel vessel, RequirementContext context)
		{
			bool result = KerbalismContracts.EquipmentState.HasValue(vessel, equipment);
			Utils.LogDebug($"{vessel.name} is candidate for {equipment}: {result}");
			return result;
		}

		internal override bool VesselMeetsCondition(Vessel vessel, RequirementContext context, out string label)
		{
			var state = KerbalismContracts.EquipmentState.GetValue(vessel, equipment);

			label = EquipmentData.StatusInfo(state);
			if (!string.IsNullOrEmpty(shortDescription))
				label = shortDescription + ": " + label;

			return state == EquipmentState.nominal;
		}
	}
}
