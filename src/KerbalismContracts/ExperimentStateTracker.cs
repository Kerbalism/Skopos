using System.Collections.Generic;
using System;

namespace Kerbalism.Contracts
{
	public static class ExperimentStateTracker
	{
		private class State {
			internal State(string experiment_id, bool running)
			{
				this.experiment_id = experiment_id;
				this.running = running;
			}

			internal string experiment_id;
			internal bool running;
		}
		private static readonly Dictionary<Guid, List<State>> states = new Dictionary<Guid, List<State>>();

		internal static void Update(Vessel v, string experiment_id, bool running)
		{
			if (v == null) return;

			List<State> stateList;
			if(!states.TryGetValue(v.id, out stateList))
			{
				stateList = new List<State>();
				states[v.id] = stateList;
			}

			foreach(var state in stateList) {
				if(state.experiment_id == experiment_id) {
					state.running = running;
					return;
				}
			}

			stateList.Add(new State(experiment_id, running));
		}

		internal static bool IsRunning(Vessel v, string experiment_id)
		{
			if (v == null) return false;

			if (states.ContainsKey(v.id))
			{
				foreach (var state in states[v.id])
				{
					if (state.experiment_id == experiment_id)
						return state.running;
				}
			}

			return false;
		}
	}
}
