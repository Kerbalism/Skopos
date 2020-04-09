using System;
using KERBALISM;
using Contracts;
using KSP.Localization;

namespace KerbalismContracts
{
	public class AltitudeState : SubRequirementState
	{
		internal double distance;
    }

	public class Altitude : SubRequirement
	{
		private int min;
		private int max;
		private double minR;
		private double maxR;
		private string description;

		public Altitude(string type, KerbalismContractRequirement requirement, ConfigNode node) : base(type, requirement)
		{
			min = Lib.ConfigValue(node, "min", 0);
			max = Lib.ConfigValue(node, "max", 0);
			minR = Lib.ConfigValue(node, "minR", 0.0);
			maxR = Lib.ConfigValue(node, "maxR", 0.0);

			description = Lib.ConfigValue<string>(node, "description", null);

			if (min == 0 && max == 0 && minR == 0 && maxR == 0)
				Utils.Log($"Invalid altitude restriction in {requirement.name}: must set a min/minR or max/maxR");
		}

		public override string GetTitle(EvaluationContext context)
		{
			if (!string.IsNullOrEmpty(description))
				return description;

			double minAlt = min;
			double maxAlt = max;

			if (min == 0 && max == 0 && context?.targetBody != null)
			{
				minAlt = minR * context.targetBody.Radius;
				maxAlt = maxR * context.targetBody.Radius;
			}

			if (minAlt == 0 && maxAlt == 0)
				return string.Empty;

			if (minAlt != 0 && maxAlt != 0)
				description = Localizer.Format("Altitude between <<1>> and <<2>>", Lib.HumanReadableDistance(minAlt), Lib.HumanReadableDistance(maxAlt));
			else if (minAlt != 0)
				description = Localizer.Format("Altitude above <<1>>", Lib.HumanReadableDistance(minAlt));
			else if (maxAlt != 0)
				description = Localizer.Format("Altitude below <<1>>", Lib.HumanReadableDistance(maxAlt));

			return description;
		}

		internal override bool CouldBeCandiate(Vessel vessel, EvaluationContext context)
		{
			var orbit = vessel.orbit;
			if (orbit == null)
				return false;

			double minAlt = min != 0 ? min : minR * context.targetBody.Radius;
			double maxAlt = max != 0 ? max : maxR * context.targetBody.Radius;

			if (minAlt != 0 && orbit.ApA < minAlt) // ap too low for min alt
				return false;
			if (maxAlt != 0 && orbit.PeA > maxAlt) // pe too high for max alt
				return false;

			return true;
		}

		internal override SubRequirementState VesselMeetsCondition(Vessel vessel, EvaluationContext context)
		{
			AltitudeState state = new AltitudeState();
			state.distance = context.Altitude(vessel);

			double minAlt = min != 0 ? min : minR * context.targetBody.Radius;
			double maxAlt = max != 0 ? max : maxR * context.targetBody.Radius;

			state.requirementMet = true;

			if (minAlt != 0 && state.distance < minAlt)
				state.requirementMet = false;
			if (maxAlt != 0 && state.distance > maxAlt)
				state.requirementMet = false;

			return state;
		}

		internal override string GetLabel(Vessel vessel, EvaluationContext context, SubRequirementState state)
		{
			AltitudeState altitudeState = (AltitudeState)state;

			if (min != 0 && altitudeState.distance < min)
				return Lib.Color("too low", Lib.Kolor.Red);

			if (max != 0 && altitudeState.distance > max)
				return Lib.Color("too high", Lib.Kolor.Red);

			return Lib.Color("alt OK", Lib.Kolor.Green);
		}
	}
}
