using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

/*
 * TODO:
 * - restrict to body
 * - restrict to situation
 * - restrict to biome
 * 
 */
namespace Kerbalism.Contracts
{
	public class ExperimentRunningFactory : ParameterFactory
	{
		protected string experiment_id;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue<string>(configNode, "experiment_id", x => experiment_id = x, this);

			return valid;
		}

		public override ContractParameter Generate(Contract contract)
		{
			return new ExperimentRunningParameter(title, experiment_id);
		}
	}

	public class ExperimentRunningParameter : VesselParameter
	{
		protected string experiment_id;

		public ExperimentRunningParameter(): base(null) {}
		public ExperimentRunningParameter(string title, string experiment_id) : base(title) { }

		protected override void OnParameterLoad(ConfigNode node)
		{
			experiment_id = ConfigNodeUtil.ParseValue<string>(node, "experiment_id");
		}

		protected override void OnParameterSave(ConfigNode node)
		{
			node.AddValue("experiment_id", experiment_id);
		}

		protected override void OnRegister()
		{
			base.OnRegister();
			KERBALISM.API.OnExperimentStateChanged.Add(RunCheck);
		}

		protected override void OnUnregister()
		{
			base.OnUnregister();
			KERBALISM.API.OnExperimentStateChanged.Remove(RunCheck);
		}

		private void RunCheck(Vessel v, string exp_id, bool running)
		{
			if (exp_id != experiment_id)
				return;
			ExperimentStateTracker.Update(v, exp_id, running);
			CheckVessel(v);
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			return ExperimentStateTracker.IsRunning(vessel, experiment_id);
		}
	}
}
