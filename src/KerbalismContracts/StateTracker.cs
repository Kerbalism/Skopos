using System.Collections.Generic;
using System;

namespace KerbalismContracts
{
	public class StateTracker
	{
		internal readonly Dictionary<Guid, List<string>> states = new Dictionary<Guid, List<string>>();
		internal string nodeName;

		public StateTracker(string nodeName)
		{
			this.nodeName = nodeName;
		}

		internal void Update(Vessel vessel, string id, bool running)
		{
			if (running)
				Add(vessel, id);
			else
				Remove(vessel, id);

			for (int i = listeners.Count - 1; i >= 0; i--)
			{
				listeners[i](vessel, id, running);
			}
		}

		internal void Add(Vessel vessel, string id)
		{
			if (vessel == null) return;

			if (!states.ContainsKey(vessel.id))
				states[vessel.id] = new List<string>();

			var list = states[vessel.id];
			if (!list.Contains(id))
				list.Add(id);
		}

		internal void Remove(Vessel vessel, string id)
		{
			if (vessel == null) return;

			if (states.ContainsKey(vessel.id))
				states[vessel.id].Remove(id);
		}

		internal bool IsRunning(Vessel vessel, string id)
		{
			if(states.ContainsKey(vessel.id))
				return states[vessel.id].Contains(id);
			return false;
		}

		internal void Load(ConfigNode node)
		{
			states.Clear();

			var myNode = node.GetNode(nodeName);
			if (myNode == null)
				return;

			foreach (var vesselNode in myNode.GetNodes())
			{
				Guid id = new Guid(vesselNode.name);
				var statesList = new List<string>();
				states[id] = statesList;

				foreach(string value in vesselNode.GetValues())
					statesList.Add(value);
			}
		}

		internal void Save(ConfigNode node)
		{
			var myNode = node.AddNode(nodeName);

			foreach (var id in states.Keys)
			{
				// test if vessel still exists
				if (FlightGlobals.FindVessel(id) == null) continue;

				var vesselNode = myNode.AddNode(id.ToString());
				foreach (var state in states[id])
					vesselNode.AddValue(state, true);
			}
		}

		internal readonly List<Action<Vessel, string, bool>> listeners = new List<Action<Vessel, string, bool>>();

		internal void AddListener(Action<Vessel, string, bool> listener)
		{
			if (!listeners.Contains(listener)) listeners.Add(listener);
		}

		internal void RemoveListener(Action<Vessel, string, bool> listener)
		{
			if (listeners.Contains(listener)) listeners.Remove(listener);
		}
	}
}
