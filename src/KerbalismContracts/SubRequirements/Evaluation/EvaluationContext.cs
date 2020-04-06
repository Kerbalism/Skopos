using System.Collections.Generic;
using FinePrint;
using KERBALISM;
using System;

namespace KerbalismContracts
{
	public class EvaluationContext
	{
		public readonly Waypoint waypoint;
		public readonly List<double> steps;
		public double now;

		private class PositionEntry
		{
			internal Vessel vessel;
			internal Vector3d position;
			internal double time;

			public PositionEntry(Vessel vessel, double time)
			{
				this.vessel = vessel;
				this.time = time;
				this.position = vessel.orbit.getPositionAtUT(time);
			}
		}
		private static readonly Dictionary<Guid, LinkedList<PositionEntry>> vesselPositions = new Dictionary<Guid, LinkedList<PositionEntry>>();

		public static void Clear()
		{
			vesselPositions.Clear();
		}

		public EvaluationContext(List<double> steps, Waypoint waypoint = null)
		{
			this.waypoint = waypoint;
			this.steps = steps;
		}

		internal Vector3d VesselPosition(Vessel vessel, int secondsAgo = 0)
		{
			if (!vesselPositions.ContainsKey(vessel.id))
				vesselPositions.Add(vessel.id, new LinkedList<PositionEntry>());

			double t = Math.Floor(now) - secondsAgo; // we're not interested in sub-second accuracy
			foreach(PositionEntry entry in vesselPositions[vessel.id])
			{
				if (entry.time == t)
					return entry.position;
			}

			while(vesselPositions[vessel.id].Count > 150)
				vesselPositions[vessel.id].RemoveLast();

			var newEntry = new PositionEntry(vessel, t);
			vesselPositions[vessel.id].AddFirst(newEntry);
			return newEntry.position;
		}

		internal Vector3d WaypointSurfacePosition()
		{
			// TODO this assumes time to be now, which is wrong
			return waypoint.celestialBody.GetWorldSurfacePosition(waypoint.latitude, waypoint.longitude, 0);
		}

		internal Vector3d BodyPosition()
		{
			// TODO this assumes time to be now, which is wrong
			return waypoint.celestialBody.position;
		}

        internal void SetStep(double step)
        {
			this.now = step;
        }
   	}
}
