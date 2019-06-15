using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{
	public class OutsideHeliopauseFactory : ParameterFactory
	{
		public override ContractParameter Generate(Contract contract)
		{
			return new OutsideHeliopause();
		}
	}

	public class OutsideHeliopause : VesselParameter
	{
		protected override string GetParameterTitle()
		{
			if (!string.IsNullOrEmpty(title)) return title;
			var sun = Lib.GetHomeSun();
			return "Leave the heliosphere of " + sun.bodyName;
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			if (vessel.mainBody.flightGlobalsIndex != Lib.GetHomeSun().flightGlobalsIndex)
				return false;

			return !KERBALISM.API.Magnetosphere(vessel);
		}
	}

}
