using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace KerbalismContracts
{
	/// <summary> Requirement: test for radiation fields on a body </summary>
	public class HasRadiationField : ContractRequirement
	{
		protected RadiationFieldType field;

		public override bool LoadFromConfig(ConfigNode configNode)
		{
			// Load base class
			bool valid = base.LoadFromConfig(configNode);
			valid &= ConfigNodeUtil.ParseValue<RadiationFieldType>(configNode, "field", x => field = x, this, RadiationFieldType.ANY, ValidateField);
			return valid;
		}

		public override void OnLoad(ConfigNode configNode)
		{
			ConfigNodeUtil.ParseValue<RadiationFieldType>(configNode, "field", x => field = x, this, RadiationFieldType.ANY, ValidateField);
		}

		public override void OnSave(ConfigNode configNode)
		{
			configNode.AddValue("field", field);
		}

		protected override string RequirementText()
		{
			return targetBody.name + " has " + RadiationField.Name(field);
		}

		public override bool RequirementMet(ConfiguredContract contract)
		{
			if(contract.targetBody == null)
				return false;

			switch(field)
			{
				case RadiationFieldType.INNER_BELT:
					return KerbalismContracts.Instance.BodyData(contract.targetBody).has_inner;
				case RadiationFieldType.OUTER_BELT:
					return KerbalismContracts.Instance.BodyData(contract.targetBody).has_outer;
				case RadiationFieldType.MAGNETOPAUSE:
					return KerbalismContracts.Instance.BodyData(contract.targetBody).has_pause;
				case RadiationFieldType.ANY:
					return KerbalismContracts.Instance.BodyData(contract.targetBody).has_inner
						|| KerbalismContracts.Instance.BodyData(contract.targetBody).has_outer
						|| KerbalismContracts.Instance.BodyData(contract.targetBody).has_pause;
			}

			return false;
		}

		protected bool ValidateField(RadiationFieldType f)
		{
			if (f == RadiationFieldType.UNDEFINED)
			{
				Utils.Log("Missing field. You must specify field = INNER_BELT, OUTER_BELT, MAGNETOPAUSE or ANY.", LogLevel.Error);
				return false;
			}
			return true;
		}
	}
}
