using System;
using Contracts;
using KERBALISM;
using KSP.Localization;

namespace KerbalismContracts
{
	public class AboveWaypoint : SubRequirement
	{
		private double min_elevation;

		public AboveWaypoint(KerbalismContractRequirement requirement, ConfigNode node) : base(requirement)
		{
			type = "AboveWaypoint";
			min_elevation = Lib.ConfigValue(node, "min_elevation", 0.0);
		}

		public override string GetTitle(Contracts.Contract contract)
		{
			if(contract == null)
				return $"Over waypoint, min. elevation {min_elevation}°";

			var waypoint = Utils.FetchWaypoint(contract);
			if (waypoint == null)
				return "";

			return $"Over {waypoint.name}, min. elevation {min_elevation}°";
		}

		internal override bool NeedsWaypoint()
		{
			return true;
		}

		internal override bool CouldBeCandiate(Vessel vessel, Contract contract)
		{
			var waypoint = Utils.FetchWaypoint(contract);
			if (waypoint == null)
				return false;
			if (waypoint.celestialBody != vessel.mainBody)
				return false;
			return true;
		}

		internal override bool VesselMeetsCondition(Vessel vessel, Contract contract, out string label)
		{
			var waypoint = Utils.FetchWaypoint(contract);

			var waypointPosition = waypoint.celestialBody.GetWorldSurfacePosition(waypoint.latitude, waypoint.longitude, 0);
			var vesselPosition = Lib.VesselPosition(vessel);
			var bodyPosition = waypoint.celestialBody.position;

			var a = Vector3d.Angle(vesselPosition - bodyPosition, waypointPosition - bodyPosition);
			var b = Vector3d.Angle(waypointPosition - vesselPosition, bodyPosition - vesselPosition);

			// a + b + elevation = 90 degrees
			var elevation = 90.0 - a - b;

			// TODO determine line of sight obstruction (there may be an occluding body)

			string elevationString = Lib.BuildString(elevation.ToString("F1"), "°");
			if (elevation < min_elevation)
				label = Localizer.Format("elev. <<1>>", Lib.Color(elevationString, Lib.Kolor.Red));
			else if (elevation - (90 - min_elevation) / 3 < min_elevation)
				label = Localizer.Format("elev. <<1>>", Lib.Color(elevationString, Lib.Kolor.Yellow));
			else
				label = Localizer.Format("elev. <<1>>", Lib.Color(elevationString, Lib.Kolor.Green));

			return elevation >= min_elevation;
		}
	}
}
