using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{	
	public class MagnetopauseFactory : RadiationFieldFactory
	{
		public override ContractParameter Generate(Contract contract)
		{
			return new MagnetopauseParameter(targetBody, crossings);
		}
	}

	public class MagnetopauseParameter : RadiationFieldParameter
	{
		public MagnetopauseParameter() : this(FlightGlobals.GetHomeBody(), 1) { }
		public MagnetopauseParameter(CelestialBody targetBody, int crossings): base(targetBody, crossings) {}

		protected override string GetParameterTitle()
		{
			if (!string.IsNullOrEmpty(title)) return title;
			return "Find the magnetopause of " + targetBody.CleanDisplayName();
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			if (vessel.mainBody != targetBody)
			{
				return false;
			}

			bool condition = false;
			if (KERBALISM.API.HasMagnetopause(targetBody))
			{
				condition = KERBALISM.API.Magnetosphere(vessel);
			}
			else
			{
				// no belt -> vessel needs to be where an inner belt would be expected
				condition = vessel.altitude < targetBody.Radius * 14
								  && vessel.altitude > targetBody.Radius * 8;
			}

			if (condition != in_field) crossings--;
			in_field = condition;
			return crossings <= 0;
		}
	}

	public class RevealMagnetopauseFactory : RevealRadiationFieldFactory
	{
		public override ContractBehaviour Generate(ConfiguredContract contract)
		{
			return new RevealMagnetopauseBehaviour(targetBody, visible);
		}
	}

	public class RevealMagnetopauseBehaviour: RevealRadiationFieldBehaviour
	{
		public RevealMagnetopauseBehaviour(): this(FlightGlobals.GetHomeBody(), true) {}
		public RevealMagnetopauseBehaviour(CelestialBody targetBody, bool visible): base(targetBody, visible) {}

		protected override void OnCompleted()
		{
			base.OnCompleted();

			KerbalismContracts.SetMagnetopauseVisible(targetBody, visible);
			if (KERBALISM.API.HasMagnetopause(targetBody))
				KERBALISM.API.Message(targetBody.CleanDisplayName() + " magnetosphere researched");
			else
				KERBALISM.API.Message(targetBody.CleanDisplayName() + " apparently has no magnetosphere");
		}
	}
}
