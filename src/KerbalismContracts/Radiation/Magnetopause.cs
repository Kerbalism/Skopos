using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{	
	public class MagnetopauseFactory : AbstractRadiationFieldFactory
	{
		public override ContractParameter Generate(Contract contract)
		{
			return new MagnetopauseParameter(targetBody, crossings_min, crossings_max);
		}
	}

	public class MagnetopauseParameter : AbstractRadiationFieldParameter
	{
		public MagnetopauseParameter() : this(FlightGlobals.GetHomeBody()) { }
		public MagnetopauseParameter(CelestialBody targetBody, int crossings_min = 1, int crossings_max = -1): base(targetBody, crossings_min, crossings_max) {}

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
				// no sphere -> vessel needs to be where the pause could be
				condition = vessel.altitude < targetBody.Radius * 15
								  && vessel.altitude > targetBody.Radius * 10;
			}

			return UpdateCrossingCount(condition);
		}
	}

	public class RevealMagnetopauseFactory : AbstractRevealRadiationFieldFactory
	{
		public override ContractBehaviour Generate(ConfiguredContract contract)
		{
			return new RevealMagnetopauseBehaviour(targetBody, visible, requireCompletion);
		}
	}

	public class RevealMagnetopauseBehaviour: AbstractRevealRadiationFieldBehaviour
	{
		public RevealMagnetopauseBehaviour(): this(FlightGlobals.GetHomeBody(), true, false) {}
		public RevealMagnetopauseBehaviour(CelestialBody targetBody, bool visible, bool requireCompletion)
			: base(targetBody, visible, requireCompletion) {}

		protected override void OnCompleted()
		{
			base.OnCompleted();
			SetVisible();
		}

		protected override void OnParameterStateChange(ContractParameter param)
		{
			base.OnParameterStateChange(param);

			if (requireCompletion || param.State != ParameterState.Complete)
			{
				return;
			}

			if (param.GetType() == typeof(MagnetopauseParameter))
			{
				SetVisible();
			}
		}

		private void SetVisible() {
			bool alreadyVisible = KERBALISM.API.IsMagnetopauseVisible(targetBody);
			KerbalismContracts.SetMagnetopauseVisible(targetBody, visible);
			if (alreadyVisible || !visible)
				return;

			if (KERBALISM.API.HasMagnetopause(targetBody))
				KERBALISM.API.Message(targetBody.CleanDisplayName() + " magnetosphere researched");
			else
				KERBALISM.API.Message(targetBody.CleanDisplayName() + " apparently has no magnetosphere");
		}
	}
}
