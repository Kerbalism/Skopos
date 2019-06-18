using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{	
	public class InnerBeltFactory : AbstractRadiationFieldFactory
	{
		public override ContractParameter Generate(Contract contract)
		{
			return new InnerBeltParameter(targetBody, crossings_min, crossings_max);
		}
	}

	public class InnerBeltParameter : AbstractRadiationFieldParameter
	{
		public InnerBeltParameter() : this(FlightGlobals.GetHomeBody()) { }
		public InnerBeltParameter(CelestialBody targetBody, int crossings_min = 1, int crossings_max = -1): base(targetBody, crossings_min, crossings_max) {}

		protected override string GetParameterTitle()
		{
			if (!string.IsNullOrEmpty(title)) return title;
			return "Find the inner radiation belt of " + targetBody.CleanDisplayName();
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			if (vessel.mainBody != targetBody)
			{
				return false;
			}

			bool condition = false;
			if (KERBALISM.API.HasInnerBelt(targetBody))
			{
				condition = KERBALISM.API.InnerBelt(vessel);
			}
			else
			{
				// no belt -> vessel needs to be where an inner belt would be expected
				condition = vessel.altitude < targetBody.Radius * 4
						  && vessel.altitude > targetBody.Radius
							 && Math.Abs(vessel.latitude) < 30;
			}

			return UpdateCrossingCount(condition);
		}
	}

	public class RevealInnerBeltFactory : AbstractRevealRadiationFieldFactory
	{
		public override ContractBehaviour Generate(ConfiguredContract contract)
		{
			return new RevealInnerBeltBehaviour(targetBody, visible, requireCompletion);
		}
	}

	public class RevealInnerBeltBehaviour: AbstractRevealRadiationFieldBehaviour
	{
		public RevealInnerBeltBehaviour(): this(FlightGlobals.GetHomeBody(), true, false) {}
		public RevealInnerBeltBehaviour(CelestialBody targetBody, bool visible, bool requireCompletion)
			: base(targetBody, visible, requireCompletion) {}


		protected override void OnParameterStateChange(ContractParameter param)
		{
			base.OnParameterStateChange(param);

			if (requireCompletion || param.State != ParameterState.Complete)
			{
				return;
			}

			if (param.GetType() == typeof(InnerBeltParameter))
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
			bool alreadyVisible = KERBALISM.API.IsInnerBeltVisible(targetBody);
			KerbalismContracts.SetInnerBeltVisible(targetBody, visible);
			if (alreadyVisible || !visible)
				return;

			if (KERBALISM.API.HasInnerBelt(targetBody))
				KERBALISM.API.Message(targetBody.CleanDisplayName() + " inner radiation belt researched");
			else
				KERBALISM.API.Message(targetBody.CleanDisplayName() + " apparently has no inner radiation belt");
		}
	}
}
