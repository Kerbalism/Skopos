using System.Collections.Generic;
using System;
/*
namespace KerbalismContracts
{
	public static class EquipmentStateTracker
	{
		static readonly Dictionary<Guid, List<string>> states = new Dictionary<Guid, List<string>>();

		internal static void Update(Vessel vessel, string equipment_id, bool running)
		{
			if (running)
				Add(vessel, equipment_id);
			else
				Remove(vessel, equipment_id);

			for (int i = listeners.Count - 1; i >= 0; i--)
			{
				listeners[i](vessel, equipment_id, running);
			}
		}

		internal static void Add(Vessel vessel, string equipment_id)
		{
			if (vessel == null) return;

			if (!states.ContainsKey(vessel.id))
				states[vessel.id] = new List<string>();

			var list = states[vessel.id];
			if (!list.Contains(equipment_id))
				list.Add(equipment_id);
		}

		internal static void Remove(Vessel vessel, string equipment_id)
		{
			if (vessel == null) return;

			if (states.ContainsKey(vessel.id))
				states[vessel.id].Remove(equipment_id);
		}

		internal static bool IsRunning(Vessel vessel, string equipment_id)
		{
			if(states.ContainsKey(vessel.id))
				return states[vessel.id].Contains(equipment_id);
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