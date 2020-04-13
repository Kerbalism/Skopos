using System;
using KERBALISM;
using Contracts;

namespace KerbalismContracts
{
	public class ExperimentRunningState : SubRequirementState
	{
		internal ExperimentState experimentState;
	}

	public class ExperimentRunning : SubRequirement
	{
		private string experimentId;
		private string description;
		private string shortDescription;

		public ExperimentRunning(string type, KerbalismContractRequirement requirement, ConfigNode node) : base(type, requirement)
		{
			experimentId = Lib.ConfigValue(node, "experimentId", "");
			description = Lib.ConfigValue<string>(node, "description", null);
			shortDescription = Lib.ConfigValue<string>(node, "shortDescription", null);
		}

		public override string GetTitle(EvaluationContext context)
		{
			string title = description ?? shortDescription;
			if (string.IsNullOrEmpty(title))
			{
				var info = ScienceDB.GetExperimentInfo(experimentId);
				title = info?.Title;
			}
			return title ?? experimentId;
		}

		internal override bool CouldBeCandiate(Vessel vessel, EvaluationContext context)
		{
			return ExperimentStateTracker.HasValue(vessel.id, experimentId);
		}

		internal override SubRequirementState VesselMeetsCondition(Vessel vessel, EvaluationContext context)
		{
			ExperimentRunningState state = new ExperimentRunningState();
			state.experimentState = ExperimentStateTracker.GetValue(vessel.id, experimentId);
			state.requirementMet = state.experimentState == ExperimentState.running;
			return state;
		}

		internal override string GetLabel(Vessel vessel, EvaluationContext context, SubRequirementState state)
		{
			ExperimentRunningState runningState = (ExperimentRunningState)state;
			string label = runningState.experimentState == ExperimentState.running ? Lib.Color(Local.Generic_RUNNING, Lib.Kolor.Green) : Lib.Color(Local.Generic_STOPPED, Lib.Kolor.Red);
			if (!string.IsNullOrEmpty(shortDescription))
				label = shortDescription + ": " + label;
			return label;
		}
	}
}
