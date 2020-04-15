using System.Collections.Generic;
using FinePrint;
using KERBALISM;
using System;

namespace KerbalismContracts
{
	public class EvaluationContext
	{
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

			public VesselPositionEntry(Vessel vessel, Vector3d bodyPosition, double time)
			{
				this.vessel = vessel;
				this.time = time;
				this.position = bodyPosition + vessel.orbit.getRelativePositionAtUT(time).xzy;
			}
		}

		// TODO evaluate the quickest caching method here. maybe sorted list doesn't have an advantage on performance, could be that a plain flat list is quicker
		private static readonly Dictionary<Guid, SortedList<double, VesselPositionEntry>> vesselPositions = new Dictionary<Guid, SortedList<double, VesselPositionEntry>>();

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
					position = body.orbit.getTruePositionAtUT(time);
			}
		}
		private static readonly Dictionary<int, SortedList<double, BodyPositionEntry>> bodyPositions = new Dictionary<int, SortedList<double, BodyPositionEntry>>();

		private class WaypointPositionEntry
		{
			internal double id;
			internal Vector3d position;
			internal double time;

			public WaypointPositionEntry(Waypoint waypoint, double time, Vector3d bodyPosition)
				:this(waypoint.latitude, waypoint.longitude, waypoint.celestialBody, time, bodyPosition)
			{
			}

			public WaypointPositionEntry(double latitude, double longitude, CelestialBody body, double time, Vector3d bodyPosition)
			{
				this.id = Id(latitude, longitude);
				this.time = time;

				Planetarium.CelestialFrame BodyFrame = default;
				if (body.rotationPeriod != 0)
				{
					var rotPeriodRecip = 1.0 / body.rotationPeriod;
					var rotationAngle = (body.initialRotation + 360.0 * rotPeriodRecip * time) % 360.0;
					var directRotAngle = (rotationAngle - Planetarium.InverseRotAngle) % 360.0;
					Planetarium.CelestialFrame.PlanetaryFrame(0.0, 90.0, directRotAngle, ref BodyFrame);
				}
				position = BodyFrame.LocalToWorld(body.GetRelSurfacePosition(latitude, longitude, 0).xzy).xzy + bodyPosition;
			}

			public static double Id(double lat, double lon)
			{
				return (lat + 90.0) * 360.0 + (lon + 180.0);
			}

			public static double Id(Waypoint waypoint)
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

		public EvaluationContext(List<double> steps, CelestialBody targetBody = null, Waypoint waypoint = null)
		{
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
			var newEntry = new VesselPositionEntry(vessel, vesselBodyPosition, t);
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

			var newEntry = new BodyPositionEntry(body, now - secondsAgo);
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

			var newEntry = new WaypointPositionEntry(lat, lon, body, now - secondsAgo, BodyPosition(waypoint.celestialBody, secondsAgo));
			positionList.Add(now - secondsAgo, newEntry);
			return newEntry.position;
		}

		internal void SetTime(double now)
        {
			this.now = Math.Floor(now); // we're not interested in sub-second precision
        }
	}
}
