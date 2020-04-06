using System;
using KERBALISM;
using Contracts;
using KSP.Localization;

namespace KerbalismContracts
{
	public class CommLinkState : SubRequirementState
	{
		internal double bw;
	}

	public class CommLink : SubRequirement
	{
		private string description;
		private double min_bandwidth;

		public CommLink(string type, KerbalismContractRequirement requirement, ConfigNode node) : base(type, requirement)
		{
			description = Lib.ConfigValue<string>(node, "description", null);
			min_bandwidth = Lib.ConfigValue(node, "min_bandwidth", 0.0);
		}

		public override string GetTitle(EvaluationContext context)
		{
			if (!string.IsNullOrEmpty(description))
				return description;

			description = min_bandwidth > 0
				? Localizer.Format("Min. Bandwidth <<1>>", Lib.HumanReadableDataRate(min_bandwidth))
				: "Online";

			return description;
		}

		internal override bool CouldBeCandiate(Vessel vessel, EvaluationContext context)
		{
			return API.VesselConnectionLinked(vessel);
		}

		internal override SubRequirementState VesselMeetsCondition(Vessel vessel, EvaluationContext context)
		{
			CommLinkState state = new CommLinkState();

			if (min_bandwidth > 0)
			{
				state.bw = API.VesselConnectionRate(vessel);
				state.requirementMet = state.bw >= min_bandwidth;
			}
			else
			{
				state.requirementMet = API.VesselConnectionLinked(vessel);
			}

			return state;
		}

		internal override string GetLabel(Vessel vessel, EvaluationContext context, SubRequirementState state)
		{
			CommLinkState commLinkState = (CommLinkState)state;
			if (min_bandwidth > 0)
			{
				var color = Lib.Kolor.Orange;
				if (commLinkState.bw > min_bandwidth * 1.2)
					color = Lib.Kolor.Green;
				if (commLinkState.bw < min_bandwidth)
					color = Lib.Kolor.Red;

				return Lib.Color(Lib.HumanReadableDataRate(commLinkState.bw), color);
			}

			if(state.requirementMet)
				return Lib.Color("online", Lib.Kolor.Green);
			return Lib.Color("offline", Lib.Kolor.Red);
		}
	}
}
