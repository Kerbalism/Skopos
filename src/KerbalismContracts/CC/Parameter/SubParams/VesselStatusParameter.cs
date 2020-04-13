using ContractConfigurator;
using Contracts;
using System;

namespace KerbalismContracts
{
	public class VesselStatusParameter : ContractParameter
	{
		internal Vessel vessel;
		internal string statusLabel;
		internal bool conditionMet;
		internal bool obsolete;

		private readonly TitleTracker titleTracker;
		private string lastTitle;

		public VesselStatusParameter()
		{
			titleTracker = new TitleTracker(this);
		}

		public VesselStatusParameter(Vessel vessel, string statusLabel, bool conditionMet)
		{
			titleTracker = new TitleTracker(this);

			this.vessel = vessel;
			this.statusLabel = statusLabel;
			this.conditionMet = conditionMet;
			this.obsolete = false;
		}

		internal void Update(string statusLabel, bool conditionMet)
		{
			this.obsolete = false;
			this.statusLabel = statusLabel;
			this.conditionMet = conditionMet;
			GetTitle();
		}

		protected override void OnSave(ConfigNode node)
		{
			base.OnSave(node);
			if(vessel != null)
				node.AddValue("vesselId", vessel.id);
		}

		protected override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
			string idString = ConfigNodeUtil.ParseValue<string>(node, "vesselId", null);
			if (!string.IsNullOrEmpty(idString))
			{
				Guid vesselId = new Guid(idString);
				vessel = FlightGlobals.FindVessel(vesselId);
			}
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			if (conditionMet)
				SetComplete();
		}

		protected override string GetTitle()
		{
			if (vessel == null)
				return "";

			string vesselName = vessel.GetDisplayName();
			if (vesselName == null)
				vesselName = vessel.name;

			string result;
			if (string.IsNullOrEmpty(statusLabel))
				result = KERBALISM.Lib.Ellipsis(vesselName, 35);
			else
				result = KERBALISM.Lib.Ellipsis(vesselName, 35) + "\n" + statusLabel;

			titleTracker.Add(result);
			if (lastTitle != result && Root != null && (Root.ContractState == Contract.State.Active || Root.ContractState == Contract.State.Failed))
			{
				titleTracker.UpdateContractWindow(result);
				lastTitle = result;
			}
			return result;
		}
	}
}
