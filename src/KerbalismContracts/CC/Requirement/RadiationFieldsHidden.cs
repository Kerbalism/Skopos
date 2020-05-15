using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace KerbalismContracts
{
	/// <summary> Requirement: test if radiation belts are hidden by KerCon </summary>
	public class RadiationFieldsHidden : ContractRequirement
	{
		protected override string RequirementText()
		{
			return Localizer.Format("#KerCon_RadiationFields_Hidden");
		}

		public override bool RequirementMet(ConfiguredContract contract)
		{
			return Configuration.HideRadiationBelts;
		}

		public override void OnSave(ConfigNode configNode) { }

		public override void OnLoad(ConfigNode configNode) { }
	}
}
