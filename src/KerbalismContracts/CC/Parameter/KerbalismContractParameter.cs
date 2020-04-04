using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;
using UnityEngine;
using KERBALISM;

namespace KerbalismContracts
{
	/// <summary> Parameter for global radiation field status: how often has a radiation field been penetrated </summary>
	public class KerbalismContractFactory : ParameterFactory
	{
		protected string requirementId;
		protected ContractConfigurator.Duration duration;
		protected ContractConfigurator.Duration allowed_downtime;
		protected int min_vessels;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue<ContractConfigurator.Duration>(configNode, "duration", x => duration = x, this, new ContractConfigurator.Duration(0.0));
			valid &= ConfigNodeUtil.ParseValue<ContractConfigurator.Duration>(configNode, "allowed_downtime", x => allowed_downtime = x, this, new ContractConfigurator.Duration(0.0));
			valid &= ConfigNodeUtil.ParseValue<string>(configNode, "id", x => requirementId = x, this, "");
			valid &= ConfigNodeUtil.ParseValue<int>(configNode, "min_vessels", x => min_vessels = x, this, 1);

			return valid;
		}

		public override ContractParameter Generate(Contract contract)
		{
			var requirement = Configuration.Requirement(requirementId);
			if (requirement == null)
			{
				Utils.Log($"There is no KerbalismContractRequirement with name '{requirementId}'.", LogLevel.Error);
				return null;
			}

			var result = new KerbalismContractParameter(requirementId, duration.Value, allowed_downtime.Value, min_vessels);

			if (requirement.NeedsWaypoint())
			{
				if(Utils.FetchWaypoint(contract) == null)
				{
					Utils.Log($"There is no waypoint in the contract, but requirement '{requirementId}' needs one.", LogLevel.Error);
					return null;
				}
			}

			return result;
		}
	}

	public class KerbalismContractParameter : ContractConfiguratorParameter
	{
		protected string requirementId;
		protected double duration;
		protected double allowed_downtime;
		protected int min_vessels;

		protected DurationParameter durationParameter;
		protected Dictionary<string, SubRequirementParameter> subParameters = new Dictionary<string, SubRequirementParameter>();

		private float lastUpdate = 0.0f;
		private const float UPDATE_FREQUENCY = 1f;

		private readonly List<Vessel> vessels = new List<Vessel>();
		private readonly List<string> conditionCounter = new List<string>();
		private KerbalismContractRequirement requirement;
		private string statusLabel = string.Empty;

		public KerbalismContractParameter() { }

		public KerbalismContractParameter(string requirementId, double duration, double allowed_downtime, int min_vessels)
		{
			this.requirementId = requirementId;
			this.duration = duration;
			this.allowed_downtime = allowed_downtime;
			this.min_vessels = min_vessels;

			this.requirement = Configuration.Requirement(requirementId);

			CreateSubParameters();
		}

		protected void OnContractLoaded(ConfiguredContract contract)
		{
			if (contract == Root)
			{
				CreateSubParameters();
			}
		}

		protected void CreateSubParameters()
		{
			if (ParameterCount > 0)
				return;

			if (requirement.NeedsWaypoint())
			{
				if (Utils.FetchWaypoint(Root) == null)
				{
					Utils.Log($"There is no waypoint in the contract, but requirement '{requirementId}' needs one.", LogLevel.Error);
				}
			}

			foreach (var req in Configuration.Requirement(requirementId).SubRequirements)
			{
				if(!subParameters.ContainsKey(req.type))
				{
					subParameters[req.type] = new SubRequirementParameter(req);
					AddParameter(subParameters[req.type]);
				}
			}

			if (duration > 0 && durationParameter == null)
			{
				durationParameter = new DurationParameter(duration, allowed_downtime);
				AddParameter(durationParameter);
			}

			Utils.LogDebug($"Created {ParameterCount} subs");
		}

		protected SubRequirementParameter SubParameter(string type)
		{
			if (subParameters.Count == 0)
			{
				for (int i = 0; i < ParameterCount; i++)
				{
					var param = GetParameter(i);

					if (param is DurationParameter dp)
						durationParameter = dp;

					if (param is SubRequirementParameter sp)
						subParameters[sp.subRequirement.type] = sp;
				}
			}
			return subParameters[type];
		}

		protected override string GetParameterTitle()
		{
			if(string.IsNullOrEmpty(statusLabel))
				return Configuration.Requirement(requirementId).title;
			return Configuration.Requirement(requirementId).title + ": " + statusLabel;
		}

		protected void SetStatusLabel(string newLabel)
		{
			if(newLabel != statusLabel)
			{
				statusLabel = newLabel;
				ContractConfigurator.ContractConfigurator.OnParameterChange.Fire(this.Root, this);
			}
		}

		protected override void OnParameterSave(ConfigNode node)
		{
			node.AddValue("requirementId", requirementId);
			node.AddValue("duration", duration);
			node.AddValue("allowed_downtime", allowed_downtime);
			node.AddValue("min_vessels", min_vessels);
		}

		protected override void OnParameterLoad(ConfigNode node)
		{
			requirementId = ConfigNodeUtil.ParseValue<string>(node, "requirementId", "");
			duration = ConfigNodeUtil.ParseValue<double>(node, "duration", 0);
			allowed_downtime = ConfigNodeUtil.ParseValue<double>(node, "allowed_downtime", 0);
			min_vessels = ConfigNodeUtil.ParseValue<int>(node, "min_vessels", 1);
			requirement = Configuration.Requirement(requirementId);
		}

		/// <summary>
		/// hard filter: vessels that do not pass this test will not be considered
		/// </summary>
		private bool CouldBeCandidate(Vessel vessel)
		{
			foreach(var sr in requirement.SubRequirements)
			{
				if (!sr.CouldBeCandiate(vessel, Root))
					return false;
			}
			return true;
		}

		/// <summary>
		/// soft filter: run on ALL vessels that pass the hard filter (CouldBeCandidate),
		/// determine if the vessel currently meets the condition (i.e. currently over location or not)
		/// </summary>
		/// <param name="label">label to add to this vessel in the status display</param>
		private bool VesselMeetsCondition(Vessel vessel, out string label)
		{
			label = string.Empty;
			bool result = true;
			foreach (var sr in requirement.SubRequirements)
			{
				string vesselLabel;
				bool meetsCondition = sr.VesselMeetsCondition(vessel, Root, out vesselLabel);
				if (meetsCondition)
					conditionCounter.Add(sr.type);
				result &= meetsCondition;

				if (!string.IsNullOrEmpty(vesselLabel))
				{
					if (string.IsNullOrEmpty(label))
						label = vesselLabel;
					else
						label += ", " + vesselLabel;
				}
			}

			return result;
		}

		/// <summary>
		/// final filter: looks at the collection of all vessels that passed the hard and soft filters,
		/// use this to check constellations, count vessels etc.
		/// </summary>
		private bool VesselsMeetCondition(List<Vessel> vessels, int vesselsMeetingAllConditions)
		{
			string statusLabel = string.Empty;

			bool result = true;
			foreach (var sr in requirement.SubRequirements)
			{
				Utils.LogDebug($"testing sub requirement {sr.type}");

				int count = 0;
				foreach (var type in conditionCounter)
					if (type == sr.type) count++;

				string label;
				result &= SubParameter(sr.type).VesselsMeetCondition(vessels, count, out label);

				if (!string.IsNullOrEmpty(label))
				{
					if (string.IsNullOrEmpty(statusLabel))
						statusLabel = label;
					else
						statusLabel += ", " + label;
				}
			}

			if (string.IsNullOrEmpty(statusLabel))
			{
				statusLabel = Lib.Color($"{vesselsMeetingAllConditions}/{min_vessels}", vesselsMeetingAllConditions > min_vessels ? Lib.Kolor.Green : Lib.Kolor.Red);
			}

			SetStatusLabel(statusLabel);

			return result;
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			if (ParameterCount == 0) return;
			if (state != ParameterState.Incomplete) return;
			if (Time.fixedTime - lastUpdate < UPDATE_FREQUENCY) return;
			lastUpdate = Time.fixedTime;

			vessels.Clear();
			conditionCounter.Clear();
			RemoveParameter(typeof(VesselStatusParameter));

			foreach (Vessel vessel in FlightGlobals.Vessels)
			{
				if (!Utils.IsVessel(vessel))
					continue;

				if (!CouldBeCandidate(vessel))
					continue;

				vessels.Add(vessel);
			}

			bool allConditionsMet = true;
			int vesselsMeetingAllConditions = 0;

			foreach (Vessel vessel in vessels)
			{
				string statusLabel;
				bool conditionMet = VesselMeetsCondition(vessel, out statusLabel);
				if (conditionMet) vesselsMeetingAllConditions++;
				AddParameter(new VesselStatusParameter(vessel, statusLabel, conditionMet));
				allConditionsMet &= conditionMet;
			}

			allConditionsMet &= VesselsMeetCondition(vessels, vesselsMeetingAllConditions);

			if (durationParameter == null)
			{
				SetState(allConditionsMet ? ParameterState.Complete : ParameterState.Incomplete);
			}
			else
			{
				durationParameter.Update(allConditionsMet);
				SetState(durationParameter.State);
			}

			ContractConfigurator.ContractConfigurator.OnParameterChange.Fire(this.Root, this);
		}
	}
}
