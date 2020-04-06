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
		internal double resetAfter;

		public DurationParameter() { }

		public DurationParameter(double duration, double allowed_downtime)
		{
			this.duration = duration;
			this.allowed_downtime = allowed_downtime;
			ResetTimer();
		}

		private void ResetTimer()
		{
			doneAfter = resetAfter = 0;
			SetIncomplete();
		}

		internal void Update(bool allConditionsMet, double now)
		{
			if (allConditionsMet) UpdateGood(now);
			else UpdateBad(now);
		}

		private void UpdateBad(double now)
		{
			if (resetAfter == 0)
				resetAfter = now + allowed_downtime;

			if(now > resetAfter)
				ResetTimer();
		}

		private void UpdateGood(double now)
		{
			resetAfter = 0;
			if (doneAfter == 0)
				doneAfter = now + duration;

			if (now > doneAfter)
				SetComplete();
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
			if(now > doneAfter)
				return "Done!";

			double remaining = doneAfter - now;

			if(resetAfter > 0)
			{
				double ttf = Math.Max(0, resetAfter - now);
				return Localizer.Format("Remaining: <<1>> (stop in: <<2>>)",
					Lib.Color(DurationUtil.StringValue(remaining), Lib.Kolor.Green),
					Lib.Color(DurationUtil.StringValue(ttf), Lib.Kolor.Yellow));
			}

			return Localizer.Format("Remaining: <<1>>",
				Lib.Color(DurationUtil.StringValue(remaining), Lib.Kolor.Green));
		}

		protected override void OnSave(ConfigNode node)
		{
			base.OnSave(node);

			node.AddValue("duration", duration);
			node.AddValue("allowed_downtime", allowed_downtime);

			node.AddValue("doneAfter", doneAfter);
			node.AddValue("resetAfter", resetAfter);
		}

		protected override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			duration = ConfigNodeUtil.ParseValue<double>(node, "duration", 0);
			allowed_downtime = ConfigNodeUtil.ParseValue<double>(node, "allowed_downtime", 0);

			doneAfter = ConfigNodeUtil.ParseValue<double>(node, "doneAfter", 0);
			resetAfter = ConfigNodeUtil.ParseValue<double>(node, "resetAfter", 0);
		}
	}
}
