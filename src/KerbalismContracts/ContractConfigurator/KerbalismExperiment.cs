using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{
	public class KerbalismExperimentFactory : ParameterFactory
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
			return new KerbalismExperimentParameter(title, experiment_id);
		}
	}

	public class KerbalismExperimentParameter : ContractConfiguratorParameter
	{
		protected string experiment_id;

		public KerbalismExperimentParameter(): base(null) {}

		public KerbalismExperimentParameter(string title, string experiment_id) : base(title) { }

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

			KERBALISM.API.onExperimentStateChanged.Add(ExperimentStateChanged);
		}

		protected override void OnUnregister()
		{
			base.OnUnregister();

			KERBALISM.API.onExperimentStateChanged.Remove(ExperimentStateChanged);
		}

		private void ExperimentStateChanged(Guid vessel_id, string exp_id, bool running)
		{
			if (exp_id != experiment_id)
				return;

			SetState(running ? ParameterState.Complete : ParameterState.Incomplete);
		}
	}

}
