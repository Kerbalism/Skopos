using System;
using FinePrint;
using Contracts;
using KERBALISM;
using KSP.Localization;

namespace KerbalismContracts
{
	public class AboveWaypointState: SubRequirementState
	{
		public double elevation;
		public double distance;
		public bool distanceMet;
		public double radialVelocity;
		public double radialVelocityChange;
		public double angularVelocity;
		internal double distance10sago;
		internal double distanceIn10s;
		internal double elev10sago;
		internal bool angularRequirementMet;
	}

	public class AboveWaypoint : SubRequirement
	{
		private double minElevation;

		private double minDistance;
		private double maxDistance;

		private double minRadialVelocity;
		private double minRadialVelocityChange;

		// angular velocities are in degrees per minute
		private double minAngularVelocity;
		private double maxAngularVelocity;

		public AboveWaypoint(string type, KerbalismContractRequirement requirement, ConfigNode node) : base(type, requirement)
		{
			minElevation = Lib.ConfigValue(node, "minElevation", 0.0);
			minAngularVelocity = Lib.ConfigValue(node, "minAngularVelocity", 0.0);
			maxAngularVelocity = Lib.ConfigValue(node, "maxAngularVelocity", 0.0);
			minDistance = Lib.ConfigValue(node, "minDistance", 0.0);
			maxDistance = Lib.ConfigValue(node, "maxDistance", 0.0);
			minRadialVelocity = Lib.ConfigValue(node, "minRadialVelocity", 0.0);
			minRadialVelocityChange = Lib.ConfigValue(node, "minRadialVelocityChange", 0.0);
		}

		public override string GetTitle(EvaluationContext context)
		{
			string waypointName = "waypoint";
			if (context?.waypoint != null)
				waypointName = context.waypoint.name;

			string result = Localizer.Format("Min. <<1>>° above <<2>>", minElevation.ToString("F1"), waypointName);

			if (minAngularVelocity > 0)
				result += ", " + Localizer.Format("min. angular vel. <<1>> °/m", minAngularVelocity.ToString("F1"));

			if (maxAngularVelocity > 0)
				result += ", " + Localizer.Format("max. angular vel. <<1>> °/m", maxAngularVelocity.ToString("F1"));

			if (minDistance > 0)
				result += ", " + Localizer.Format("min. distance <<1>>", Lib.HumanReadableDistance(minDistance));

			if (maxDistance > 0)
				result += ", " + Localizer.Format("max. distance <<1>>", Lib.HumanReadableDistance(maxDistance));

			if (minRadialVelocity > 0)
				result += ", " + Localizer.Format("min. radial vel. <<1>>", Lib.HumanReadableSpeed(minRadialVelocity));

			if (minRadialVelocityChange > 0)
				result += ", " + Localizer.Format("min. radial vel. change <<1>>/s", Lib.HumanReadableSpeed(minRadialVelocityChange));

			return result;
		}

		internal override bool NeedsWaypoint()
		{
			return true;
		}

		internal override bool CouldBeCandiate(Vessel vessel, EvaluationContext context)
		{
			if (context.waypoint == null)
				return false;
			if (context.waypoint.celestialBody != vessel.mainBody)
				return false;

			var orbit = vessel.orbit;
			if (orbit == null)
				return false;

			return true;
		}

		internal override SubRequirementState VesselMeetsCondition(Vessel vessel, EvaluationContext context)
		{
			AboveWaypointState state = new AboveWaypointState();

			state.elevation = GetElevation(vessel, context);
			state.distance = GetDistance(vessel, context);

			bool meetsCondition = state.elevation >= minElevation;

			if (minDistance > 0 || maxDistance > 0)
			{
				state.distanceMet = true;
				if (minDistance > 0)
					state.distanceMet &= minDistance <= state.distance;
				if (maxDistance > 0)
					state.distanceMet &= maxDistance >= state.distance;

				meetsCondition &= state.distanceMet;
			}
			
			if (minRadialVelocity > 0 || minRadialVelocityChange > 0)
			{
				state.distance10sago = GetDistance(vessel, context, 10);
				state.radialVelocity = Math.Abs((state.distance10sago - state.distance) / 10.0);

				if (minRadialVelocity > 0)
					meetsCondition &= state.radialVelocity >= minRadialVelocity;

				if(minRadialVelocityChange > 0)
				{
					state.distanceIn10s = GetDistance(vessel, context, -10);
					var radialVelocityIn10s = Math.Abs((state.distanceIn10s - state.distance) / 10.0);
					state.radialVelocityChange = Math.Abs((radialVelocityIn10s - state.radialVelocity) / 10.0);
					meetsCondition &= state.radialVelocityChange >= minRadialVelocityChange;
					//Utils.LogDebug($"{context.waypoint.name} {context.now} el {state.elevation.ToString("F2")} dist {state.distance.ToString("F0")} dist-10s {state.distance10sago.ToString("F0")} dist-20s {state.distanceIn10s.ToString("F0")} radvel {state.radialVelocity.ToString("F1")} radvel-10s {radialVelocityIn10s.ToString("F1")} delta radvel {state.radialVelocityChange.ToString("F1")}");
				}
			}

			if (minAngularVelocity > 0 || maxAngularVelocity > 0)
			{
				state.elev10sago = GetElevation(vessel, context, 10);
				state.angularVelocity = Math.Abs((state.elev10sago - state.elevation) * 6.0); // radial velocity is in degrees/minute
				state.angularRequirementMet = true;

				if (minAngularVelocity > 0)
					state.angularRequirementMet &= state.angularVelocity >= minAngularVelocity;
				if (maxAngularVelocity > 0)
					state.angularRequirementMet &= state.angularVelocity <= maxAngularVelocity;

				meetsCondition &= state.angularRequirementMet;
			}

			state.requirementMet = meetsCondition;

			return state;
		}

