using System;
using KERBALISM;
using Contracts;
using KSP.Localization;

namespace KerbalismContracts
{
	public class AltitudeState : SubRequirementState
	{
		internal double alt;
	}

	public class Altitude : SubRequirement
	{
		private int min;
		private int max;
		private string description;

		public Altitude(string type, KerbalismContractRequirement requirement, ConfigNode node) : base(type, requirement)
		{
			min = Lib.ConfigValue(node, "min", 0);
			max = Lib.ConfigValue(node, "max", 0);
			description = Lib.ConfigValue<string>(node, "description", null);

			if (min == 0 && max == 0)
				Utils.Log($"Invalid altitude restriction in {requirement.name}: must at least set min or max");
		}

		public override string GetTitle(EvaluationContext parameters)
		{
			if (!string.IsNullOrEmpty(description))
				return description;
			
			if (min != 0 && max != 0)
				description = Localizer.Format("Altitude between <<1>> and <<2>>", Lib.HumanReadableDistance(min), Lib.HumanReadableDistance(max));
			else if (min != 0)
				description = Localizer.Format("Altitude above <<1>>", Lib.HumanReadableDistance(min));
			else if (max != 0)
				description = Localizer.Format("Altitude below <<1>>", Lib.HumanReadableDistance(max));
			else
				description = "Invalid altitude restriction";

			return description;
		}

		internal override bool CouldBeCandiate(Vessel vessel, EvaluationContext context)
		{
			var orbit = vessel.orbit;
			if (orbit == null)
				return false;

			if (min != 0 && orbit.ApA < min) // ap too low for min alt
				return false;
			if (max != 0 && orbit.PeA > max) // pe too high for max alt
				return false;

			return true;
		}

		internal override SubRequirementState VesselMeetsCondition(Vessel vessel, EvaluationContext context)
		{
			AltitudeState state = new AltitudeState();
			state.alt = vessel.altitude;

			if (min != 0 && state.alt < min)
			{
				state.requirementMet = false;
				return state;
			}

			if (max != 0 && state.alt > max)
			{
				state.requirementMet = false;
				return state;
			}

			state.requirementMet = true;
			return state;
		}

		internal override string GetLabel(Vessel vessel, EvaluationContext context, SubRequirementState state)
		{
			AltitudeState altitudeState = (AltitudeState)state;

			if (min != 0 && altitudeState.alt < min)
				return Lib.Color("too low", Lib.Kolor.Red);

			if (max != 0 && altitudeState.alt > max)
				return Lib.Color("too high", Lib.Kolor.Red);

			return Lib.Color("alt OK", Lib.Kolor.Green);
		}
	}
}
