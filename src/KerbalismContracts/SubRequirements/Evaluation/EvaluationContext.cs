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
		public readonly double ut = Planetarium.GetUniversalTime();

		private class VesselPositionEntry
		{
			internal Vessel vessel;
			internal Vector3d position;
			internal double time;

			public VesselPositionEntry(Vessel vessel, double time)
			{
				this.vessel = vessel;
				this.time = time;
				this.position = vessel.orbit.getPositionAtUT(time);
			}
		}
		private static readonly Dictionary<Guid, LinkedList<VesselPositionEntry>> vesselPositions = new Dictionary<Guid, LinkedList<VesselPositionEntry>>();

		private class BodyPositionEntry
		{
			internal int flightGlobalsIndex;
			internal Vector3d position;
			internal double time;

			public BodyPositionEntry(CelestialBody body, double time)
			{
				this.flightGlobalsIndex = body.flightGlobalsIndex;
				this.time = time;
				if (body.orbit == null) // suns?
					position = body.position;
				else
					position = body.orbit.getPositionAtUT(time);
			}
		}
		private static readonly Dictionary<int, LinkedList<BodyPositionEntry>> bodyPositions = new Dictionary<int, LinkedList<BodyPositionEntry>>();

		private class WaypointPositionEntry
		{
			internal Guid id;
			internal Vector3d position;
			internal double time;

			public WaypointPositionEntry(Waypoint waypoint, double time, Vector3d bodyPosition)
			{
				this.id = waypoint.navigationId;
				this.time = time;

				CelestialBody body = waypoint.celestialBody;
				Planetarium.CelestialFrame BodyFrame = default;
				if (!body.inverseRotation && body.rotates && body.rotationPeriod != 0.0 && (!body.tidallyLocked || (body.orbit != null && body.orbit.period != 0.0)))
				{
					var rotPeriodRecip = 1.0 / body.rotationPeriod;
					var rotationAngle = (body.initialRotation + 360.0 * rotPeriodRecip * time) % 360.0;
					var directRotAngle = (rotationAngle - Planetarium.InverseRotAngle) % 360.0;
					Planetarium.CelestialFrame.PlanetaryFrame(0.0, 90.0, directRotAngle, ref BodyFrame);
				}

				position = BodyFrame.LocalToWorld(body.GetRelSurfacePosition(waypoint.latitude, waypoint.longitude, 0).xzy).xzy + bodyPosition;
			}
		}
		private static readonly Dictionary<Guid, LinkedList<WaypointPositionEntry>> waypointPositions = new Dictionary<Guid, LinkedList<WaypointPositionEntry>>();

		public static void Clear()
		{
			vesselPositions.Clear();
			bodyPositions.Clear();
			waypointPositions.Clear();
		}

		public EvaluationContext(List<double> steps, Waypoint waypoint = null)
		{
			this.waypoint = waypoint;
			this.steps = steps;
		}

		internal Vector3d VesselPosition(Vessel vessel, int secondsAgo = 0)
		{
			if (!vesselPositions.ContainsKey(vessel.id))
				vesselPositions.Add(vessel.id, new LinkedList<VesselPositionEntry>());

			var positionList = vesselPositions[vessel.id];
			double t = now - secondsAgo;
			foreach(VesselPositionEntry entry in positionList)
			{
				if (entry.time == t)
					return entry.position;
			}

			while(positionList.Count > 150)
				positionList.RemoveLast();

			var newEntry = new VesselPositionEntry(vessel, t);
			positionList.AddFirst(newEntry);
			return newEntry.position;
		}

		internal Vector3d BodyPosition(CelestialBody body, int secondsAgo = 0)
		{
			if (body.orbit == null)
				return body.position;

			if (!bodyPositions.ContainsKey(body.flightGlobalsIndex))
				bodyPositions.Add(body.flightGlobalsIndex, new LinkedList<BodyPositionEntry>());

			var positionList = bodyPositions[body.flightGlobalsIndex];
			foreach (BodyPositionEntry entry in positionList)
			{
				if (entry.time == now - secondsAgo)
					return entry.position;
			}

			while (positionList.Count > 150)
				positionList.RemoveLast();

			var newEntry = new BodyPositionEntry(body, now - secondsAgo);
			positionList.AddFirst(newEntry);
			return newEntry.position;
		}

		internal Vector3d WaypointSurfacePosition(int secondsAgo = 0)
		{
			if (!waypointPositions.ContainsKey(waypoint.navigationId))
				waypointPositions.Add(waypoint.navigationId, new LinkedList<WaypointPositionEntry>());

			var positionList = waypointPositions[waypoint.navigationId];
			foreach(WaypointPositionEntry entry in positionList)
			{
				if (entry.time == now - secondsAgo)
					return entry.position;
			}

			while (positionList.Count > 150)
				positionList.RemoveLast();

			var newEntry = new WaypointPositionEntry(waypoint, now - secondsAgo, BodyPosition(waypoint.celestialBody));
			positionList.AddFirst(newEntry);
			return newEntry.position;
		}

		internal void SetTime(double now)
        {
			this.now = Math.Floor(now); // we're not interested in sub-second precision
        }
	}
}
