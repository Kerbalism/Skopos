using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;
using UnityEngine;
using KERBALISM;
using System;

namespace KerbalismContracts
{
	/// <summary> Parameter for global radiation field status: how often has a radiation field been penetrated </summary>
	public class KerbalismContractFactory : ParameterFactory
	{
		protected string requirementId;
		protected ContractConfigurator.Duration duration;
		protected ContractConfigurator.Duration allowedDowntime;
		protected int minVessels;
		protected int waypointIndex;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue<ContractConfigurator.Duration>(configNode, "duration", x => duration = x, this, new ContractConfigurator.Duration(0.0));
			valid &= ConfigNodeUtil.ParseValue<ContractConfigurator.Duration>(configNode, "allowedDowntime", x => allowedDowntime = x, this, new ContractConfigurator.Duration(0.0));
			valid &= ConfigNodeUtil.ParseValue<string>(configNode, "id", x => requirementId = x, this, "");
			valid &= ConfigNodeUtil.ParseValue<int>(configNode, "minVessels", x => minVessels = x, this, 1);
			valid &= ConfigNodeUtil.ParseValue<int>(configNode, "waypointIndex", x => waypointIndex = x, this, 0);
			valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "hideChildren", x => hideChildren = x, this, false);
			valid &= ConfigNodeUtil.ParseValue<string>(configNode, "title", x => title = x, this, "");

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

			var result = new KerbalismContractParameter(title, requirementId, duration.Value, allowedDowntime.Value, minVessels, waypointIndex, hideChildren);

