using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{	
	public class OuterBeltFactory : AbstractRadiationFieldFactory
	{
		public override ContractParameter Generate(Contract contract)
		{
			return new OuterBeltParameter(targetBody, crossings_min, crossings_max);
		}
	}

	public class OuterBeltParameter : AbstractRadiationFieldParameter
	{
		public OuterBeltParameter() : this(FlightGlobals.GetHomeBody()) { }
		public OuterBeltParameter(CelestialBody targetBody, int crossings_min = 1, int crossings_max = -1): base(targetBody, crossings_min, crossings_max) {}

		protected override string GetParameterTitle()
		{
			if (!string.IsNullOrEmpty(title)) return title;
			return "Find the outer radiation belt of " + targetBody.CleanDisplayName();
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			if (vessel.mainBody != targetBody)
			{
				return false;
			}

			bool condition = false;
			if (KERBALISM.API.HasOuterBelt(targetBody))
			{
				condition = KERBALISM.API.OuterBelt(vessel);
			}
			else
			{
				// no belt -> vessel needs to be where an outer belt would be expected
				condition = vessel.altitude < targetBody.Radius * 8
								  && vessel.altitude > targetBody.Radius * 5;
			}

			return UpdateCrossingCount(condition);
		}
	}

	public class RevealOuterBeltFactory : AbstractRevealRadiationFieldFactory
	{
		public override ContractBehaviour Generate(ConfiguredContract contract)
		{
			return new RevealOuterBeltBehaviour(targetBody, visible, requireCompletion);
		}
	}

	public class RevealOuterBeltBehaviour : AbstractRevealRadiationFieldBehaviour
	{
		public RevealOuterBeltBehaviour() : this(FlightGlobals.GetHomeBody(), true, false) { }
		public RevealOuterBeltBehaviour(CelestialBody targetBody, bool visible, bool requireCompletion)
			: base(targetBody, visible, requireCompletion) { }


		protected override void OnParameterStateChange(ContractParameter param)
		{
			base.OnParameterStateChange(param);

			if (requireCompletion || param.State != ParameterState.Complete)
			{
				return;
			}

			if (param.GetType() == typeof(OuterBeltParameter))
			{
				SetVisible();
			}
		}

		protected override void OnCompleted()
		{
			base.OnCompleted();
			SetVisible();
		}

		private void SetVisible()
		{
			bool alreadyVisible = KERBALISM.API.IsOuterBeltVisible(targetBody);
			KerbalismContracts.SetOuterBeltVisible(targetBody, visible);
			if (alreadyVisible || !visible)
				return;

			if (KERBALISM.API.HasOuterBelt(targetBody))
				KERBALISM.API.Message(targetBody.CleanDisplayName() + " outer radiation belt researched");
			else
				KERBALISM.API.Message(targetBody.CleanDisplayName() + " apparently has no outer radiation belt");
		}
	}
}
