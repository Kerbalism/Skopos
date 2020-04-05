using System;
using KERBALISM;
using Contracts;
using KSP.Localization;

namespace KerbalismContracts
{
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

		public override string GetTitle(RequirementContext parameters)
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

		internal override bool CouldBeCandiate(Vessel vessel, RequirementContext context)
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

		internal override bool VesselMeetsCondition(Vessel vessel, RequirementContext context, out string label)
		{
			var alt = vessel.altitude;

			if(min != 0 && alt < min)
			{
				label = Lib.Color("too low", Lib.Kolor.Red);
				return false;
			}

			if(max != 0 && alt > max)
			{
				label = Lib.Color("too high", Lib.Kolor.Red);
				return false;
			}

			label = Lib.Color("alt OK", Lib.Kolor.Green);
			return true;
		}
	}
}
