using System.Collections.Generic;
using System;

namespace Kerbalism.Contracts
{
	public static class ExperimentStateTracker
	{
		private class State {
			internal State(bool inner_belt, bool outer_belt, bool magnetosphere)
			{
				this.inner_belt = inner_belt;
				this.outer_belt = outer_belt;
				this.magnetosphere = magnetosphere;
			}

			internal bool inner_belt;
			internal bool outer_belt;
			internal bool magnetosphere;
		}
		private static readonly Dictionary<Guid, State> states = new Dictionary<Guid, State>();

		internal static void Update(Vessel v, bool inner_belt, bool outer_belt, bool magnetosphere)
		{
			if (!KerbalismContracts.RelevantVessel(v))
				return;

			var bd = KerbalismContracts.Instance.BodyData(v.mainBody);

			if (!states.ContainsKey(v.id))
			{
				states.Add(v.id, new State(inner_belt, outer_belt, magnetosphere));
			}
			else
			{
				var state = states[v.id];

				if (state.inner_belt != inner_belt) {
#if DEBUG
					Lib.Log(v + " crossed boundary of inner belt of " + v.mainBody + ": " + inner_belt);
#endif
					bd.inner_crossings++;
				}

				if (state.outer_belt != outer_belt)
				{
#if DEBUG
					Lib.Log(v + " crossed boundary of outer belt of " + v.mainBody + ": " + inner_belt);
#endif
					bd.outer_crossings++;
				}

				if (state.magnetosphere != magnetosphere)
				{
#if DEBUG
					Lib.Log(v + " crossed boundary of magnetosphere of " + v.mainBody + ": " + inner_belt);
#endif
					bd.pause_crossings++;
				}

				state.inner_belt = inner_belt;
				state.outer_belt = outer_belt;
				state.magnetosphere = magnetosphere;

				bool isSun = KERBALISM.Lib.IsSun(v.mainBody);

				if(!isSun && !bd.inner_visible && bd.inner_crossings > Settings.inner_discovery_crossings)
				{
					KerbalismContracts.SetInnerBeltVisible(v.mainBody, true);
				}
				if (!isSun && !bd.outer_visible && bd.outer_crossings > Settings.outer_discovery_crossings)
				{
					KerbalismContracts.SetOuterBeltVisible(v.mainBody, true);
				}
				if (!bd.pause_visible && bd.pause_crossings > Settings.pause_discovery_crossings)
				{
					KerbalismContracts.SetMagnetopauseVisible(v.mainBody, true);
				}
			}


			for (int i = listeners.Count - 1; i >= 0; i--)
			{
				listeners[i](v, inner_belt, outer_belt, magnetosphere);
			}
		}

		internal static bool InnerBelt(Vessel v)
		{
			if (v == null) return false;

			if (states.ContainsKey(v.id))
			{
				return states[v.id].inner_belt;
			}

			return false;
		}

		internal static bool OuterBelt(Vessel v)
		{
			if (v == null) return false;

			if (states.ContainsKey(v.id))
			{
				return states[v.id].outer_belt;
			}

			return false;
		}

		internal static bool Magnetosphere(Vessel v)
		{
			if (v == null) return false;

			if (states.ContainsKey(v.id))
			{
				return states[v.id].magnetosphere;
			}

			return false;
		}

		private static readonly List<Action<Vessel, bool, bool, bool>> listeners = new List<Action<Vessel, bool, bool, bool>>();

		internal static void AddListener(Action<Vessel, bool, bool, bool> listener)
		{
			if (!listeners.Contains(listener)) listeners.Add(listener);
		}

		internal static void RemoveListener(Action<Vessel, bool, bool, bool> listener)
		{
			if (listeners.Contains(listener)) listeners.Remove(listener);
		}

	}
}
