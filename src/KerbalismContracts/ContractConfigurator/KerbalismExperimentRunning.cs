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
	public class KerbalismExperimentRunningFactory : ParameterFactory
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
			return new KerbalismExperimentRunningParameter(title, experiment_id);
		}
	}

	public class KerbalismExperimentRunningParameter : VesselParameter
	{
		protected string experiment_id;
		private Dictionary<Guid, bool> runningExperiments = new Dictionary<Guid, bool>();

		public KerbalismExperimentRunningParameter(): base(null) {}

		public KerbalismExperimentRunningParameter(string title, string experiment_id) : base(title) { }

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

			KERBALISM.API.OnExperimentStateChanged.Add(ExperimentStateChanged);
		}

		protected override void OnUnregister()
		{
			base.OnUnregister();

			KERBALISM.API.OnExperimentStateChanged.Remove(ExperimentStateChanged);
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			if (!runningExperiments.ContainsKey(vessel.id))
				return false;
			return runningExperiments[vessel.id];
		}

		private void ExperimentStateChanged(Vessel v, string exp_id, bool running)
		{
			if (exp_id != experiment_id)
				return;

			runningExperiments[v.id] = running;
			CheckVessel(v);
		}
	}

}
