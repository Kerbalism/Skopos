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

			if (conditionMet)
			{
				Utils.LogDebug($"set complete");
				SetComplete();
			}
		}

		protected override string GetTitle()
		{
			if (string.IsNullOrEmpty(statusLabel))
				return KERBALISM.Lib.Ellipsis(vessel.GetDisplayName(), 25);

			return KERBALISM.Lib.Ellipsis(vessel.GetDisplayName(), 25) + ": " + statusLabel;
		}
	}
}
