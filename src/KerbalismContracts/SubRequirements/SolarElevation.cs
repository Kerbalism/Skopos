using System;
using KERBALISM;
using Contracts;
using KSP.Localization;

namespace KerbalismContracts
{
	internal class SolarElevationState : SubRequirementState
	{
		internal double solarElevation;
    }

	public class SolarElevation : SubRequirement
	{
		private double min;
		private double max;
		private string description;

		public SolarElevation(string type, KerbalismContractRequirement requirement, ConfigNode node) : base(type, requirement)
		{
			min = Lib.ConfigValue(node, "min", double.MinValue);
			max = Lib.ConfigValue(node, "max", double.MaxValue);
			description = Lib.ConfigValue<string>(node, "description", null);
		}

		public override string GetTitle(EvaluationContext context)
		{
			if (!string.IsNullOrEmpty(description))
				return description;

			if (min != double.MinValue && max != double.MaxValue)
				description = Localizer.Format("Solar elevation between <<1>> ° and <<2>> °", min.ToString("F1"), max.ToString("F1"));
			else if (min != double.MinValue)
				description = Localizer.Format("Solar elevation above <<1>> °", min.ToString("F1"));
			else if (max != double.MaxValue)
				description = Localizer.Format("Solar elevation below <<1>> °", max.ToString("F1"));

			return description;
		}

		internal override bool CouldBeCandiate(Vessel vessel, EvaluationContext context)
		{
			return vessel.orbit != null && !Lib.IsSun(vessel.mainBody);
		}

		internal override SubRequirementState VesselMeetsCondition(Vessel vessel, EvaluationContext context)
		{
			SolarElevationState state = new SolarElevationState();

			var vesselPosition = context.VesselPosition(vessel);
			var mainBodyPosition = context.BodyPosition(vessel.mainBody);
			var sunPosition = context.BodyPosition(Lib.GetParentSun(vessel.mainBody));

			var a = vesselPosition - mainBodyPosition;
			var b = sunPosition - mainBodyPosition;
			state.solarElevation = Vector3d.Angle(a, b);

			state.requirementMet = true;

			if (min != double.MinValue && state.solarElevation < min)
				state.requirementMet = false;
			if (max != double.MaxValue && state.solarElevation > max)
				state.requirementMet = false;

			return state;
		}

		internal override string GetLabel(Vessel vessel, EvaluationContext context, SubRequirementState state)
		{
			SolarElevationState elevationState = (SolarElevationState)state;
			string degreesString = elevationState.solarElevation.ToString("F1") + " °";
			return Localizer.Format("Solar elevation: <<1>>",
				Lib.Color(degreesString, elevationState.requirementMet ? Lib.Kolor.Green : Lib.Kolor.Red));
		}
	}
}
