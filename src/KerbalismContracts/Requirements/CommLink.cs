using System;
using KERBALISM;
using Contracts;
using KSP.Localization;

namespace KerbalismContracts
{
	public class CommLink : SubRequirement
	{
		private string description;
		private double min_bandwidth;

		public CommLink(string type, KerbalismContractRequirement requirement, ConfigNode node) : base(type, requirement)
		{
			description = Lib.ConfigValue<string>(node, "description", null);
			min_bandwidth = Lib.ConfigValue(node, "min_bandwidth", 0.0);
		}

		public override string GetTitle(Contracts.Contract contract)
		{
			if (!string.IsNullOrEmpty(description))
				return description;

			description = min_bandwidth > 0
				? Localizer.Format("Min. Bandwidth <<1>>", Lib.HumanReadableDataRate(min_bandwidth))
				: "Online";

			return description;
		}

		internal override bool CouldBeCandiate(Vessel vessel, Contract contract)
		{
			return API.VesselConnectionLinked(vessel);
		}

		internal override bool VesselMeetsCondition(Vessel vessel, Contract contract, out string label)
		{
			if (min_bandwidth > 0)
			{
				double bw = API.VesselConnectionRate(vessel);

				var color = Lib.Kolor.Orange;
				if (bw > min_bandwidth * 1.2)
					color = Lib.Kolor.Green;
				if (bw < min_bandwidth)
					color = Lib.Kolor.Red;

				label = Lib.Color(Lib.HumanReadableDataRate(bw), color);

				return bw >= min_bandwidth;
			}

			if(API.VesselConnectionLinked(vessel))
			{
				label = Lib.Color("online", Lib.Kolor.Green);
				return true;
			}

			label = Lib.Color("offline", Lib.Kolor.Red);
			return false;
		}
	}
}
