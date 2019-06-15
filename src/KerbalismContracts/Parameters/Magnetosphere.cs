using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{
	public class MagnetosphereFactory : ParameterFactory
	{
		public override ContractParameter Generate(Contract contract)
		{
			return new Magnetosphere();
		}
	}

	public class Magnetosphere : VesselParameter
	{
		protected override string GetParameterTitle()
		{
			if (!string.IsNullOrEmpty(title)) return title;
			return "Be in the magnetosphere";
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			return KERBALISM.API.Magnetosphere(vessel);
		}
	}

}
