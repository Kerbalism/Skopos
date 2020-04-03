using System.Collections.Generic;
using System;
using KERBALISM;

namespace KerbalismContracts
{
	public class VesselRadiationFieldStatus
	{
		internal bool inner_belt;
		internal bool outer_belt;
		internal bool magnetosphere;
		internal int inner_crossings;
		internal int outer_crossings;
		internal int magneto_crossings;
		internal int bodyIndex;

		internal VesselRadiationFieldStatus(CelestialBody body, bool inner_belt, bool outer_belt, bool magnetosphere)
		{
			this.inner_belt = inner_belt;
			this.outer_belt = outer_belt;
			this.magnetosphere = magnetosphere;
			bodyIndex = body.flightGlobalsIndex;
		}

		internal VesselRadiationFieldStatus(ConfigNode node)
		{
			inner_belt = Lib.ConfigValue(node, "inner_belt", false);
			outer_belt = Lib.ConfigValue(node, "outer_belt", false);
			magnetosphere = Lib.ConfigValue(node, "magnetosphere", false);
			inner_crossings = Lib.ConfigValue(node, "inner_crossings", 0);
			outer_crossings = Lib.ConfigValue(node, "outer_crossings", 0);
			magneto_crossings = Lib.ConfigValue(node, "magneto_crossings", 0);
			bodyIndex = Lib.ConfigValue(node, "bodyIndex", -1);
		}

		internal void Save(ConfigNode node)
		{
			node.AddValue("inner_belt", inner_belt);
			node.AddValue("outer_belt", outer_belt);
			node.AddValue("magnetosphere", magnetosphere);
			node.AddValue("inner_crossings", inner_crossings);
			node.AddValue("outer_crossings", outer_crossings);
			node.AddValue("magneto_crossings", magneto_crossings);
			node.AddValue("bodyIndex", bodyIndex);
		}
	}

	public static class RadiationFieldTracker
	{
		private static readonly Dictionary<Guid, List<VesselRadiationFieldStatus>> states = new Dictionary<Guid, List<VesselRadiationFieldStatus>>();

		internal static void Update(Vessel vessel, bool inner_belt, bool outer_belt, bool magnetosphere)
		{
			if (!Utils.IsVessel(vessel))
				return;

			if (!states.ContainsKey(vessel.id))
			{
				states.Add(vessel.id, new List<VesselRadiationFieldStatus>());
			}

			// also update the global radiation field status
			GlobalRadiationFieldStatus bd = KerbalismContracts.Instance.BodyData(vessel.mainBody);

			var statesForVessel = states[vessel.id];
			VesselRadiationFieldStatus state = statesForVessel.Find(s => s.bodyIndex == vessel.mainBody.flightGlobalsIndex);
			if (state == null)
			{
				statesForVessel.Add(new VesselRadiationFieldStatus(vessel.mainBody, inner_belt, outer_belt, magnetosphere));
			}
			else
			{
				if (state.inner_belt != inner_belt)
				{
					state.inner_crossings++;
					bd.inner_crossings++;
				}

				if (state.outer_belt != outer_belt)
				{
					state.outer_crossings++;
					bd.outer_crossings++;
				}

				if (state.magnetosphere != magnetosphere)
				{
					state.magneto_crossings++;
					bd.magneto_crossings++;
				}

				state.inner_belt = inner_belt;
				state.outer_belt = outer_belt;
				state.magnetosphere = magnetosphere;
			}

			for (int i = listeners.Count - 1; i >= 0; i--)
			{
				listeners[i](vessel, state);
			}
		}

		/// <summary>
		/// Return all radiation field states for this vessel
		/// </summary>
		internal static List<VesselRadiationFieldStatus> RadiationFieldStates(Vessel v)
		{
			if (v == null) return null;

			if (states.ContainsKey(v.id))
			{
				return states[v.id];
			}
			return null;
		}

		/// <summary>
		/// Return the radiation field state of the vessel in its current main body
		/// </summary>
		internal static VesselRadiationFieldStatus RadiationFieldState(Vessel v)
		{
			if (v == null) return null;

			if (states.ContainsKey(v.id))
			{
				return states[v.id].Find(s => s.bodyIndex == v.mainBody.flightGlobalsIndex);
			}
			return null;
		}

		private static readonly List<Action<Vessel, VesselRadiationFieldStatus>> listeners = new List<Action<Vessel, VesselRadiationFieldStatus>>();

		internal static void AddListener(Action<Vessel, VesselRadiationFieldStatus> listener)
		{
			if (!listeners.Contains(listener)) listeners.Add(listener);
		}

		internal static void RemoveListener(Action<Vessel, VesselRadiationFieldStatus> listener)
		{
			if (listeners.Contains(listener)) listeners.Remove(listener);
		}

		internal static void Save(ConfigNode node)
		{
			var myNode = node.AddNode("RadiationFieldTracker");

			foreach (var id in states.Keys)
			{
				// test if vessel still exists
				if (FlightGlobals.FindVessel(id) == null) continue;

				var vesselNode = myNode.AddNode(id.ToString());
				foreach (var state in states[id])
					state.Save(vesselNode.AddNode("VesselBodyData"));
			}
		}

		internal static void Load(ConfigNode node)
		{
			states.Clear();

			var myNode = node.GetNode("RadiationFieldTracker");
			if (myNode == null)
				return;

			foreach (var vesselNode in myNode.GetNodes())
			{
				Guid id = new Guid(vesselNode.name);
				var statesList = new List<VesselRadiationFieldStatus>();
				states[id] = statesList;

				foreach (var stateNode in vesselNode.GetNodes())
					statesList.Add(new VesselRadiationFieldStatus(stateNode));
			}
		}
	}
}
