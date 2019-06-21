using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{
	public class CanTransmitScienceFactory : ParameterFactory
	{
		public override ContractParameter Generate(Contract contract)
		{
			return new CanTransmitScience();
		}
	}

	public class CanTransmitScience : VesselParameter
	{
		private float lastUpdate = 0.0f;
		private const float UPDATE_FREQUENCY = 0.25f;

		protected override string GetParameterTitle()
		{
			if (!string.IsNullOrEmpty(title)) return title;
			var sun = Lib.GetHomeSun();
			return "Can transmit science";
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			return KERBALISM.API.VesselConnectionRate(vessel) > double.Epsilon;
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if (UnityEngine.Time.fixedTime - lastUpdate > UPDATE_FREQUENCY)
			{
				lastUpdate = UnityEngine.Time.fixedTime;
				CheckVessel(FlightGlobals.ActiveVessel);
			}
		}

	}

}
