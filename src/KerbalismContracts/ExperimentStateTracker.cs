using System.Collections.Generic;
using System;

namespace KerbalismContracts
{
	internal enum ExperimentState
	{
		running, stopped
	}

	public static class ExperimentStateTracker
	{
		internal class StateEntry
		{
			internal string id;
			internal ExperimentState value;

			public StateEntry(string id, ExperimentState value)
			{
				this.id = id;
				this.value = value;
			}

			public StateEntry(ConfigNode node)
			{
				id = KERBALISM.Lib.ConfigValue(node, "id", "");
				value = KERBALISM.Lib.ConfigEnum(node, "value", ExperimentState.stopped);
			}

			internal void Save(ConfigNode node)
			{
				node.AddValue("id", id);
				node.AddValue("state", value);
			}
		}

		private static readonly Dictionary<Guid, List<StateEntry>> states = new Dictionary<Guid, List<StateEntry>>();

		internal static void Remove(Guid id)
		{
			states.Remove(id);
		}

		private static readonly List<Action<Guid, string, ExperimentState>> listeners = new List<Action<Guid, string, ExperimentState>>();

		internal static void Update(Guid vesselId, string id, int state)
		{
			if (vesselId == null)
				return;

			if (!states.ContainsKey(vesselId))
				states[vesselId] = new List<StateEntry>();

			var list = states[vesselId];
			var entry = list.Find(e => e.id == id);

			bool changed = false;

			// Kerbalism ExpStatus { 0: Stopped, 1: Running, 2: Forced, 3: Waiting, 4: Issue, 5: Broken }
			ExperimentState value = (state == 1 || state == 2) ? ExperimentState.running : ExperimentState.stopped;

			Utils.LogDebug($"{vesselId} {id} {state} ({value})");

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
					listeners[i](vesselId, id, value);
			}
		}

		internal static ExperimentState GetValue(Guid vesselId, string id)
		{
			var list = states[vesselId];
			var entry = list.Find(e => e.id == id);
			if (entry != null)
				return entry.value;
			return ExperimentState.stopped;
		}

		internal static bool HasValue(Guid vesselId, string id)
		{
			List<StateEntry> list;
			if (states.TryGetValue(vesselId, out list))
				return list.Find(e => e.id == id) != null;
			return false;
		}

		internal static void Load(ConfigNode node)
		{
			states.Clear();

			var myNode = node.GetNode("ExperimentState");
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

		internal static void Save(ConfigNode node)
		{
			var myNode = node.AddNode("ExperimentState");

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

		internal static void AddListener(Action<Guid, string, ExperimentState> listener)
		{
			if (!listeners.Contains(listener)) listeners.Add(listener);
		}

		internal static void RemoveListener(Action<Guid, string, ExperimentState> listener)
		{
			if (listeners.Contains(listener)) listeners.Remove(listener);
		}
	}
}
