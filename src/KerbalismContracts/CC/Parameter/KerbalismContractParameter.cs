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
		protected int waypoint_index;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue<ContractConfigurator.Duration>(configNode, "duration", x => duration = x, this, new ContractConfigurator.Duration(0.0));
			valid &= ConfigNodeUtil.ParseValue<ContractConfigurator.Duration>(configNode, "allowed_downtime", x => allowed_downtime = x, this, new ContractConfigurator.Duration(0.0));
			valid &= ConfigNodeUtil.ParseValue<string>(configNode, "id", x => requirementId = x, this, "");
			valid &= ConfigNodeUtil.ParseValue<int>(configNode, "min_vessels", x => min_vessels = x, this, 1);
			valid &= ConfigNodeUtil.ParseValue<int>(configNode, "waypoint_index", x => waypoint_index = x, this, 0);

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

			var result = new KerbalismContractParameter(title, requirementId, duration.Value, allowed_downtime.Value, min_vessels, waypoint_index);

			if (requirement.NeedsWaypoint())
			{
				if(Utils.FetchWaypoint(new RequirementContext(contract, waypoint_index)) == null)
				{
					Utils.Log($"There is no waypoint (index {waypoint_index}) in the contract, but requirement '{requirementId}' needs one.", LogLevel.Error);
					return null;
				}
			}

			return result;
		}
	}

	public class KerbalismContractParameter : ContractConfiguratorParameter
	{
		protected string requirementId;
		protected string title;
		protected double duration;
		protected double allowed_downtime;
		protected int min_vessels;
		protected int waypoint_index;

		protected DurationParameter durationParameter;
		protected List<SubRequirementParameter> subRequirementParameters = new List<SubRequirementParameter>();

		private float lastUpdate = 0.0f;
		private const float UPDATE_FREQUENCY = 1f;

		private readonly List<Vessel> vessels = new List<Vessel>();
		private KerbalismContractRequirement requirement;
		private string statusLabel = string.Empty;
		internal RequirementContext context;

		public KerbalismContractParameter() { }

		public KerbalismContractParameter(string title, string requirementId, double duration, double allowed_downtime, int min_vessels, int waypoint_index)
		{
			this.title = title;
			this.requirementId = requirementId;
			this.duration = duration;
			this.allowed_downtime = allowed_downtime;
			this.min_vessels = min_vessels;
			this.waypoint_index = waypoint_index;

			this.requirement = Configuration.Requirement(requirementId);

			CreateSubParameters();
		}

		public RequirementContext GetContext()
		{
			if (context == null)
				context = new RequirementContext(Root, waypoint_index);

			return context;
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

			if (requirement.NeedsWaypoint() && Utils.FetchWaypoint(GetContext()) == null)
			{
				Utils.Log($"There is no waypoint in the contract, but requirement '{requirementId}' needs one.", LogLevel.Warning);
			}

			foreach (var req in Configuration.Requirement(requirementId).SubRequirements)
			{
				Utils.LogDebug($"Creating sub requirement for {requirementId}.{req.type}...");
				var sub = new SubRequirementParameter(req, waypoint_index);
				subRequirementParameters.Add(sub);
				AddParameter(sub);
			}

			if (duration > 0 && durationParameter == null)
			{
				Utils.LogDebug($"Duration {duration} allowed downtime {allowed_downtime}");
				durationParameter = new DurationParameter(duration, allowed_downtime);
				AddParameter(durationParameter);
			}

			Utils.LogDebug($"Created {ParameterCount} sub parameters");
		}

		protected override string GetParameterTitle()
		{
			string result = title ?? Configuration.Requirement(requirementId).title;
			if (!string.IsNullOrEmpty(statusLabel))
				result += ": " + statusLabel;
			return result;
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
				if (!sr.CouldBeCandiate(vessel, GetContext()))
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
			foreach(var sp in subRequirementParameters)
			{
				string vesselLabel;
				bool meetsCondition = sp.subRequirement.VesselMeetsCondition(vessel, GetContext(), out vesselLabel);
				if (meetsCondition)
					sp.matchCounter++;

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
			foreach(var subParameter in subRequirementParameters)
			{
				string label;

				int count = subParameter.matchCounter;
				result &= subParameter.subRequirement.VesselsMeetCondition(vessels, count, GetContext(), out label);

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

			RemoveParameter(typeof(VesselStatusParameter));
			vessels.Clear();
			foreach (var sp in subRequirementParameters)
				sp.matchCounter = 0;

			foreach (Vessel vessel in FlightGlobals.Vessels)
			{
				if (!Utils.IsVessel(vessel))
					continue;

				if (!CouldBeCandidate(vessel))
					continue;

				vessels.Add(vessel);
			}

			bool allConditionsMet = vessels.Count > 0;
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
