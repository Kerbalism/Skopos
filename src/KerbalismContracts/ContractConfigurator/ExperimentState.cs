using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{
	public enum ExperimentState { UNDEFINED, STOPPED, RUNNING };

	public class ExperimentStateFactory : ParameterFactory
	{
		protected string experiment_id;
		protected ExperimentState expected_state;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue<string>(configNode, "experiment_id", x => experiment_id = x, this, string.Empty, ValidateExperimentId);
			valid &= ConfigNodeUtil.ParseValue<ExperimentState>(configNode, "expected_state", x => expected_state = x, this, ExperimentState.UNDEFINED, ValidateState);

			return valid;
		}

		public override ContractParameter Generate(Contract contract)
		{
			return new ExperimentStateParameter(experiment_id, expected_state, title);
		}

		private bool ValidateExperimentId(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				LoggingUtil.LogError(this, "Missing experiment_id.");
				return false;
			}

			if (KERBALISM.ScienceDB.GetExperimentInfo(id) == null)
			{
				LoggingUtil.LogError(this, "Invalid experiment_id " + id);
				return false;
			}

			return true;
		}

		protected bool ValidateState(ExperimentState s)
		{
			if (s == ExperimentState.UNDEFINED)
			{
				LoggingUtil.LogError(this, "Missing field. You must specify expected_state = STOPPED or RUNNING.");
				return false;
			}

			return true;
		}
	}

	public class ExperimentStateParameter : VesselParameter
	{
		protected string experiment_id;
		protected ExperimentState expected_state;

		public ExperimentStateParameter() : base(null) { }

		public ExperimentStateParameter(string experiment_id, ExperimentState expected_state, string title)
		{
			this.experiment_id = experiment_id;
			this.expected_state = expected_state;
			this.title = title;
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			if (vessel == null) return false;

			LoggingUtil.LogVerbose(this, "Checking ExperimentStateParameter.VesselMeetsCondition: " + vessel);

			return IsExperimentRunningOnVessel(vessel, experiment_id) == expected_state;
		}

		/// <summary> works for loaded and unloaded vessel. very slow method, don't use it every tick </summary>
		/// TODO improve performance
		public static ExperimentState IsExperimentRunningOnVessel(Vessel vessel, string experiment_id)
		{
			if (vessel.loaded)
			{
				foreach (KERBALISM.Experiment e in vessel.FindPartModulesImplementing<KERBALISM.Experiment>())
				{
					if (e.enabled && e.experiment_id == experiment_id)
						if(e.Running) return ExperimentState.RUNNING;
				}
			}
			else
			{
				var PD = new Dictionary<string, KERBALISM.Lib.Module_prefab_data>();
				foreach (ProtoPartSnapshot p in vessel.protoVessel.protoPartSnapshots)
				{
					// get part prefab (required for module properties)
					Part part_prefab = PartLoader.getPartInfoByName(p.partName).partPrefab;
					// get all module prefabs
					var module_prefabs = part_prefab.FindModulesImplementing<PartModule>();
					// clear module indexes
					PD.Clear();
					foreach (ProtoPartModuleSnapshot m in p.modules)
					{
						// get the module prefab
						// if the prefab doesn't contain this module, skip it
						PartModule module_prefab = KERBALISM.Lib.ModulePrefab(module_prefabs, m.moduleName, PD);
						if (!module_prefab) continue;
						// if the module is disabled, skip it
						// note: this must be done after ModulePrefab is called, so that indexes are right
						if (!KERBALISM.Lib.Proto.GetBool(m, "isEnabled")) continue;

						if (m.moduleName == "Experiment"
							&& ((KERBALISM.Experiment)module_prefab).experiment_id == experiment_id)
						{
							if (KERBALISM.Experiment.IsRunning(KERBALISM.Lib.Proto.GetEnum(m, "expState", KERBALISM.Experiment.RunningState.Stopped)))
								return ExperimentState.RUNNING;
						}
					}
				}
			}

			return ExperimentState.STOPPED;
		}
	}
}