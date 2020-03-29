using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace KerbalismContracts
{
	public class ShowRadiationFieldFactory : BehaviourFactory
	{
		protected RadiationFieldType field;
		protected bool set_visible = false;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue<RadiationFieldType>(configNode, "field", x => field = x, this, RadiationFieldType.UNDEFINED, ValidateField);
			valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "set_visible", x => set_visible = x, this, true);

			return valid;
		}

		private bool ValidateField(RadiationFieldType f)
		{
			if (f == RadiationFieldType.UNDEFINED)
			{
				LoggingUtil.LogError(this, "Missing field. You must specify field = INNER_BELT, OUTER_BELT, MAGNETOPAUSE or ANY.");
				return false;
			}
			return true;
		}

		public override ContractBehaviour Generate(ConfiguredContract contract)
		{
			return new ShowRadiationField(targetBody, field, set_visible);
		}
	}

	public class ShowRadiationField : ContractBehaviour
	{
		protected CelestialBody targetBody;
		protected RadiationFieldType field;
		protected bool set_visible;

		public ShowRadiationField() : base() { }

		public ShowRadiationField(CelestialBody targetBody, RadiationFieldType field, bool set_visible)
		{
			this.targetBody = targetBody;
			this.field = field;
			this.set_visible = set_visible;
		}

		protected override void OnLoad(ConfigNode configNode)
		{
			base.OnLoad(configNode);

			targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(configNode, "targetBody");
			field = ConfigNodeUtil.ParseValue<RadiationFieldType>(configNode, "field", RadiationFieldType.UNDEFINED);
			set_visible = ConfigNodeUtil.ParseValue<bool>(configNode, "set_visible", true);
		}

		protected override void OnSave(ConfigNode configNode)
		{
			base.OnSave(configNode);

			configNode.AddValue("targetBody", targetBody.name);
			configNode.AddValue("field", field);
			configNode.AddValue("set_visible", set_visible);
		}

		protected override void OnCompleted()
		{
			base.OnCompleted();
			DoShow();
		}

		protected override void OnParameterStateChange(ContractParameter param)
		{
			base.OnParameterStateChange(param);

			var matchingParameter = GetMatchingParameter(param);
			if (matchingParameter == null)
				return;

			if (matchingParameter.State == ParameterState.Complete)
				DoShow();
		}

		protected RadiationFieldParameter GetMatchingParameter(ContractParameter param)
		{
			var radiationFieldParameter = param as RadiationFieldParameter;
			if (radiationFieldParameter == null)
			{
				foreach (ContractParameter child in param.GetChildren())
				{
					var result = GetMatchingParameter(child);
					if (result != null) return result;
				}
			}
			else if (radiationFieldParameter.field == field || field == RadiationFieldType.ANY)
			{
				return radiationFieldParameter;
			}
			return null;
		}

		protected void DoShow()
		{
			if (targetBody == null) return;

			switch (field)
			{
				case RadiationFieldType.INNER_BELT:
					KerbalismContracts.SetInnerBeltVisible(targetBody, set_visible);
					break;

				case RadiationFieldType.OUTER_BELT:
					KerbalismContracts.SetOuterBeltVisible(targetBody, set_visible);
					break;

				case RadiationFieldType.MAGNETOPAUSE:
					KerbalismContracts.SetMagnetopauseVisible(targetBody, set_visible);
					break;

				case RadiationFieldType.ANY:
					KerbalismContracts.SetInnerBeltVisible(targetBody, set_visible);
					KerbalismContracts.SetOuterBeltVisible(targetBody, set_visible);
					KerbalismContracts.SetMagnetopauseVisible(targetBody, set_visible);
					break;
			}
		}
	}
}
