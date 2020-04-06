using System;
using KERBALISM;
using Contracts;

namespace KerbalismContracts
{
	public class EquipmentRunningState : SubRequirementState
	{
		internal EquipmentState equipmentState;
	}

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

		public override string GetTitle(EvaluationContext context)
		{
			return description ?? shortDescription ?? equipment;
		}

		internal override bool CouldBeCandiate(Vessel vessel, EvaluationContext context)
		{
			return KerbalismContracts.EquipmentState.HasValue(vessel, equipment);
		}

		internal override SubRequirementState VesselMeetsCondition(Vessel vessel, EvaluationContext context)
		{
			EquipmentRunningState state = new EquipmentRunningState();
			state.equipmentState = KerbalismContracts.EquipmentState.GetValue(vessel, equipment);
			state.requirementMet = state.equipmentState == EquipmentState.nominal;
			return state;
		}

		internal override string GetLabel(Vessel vessel, EvaluationContext context, SubRequirementState state)
		{
			EquipmentRunningState equipmentRunningState = (EquipmentRunningState)state;
			string label = EquipmentData.StatusInfo(equipmentRunningState.equipmentState);
			if (!string.IsNullOrEmpty(shortDescription))
				label = shortDescription + ": " + label;
			return label;
		}
	}
}