			if (requirement.NeedsWaypoint())
			{
				if(Utils.FetchWaypoint(contract, waypointIndex) == null)
				{
					Utils.Log($"There is no waypoint (index {waypointIndex}) in the contract, but requirement '{requirementId}' needs one.", LogLevel.Error);
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
		protected double allowedDowntime;
		protected int minVessels;
		protected int waypointIndex;

		protected DurationParameter durationParameter;
		protected List<SubRequirementParameter> subRequirementParameters = new List<SubRequirementParameter>();

		private double lastUpdate = 0.0f;

		private readonly List<Vessel> vessels = new List<Vessel>();
		private KerbalismContractRequirement requirement;
		private string statusLabel = string.Empty;
		internal EvaluationContext context;

		public KerbalismContractParameter() { }

		public KerbalismContractParameter(string title, string requirementId, double duration, double allowedDowntime, int minVessels, int waypointIndex, bool hideChildren)
		{
			this.requirementId = requirementId;
			this.duration = duration;
			this.allowedDowntime = allowedDowntime;
			this.minVessels = minVessels;
			this.waypointIndex = waypointIndex;
			this.title = title;
			this.hideChildren = hideChildren;

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
			if (subRequirementParameters.Count > 0)
				return;

			for(int i = 0; i < ParameterCount; i++)
			{
				var p = GetParameter(i);
				if(p is SubRequirementParameter srp)
					subRequirementParameters.Add(srp);

				if (p is DurationParameter dp)
					durationParameter = dp;
			}

			if (subRequirementParameters.Count > 0)
				return;

			foreach (var req in Configuration.Requirement(requirementId).SubRequirements)
			{
				Utils.LogDebug($"Creating sub requirement for {requirementId}.{req.type}...");
				var sub = new SubRequirementParameter(req);
				subRequirementParameters.Add(sub);
				AddParameter(sub);
			}

			if (duration > 0 && durationParameter == null)
			{
				Utils.LogDebug($"Duration {duration} allowed downtime {allowedDowntime}");
				durationParameter = new DurationParameter(duration, allowedDowntime);
				AddParameter(durationParameter);
			}

			Utils.LogDebug($"Created {ParameterCount} sub parameters");
		}

		protected override string GetParameterTitle()
		{
			string result = !string.IsNullOrEmpty(title) ? title : Configuration.Requirement(requirementId).title;
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
			node.AddValue("allowedDowntime", allowedDowntime);
			node.AddValue("minVessels", minVessels);
			node.AddValue("waypointIndex", waypointIndex);
		}

		protected override void OnParameterLoad(ConfigNode node)
		{
			requirementId = ConfigNodeUtil.ParseValue<string>(node, "requirementId", "");
			duration = ConfigNodeUtil.ParseValue<double>(node, "duration", 0);
			allowedDowntime = ConfigNodeUtil.ParseValue<double>(node, "allowedDowntime", 0);
			minVessels = ConfigNodeUtil.ParseValue<int>(node, "minVessels", 1);
			waypointIndex = ConfigNodeUtil.ParseValue<int>(node, "waypointIndex", 0);
			requirement = Configuration.Requirement(requirementId);
		}

		/// <summary>
		/// hard filter: vessels that do not pass this test will not be considered
		/// </summary>
		private bool CouldBeCandidate(Vessel vessel)
		{
			foreach(var sr in requirement.SubRequirements)
			{
				if (!sr.CouldBeCandiate(vessel, context))
					return false;
			}
			return true;
		}

		/// <summary>
		/// soft filter: run on ALL vessels that pass the hard filter (CouldBeCandidate),
		/// determine if the vessel currently meets the condition (i.e. currently over location or not)
		/// </summary>
		/// <param name="label">label to add to this vessel in the status display</param>
		private bool VesselMeetsCondition(Vessel vessel, bool doUpdateLabel, out string label)
		{
			label = string.Empty;
			bool result = true;

			foreach(var sp in subRequirementParameters)
			{
				var state = sp.subRequirement.VesselMeetsCondition(vessel, context);

				if (state.requirementMet)
					sp.matchCounter++;

				result &= state.requirementMet;

				if (doUpdateLabel)
				{
					var vesselLabel = sp.subRequirement.GetLabel(vessel, context, state);
					if (!string.IsNullOrEmpty(vesselLabel))
					{
						if (string.IsNullOrEmpty(label))
							label = " - " + vesselLabel;
						else
							label += "\n - " + vesselLabel;
					}
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
			bool result = true;
			foreach(var subParameter in subRequirementParameters)
				result &= subParameter.VesselsMeetCondition(vessels);

			string statusLabel = Lib.Color($"{vesselsMeetingAllConditions}/{minVessels}",
					vesselsMeetingAllConditions >= minVessels ? Lib.Kolor.Green : Lib.Kolor.Red);

			SetStatusLabel(statusLabel);
			
			return result;
		}

		protected EvaluationContext CreateContext(double secondsSinceLastUpdate)
		{
			double now = Planetarium.GetUniversalTime();
			List<double> steps = new List<double>();

			double stepsNeeded = secondsSinceLastUpdate / requirement.max_step;
			double stepLength = secondsSinceLastUpdate / Math.Ceiling(stepsNeeded);

			for (int s = (int)stepsNeeded - 1; s > 1; s--)
				steps.Add(now - s * stepLength);
			steps.Add(now);

			return new EvaluationContext(steps, Utils.FetchWaypoint(Root, waypointIndex));
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if (ParameterCount == 0) return;
			if (state != ParameterState.Incomplete) return;

			if (subRequirementParameters.Count == 0)
				CreateSubParameters();

			var lastUpdateAge = Planetarium.GetUniversalTime() - lastUpdate;
			if (lastUpdateAge < 1.0) return;
			lastUpdate = Planetarium.GetUniversalTime();

			RemoveAllParameters(typeof(VesselStatusParameter));
			vessels.Clear();
			context = CreateContext(lastUpdateAge);

			foreach (var sp in subRequirementParameters)
				sp.ResetContext(context);

			foreach (Vessel vessel in FlightGlobals.Vessels)
			{
				if (!Utils.IsVessel(vessel))
					continue;

				if (!CouldBeCandidate(vessel))
					continue;

				vessels.Add(vessel);
			}

			int stepCount = context.steps.Count;
			for(int i = 0; i < stepCount; i++)
			{
				var now = context.steps[i];
				context.SetTime(now);

				int vesselsMeetingAllConditions = 0;
				bool doLabelUpdate = !hideChildren && i + 1 == stepCount;

				foreach (Vessel vessel in vessels)
				{
					string statusLabel;

					bool conditionMet = VesselMeetsCondition(vessel, doLabelUpdate, out statusLabel);
					if (conditionMet) vesselsMeetingAllConditions++;

					if (doLabelUpdate)
						AddParameter(new VesselStatusParameter(vessel, statusLabel, conditionMet));
				}

				bool allConditionsMet = vesselsMeetingAllConditions >= minVessels;
				allConditionsMet &= VesselsMeetCondition(vessels, vesselsMeetingAllConditions);

				if (durationParameter == null)
					SetState(allConditionsMet ? ParameterState.Complete : ParameterState.Incomplete);
				else
				{
					durationParameter.Update(allConditionsMet, now);
					SetState(durationParameter.State);
				}

				if (state == ParameterState.Complete)
					break;
			}

			ContractConfigurator.ContractConfigurator.OnParameterChange.Fire(this.Root, this);
		}

		private void RemoveAllParameters(Type type)
		{
			int c;
			do
			{
				c = ParameterCount;
				RemoveParameter(type);
			} while (c > ParameterCount);
		}
	}
}
