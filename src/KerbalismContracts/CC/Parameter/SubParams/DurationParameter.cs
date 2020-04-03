using System;
using Contracts;
using ContractConfigurator;
using KSP.Localization;

namespace KerbalismContracts
{
	public class DurationParameter : ContractParameter
	{
		internal double duration;
		internal double endTime;

		public DurationParameter() { }

		public DurationParameter(double duration)
		{
			this.duration = duration;
			this.Optional = false;
		}

		protected override string GetHashString()
		{
			return (Root != null ? (Root.MissionSeed.ToString() + Root.DateAccepted.ToString()) : "") + ID + "/duration";
		}

		protected override string GetTitle()
		{
			if (endTime == 0)
				return Localizer.Format("Duration: <<1>>", DurationUtil.StringValue(duration));
			double remaining = endTime - Planetarium.GetUniversalTime();
			if (remaining > 0)
				return Localizer.Format("Remaining: <<1>>", DurationUtil.StringValue(remaining));
			return "Done!";
		}

		protected override void OnSave(ConfigNode node)
		{
			base.OnSave(node);

			node.AddValue("duration", duration);
			node.AddValue("endTime", endTime);
		}

		protected override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			duration = ConfigNodeUtil.ParseValue<double>(node, "duration", 0);
			endTime = ConfigNodeUtil.ParseValue<double>(node, "endTime", 0);
		}

		internal void ResetTime()
		{
			Utils.LogDebug($"set incomplete");
			SetIncomplete();
			endTime = 0;
		}

		internal void UpdateWhileConditionMet()
		{
			if (endTime == 0)
			{
				endTime = Planetarium.GetUniversalTime() + duration;
			}

			OnStateChange.Fire(this, state);
		}
	}
}
