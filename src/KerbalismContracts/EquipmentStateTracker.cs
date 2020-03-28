using System.Collections.Generic;
using System;
/*
namespace KerbalismContracts
{
	public static class ExperimentStateTracker
	{
		static readonly Dictionary<Guid, List<string>> states = new Dictionary<Guid, List<string>>();

		internal static void Update(Vessel vessel, string experiment_id, bool running)
		{
			if (running)
				Add(vessel, experiment_id);
			else
				Remove(vessel, experiment_id);

			for (int i = listeners.Count - 1; i >= 0; i--)
			{
				listeners[i](vessel, experiment_id, running);
			}
		}

		internal static void Add(Vessel vessel, string experient_id)
		{
			if (vessel == null) return;

			if (!states.ContainsKey(vessel.id))
				states[vessel.id] = new List<string>();

			var list = states[vessel.id];
			if (!list.Contains(experient_id))
				list.Add(experient_id);
		}

		internal static void Remove(Vessel vessel, string experient_id)
		{
			if (vessel == null) return;

			if (states.ContainsKey(vessel.id))
				states[vessel.id].Remove(experient_id);
		}

		internal static bool IsRunning(Vessel vessel, string experiment_id)
		{
			if(states.ContainsKey(vessel.id))
				return states[vessel.id].Contains(experiment_id);
			return false;
		}

		private static readonly List<Action<Vessel, string, bool>> listeners = new List<Action<Vessel, string, bool>>();

		internal static void AddListener(Action<Vessel, string, bool> listener)
		{
			if (!listeners.Contains(listener)) listeners.Add(listener);
		}

		internal static void RemoveListener(Action<Vessel, string, bool> listener)
		{
			if (listeners.Contains(listener)) listeners.Remove(listener);
		}
	}
}
*/