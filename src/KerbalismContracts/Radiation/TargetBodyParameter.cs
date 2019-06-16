using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{
	public abstract class TargetBodyParameter : VesselParameter
	{
		protected override void OnParameterLoad(ConfigNode node)
		{
			base.OnParameterLoad(node);
			targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody", FlightGlobals.GetHomeBody());
		}

		protected override void OnParameterSave(ConfigNode node)
		{
			base.OnParameterSave(node);
			node.AddValue("targetBody", targetBody.name);
		}
	}
}
