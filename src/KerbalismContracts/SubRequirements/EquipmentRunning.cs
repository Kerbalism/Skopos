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
		private string equipmentId;
		private string description;
		private string shortDescription;

		public EquipmentRunning(string type, KerbalismContractRequirement requirement, ConfigNode node) : base(type, requirement)
		{
			equipmentId = Lib.ConfigValue(node, "equipmentId", "");
			description = Lib.ConfigValue<string>(node, "description", null);
			shortDescription = Lib.ConfigValue<string>(node, "shortDescription", null);
		}

		public override string GetTitle(EvaluationContext context)
		{
			return description ?? shortDescription ?? equipmentId;
		}

		internal override bool CouldBeCandiate(Vessel vessel, EvaluationContext context)
		{
			return KerbalismContracts.EquipmentState.HasValue(vessel, equipmentId);
		}

		internal override SubRequirementState VesselMeetsCondition(Vessel vessel, EvaluationContext context)
		{
			EquipmentRunningState state = new EquipmentRunningState();
			state.equipmentState = KerbalismContracts.EquipmentState.GetValue(vessel, equipmentId);
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
