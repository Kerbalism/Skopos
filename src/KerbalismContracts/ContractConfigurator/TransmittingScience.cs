using Contracts;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{
	public class TransmittingScienceFactory : ParameterFactory
	{
		protected string subject_id = string.Empty;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);
			valid &= ConfigNodeUtil.ParseValue<string>(configNode, "subject_id", x => subject_id = x, this, string.Empty);
			return valid;
		}

		public override ContractParameter Generate(Contract contract)
		{
			return new TransmittingScienceParameter(title, subject_id);
		}
	}

	public class TransmittingScienceParameter : VesselParameter
	{
		protected string subject_id = string.Empty;

		public TransmittingScienceParameter() : base(null) { }
		public TransmittingScienceParameter(string title, string subject_id) : base(title)
		{
			this.subject_id = subject_id;
		}

		protected override string GetParameterTitle()
		{
			if (!string.IsNullOrEmpty(title)) return title;
			var sun = Lib.GetHomeSun();
			return "Transmitting science data";
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

		private void RunCheck(Vessel v, string subj_id, bool can_transmit)
		{
			TransmissionStateTracker.Update(v, subj_id, can_transmit);
			CheckVessel(v);
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			if(string.IsNullOrEmpty(subject_id)) {
				return !string.IsNullOrEmpty(TransmissionStateTracker.Transmitting(vessel));
			}

			return TransmissionStateTracker.Transmitting(vessel) == subject_id;
		}
	}
}
