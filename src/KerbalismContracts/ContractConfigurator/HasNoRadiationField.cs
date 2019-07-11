using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{
	public class HasNoRadiationFieldFactory : HasRadiationFieldFactory
	{
		public override ContractParameter Generate(Contract contract)
		{
			if (!ValidateTargetBody())
			{
				return null;
			}

			return new HasNoRadiationFieldParameter(field, targetBody, title);
		}
	}

	public class HasNoRadiationFieldParameter : HasRadiationFieldParameter
	{
		public HasNoRadiationFieldParameter()
			: base()
		{ }

		public HasNoRadiationFieldParameter(RadiationField field, CelestialBody targetBody, string title)
			: base(field, targetBody, title)
		{ }

		protected override string GetParameterTitle()
		{
			if (!string.IsNullOrEmpty(title)) return title;

			string bodyName = targetBody != null ? targetBody.CleanDisplayName() : "a body";
			return bodyName + " has no " + RadiationFieldParameter.FieldName(field);
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			if (vessel == null) return false;

			LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);

			if (targetBody != null && vessel.mainBody != targetBody) return false;

			return !HasField(vessel);
		}
	}
}
