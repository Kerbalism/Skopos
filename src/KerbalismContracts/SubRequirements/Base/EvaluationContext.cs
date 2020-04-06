using FinePrint;
using KERBALISM;

namespace KerbalismContracts
{
	public class EvaluationContext
	{
		public readonly Waypoint waypoint;

		public EvaluationContext(Waypoint waypoint = null)
		{
			this.waypoint = waypoint;
		}

		internal Vector3d VesselPosition(Vessel vessel, int secondsAgo)
		{
			if (secondsAgo == 0)
				return Lib.VesselPosition(vessel);

			return vessel.orbit.getPositionAtUT(Planetarium.GetUniversalTime() - secondsAgo);
		}
	}
}
