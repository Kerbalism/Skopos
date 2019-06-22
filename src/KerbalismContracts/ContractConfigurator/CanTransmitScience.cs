using Contracts;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{
	public class CanTransmitScienceFactory : ParameterFactory
	{
		public override ContractParameter Generate(Contract contract)
		{
			return new CanTransmitScienceParameter(title);
		}
	}

	public class CanTransmitScienceParameter : VesselParameter
	{
		public CanTransmitScienceParameter(): base(null) {}
		public CanTransmitScienceParameter(string title): base(title) {}

		protected override string GetParameterTitle()
		{
			if (!string.IsNullOrEmpty(title)) return title;
			var sun = Lib.GetHomeSun();
			return "Can transmit science data";
		}

		protected override void OnRegister()
		{
			base.OnRegister();
			KERBALISM.API.OnTransmitStateChanged.Add(RunCheck);
		}

		protected override void OnUnregister()
		{
			base.OnUnregister();
			KERBALISM.API.OnTransmitStateChanged.Remove(RunCheck);
		}

		private void RunCheck(Vessel v, string subject_id, bool can_transmit)
		{
			TransmissionStateTracker.Update(v, subject_id, can_transmit);
			CheckVessel(v);
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			return TransmissionStateTracker.CanTransmit(vessel);
		}
	}

}
