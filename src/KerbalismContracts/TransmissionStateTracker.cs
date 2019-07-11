using System.Collections.Generic;
using System;

namespace Kerbalism.Contracts
{
	public static class TransmissionStateTracker
	{
		private class State {
			internal State(string subject_id, bool can_transmit)
			{
				this.subject_id = subject_id;
				this.can_transmit = can_transmit;
			}

			internal string subject_id;
			internal bool can_transmit;
		}
		private static readonly Dictionary<Guid, State> states = new Dictionary<Guid, State>();

		internal static void Update(Vessel v, string subject_id, bool can_transmit)
		{
			if (v == null) return;

			if(!states.ContainsKey(v.id))
			{
				states.Add(v.id, new State(subject_id, can_transmit));
			}
			else
			{
				var state = states[v.id];
				state.subject_id = subject_id;
				state.can_transmit = can_transmit;
			}
		}

		internal static bool CanTransmit(Vessel v)
		{
			if (v == null) return false;

			if (states.ContainsKey(v.id))
			{
				return states[v.id].can_transmit;
			}

			return false;
		}

		internal static string Transmitting(Vessel v)
		{
			if (v == null) return string.Empty;

			if (states.ContainsKey(v.id))
			{
				return states[v.id].subject_id;
			}

			return string.Empty;
		}

	}
}
