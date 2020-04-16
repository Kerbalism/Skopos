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
		internal string requirementId;
		internal string subRequirementType;

		internal EvaluationContext context;

		private readonly TitleTracker titleTracker;
		private string lastTitle;

		public SubRequirementParameter()
		{
			titleTracker = new TitleTracker(this);
		}

		public SubRequirementParameter(SubRequirement subRequirement)
		{
			titleTracker = new TitleTracker(this);

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
			string result = subRequirement.GetTitle(context);
			titleTracker.Add(result);
			if(lastTitle != result && Root != null && (Root.ContractState == Contract.State.Active || Root.ContractState == Contract.State.Failed))
			{
				titleTracker.UpdateContractWindow(result);
				lastTitle = result;
			}
			return result;
		}

		internal bool VesselsMeetCondition(List<Vessel> vessels, out string statusLabel)
		{
			completed = subRequirement.VesselsMeetCondition(vessels, context, out statusLabel);

			if (completed)
				SetComplete();
			else
				SetIncomplete();

			return completed;
		}

		internal void ResetContext(EvaluationContext context)
		{
			this.context = context;
		}
	}
}
