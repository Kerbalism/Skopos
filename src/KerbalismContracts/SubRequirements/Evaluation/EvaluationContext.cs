using System.Collections.Generic;
using FinePrint;
using System;

namespace KerbalismContracts
{
	public class EvaluationContext
	{
		internal readonly IUniverseEvaluator evaluator;

		public readonly Waypoint waypoint;
		public readonly CelestialBody targetBody;
		public readonly List<double> steps;
		public double now;
		public readonly double ut = Planetarium.GetUniversalTime();

		private class VesselPositionEntry
		{
			internal Vessel vessel;
			internal Vector3d position;
			internal double time;

			public VesselPositionEntry(IUniverseEvaluator evaluator, Vessel vessel, Vector3d vesselBodyPosition, double time)
			{
				this.vessel = vessel;
				this.time = time;
				this.position = evaluator.GetVesselPosition(vessel, vesselBodyPosition, time);
			}
		}

		// TODO evaluate the quickest caching method here. maybe sorted list doesn't have an advantage on performance, could be that a plain flat list is quicker
		private static readonly Dictionary<Guid, SortedList<double, VesselPositionEntry>> vesselPositions = new Dictionary<Guid, SortedList<double, VesselPositionEntry>>();

		private class BodyPositionEntry
		{
			internal int flightGlobalsIndex;
			internal Vector3d position;
			internal double time;

			public BodyPositionEntry(IUniverseEvaluator evaluator, CelestialBody body, double time)
			{
				this.flightGlobalsIndex = body.flightGlobalsIndex;
				this.time = time;
				this.position = evaluator.GetBodyPosition(body, time);
			}
		}
		private static readonly Dictionary<int, SortedList<double, BodyPositionEntry>> bodyPositions = new Dictionary<int, SortedList<double, BodyPositionEntry>>();

		private class WaypointPositionEntry
		{
			internal double id;
			internal Vector3d position;
			internal double time;

			public WaypointPositionEntry(IUniverseEvaluator evaluator, Waypoint waypoint, double time, Vector3d bodyPosition)
				:this(evaluator, waypoint.latitude, waypoint.longitude, waypoint.celestialBody, time, bodyPosition)
			{
			}

			public WaypointPositionEntry(IUniverseEvaluator evaluator, double latitude, double longitude, CelestialBody body, double time, Vector3d bodyPosition)
			{
				this.id = Id(latitude, longitude);
				this.time = time;

				this.position = evaluator.GetWaypointPosition(latitude, longitude, body, bodyPosition, time);
			}

			internal static double Id(double lat, double lon)
			{
				return (lat + 90.0) * 360.0 + (lon + 180.0);
			}

			internal static double Id(Waypoint waypoint)
			{
				return Id(waypoint.latitude, waypoint.longitude);
			}
		}

		internal double Altitude(Vessel vessel, CelestialBody referenceBody = null)
		{
			CelestialBody body = referenceBody ?? vessel.mainBody;
			Vector3d vesselPosition = VesselPosition(vessel);
			Vector3d bodyPosition = BodyPosition(body);
			return (vesselPosition - bodyPosition).magnitude - body.Radius;
		}

		private static readonly Dictionary<double, SortedList<double, WaypointPositionEntry>> waypointPositions = new Dictionary<double, SortedList<double, WaypointPositionEntry>>();

		public static void Clear()
		{
			vesselPositions.Clear();
			bodyPositions.Clear();
			waypointPositions.Clear();
		}

		public EvaluationContext(IUniverseEvaluator evaluator, List<double> steps, CelestialBody targetBody = null, Waypoint waypoint = null)
		{
			this.evaluator = evaluator;
			this.targetBody = targetBody ?? waypoint?.celestialBody;
			this.waypoint = waypoint;
			this.steps = steps;
		}

		internal Vector3d VesselPosition(Vessel vessel, int secondsAgo = 0)
		{
			if (!vesselPositions.ContainsKey(vessel.id))
				vesselPositions.Add(vessel.id, new SortedList<double, VesselPositionEntry>());

			var positionList = vesselPositions[vessel.id];

			double t = now - secondsAgo;
			VesselPositionEntry entry;
			if(positionList.TryGetValue(t, out entry))
				return entry.position;

			while (positionList.Count > 150)
				positionList.RemoveAt(0);

			var vesselBodyPosition = BodyPosition(vessel.mainBody, secondsAgo);
			var newEntry = new VesselPositionEntry(evaluator, vessel, vesselBodyPosition, t);
			positionList.Add(t, newEntry);
			return newEntry.position;
		}

		internal Vector3d BodyPosition(CelestialBody body, int secondsAgo = 0)
		{
			if (body.orbit == null)
				return body.position;

			if (!bodyPositions.ContainsKey(body.flightGlobalsIndex))
				bodyPositions.Add(body.flightGlobalsIndex, new SortedList<double, BodyPositionEntry>());

			var positionList = bodyPositions[body.flightGlobalsIndex];
			BodyPositionEntry entry;
			if(positionList.TryGetValue(now - secondsAgo, out entry))
				return entry.position;

			while (positionList.Count > 150)
				positionList.RemoveAt(0);

			var newEntry = new BodyPositionEntry(evaluator, body, now - secondsAgo);
			positionList.Add(now - secondsAgo, newEntry);
			return newEntry.position;
		}

		internal Vector3d WaypointSurfacePosition(int secondsAgo = 0)
		{
			return SurfacePosition(waypoint.latitude, waypoint.longitude, waypoint.celestialBody, secondsAgo);
		}

		internal Vector3d SurfacePosition(double lat, double lon, CelestialBody body, int secondsAgo = 0)
		{
			if (!waypointPositions.ContainsKey(WaypointPositionEntry.Id(lat, lon)))
				waypointPositions.Add(WaypointPositionEntry.Id(lat, lon), new SortedList<double, WaypointPositionEntry>());

			var positionList = waypointPositions[WaypointPositionEntry.Id(lat, lon)];
			WaypointPositionEntry entry;
			if (positionList.TryGetValue(now - secondsAgo, out entry))
				return entry.position;

			while (positionList.Count > 150)
				positionList.RemoveAt(0);

			var newEntry = new WaypointPositionEntry(evaluator, lat, lon, body, now - secondsAgo, BodyPosition(body, secondsAgo));
			positionList.Add(now - secondsAgo, newEntry);
			return newEntry.position;
		}

		internal void SetTime(double now)
        {
			this.now = Math.Floor(now); // we're not interested in sub-second precision
        }
	}
}
