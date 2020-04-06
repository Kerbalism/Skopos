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

		/// <summary> the number of vessels meeting this condition </summary>
		internal int matchCounter;
		internal EvaluationContext context;

		public SubRequirementParameter() { }

		public SubRequirementParameter(SubRequirement subRequirement)
		{
			this.subRequirement = subRequirement;
			this.requirementId = subRequirement.parent.name;
			this.subRequirementType = subRequirement.type;
			optional = true;
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

			var requirement = Configuration.Requirement(requirementId);
			subRequirement = requirement.SubRequirements.Find(sr => sr.type == subRequirementType);
		}

		protected override void OnSave(ConfigNode node)
		{
			base.OnSave(node);

			node.AddValue("requirementId", subRequirement.parent.name);
			node.AddValue("subRequirementType", subRequirement.type);
		}

		protected override string GetTitle()
		{
			return subRequirement.GetTitle(context);
		}

		internal bool VesselsMeetCondition(List<Vessel> vessels, out string label)
		{
			completed = subRequirement.VesselsMeetCondition(vessels, matchCounter, context, out label);

			if (completed)
				SetComplete();
			else
				SetIncomplete();

			return completed;
		}

		internal void ResetContext(EvaluationContext context)
		{
			this.context = context;
			matchCounter = 0;
		}
	}
}
