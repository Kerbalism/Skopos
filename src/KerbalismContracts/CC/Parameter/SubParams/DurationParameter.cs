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
		internal double allowedDowntime;
		internal bool allowReset;
		internal double waitDuration;

		// internal state
		internal double doneAfter;
		internal double resetAfter;
		internal double forceStartAfter;

		public DurationParameter() { }

		public DurationParameter(double duration, double allowedDowntime, bool allowReset, double waitDuration)
		{
			this.duration = duration;
			this.allowedDowntime = allowedDowntime;
			this.allowReset = allowReset;
			this.waitDuration = waitDuration;

			ResetTimer();
		}

		private void ResetTimer()
		{
			if (doneAfter != 0 && !allowReset)
				SetFailed();
			else
				SetIncomplete();

			doneAfter = 0;
			resetAfter = 0;
		}

		internal void Update(bool allConditionsMet, double now)
		{
			if (waitDuration > 0 && forceStartAfter == 0)
			{
				forceStartAfter = now + waitDuration;
				doneAfter = now + waitDuration + duration;
			}

			if (allConditionsMet) UpdateGood(now);
			else UpdateBad(now);
		}

		private void UpdateBad(double now)
		{
			if (resetAfter == 0)
				resetAfter = now + allowedDowntime;

			if (now > resetAfter)
				ResetTimer();
		}

		private void UpdateGood(double now)
		{
			if (doneAfter == 0)
				doneAfter = now + duration;

			resetAfter = 0;

			if (now > doneAfter)
				SetComplete();
		}

		protected override string GetHashString()
		{
			return (Root != null ? (Root.MissionSeed.ToString() + Root.DateAccepted.ToString()) : "") + ID + "/duration";
		}

		protected override string GetTitle()
		{
			double now = Planetarium.GetUniversalTime();

			if (doneAfter == 0 && resetAfter == 0)
			{
				string result = Localizer.Format("Duration: <<1>>", DurationUtil.StringValue(duration));

				if (allowedDowntime > 0)
					result += "\n\t - " + Localizer.Format("Allows interruptions up to <<1>>",
						DurationUtil.StringValue(allowedDowntime));
				else
					result += "\n\t - " + "Does not allow interruptions";

				if (waitDuration > 0)
				{
					if(forceStartAfter == 0)
						result += "\n\t - " + Localizer.Format("Time starts <<1>> after accepting the contract",
							DurationUtil.StringValue(waitDuration));
					else
						result += "\n\t - " + Localizer.Format("Time starts in <<1>>",
							Lib.Color(DurationUtil.StringValue(Math.Max(0, forceStartAfter - now)), Lib.Kolor.Yellow));
				}

				if (!allowReset)
					result += "\n\t - " + Lib.Color("Will fail if interrupted beyond allowance", Lib.Kolor.Orange);

				return result;
			}

			if(now > doneAfter)
				return "Done!";

			double remaining = doneAfter - now;

			if(resetAfter != 0)
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
			node.AddValue("allowedDowntime", allowedDowntime);
			node.AddValue("waitDuration", waitDuration);

			node.AddValue("doneAfter", doneAfter);
			node.AddValue("resetAfter", resetAfter);
			node.AddValue("forceStartAfter", forceStartAfter);
		}

		protected override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			duration = ConfigNodeUtil.ParseValue(node, "duration", 0.0);
			allowedDowntime = ConfigNodeUtil.ParseValue(node, "allowedDowntime", 0.0);
			waitDuration = ConfigNodeUtil.ParseValue(node, "waitDuration", 0.0);

			doneAfter = ConfigNodeUtil.ParseValue(node, "doneAfter", 0.0);
			resetAfter = ConfigNodeUtil.ParseValue(node, "resetAfter", double.MaxValue);
			forceStartAfter = ConfigNodeUtil.ParseValue(node, "forceStartAfter", 0.0);
		}
	}
}
