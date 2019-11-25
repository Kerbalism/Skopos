using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{
	public class ExperimentDoneFactory : ParameterFactory
	{
		protected string experiment;
		protected string biome;
		protected string situation;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue<string>(configNode, "experiment", x => experiment = x, this, string.Empty);
			valid &= ConfigNodeUtil.ParseValue<string>(configNode, "biome", x => biome = x, this, string.Empty);
			valid &= ConfigNodeUtil.ParseValue<string>(configNode, "situation", x => situation = x, this, string.Empty);

			return valid;
		}

		public override ContractParameter Generate(Contract contract)
		{
			return new ExperimentDone(experiment, targetBody, biome, situation, title);
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

	public class ExperimentDone : VesselParameter
	{
		protected string experiment = string.Empty;
		protected string biome = string.Empty;
		protected string situation = string.Empty;
		protected bool finished = false;

		public ExperimentDone() { }
		public ExperimentDone(string experiment, CelestialBody targetBody, string biome, string situation, string title)
		{
			this.title = title;
			this.experiment = experiment;
			this.targetBody = targetBody;
			this.biome = biome;
			this.situation = situation;
		}

		protected override string GetParameterTitle()
		{
			if (!string.IsNullOrEmpty(title)) return title;

			return "Collect " + experiment;
		}

		protected override void OnParameterLoad(ConfigNode node)
		{
			base.OnParameterLoad(node);

			experiment = ConfigNodeUtil.ParseValue(node, "experiment", string.Empty);
			biome = ConfigNodeUtil.ParseValue(node, "biome", string.Empty);
			situation = ConfigNodeUtil.ParseValue(node, "situation", string.Empty);
			finished = ConfigNodeUtil.ParseValue(node, "finished", false);
			title = ConfigNodeUtil.ParseValue(node, "title", string.Empty);
			targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody", (CelestialBody)null);
		}

		protected override void OnParameterSave(ConfigNode node)
		{
			base.OnParameterSave(node);

			node.AddValue("biome", biome);
			node.AddValue("situation", situation);
			node.AddValue("experiment", experiment);
			node.AddValue("finished", finished);
			node.AddValue("title", title);
			if(targetBody != null) node.AddValue("targetBody", targetBody.name);
		}

		protected override void OnRegister()
		{
			base.OnRegister();
			GameEvents.OnScienceRecieved.Add(new EventData<float, ScienceSubject, ProtoVessel, bool>.OnEvent(OnScienceReceived));
		}

		protected override void OnUnregister()
		{
			base.OnUnregister();
			GameEvents.OnScienceRecieved.Remove(new EventData<float, ScienceSubject, ProtoVessel, bool>.OnEvent(OnScienceReceived));
		}

		protected void OnScienceReceived(float science, ScienceSubject subject, ProtoVessel protoVessel, bool reverseEngineered)
		{
			if(protoVessel == null || reverseEngineered)
			{
				Lib.Log("Science received without vessel, or reverse engineered");
				return;
			}

			int p = subject.id.IndexOf('@');
			string id = subject.id.Substring(0, p);
			string specifier = subject.id.Substring(p + 1);

			bool match = id == experiment;
			if (match && targetBody != null) match = specifier.StartsWith(targetBody.name, StringComparison.Ordinal);
			if (match && !string.IsNullOrEmpty(situation)) match = specifier.Contains(situation);
			if (match && !string.IsNullOrEmpty(biome)) match = specifier.EndsWith(biome, StringComparison.Ordinal);

			if (match) finished = true;

			CheckVessel(protoVessel.vesselRef);
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			return finished;
		}
	}
}
