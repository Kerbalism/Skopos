using System;
namespace KerbalismContracts
{
	public interface IUniverseEvaluator
	{
		Vector3d GetVesselPosition(Vessel vessel, Vector3d vesselBodyPosition, double time);
		Vector3d GetBodyPosition(CelestialBody body, double time);
		Vector3d GetWaypointPosition(double latitude, double longitude, CelestialBody body, Vector3d bodyPosition, double time);
	}

	public class StockUniverseEvaluator : IUniverseEvaluator
	{
		public Vector3d GetBodyPosition(CelestialBody body, double time)
		{
			if (body.orbit == null) // suns?
				return body.position;
			else
				return body.orbit.getTruePositionAtUT(time);
		}

		public Vector3d GetVesselPosition(Vessel vessel, Vector3d vesselBodyPosition, double time)
		{
			return vesselBodyPosition + vessel.orbit.getRelativePositionAtUT(time).xzy;
		}

		public Vector3d GetWaypointPosition(double latitude, double longitude, CelestialBody body, Vector3d bodyPosition, double time)
		{
			Planetarium.CelestialFrame BodyFrame = default;
			if (body.rotationPeriod != 0)
			{
				var rotPeriodRecip = 1.0 / body.rotationPeriod;
				var rotationAngle = (body.initialRotation + 360.0 * rotPeriodRecip * time) % 360.0;
				var directRotAngle = (rotationAngle - Planetarium.InverseRotAngle) % 360.0;
				Planetarium.CelestialFrame.PlanetaryFrame(0.0, 90.0, directRotAngle, ref BodyFrame);
			}
			return BodyFrame.LocalToWorld(body.GetRelSurfacePosition(latitude, longitude, 0).xzy).xzy + bodyPosition;
		}
	}
}
