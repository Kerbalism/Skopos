using System;
using Contracts;
using ContractConfigurator;
using System.Collections.Generic;

namespace KerbalismContracts
{
	public class SubRequirementParameter : ContractParameter
	{
		public SubRequirement subRequirement { get; private set; }
		private bool completed;
		private string requirementId;
		private string subRequirementType;
		private int waypoint_index;

		/// <summary> the number of vessels meeting this condition </summary>
		internal int matchCounter;
		internal RequirementContext context;

		public SubRequirementParameter() { }

		public SubRequirementParameter(SubRequirement subRequirement, int waypoint_index)
		{
			this.subRequirement = subRequirement;
			this.waypoint_index = waypoint_index;
			this.requirementId = subRequirement.parent.name;
			this.subRequirementType = subRequirement.type;
			optional = true;
		}

		public RequirementContext GetContext()
		{
			if (context == null)
				context = new RequirementContext(Root, waypoint_index);

			return context;
		}

		protected override string GetHashString()
		{
			return (Root != null ? (Root.MissionSeed.ToString() + Root.DateAccepted.ToString()) : "") + ID + "/sr" + subRequirementType;
		}

		protected override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			requirementId = ConfigNodeUtil.ParseValue<string>(node, "requirementId", "");
			subRequirementType = ConfigNodeUtil.ParseValue<string>(node, "subRequirementType", "");
			waypoint_index = ConfigNodeUtil.ParseValue<int>(node, "waypoint_index", 0);

			var requirement = Configuration.Requirement(requirementId);
			subRequirement = requirement.SubRequirements.Find(sr => sr.type == subRequirementType);
		}

		protected override void OnSave(ConfigNode node)
		{
			base.OnSave(node);

			node.AddValue("requirementId", subRequirement.parent.name);
			node.AddValue("subRequirementType", subRequirement.type);
			node.AddValue("waypoint_index", waypoint_index);
		}

		protected override string GetTitle()
		{
			return subRequirement.GetTitle(GetContext());
		}

		internal bool VesselsMeetCondition(List<Vessel> vessels, int timesConditionMet, out string label)
		{
			completed = subRequirement.VesselsMeetCondition(vessels, timesConditionMet, GetContext(), out label);

			if (completed)
			{
				Utils.LogDebug($"{subRequirement.type} set complete");
				SetComplete();
			}
			else
			{
				Utils.LogDebug($"{subRequirement.type} Set incomplete");
				SetIncomplete();
			}

			return completed;
		}
	}
}
