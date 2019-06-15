using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{
	public class InnerBeltFactory : ParameterFactory
	{
		public override ContractParameter Generate(Contract contract)
		{
			return new InnerBelt();
		}
	}

	public class InnerBelt : VesselParameter
	{
		protected override string GetParameterTitle()
		{
			if (!string.IsNullOrEmpty(title)) return title;
			return "Be in the inner radiation belt";
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			return KERBALISM.API.InnerBelt(vessel);
		}
	}

}
