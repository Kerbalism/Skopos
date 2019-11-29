using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{
	public class ExperimentRunningFactory : ParameterFactory
	{
		protected string experiment;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue<string>(configNode, "experiment", x => experiment = x, this, string.Empty, ValidateExperimentId);

			return valid;
		}

		public override ContractParameter Generate(Contract contract)
		{
			return new ExperimentRunningParameter(experiment, title);
		}

		private bool ValidateExperimentId(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				LoggingUtil.LogError(this, "Missing experiment_id.");
				return false;
			}

			return true;
		}
	}

	public class ExperimentRunningParameter : VesselParameter
	{
		protected string experiment;

		public ExperimentRunningParameter() : base(null) { }

		public ExperimentRunningParameter(string experiment, string title)
		{
			this.experiment = experiment;
			this.title = title;
		}

		protected override void OnParameterSave(ConfigNode node)
		{
			base.OnParameterSave(node);
			node.AddValue("experiment", experiment);
		}

		protected override void OnParameterLoad(ConfigNode node)
		{
			base.OnParameterLoad(node);
			experiment = node.GetValue("experiment");
		}

		protected override void OnRegister()
		{
			base.OnRegister();
			ExperimentStateTracker.AddListener(StateChanged);

			foreach(Vessel v in FlightGlobals.Vessels)
			{
				if (!KerbalismContracts.RelevantVessel(v))
					return;

				if(KERBALISM.API.ExperimentIsRunning(v, experiment))
				{
					Lib.Log("Experiment Running detected upon register on " + v + " " + experiment);
					ExperimentStateTracker.Add(v, experiment);
				}
			}
		}

		protected override void OnUnregister()
		{
			base.OnUnregister();
			ExperimentStateTracker.RemoveListener(StateChanged);
		}

		protected void StateChanged(Vessel vessel, string experiment_id, bool running)
		{
			CheckVessel(vessel);
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			if (vessel == null) return false;

			Lib.Log("Checking ExperimentStateParameter.VesselMeetsCondition: " + vessel + " " + experiment);

			return ExperimentStateTracker.IsRunning(vessel, experiment);
		}	
	}
}