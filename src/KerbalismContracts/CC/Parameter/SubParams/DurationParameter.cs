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
		internal DurationType durationType;

		public enum DurationType
		{
			countdown, accumulating
		}

		// internal state
		private enum DurationState
		{
			off, preRun, running, preReset, done, failed
		}
		private DurationState durationState = DurationState.off;
		private double failAfter;
		private double doneAfter;
		private double startAfter;
		private double accumulatedDuration;

		private double previousRunningTime;

		private readonly TitleTracker titleTracker;
		private string lastTitle;

		public DurationParameter()
		{
			titleTracker = new TitleTracker(this);
		}

		public DurationParameter(double duration, double allowedDowntime, bool allowReset, double waitDuration, DurationType durationType)
		{
			titleTracker = new TitleTracker(this);

			this.duration = duration;
			this.allowedDowntime = allowedDowntime;
			this.allowReset = allowReset;
			this.waitDuration = waitDuration;
			this.durationType = durationType;
		}

		private void UpdateGood(double now)
		{
			switch (durationState)
			{
				case DurationState.off:
				case DurationState.preRun:
					durationState = DurationState.running;
					doneAfter = now + duration;
					break;

				case DurationState.preReset:
					durationState = DurationState.running;
					break;

				case DurationState.running:
					switch(durationType)
					{
						case DurationType.countdown:
							if (now > doneAfter)
								durationState = DurationState.done;
							break;

						case DurationType.accumulating:
							if (previousRunningTime != 0)
								accumulatedDuration += now - previousRunningTime;
							if (accumulatedDuration > duration)
								durationState = DurationState.done;
							break;
					}
					break;
			}
			previousRunningTime = now;

			if (durationState == DurationState.done)
				SetComplete();
		}

		private void UpdateBad(double now)
		{
			previousRunningTime = 0;
			switch (durationState)
			{
				case DurationState.off:
					if(waitDuration > 0)
					{
						startAfter = now + waitDuration;
						durationState = DurationState.preRun;
					}
					break;

				case DurationState.preRun:
					if (now > startAfter)
					{
						doneAfter = now + duration;
						if (allowedDowntime > 0)
						{
							failAfter = now + allowedDowntime;
							durationState = DurationState.preReset;
						}
						else
						{
							durationState = allowReset ? DurationState.off : DurationState.failed;
						}
					}
					break;

				case DurationState.running:
					if (allowedDowntime > 0)
					{
						failAfter = now + allowedDowntime;
						durationState = DurationState.preReset;
						break;
					}
					durationState = allowReset ? DurationState.off : DurationState.failed;
					break;

				case DurationState.preReset:
					if (now > failAfter)
						durationState = allowReset ? DurationState.off : DurationState.failed;
					break;
			}

			if (durationState == DurationState.failed)
				SetFailed();
		}

		public void Update(bool allConditionsMet, double now)
		{
			if (allConditionsMet) UpdateGood(now);
			else UpdateBad(now);

			GetTitle();
		}

		protected override string GetHashString()
		{
			return (Root != null ? (Root.MissionSeed.ToString() + Root.DateAccepted.ToString()) : "") + ID + "/duration";
		}

		private static string KerCon_DoesNotAllowInterruptions = Localizer.Format("#KerCon_DoesNotAllowInterruptions");
		private static string KerCon_WillFailIfInterrupted = Lib.Color(Localizer.Format("#KerCon_WillFailIfInterrupted"), Lib.Kolor.Orange);

		private string KerCon_AllowsInterruptionsUpTo;
		private string KerCon_TimeStartsAfterAccepting;
		private string KerCon_Duration_X;

		/// <summary> Cache some strings for performance gains (less memory churning) </summary>
		private void InitStrings()
		{
			if (!string.IsNullOrEmpty(KerCon_AllowsInterruptionsUpTo))
				return;

			KerCon_AllowsInterruptionsUpTo = Localizer.Format("#KerCon_AllowsInterruptionsUpTo", // Allows interruptions up to <<1>>
							DurationUtil.StringValue(allowedDowntime));
			KerCon_TimeStartsAfterAccepting = Localizer.Format("#KerCon_TimeStartsAfterAccepting", // Time starts <<1>> after accepting the contract"
							Lib.Color(DurationUtil.StringValue(waitDuration), Lib.Kolor.Yellow));
			KerCon_Duration_X = Localizer.Format("#KerCon_Duration_X", DurationUtil.StringValue(duration));
		}

		protected override string GetTitle()
		{
			InitStrings();

			string result = "";
			double now = Planetarium.GetUniversalTime();

			string remainingStr = durationType == DurationType.countdown
				? DurationUtil.StringValue(Math.Max(0, doneAfter - now))
				: DurationUtil.StringValue(Math.Max(0, duration - accumulatedDuration));

			switch (durationState)
			{
				case DurationState.off:
					result = KerCon_Duration_X; // Duration: <<1>>
					if (waitDuration > 0)
						result += "\n\t - " + KerCon_TimeStartsAfterAccepting;
					if (allowedDowntime > 0)
						result += "\n\t - " + KerCon_AllowsInterruptionsUpTo;
					else
						result += "\n\t - " + KerCon_DoesNotAllowInterruptions; // Does not allow interruptions
					if (!allowReset)
						result += "\n\t - " + KerCon_WillFailIfInterrupted; // Will fail if interrupted beyond allowance

					break;

				case DurationState.preRun:
					result = KerCon_Duration_X; // Duration: <<1>>
					result += "\n\t - " + Localizer.Format("#KerCon_TimeStartsIn", // Time starts in <<1>>
							Lib.Color(DurationUtil.StringValue(Math.Max(0, startAfter - now)), Lib.Kolor.Yellow));
					if (allowedDowntime > 0)
						result += "\n\t - " + KerCon_AllowsInterruptionsUpTo;

					break;

				case DurationState.running:
					result = Localizer.Format("#KerCon_Reamining_X", Lib.Color(remainingStr, Lib.Kolor.Green)); // Remaining: <<1>>
					//if (allowedDowntime > 0)
					//	result += "\n\t - " + Localizer.Format("#KerCon_AllowsInterruptionsUpTo", // Allows interruptions up to <<1>>
					//		DurationUtil.StringValue(allowedDowntime));

					break;

				case DurationState.preReset:
					result = Localizer.Format("#KerCon_Reamining_X_stopIn_Y", Lib.Color(remainingStr, Lib.Kolor.Green), // Remaining: <<1>> (stop in: <<2>>)
						Lib.Color(DurationUtil.StringValue(Math.Max(0, failAfter - now)), allowReset ? Lib.Kolor.Yellow : Lib.Kolor.Red));

					break;

				case DurationState.done:
					result = "#autoLOC_135144"; // Success!!
					break;

				case DurationState.failed:
					result = "#KerCon_TimeIsUp"; // Time is up!
					break;
			}

			if (result.StartsWith("#"))
				result = Localizer.Format(result);

			titleTracker.Add(result);
			if (lastTitle != result && Root != null && (Root.ContractState == Contract.State.Active || Root.ContractState == Contract.State.Failed))
			{
				titleTracker.UpdateContractWindow(result);
				lastTitle = result;
			}
			return result;
		}

		protected override void OnSave(ConfigNode node)
		{
			base.OnSave(node);

			node.AddValue("duration", duration);
			node.AddValue("allowedDowntime", allowedDowntime);
			node.AddValue("waitDuration", waitDuration);
			node.AddValue("allowReset", allowReset);
			node.AddValue("durationType", durationType);

			node.AddValue("doneAfter", doneAfter);
			node.AddValue("failAfter", failAfter);
			node.AddValue("startAfter", startAfter);
			node.AddValue("durationState", durationState);
			node.AddValue("accumulatedDuration", accumulatedDuration);
		}

		protected override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			duration = ConfigNodeUtil.ParseValue(node, "duration", 0.0);
			allowedDowntime = ConfigNodeUtil.ParseValue(node, "allowedDowntime", 0.0);
			waitDuration = ConfigNodeUtil.ParseValue(node, "waitDuration", 0.0);
			allowReset = ConfigNodeUtil.ParseValue(node, "allowReset", true);
			durationType = Lib.ConfigEnum(node, "durationType", DurationType.countdown);

			doneAfter = ConfigNodeUtil.ParseValue(node, "doneAfter", 0.0);
			failAfter = ConfigNodeUtil.ParseValue(node, "failAfter", 0.0);
			startAfter = ConfigNodeUtil.ParseValue(node, "startAfter", 0.0);
			accumulatedDuration = ConfigNodeUtil.ParseValue(node, "accumulatedDuration", 0.0);
			durationState = Lib.ConfigEnum(node, "durationState", DurationState.off);
		}
	}
}
