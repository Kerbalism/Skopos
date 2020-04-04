using System;
using FinePrint;
using Contracts;
using KERBALISM;
using KSP.Localization;

namespace KerbalismContracts
{
	public class AboveWaypoint : SubRequirement
	{
		private double min_elevation;
		private double min_radial_velocity;
		private double max_radial_velocity;
		private double min_distance;
		private double max_distance;
		private double min_distance_change;
		private int waypoint_index;

		public AboveWaypoint(string type, KerbalismContractRequirement requirement, ConfigNode node) : base(type, requirement)
		{
			min_elevation = Lib.ConfigValue(node, "min_elevation", 0.0);
			min_radial_velocity = Lib.ConfigValue(node, "min_radial_velocity", 0.0);
			max_radial_velocity = Lib.ConfigValue(node, "max_radial_velocity", 0.0);
			min_distance = Lib.ConfigValue(node, "min_distance", 0.0);
			max_distance = Lib.ConfigValue(node, "max_distance", 0.0);
			min_distance_change = Lib.ConfigValue(node, "min_distance_change", 0.0);
			waypoint_index = Lib.ConfigValue(node, "waypoint_index", 0);
		}

		public override string GetTitle(Contracts.Contract contract)
		{
			string waypointName = "waypoint";
			var waypoint = Utils.FetchWaypoint(contract, waypoint_index);
			if (waypoint != null)
				waypointName = waypoint.name;

			string result = Localizer.Format("Min. <<1>>° above <<2>>", min_elevation.ToString("F1"), waypointName);

			if (min_radial_velocity > 0)
				result += ", " + Localizer.Format("min. radial velocity <<1>>°/s", min_radial_velocity.ToString("F1"));

			if (max_radial_velocity > 0)
				result += ", " + Localizer.Format("max. radial velocity <<1>>°/s", max_radial_velocity.ToString("F1"));

			if (min_distance > 0)
				result += ", " + Localizer.Format("min. distance <<1>>", Lib.HumanReadableDistance(min_distance));

			if (max_distance > 0)
				result += ", " + Localizer.Format("max. distance <<1>>", Lib.HumanReadableDistance(max_distance));

			if (min_distance_change > 0)
				result += ", " + Localizer.Format("min. ∆ distance <<1>>", Lib.HumanReadableSpeed(min_distance_change));

			return result;
		}

		internal override bool NeedsWaypoint()
		{
			return true;
		}

		internal override bool CouldBeCandiate(Vessel vessel, Contract contract)
		{
			var waypoint = Utils.FetchWaypoint(contract, waypoint_index);
			if (waypoint == null)
				return false;
			if (waypoint.celestialBody != vessel.mainBody)
				return false;

			var orbit = vessel.orbit;
			if (orbit == null)
				return false;

			// this will not work correctly with contracts that have multiple waypoints
			// var minInclination = Math.Max(0, Math.Abs(waypoint.latitude) - (90 - min_elevation));
			// if (orbit.inclination < minInclination)
			// 	return false;

			return true;
		}

		internal override bool VesselMeetsCondition(Vessel vessel, Contract contract, out string label)
		{
			Waypoint waypoint = Utils.FetchWaypoint(contract, waypoint_index);
			Vector3d vesselPosition = Lib.VesselPosition(vessel);
			double elevation = GetElevation(waypoint, vesselPosition);
			double distance = GetDistance(waypoint, vesselPosition);

			// TODO determine line of sight obstruction (there may be an occluding body)

			string elevationString = Lib.BuildString(elevation.ToString("F1"), "°");
			if (elevation < min_elevation)
				label = Localizer.Format("elev. <<1>>", Lib.Color(elevationString, Lib.Kolor.Red));
			else if (elevation - (90 - min_elevation) / 3 < min_elevation)
				label = Localizer.Format("elev. <<1>>", Lib.Color(elevationString, Lib.Kolor.Yellow));
			else
				label = Localizer.Format("elev. <<1>>", Lib.Color(elevationString, Lib.Kolor.Green));

			bool meetsCondition = elevation >= min_elevation;

			if (min_distance > 0 || max_distance > 0)
			{
				bool distanceMet = true;
				if (min_distance > 0)
					distanceMet &= min_distance <= distance;
				if (max_distance > 0)
					distanceMet &= max_distance >= distance;

				label += " " + Localizer.Format("d <<1>>", Lib.Color(Lib.HumanReadableDistance(distance),
					distanceMet ? Lib.Kolor.Green : Lib.Kolor.Red));

				meetsCondition &= distanceMet;
			}

			if(min_distance_change > 0)
			{
				Vector3d positionIn10s = vessel.orbit.getPositionAtUT(Planetarium.GetUniversalTime() + 10);
				double distanceIn10s = GetDistance(waypoint, positionIn10s);

				var distanceChange = Math.Abs((distance - distanceIn10s) / 10.0);

				label += " " + Localizer.Format("∆d <<1>>", Lib.Color(Lib.HumanReadableSpeed(distanceChange),
					distanceChange >= min_distance_change ? Lib.Kolor.Green : Lib.Kolor.Red));

				meetsCondition &= distanceChange >= min_distance_change;
			}

			if (meetsCondition && (min_radial_velocity > 0 || max_radial_velocity > 0))
			{
				Vector3d positionIn10s = vessel.orbit.getPositionAtUT(Planetarium.GetUniversalTime() + 10);
				double elevationIn10s = GetElevation(waypoint, positionIn10s);

				var radialVelocity = Math.Abs((elevation - elevationIn10s) / 10.0);

				if(min_radial_velocity > 0)
					meetsCondition &= radialVelocity >= min_radial_velocity;

				if(max_radial_velocity > 0)
					meetsCondition &= radialVelocity <= max_radial_velocity;

				label += " " + Lib.Color(radialVelocity.ToString("F1") + "°/s", meetsCondition ? Lib.Kolor.Green : Lib.Kolor.Red);
			}

			if (meetsCondition && min_distance_change > 0)
			{
				Vector3d positionIn10s = vessel.orbit.getPositionAtUT(Planetarium.GetUniversalTime() + 10);

				double elevationIn10s = GetElevation(waypoint, positionIn10s);

				var radialVelocity = Math.Abs((elevation - elevationIn10s) / 10.0);

				if (min_radial_velocity > 0)
					meetsCondition &= radialVelocity >= min_radial_velocity;

				if (max_radial_velocity > 0)
					meetsCondition &= radialVelocity <= max_radial_velocity;

				label += " " + Lib.Color(radialVelocity.ToString("F1") + "°/s", meetsCondition ? Lib.Kolor.Green : Lib.Kolor.Red);
			}

			return meetsCondition;
		}

		private double GetElevation(Waypoint waypoint, Vector3d vesselPosition)
		{
			var waypointPosition = waypoint.celestialBody.GetWorldSurfacePosition(waypoint.latitude, waypoint.longitude, 0);
			var bodyPosition = waypoint.celestialBody.position;

			var a = Vector3d.Angle(vesselPosition - bodyPosition, waypointPosition - bodyPosition);
			var b = Vector3d.Angle(waypointPosition - vesselPosition, bodyPosition - vesselPosition);

			// a + b + elevation = 90 degrees
			return 90.0 - a - b;
		}

		private double GetDistance(Waypoint waypoint, Vector3d vesselPosition)
		{
			var waypointPosition = waypoint.celestialBody.GetWorldSurfacePosition(waypoint.latitude, waypoint.longitude, 0);
			var v = vesselPosition - waypointPosition;
			return v.magnitude;
		}
	}
}
