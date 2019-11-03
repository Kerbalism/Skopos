using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{
	public class ExperimentProgressFactory : ParameterFactory
	{
		protected string experiment_id;
		protected int min_progress;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue<string>(configNode, "experiment_id", x => experiment_id = x, this, string.Empty, ValidateExperimentId);
			valid &= ConfigNodeUtil.ParseValue<int>(configNode, "min_progress", x => min_progress = x, this, 100);

			return valid;
		}

		public override ContractParameter Generate(Contract contract)
		{
			return new ExperimentProgressParameter(experiment_id, min_progress, title);
		}

		private bool ValidateExperimentId(string id)
		{
			if(string.IsNullOrEmpty(id))
			{
				LoggingUtil.LogError(this, "Missing experiment_id.");
				return false;
			}

			if(KERBALISM.ScienceDB.GetExperimentInfo(id) == null)
			{
				LoggingUtil.LogError(this, "Invalid experiment_id " + id);
				return false;
			}

			return true;
		}
	}

	public class ExperimentProgressParameter : ContractConfiguratorParameter
	{
		protected string experiment_id;
		protected int min_progress;
		protected KERBALISM.ExperimentInfo experimentInfo;

		private float lastRealUpdate = 0.0f;
		private double lastGameTimeUpdate = 0.0;

		private const float REAL_UPDATE_FREQUENCY = 5.0f;
		private const double GAME_UPDATE_FREQUENCY = 100.0;

		public ExperimentProgressParameter(): base(null) {}

		public ExperimentProgressParameter(string experiment_id, int min_progress, string title)
		{
			this.experiment_id = experiment_id;
			this.min_progress = min_progress;
			this.title = title;
			experimentInfo = KERBALISM.ScienceDB.GetExperimentInfo(experiment_id);
		}

		protected override string GetParameterTitle()
		{
			if (!string.IsNullOrEmpty(title)) return title;
			if (experimentInfo == null) return "Run an experiment";
			return "Research " + (min_progress == 100 ? "" : min_progress.ToString() + "% of ") + experimentInfo.Title;
		}

		protected override void OnParameterSave(ConfigNode node)
		{
			node.AddValue("experiment_id", experiment_id);
			node.AddValue("min_progress", min_progress);
		}

		protected override void OnParameterLoad(ConfigNode node)
		{
			experiment_id = ConfigNodeUtil.ParseValue<string>(node, "experiment_id", string.Empty);
			min_progress = ConfigNodeUtil.ParseValue<int>(node, "min_progress");
			experimentInfo = KERBALISM.ScienceDB.GetExperimentInfo(experiment_id);
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			// Do a check if either:
			//   REAL_UPDATE_FREQUENCY of real time has elapsed
			//   GAME_UPDATE_FREQUENCY of game time has elapsed
			if (UnityEngine.Time.fixedTime - lastRealUpdate > REAL_UPDATE_FREQUENCY ||
				Planetarium.GetUniversalTime() - lastGameTimeUpdate > GAME_UPDATE_FREQUENCY)
			{
				lastRealUpdate = UnityEngine.Time.fixedTime;
				lastGameTimeUpdate = Planetarium.GetUniversalTime();

				var data = KERBALISM.ScienceDB.GetSubjectDataFromStockId(experiment_id);
				if(data != null && data.PercentRetrieved * 100 >= min_progress) {
					SetState(ParameterState.Complete);
				}

				// Force a call to GetTitle to update the contracts app
				GetTitle();
			}
		}
	}
}