		internal override string GetLabel(Vessel vessel, EvaluationContext context, SubRequirementState state)
		{
			string label = string.Empty;

			AboveWaypointState wpState = (AboveWaypointState)state;

			string elevationString = Lib.BuildString(wpState.elevation.ToString("F1"), " °");
			if (wpState.elevation < minElevation)
				label = Localizer.Format("elevation above <<1>>: <<2>>",
					context.waypoint.name, Lib.Color(elevationString, Lib.Kolor.Red));
			else if (wpState.elevation - (90 - minElevation) / 3 < minElevation)
				label = Localizer.Format("elevation above <<1>>: <<2>>",
					context.waypoint.name, Lib.Color(elevationString, Lib.Kolor.Yellow));
			else
				label = Localizer.Format("elevation above <<1>>: <<2>>",
					context.waypoint.name, Lib.Color(elevationString, Lib.Kolor.Green));

			if (minDistance > 0 || maxDistance > 0)
			{
				label += "\n\t" + Localizer.Format("distance: <<1>>", Lib.Color(Lib.HumanReadableDistance(wpState.distance),
					wpState.distanceMet ? Lib.Kolor.Green : Lib.Kolor.Red));
			}

			if (minRadialVelocity > 0)
			{
				label += "\n\t" + Localizer.Format("radial velocity: <<1>>", Lib.Color(Lib.HumanReadableSpeed(wpState.radialVelocity),
					wpState.radialVelocity >= minRadialVelocity ? Lib.Kolor.Green : Lib.Kolor.Red));
			}

			if (minRadialVelocityChange > 0)
			{
				var radialVelocityChangeString = Lib.HumanReadableSpeed(wpState.radialVelocityChange) + "/s";
				label += "\n\t" + Localizer.Format("radial velocity change: <<1>>", Lib.Color(radialVelocityChangeString,
					wpState.radialVelocityChange >= minRadialVelocityChange ? Lib.Kolor.Green : Lib.Kolor.Red));
			}

			if (minAngularVelocity > 0 || maxAngularVelocity > 0)
			{
				label += "\n\t" + Localizer.Format("angular velocity: <<1>>", Lib.Color(wpState.angularVelocity.ToString("F1") + " °/m",
					wpState.angularRequirementMet ? Lib.Kolor.Green : Lib.Kolor.Red));
			}

			return label;
		}

		internal static double GetElevation(Vessel vessel, EvaluationContext context, int secondsAgo = 0)
		{
			var wp = context.waypoint;
			return GetElevation(vessel, wp.latitude, wp.longitude, wp.celestialBody, context, secondsAgo);
		}

		internal static double GetElevation(Vessel vessel, double lat, double lon, CelestialBody body, EvaluationContext context, int secondsAgo = 0)
		{
			Vector3d waypointPosition = context.SurfacePosition(lat, lon, body, secondsAgo);
			Vector3d bodyPosition = context.BodyPosition(body, secondsAgo);
			Vector3d vesselPosition = context.VesselPosition(vessel, secondsAgo);

			var a = Vector3d.Angle(vesselPosition - bodyPosition, waypointPosition - bodyPosition);
			var b = Vector3d.Angle(waypointPosition - vesselPosition, bodyPosition - vesselPosition);

			// Utils.LogDebug($"wp {waypointPosition} body {bodyPosition} vessel {vesselPosition} a {a} b {b}");

			// a + b + elevation = 90 degrees
			return 90.0 - a - b;
		}

		internal static double GetDistance(Vessel vessel, EvaluationContext context, int secondsAgo = 0)
		{
			var waypointPosition = context.WaypointSurfacePosition(secondsAgo);
			Vector3d vesselPosition = context.VesselPosition(vessel, secondsAgo);
			var v = vesselPosition - waypointPosition;
			return v.magnitude;
		}
	}
}
