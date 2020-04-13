using System;
using KERBALISM;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace KerbalismContracts
{
	/// <summary> Parameter for radiation field shenanigans that are related to a vessel (like max. crossings of inner belt, or avoid belt to moon type of contracts) </summary>
	public class VesselExperimentRunningFactory : ParameterFactory
	{
		protected string experimentId;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue<string>(configNode, "experimentId", x => experimentId = x, this, "");

			if (string.IsNullOrEmpty(experimentId))
			{
				LoggingUtil.LogError(GetType(), ErrorPrefix() + ": experimentId cannot be empty");
				valid = false;
			}

			return valid;
		}

		public override ContractParameter Generate(Contract contract)
		{
			return new VesselExperimentRunningParameter(experimentId, title);
		}

		private bool ValidateField(RadiationFieldType f)
		{
			if(f == RadiationFieldType.UNDEFINED)
			{
				LoggingUtil.LogError(this, "Missing field. You must specify field = INNER_BELT, OUTER_BELT, MAGNETOPAUSE or ANY.");
				return false;
			}
			return true;
		}

	}

	public class VesselExperimentRunningParameter : VesselParameter
	{
		protected string experimentId;

		public VesselExperimentRunningParameter(): base(null) {}

		public VesselExperimentRunningParameter(string experimentId, string title)
		{
			this.experimentId = experimentId;
			this.title = title;
		}

		protected override string GetParameterTitle()
		{
			if (!string.IsNullOrEmpty(title))
				return title;
			title = ScienceDB.GetExperimentInfo(experimentId)?.Title ?? experimentId;
			title = Localizer.Format("Run experiment <<1>>", title);
			return title;
		}

		protected override void OnParameterSave(ConfigNode node)
		{
			base.OnParameterSave(node);

			node.AddValue("experimentId", experimentId);
			node.AddValue("title", title);
		}

		protected override void OnParameterLoad(ConfigNode node)
		{
			base.OnParameterLoad(node);

			experimentId = ConfigNodeUtil.ParseValue(node, "experimentId", string.Empty);
			title = ConfigNodeUtil.ParseValue(node, "title", string.Empty);
		}

		protected override void OnRegister()
		{
			base.OnRegister();
			ExperimentStateTracker.AddListener(RunCheck);
		}

		protected override void OnUnregister()
		{
			base.OnUnregister();
			ExperimentStateTracker.RemoveListener(RunCheck);
		}

		private void RunCheck(Guid vesselId, string experimentId, ExperimentState state)
		{
			if (experimentId == this.experimentId)
				CheckVessel(FlightGlobals.FindVessel(vesselId));
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			if (!ExperimentStateTracker.HasValue(vessel.id, experimentId))
				return false;

			return ExperimentStateTracker.GetValue(vessel.id, experimentId) == ExperimentState.running;
		}
	}
}
