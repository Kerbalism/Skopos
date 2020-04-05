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

		private double min_distance;
		private double max_distance;

		// min distance change will be ORed with radial velocity change requirements.
		// so you either need a minimal distance change, OR a minimal radial velocity.
		private double min_relative_speed;
		private double min_radial_velocity;
		private double max_radial_velocity;

		public AboveWaypoint(string type, KerbalismContractRequirement requirement, ConfigNode node) : base(type, requirement)
		{
			min_elevation = Lib.ConfigValue(node, "min_elevation", 0.0);
			min_radial_velocity = Lib.ConfigValue(node, "min_radial_velocity", 0.0);
			max_radial_velocity = Lib.ConfigValue(node, "max_radial_velocity", 0.0);
			min_distance = Lib.ConfigValue(node, "min_distance", 0.0);
			max_distance = Lib.ConfigValue(node, "max_distance", 0.0);
			min_relative_speed = Lib.ConfigValue(node, "min_relative_speed", 0.0);
		}

		public override string GetTitle(RequirementContext context)
		{
			string waypointName = "waypoint";
			var waypoint = Utils.FetchWaypoint(context);
			if (waypoint != null)
				waypointName = waypoint.name;

			string result = Localizer.Format("Min. <<1>>° above <<2>>", min_elevation.ToString("F1"), waypointName);

			if (min_radial_velocity > 0)
				result += ", " + Localizer.Format("min. radial vel. <<1>> °/s", min_radial_velocity.ToString("F1"));

			if (max_radial_velocity > 0)
				result += ", " + Localizer.Format("max. radial vel. <<1>> °/s", max_radial_velocity.ToString("F1"));

			if (min_distance > 0)
				result += ", " + Localizer.Format("min. distance <<1>>", Lib.HumanReadableDistance(min_distance));

			if (max_distance > 0)
				result += ", " + Localizer.Format("max. distance <<1>>", Lib.HumanReadableDistance(max_distance));

			if (min_relative_speed > 0)
				result += ", " + Localizer.Format("min. relative vel. <<1>>", Lib.HumanReadableSpeed(min_relative_speed));

			return result;
		}

		internal override bool NeedsWaypoint()
		{
			return true;
		}

		internal override bool CouldBeCandiate(Vessel vessel, RequirementContext context)
		{
			var waypoint = Utils.FetchWaypoint(context);
			if (waypoint == null)
				return false;
			if (waypoint.celestialBody != vessel.mainBody)
				return false;

			var orbit = vessel.orbit;
			if (orbit == null)
				return false;

			return true;
		}

		internal override bool VesselMeetsCondition(Vessel vessel, RequirementContext context, out string label)
		{
			Waypoint waypoint = Utils.FetchWaypoint(context);
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

			Vector3d positionIn10s = vesselPosition;
			bool changeRequirementsMet = true;

			if(min_relative_speed > 0 || min_radial_velocity > 0 || max_radial_velocity > 0)
			{
				changeRequirementsMet = false;
				positionIn10s = vessel.orbit.getPositionAtUT(Planetarium.GetUniversalTime() + 10);
			}

			if (min_relative_speed > 0)
			{
				double distanceIn10s = GetDistance(waypoint, positionIn10s);
				var distanceChange = Math.Abs((distance - distanceIn10s) / 10.0);

				label += " " + Localizer.Format("rel. velocity <<1>>", Lib.Color(Lib.HumanReadableSpeed(distanceChange),
					distanceChange >= min_relative_speed ? Lib.Kolor.Green : Lib.Kolor.Red));

				changeRequirementsMet |= distanceChange >= min_relative_speed;
			}

			if (min_radial_velocity > 0 || max_radial_velocity > 0)
			{
				double elevationIn10s = GetElevation(waypoint, positionIn10s);
				var radialVelocity = Math.Abs((elevation - elevationIn10s) / 10.0);

				bool radialVelocityRequirementMet = true;
				if(min_radial_velocity > 0)
					radialVelocityRequirementMet &= radialVelocity >= min_radial_velocity;
				if(max_radial_velocity > 0)
					radialVelocityRequirementMet &= radialVelocity <= max_radial_velocity;

				label += " " + Lib.Color(radialVelocity.ToString("F1") + " °/s", radialVelocityRequirementMet ? Lib.Kolor.Green : Lib.Kolor.Red);

				changeRequirementsMet |= radialVelocityRequirementMet;
			}

			meetsCondition &= changeRequirementsMet;

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
