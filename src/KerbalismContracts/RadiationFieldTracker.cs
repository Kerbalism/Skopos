using System.Collections.Generic;
using System;

namespace Kerbalism.Contracts
{
	public class RadiationFieldState
	{
		internal RadiationFieldState() { }

		internal RadiationFieldState(CelestialBody body, bool inner_belt, bool outer_belt, bool magnetosphere)
		{
			this.inner_belt = inner_belt;
			this.outer_belt = outer_belt;
			this.magnetosphere = magnetosphere;
			bodyIndex = body.flightGlobalsIndex;
		}

		internal bool IsValid()
		{
			return inner_crossings >= 0 && outer_crossings >= 0 && magneto_crossings >= 0;
		}

		internal void Validate()
		{
			inner_crossings = 0;
			outer_crossings = 0;
			magneto_crossings = 0;
		}

		internal bool inner_belt;
		internal bool outer_belt;
		internal bool magnetosphere;
		internal int inner_crossings = -1;
		internal int outer_crossings = -1;
		internal int magneto_crossings = -1;
		internal int bodyIndex = -1;
	}

	public static class RadiationFieldTracker
	{
		private static readonly Dictionary<Guid, List<RadiationFieldState>> states = new Dictionary<Guid, List<RadiationFieldState>>();

		internal static void Update(Vessel v, bool inner_belt, bool outer_belt, bool magnetosphere)
		{
			if (!KerbalismContracts.RelevantVessel(v))
				return;

			var bd = KerbalismContracts.Instance.BodyData(v.mainBody);

			if (!states.ContainsKey(v.id))
			{
				states.Add(v.id, new List<RadiationFieldState>());
			}

			var statesForVessel = states[v.id];
			var state = statesForVessel.Find(s => s.bodyIndex == v.mainBody.flightGlobalsIndex);
			if(state == null)
			{
				statesForVessel.Add(new RadiationFieldState(v.mainBody, inner_belt, outer_belt, magnetosphere));
			}
			else
			{
				if(!state.IsValid())
				{
					// got our first update, set all counters to 0 so we don't count spawning into a field as crossing into it
					state.Validate();
				}
				else
				{
					if (state.inner_belt != inner_belt) state.inner_crossings++;
					if (state.outer_belt != outer_belt) state.outer_crossings++;
					if (state.magnetosphere != magnetosphere) state.magneto_crossings++;
				}

				state.inner_belt = inner_belt;
				state.outer_belt = outer_belt;
				state.magnetosphere = magnetosphere;
			}

			for (int i = listeners.Count - 1; i >= 0; i--)
			{
				listeners[i](v, state);
			}
		}

		internal static List<RadiationFieldState> RadiationFieldStates(Vessel v)
		{
			if (v == null) return null;

			if (states.ContainsKey(v.id))
			{
				return states[v.id];
			}
			return null;
		}

		internal static RadiationFieldState RadiationFieldState(Vessel v)
		{
			if (v == null) return null;

			if (states.ContainsKey(v.id))
			{
				return states[v.id].Find(s => s.bodyIndex == v.mainBody.flightGlobalsIndex);
			}
			return null;
		}

		private static readonly List<Action<Vessel, RadiationFieldState>> listeners = new List<Action<Vessel, RadiationFieldState>>();

		internal static void AddListener(Action<Vessel, RadiationFieldState> listener)
		{
			if (!listeners.Contains(listener)) listeners.Add(listener);
		}

		internal static void RemoveListener(Action<Vessel, RadiationFieldState> listener)
		{
			if (listeners.Contains(listener)) listeners.Remove(listener);
		}

		internal static void Save(ConfigNode node)
		{
			foreach(var id in states.Keys)
			{
				// test if vessel still exists
				if(FlightGlobals.FindVessel(id) == null) continue;

				var vesselNode = node.AddNode(id.ToString());
				foreach(var state in states[id])
				{
					var stateNode = vesselNode.AddNode(state.bodyIndex.ToString());
					stateNode.SetValue("inner_belt", state.inner_belt);
					stateNode.SetValue("outer_belt", state.outer_belt);
					stateNode.SetValue("magnetosphere", state.magnetosphere);
					stateNode.SetValue("inner_crossings", state.inner_crossings);
					stateNode.SetValue("outer_crossings", state.outer_crossings);
					stateNode.SetValue("magneto_crossings", state.magneto_crossings);
				}
			}
		}

		internal static void Load(ConfigNode node)
		{
			states.Clear();

			foreach(var vesselNode in node.GetNodes())
			{
				Guid id = Guid.Parse(vesselNode.name);
				var statesList = new List<RadiationFieldState>();
				states[id] = statesList;

				foreach(var stateNode in vesselNode.GetNodes())
				{
					var state = new RadiationFieldState();
					statesList.Add(state);

					state.bodyIndex = int.Parse(stateNode.name);
					state.inner_belt = Lib.ConfigValue(stateNode, "inner_belt", false);
					state.outer_belt = Lib.ConfigValue(stateNode, "outer_belt", false);
					state.magnetosphere = Lib.ConfigValue(stateNode, "magnetosphere", false);
					state.inner_crossings = Lib.ConfigValue(stateNode, "inner_crossings", 0);
					state.outer_crossings = Lib.ConfigValue(stateNode, "outer_crossings", 0);
					state.magneto_crossings = Lib.ConfigValue(stateNode, "magneto_crossings", 0);
				}
			}
		}
	}
}
