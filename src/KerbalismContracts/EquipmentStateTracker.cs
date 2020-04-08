using System.Collections.Generic;
using System;

namespace KerbalismContracts
{
	public class EquipmentStateTracker
	{
		internal class StateEntry
		{
			internal string id;
			internal EquipmentState value;

			public StateEntry(string id, EquipmentState value)
			{
				this.id = id;
				this.value = value;
			}

			public StateEntry(ConfigNode node)
			{
				id = KERBALISM.Lib.ConfigValue(node, "id", "");
				value = KERBALISM.Lib.ConfigEnum(node, "value", EquipmentState.off);
			}

			internal void Save(ConfigNode node)
			{
				node.AddValue("id", id);
				node.AddValue("state", value);
			}
		}

		internal readonly Dictionary<Guid, List<StateEntry>> states = new Dictionary<Guid, List<StateEntry>>();
		internal readonly List<Action<Vessel, string, EquipmentState>> listeners = new List<Action<Vessel, string, EquipmentState>>();

		public EquipmentStateTracker()
		{
			GameEvents.onVesselChange.Add((vessel) => { states.Remove(vessel.id); });
		}

		internal void Update(Vessel vessel, string id, EquipmentState value)
		{
			if (vessel == null)
				return;

			if (!states.ContainsKey(vessel.id))
				states[vessel.id] = new List<StateEntry>();

			var list = states[vessel.id];
			var entry = list.Find(e => e.id == id);

			bool changed = false;

			if (entry == null)
			{
				changed = true;
				list.Add(new StateEntry(id, value));
			}
			else
			{
				changed = entry.value != value;
				entry.value = value;
			}

			if (changed)
			{
				for (int i = listeners.Count - 1; i >= 0; i--)
					listeners[i](vessel, id, value);
			}
		}

		internal EquipmentState GetValue(Vessel vessel, string id)
		{
			var list = states[vessel.id];
			var entry = list.Find(e => e.id == id);
			if (entry != null)
				return entry.value;
			return EquipmentState.off;
		}

		internal bool HasValue(Vessel vessel, string id)
		{
			List<StateEntry> list;
			if (states.TryGetValue(vessel.id, out list))
				return list.Find(e => e.id == id) != null;
			return false;
		}

		internal void Load(ConfigNode node)
		{
			states.Clear();

			var myNode = node.GetNode("EquipmentState");
			if (myNode == null)
				return;

			foreach (var vesselNode in myNode.GetNodes())
			{
				Guid id = new Guid(vesselNode.name);
				var statesList = new List<StateEntry>();
				states[id] = statesList;

				foreach (var n in vesselNode.GetNodes("Entry"))
					statesList.Add(new StateEntry(n));
			}
		}

		internal void Save(ConfigNode node)
		{
			var myNode = node.AddNode("EquipmentState");

			foreach (var id in states.Keys)
			{
				// test if vessel still exists
				if (FlightGlobals.FindVessel(id) == null)
					continue;

				var vesselNode = myNode.AddNode(id.ToString());
				foreach (var state in states[id])
					state.Save(vesselNode.AddNode("Entry"));
			}
		}

		internal void AddListener(Action<Vessel, string, EquipmentState> listener)
		{
			if (!listeners.Contains(listener)) listeners.Add(listener);
		}

		internal void RemoveListener(Action<Vessel, string, EquipmentState> listener)
		{
			if (listeners.Contains(listener)) listeners.Remove(listener);
		}
	}
}
