using System;
using Contracts;
using ContractConfigurator;
using KSP.Localization;
using KERBALISM;

namespace KerbalismContracts
{
	public class DurationParameter : ContractParameter
	{
		internal double duration;
		internal double allowed_downtime;

		// internal state
		internal double doneAfter;
		internal double downAfter;

		public DurationParameter() { }

		public DurationParameter(double duration, double allowed_downtime)
		{
			this.duration = duration;
			this.allowed_downtime = allowed_downtime;
		}

		private void ResetTimer()
		{
			doneAfter = downAfter = 0;
		}

		internal void Update(bool allConditionsMet)
		{
			if (allConditionsMet) UpdateGood();
			else UpdateBad();
		}

		private void UpdateBad()
		{
			double now = Planetarium.GetUniversalTime();

			if(now > downAfter)
			{
				SetIncomplete();
				ResetTimer();
			}
		}

		private void UpdateGood()
		{
			double now = Planetarium.GetUniversalTime();

			if (doneAfter == 0) doneAfter = now + duration;

			downAfter = now + allowed_downtime;
			if (now > doneAfter) SetComplete();
		}

		protected override string GetHashString()
		{
			return (Root != null ? (Root.MissionSeed.ToString() + Root.DateAccepted.ToString()) : "") + ID + "/duration";
		}

		protected override string GetTitle()
		{
			if (doneAfter == 0)
			{
				if (allowed_downtime > 0)
					return Localizer.Format("Duration: <<1>> (allows interruptions up to <<2>>)",
						DurationUtil.StringValue(duration), DurationUtil.StringValue(allowed_downtime));

				return Localizer.Format("Duration: <<1>>", DurationUtil.StringValue(duration));
			}

			double now = Planetarium.GetUniversalTime();
			if(doneAfter > now)
				return "Done!";

			double remaining = doneAfter - now;

			if(allowed_downtime > 0)
			{
				double ttf = downAfter - now;
				return Localizer.Format("Remaining: <<1>> (interrupted, stops in: <<2>>)",
					DurationUtil.StringValue(remaining), Lib.Color(DurationUtil.StringValue(ttf), Lib.Kolor.Yellow));
			}

			return Localizer.Format("Remaining: <<1>>", DurationUtil.StringValue(remaining));
		}

		protected override void OnSave(ConfigNode node)
		{
			base.OnSave(node);

			node.AddValue("duration", duration);
			node.AddValue("allowed_downtime", allowed_downtime);

			node.AddValue("doneAfter", doneAfter);
			node.AddValue("downAfter", downAfter);
		}

		protected override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			duration = ConfigNodeUtil.ParseValue<double>(node, "duration", 0);
			allowed_downtime = ConfigNodeUtil.ParseValue<double>(node, "allowed_downtime", 0);

			doneAfter = ConfigNodeUtil.ParseValue<double>(node, "doneAfter", 0);
			downAfter = ConfigNodeUtil.ParseValue<double>(node, "downAfter", 0);
		}
	}
}
