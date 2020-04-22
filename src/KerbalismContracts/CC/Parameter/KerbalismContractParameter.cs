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
	public class Arguments
	{
		public ContractConfigurator.Duration duration;
		public ContractConfigurator.Duration allowedDowntime;
		public ContractConfigurator.Duration waitDuration;
		public string requirementId;
		public int minVessels;
		public int waypointIndex;
		public bool allowReset;
		public string title;
		public bool hideChildren;
		public bool allowUnpowered;
		public DurationParameter.DurationType durationType;

		public Arguments(ConfigNode configNode)
		{
			duration = ConfigNodeUtil.ParseValue(configNode, "duration", new ContractConfigurator.Duration(0.0));
			allowedDowntime = ConfigNodeUtil.ParseValue(configNode, "allowedDowntime", new ContractConfigurator.Duration(0.0));
			waitDuration = ConfigNodeUtil.ParseValue(configNode, "waitDuration", new ContractConfigurator.Duration(0.0));
			requirementId = ConfigNodeUtil.ParseValue(configNode, "id", "");
			minVessels = ConfigNodeUtil.ParseValue(configNode, "minVessels", 1);
			waypointIndex = ConfigNodeUtil.ParseValue(configNode, "waypointIndex", 0);
			allowReset = ConfigNodeUtil.ParseValue(configNode, "allowReset", true);
			title = ConfigNodeUtil.ParseValue(configNode, "title", "");
			hideChildren = ConfigNodeUtil.ParseValue(configNode, "hideChildren", false);
			durationType = Lib.ConfigEnum(configNode, "durationType", DurationParameter.DurationType.countdown);
			allowUnpowered = ConfigNodeUtil.ParseValue(configNode, "allowUnpowered", false);
		}
	}

	/// <summary> Parameter for global radiation field status: how often has a radiation field been penetrated </summary>
	public class KerbalismContractFactory : ParameterFactory
	{
		protected Arguments arguments;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);
			arguments = new Arguments(configNode);
			return valid;
		}

		public override ContractParameter Generate(Contract contract)
		{
			var requirement = Configuration.Requirement(arguments.requirementId);
			if (requirement == null)
			{
				Utils.Log($"There is no KerbalismContractRequirement with name '{arguments.requirementId}'.", LogLevel.Error);
				return null;
			}

			var result = new KerbalismContractParameter(arguments, targetBody);

			if (requirement.NeedsWaypoint())
			{
				if(Utils.FetchWaypoint(contract, arguments.waypointIndex) == null)
				{
					Utils.Log($"There is no waypoint (index {arguments.waypointIndex}) in the contract, but requirement '{arguments.requirementId}' needs one.", LogLevel.Error);
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
		protected double waitDuration;
		protected int minVessels;
		protected int waypointIndex;
		protected bool allowReset;
		protected DurationParameter.DurationType durationType;
		protected bool allowUnpowered;

		protected DurationParameter durationParameter;
		protected readonly List<SubRequirementParameter> subRequirementParameters = new List<SubRequirementParameter>();
		protected readonly List<VesselStatusParameter> vesselStatusParameters = new List<VesselStatusParameter>();

		private double lastUpdate = 0.0f;

		private readonly List<Vessel> vessels = new List<Vessel>();
		private KerbalismContractRequirement requirement;
		private string statusLabel = string.Empty;
		internal EvaluationContext context;

		public KerbalismContractParameter() { }

		public KerbalismContractParameter(Arguments arguments, CelestialBody targetBody)
		{
			this.requirementId = arguments.requirementId;
			this.duration = arguments.duration.Value;
			this.allowedDowntime = arguments.allowedDowntime.Value;
			this.waitDuration = arguments.waitDuration.Value;
			this.allowReset = arguments.allowReset;
			this.minVessels = arguments.minVessels;
			this.waypointIndex = arguments.waypointIndex;
			this.title = arguments.title;
			this.hideChildren = arguments.hideChildren;
			this.durationType = arguments.durationType;
			this.allowUnpowered = arguments.allowUnpowered;
			this.targetBody = targetBody;

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

			int count = ParameterCount;
			for(int i = count - 1; i >= 0; i--)
			{
				var p = GetParameter(i);
				if(p is SubRequirementParameter srp)
				{
					if(srp.subRequirement != null)
						subRequirementParameters.Add(srp);
					else
						RemoveParameter(i); // this can happen when contract definitions change
				}

				if (p is DurationParameter dp)
					durationParameter = dp;

				if (p is VesselStatusParameter vsp)
					vesselStatusParameters.Add(vsp);
			}

			if (subRequirementParameters.Count > 0)
				return;

			foreach (var req in Configuration.Requirement(requirementId).SubRequirements)
			{
				var sub = new SubRequirementParameter(req);
				subRequirementParameters.Add(sub);
				AddParameter(sub);
			}

			if (duration > 0 && durationParameter == null)
			{
				durationParameter = new DurationParameter(duration, allowedDowntime, allowReset, waitDuration, durationType);
				AddParameter(durationParameter);
			}
		}

		protected override string GetParameterTitle()
		{
			// make sure to never return an empty string here (the contract window won't show the parameter if the title was empty once)
			string result = title;
			if(string.IsNullOrEmpty(result)) result = Configuration.Requirement(requirementId).title;
			if (string.IsNullOrEmpty(result)) result = requirementId;

			if (!string.IsNullOrEmpty(statusLabel))
				result += ": " + statusLabel;

			return result;
		}

		protected void SetStatusLabel(string newLabel)
		{
			if(newLabel != statusLabel)
			{
				statusLabel = newLabel;
				GetTitle();
			}
		}

		protected override void OnParameterSave(ConfigNode node)
		{
			node.AddValue("requirementId", requirementId);
			node.AddValue("duration", duration);
			node.AddValue("allowedDowntime", allowedDowntime);
			node.AddValue("waitDuration", waitDuration);
			node.AddValue("allowReset", allowReset);
			node.AddValue("minVessels", minVessels);
			node.AddValue("waypointIndex", waypointIndex);
			node.AddValue("lastUpdate", lastUpdate);
			node.AddValue("durationType", durationType);
			node.AddValue("allowUnpowered", allowUnpowered);
			if (targetBody != null)
				node.AddValue("targetBody", targetBody.name);
		}

		protected override void OnParameterLoad(ConfigNode node)
		{
			requirementId = ConfigNodeUtil.ParseValue(node, "requirementId", "");
			duration = ConfigNodeUtil.ParseValue(node, "duration", 0.0);
			allowedDowntime = ConfigNodeUtil.ParseValue(node, "allowedDowntime", 0.0);
			waitDuration = ConfigNodeUtil.ParseValue(node, "waitDuration", 0.0);
			allowReset = ConfigNodeUtil.ParseValue(node, "allowReset", true);
			minVessels = ConfigNodeUtil.ParseValue(node, "minVessels", 1);
			waypointIndex = ConfigNodeUtil.ParseValue(node, "waypointIndex", 0);
			lastUpdate = ConfigNodeUtil.ParseValue(node, "lastUpdate", 0.0);
			requirement = Configuration.Requirement(requirementId);
			durationType = Lib.ConfigEnum(node, "durationType", DurationParameter.DurationType.countdown);
			allowUnpowered = ConfigNodeUtil.ParseValue(node, "allowUnpowered", false);
			targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody", (CelestialBody)null);
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

			if (!allowUnpowered)
			{
				var powered = API.ResourceAmount(vessel, "ElectricCharge") > 0.01;
				if(!powered)
				{
					if(doUpdateLabel)
						label = Lib.Color("No Electricity", Lib.Kolor.Red);
					return false;
				}
			}

			foreach(var sp in subRequirementParameters)
			{
				if (sp.subRequirement == null)
					continue;

				var state = sp.subRequirement.VesselMeetsCondition(vessel, context);
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
		private bool VesselsMeetCondition(List<Vessel> vessels)
		{
			bool result = vessels.Count >= minVessels;
			string statusLabel = string.Empty;

			foreach (var subParameter in subRequirementParameters)
			{
				string label;
				result &= subParameter.VesselsMeetCondition(vessels, out label);
				if (!string.IsNullOrEmpty(label))
				{
					if (!string.IsNullOrEmpty(statusLabel))
						statusLabel += " ";
					statusLabel += label;
				}
			}

			if(minVessels > 1)
			{
				statusLabel = Lib.Color($"{vessels.Count}/{minVessels}",
						vessels.Count >= minVessels ? Lib.Kolor.Green : Lib.Kolor.Red)
					+ " " + statusLabel;
			}

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

			return new EvaluationContext(steps, targetBody, Utils.FetchWaypoint(Root, waypointIndex));
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if (ParameterCount == 0) return;
			if (state != ParameterState.Incomplete) return;

			if (!KerbalismContractsMain.KerbalismInitialized) return;

			if (lastUpdate == 0)
			{
				lastUpdate = Planetarium.GetUniversalTime();
				return;
			}

			bool childParameterChanged = false;
			if (subRequirementParameters.Count == 0)
			{
				childParameterChanged = true;
				CreateSubParameters();
			}

			var lastUpdateAge = Planetarium.GetUniversalTime() - lastUpdate;
			if (lastUpdateAge < 1.0) return;
			lastUpdate = Planetarium.GetUniversalTime();

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

			if (!hideChildren)
			{
				foreach (var vsp in vesselStatusParameters)
					vsp.obsolete = true;
			}

			List<Vessel> vesselsMeetingCondition = new List<Vessel>();

			int stepCount = context.steps.Count;
			for(int i = 0; i < stepCount; i++)
			{
				var now = context.steps[i];
				context.SetTime(now);

				vesselsMeetingCondition.Clear();
				bool doLabelUpdate = !hideChildren && i + 1 == stepCount;

				foreach (Vessel vessel in vessels)
				{
					// Note considering early termination for performance gains:
					// If we already know that we have enough vessels to satisfy
					// our requirement, and if we don't have to update labels,
					// then we don' tneed to test all vessels. However, this
					// doesn't work when we have complex requirements that need
					// to consider multiple vessels at once (like body surface
					// observation percentage). We could change the implementation
					// to continuously integrate one vessel at a time into the
					// multi-vessel test and abort as soon as that one is satisfied,
					// but if that also calculates a number visible to the user
					// (like percentage of surface observed), that number would
					// be wrong. So we need to test all vessels, all the time.

					// if (!doLabelUpdate && vesselsMeetingCondition.Count >= minVessels)
					//	break;

					string statusLabel;

					bool conditionMet = VesselMeetsCondition(vessel, doLabelUpdate, out statusLabel);
					if (conditionMet) vesselsMeetingCondition.Add(vessel);

					if (doLabelUpdate)
						childParameterChanged |= UpdateVesselStatus(vessel, statusLabel, conditionMet);
				}

				bool allConditionsMet = vesselsMeetingCondition.Count >= minVessels;
				allConditionsMet &= VesselsMeetCondition(vesselsMeetingCondition);

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

			childParameterChanged |= RemoveObsoleteVesselStatusParameters();
			if(childParameterChanged)
				ContractConfigurator.ContractConfigurator.OnParameterChange.Fire(this.Root, this);
		}

		private bool RemoveObsoleteVesselStatusParameters()
		{
			bool parameterRemoved = false;
			var count = vesselStatusParameters.Count;
			for (int i = count - 1; i >= 0; i--)
			{
				var vsp = vesselStatusParameters[i];
				if (vsp.obsolete)
				{
					RemoveParameter(vsp);
					vesselStatusParameters.RemoveAt(i);
					parameterRemoved = true;
				}
			}
			return parameterRemoved;
		}

		private bool UpdateVesselStatus(Vessel vessel, string statusLabel, bool conditionMet)
		{
			foreach (VesselStatusParameter vsp in vesselStatusParameters)
			{
				if (vsp.vessel == vessel)
				{
					vsp.Update(statusLabel, conditionMet);
					return false;
				}
			}

			VesselStatusParameter p = new VesselStatusParameter(vessel, statusLabel, conditionMet);
			vesselStatusParameters.Add(p);
			AddParameter(p);
			return true;
		}
	}
}
