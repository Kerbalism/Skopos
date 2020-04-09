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
		public bool changeRequirementsMet;
		public double distanceChange;
		public double radialVelocity;
		internal bool radialVelocityRequirementMet;
		internal double distance10sago;
		internal double elev10sago;
	}

	public class AboveWaypoint : SubRequirement
	{
		private double minElevation;

		private double minDistance;
		private double maxDistance;

		// min distance change will be ORed with radial velocity change requirements.
		// so you either need a minimal distance change, OR a minimal radial velocity.
		private double minRelativeSpeed;

		// radial velocities are in degrees per minute
		private double minRadialVelocity;
		private double maxRadialVelocity;

		public AboveWaypoint(string type, KerbalismContractRequirement requirement, ConfigNode node) : base(type, requirement)
		{
			minElevation = Lib.ConfigValue(node, "minElevation", 0.0);
			minRadialVelocity = Lib.ConfigValue(node, "minRadialVelocity", 0.0);
			maxRadialVelocity = Lib.ConfigValue(node, "maxRadialVelocity", 0.0);
			minDistance = Lib.ConfigValue(node, "minDistance", 0.0);
			maxDistance = Lib.ConfigValue(node, "maxDistance", 0.0);
			minRelativeSpeed = Lib.ConfigValue(node, "minRelativeSpeed", 0.0);
		}

		public override string GetTitle(EvaluationContext context)
		{
			string waypointName = "waypoint";
			if (context?.waypoint != null)
				waypointName = context.waypoint.name;

			string result = Localizer.Format("Min. <<1>>° above <<2>>", minElevation.ToString("F1"), waypointName);

			if (minRadialVelocity > 0)
				result += ", " + Localizer.Format("min. radial vel. <<1>> °/m", minRadialVelocity.ToString("F1"));

			if (maxRadialVelocity > 0)
				result += ", " + Localizer.Format("max. radial vel. <<1>> °/m", maxRadialVelocity.ToString("F1"));

			if (minDistance > 0)
				result += ", " + Localizer.Format("min. distance <<1>>", Lib.HumanReadableDistance(minDistance));

			if (maxDistance > 0)
				result += ", " + Localizer.Format("max. distance <<1>>", Lib.HumanReadableDistance(maxDistance));

			if (minRelativeSpeed > 0)
				result += ", " + Localizer.Format("min. relative vel. <<1>>", Lib.HumanReadableSpeed(minRelativeSpeed));

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

			state.changeRequirementsMet = true;

			if (minRelativeSpeed > 0 || minRadialVelocity > 0 || maxRadialVelocity > 0)
				state.changeRequirementsMet = false;
			
			if (minRelativeSpeed > 0)
			{
				state.distance10sago = GetDistance(vessel, context, 10);
				state.distanceChange = Math.Abs((state.distance10sago - state.distance) / 10.0);
				state.changeRequirementsMet |= state.distanceChange >= minRelativeSpeed;
			}

			if (minRadialVelocity > 0 || maxRadialVelocity > 0)
			{
				state.elev10sago = GetElevation(vessel, context, 10);
				state.radialVelocity = Math.Abs((state.elev10sago - state.elevation) * 6.0); // radial velocity is in degrees/minute

				state.radialVelocityRequirementMet = true;
				if (minRadialVelocity > 0)
					state.radialVelocityRequirementMet &= state.radialVelocity >= minRadialVelocity;
				if (maxRadialVelocity > 0)
					state.radialVelocityRequirementMet &= state.radialVelocity <= maxRadialVelocity;

				state.changeRequirementsMet |= state.radialVelocityRequirementMet;
			}

			meetsCondition &= state.changeRequirementsMet;

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

			if (minRelativeSpeed > 0)
			{
				label += "\n\t" + Localizer.Format("relative velocity: <<1>>", Lib.Color(Lib.HumanReadableSpeed(wpState.distanceChange),
					wpState.distanceChange >= minRelativeSpeed ? Lib.Kolor.Green : Lib.Kolor.Red));
			}

			if (minRadialVelocity > 0 || maxRadialVelocity > 0)
			{
				label += "\n\t" + Localizer.Format("angular velocity: <<1>>", Lib.Color(wpState.radialVelocity.ToString("F1") + " °/m",
					wpState.radialVelocityRequirementMet ? Lib.Kolor.Green : Lib.Kolor.Red));
			}

			return label;
		}

		private double GetElevation(Vessel vessel, EvaluationContext context, int secondsAgo = 0)
		{
			Vector3d waypointPosition = context.WaypointSurfacePosition(secondsAgo);
			Vector3d bodyPosition = context.BodyPosition(context.waypoint.celestialBody, secondsAgo);
			Vector3d vesselPosition = context.VesselPosition(vessel, secondsAgo);

			var a = Vector3d.Angle(vesselPosition - bodyPosition, waypointPosition - bodyPosition);
			var b = Vector3d.Angle(waypointPosition - vesselPosition, bodyPosition - vesselPosition);

			// a + b + elevation = 90 degrees
			return 90.0 - a - b;
		}

		private double GetDistance(Vessel vessel, EvaluationContext context, int secondsAgo = 0)
		{
			var waypointPosition = context.WaypointSurfacePosition(secondsAgo);
			Vector3d vesselPosition = context.VesselPosition(vessel, secondsAgo);
			var v = vesselPosition - waypointPosition;
			return v.magnitude;
		}
	}
}
