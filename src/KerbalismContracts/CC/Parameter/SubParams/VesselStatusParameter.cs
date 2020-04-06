using System;
using Contracts;

namespace KerbalismContracts
{
	public class VesselStatusParameter : ContractParameter
	{
		internal Vessel vessel;
		internal string statusLabel;
		internal bool conditionMet;

		public VesselStatusParameter() { }

		public VesselStatusParameter(Vessel vessel, string statusLabel, bool conditionMet)
		{
			this.vessel = vessel;
			this.statusLabel = statusLabel;
			this.conditionMet = conditionMet;
			optional = true;
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			if (vessel != null && conditionMet)
			{
				SetComplete();
			}
		}

		protected override string GetTitle()
		{
			if (vessel == null)
				return "";

			string vesselName = vessel.GetDisplayName();
			if (vesselName == null)
				vesselName = vessel.name;

			if (string.IsNullOrEmpty(statusLabel))
				return KERBALISM.Lib.Ellipsis(vesselName, 35);

			return KERBALISM.Lib.Ellipsis(vesselName, 35) + "\n" + statusLabel;
		}
	}
}
