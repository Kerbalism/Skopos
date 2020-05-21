using System.Collections.Generic;
using KERBALISM;
using KSP.Localization;
using System;

namespace KerbalismContracts
{
	public class ObserveBodyState : SubRequirementState
	{
		internal double distance;
		internal CelestialBody occluder;
		internal double angularVelocity;
		internal bool angularRequirementMet;
	}

	public class ObserveBody : SubRequirement
	{
		internal double maxDistance;
		internal double maxDistanceAU;
		internal double minSurface;
		internal double minElevation;
		internal double minAngularVelocity;
		internal double maxAngularVelocity;

		public ObserveBody(string type, KerbalismContractRequirement requirement, ConfigNode node) : base(type, requirement)
		{
			maxDistance = Lib.ConfigValue(node, "maxDistance", 0.0);
			maxDistanceAU = Lib.ConfigValue(node, "maxDistanceAU", 0.0);
			minSurface = Lib.ConfigValue(node, "minSurface", 0.0);
			minAngularVelocity = Lib.ConfigValue(node, "minAngularVelocity", 0.0);
			maxAngularVelocity = Lib.ConfigValue(node, "maxAngularVelocity", 0.0);
			minElevation = Lib.ConfigValue(node, "minElevation", 0.0);
		}

		public override string GetTitle(EvaluationContext context)
		{
			string targetName = context?.targetBody?.displayName.LocalizeRemoveGender() ?? Localizer.Format("#KerCon_ABody");
			string result = Localizer.Format("#KerCon_LineOfSightToX", targetName); // Line of sight to <<1>>

			double distance = maxDistance;
			if (distance == 0 && maxDistanceAU != 0)
				distance = Sim.AU * maxDistanceAU;

			if (distance != 0)
				result += ", " + Localizer.Format("#KerCon_MaxDistanceX", Lib.HumanReadableDistance(distance));

			if (minSurface != 0)
				result += ", " + Localizer.Format("#KerCon_XofSurfaceObserved", Lib.HumanReadablePerc(minSurface / 100.0)); // <<1>> of surface observed

			if (minAngularVelocity > 0)
				result += ", " + Localizer.Format("#KerCon_MinAngularVelX", minAngularVelocity.ToString("F1"));

			if (maxAngularVelocity > 0)
				result += ", " + Localizer.Format("#KerCon_MaxAngularVelX", maxAngularVelocity.ToString("F1"));

			return result;
		}

		internal bool TrackToCommonAncestor(CelestialBody vesselMainBody, CelestialBody targetBody,
			out CelestialBody vesselAnchestor, out CelestialBody targetAnchestor)
		{
			targetAnchestor = targetBody;
			do
			{
				vesselAnchestor = vesselMainBody;
				do
				{
					if (vesselAnchestor.referenceBody == targetAnchestor.referenceBody)
						return true;
					vesselAnchestor = vesselAnchestor.referenceBody;
				} while (vesselAnchestor != null);

				targetAnchestor = targetAnchestor.referenceBody;
			} while (targetAnchestor != null);

			return false;
		}

		internal override bool CouldBeCandiate(Vessel vessel, EvaluationContext context)
		{
			if (context.targetBody == null || vessel.orbit == null)
				return false;

			double distance = maxDistance;
			if (distance == 0 && maxDistanceAU != 0)
				distance = Sim.AU * maxDistanceAU;

			// if distance is not a limiting factor, every vessel is a candidate
			if (distance == 0)
				return true;

			// if vessel is orbiting target body...
			if(vessel.mainBody == context.targetBody)
				return vessel.orbit.PeA < distance;

			// look for a parent body that is orbiting our target body (i.e. moon -> earth -> sun = target)
			var vesselOrbitingBody = vessel.mainBody;
			while (vesselOrbitingBody.referenceBody != null && vesselOrbitingBody.referenceBody != context.targetBody)
				vesselOrbitingBody = vesselOrbitingBody.referenceBody;

			if(vesselOrbitingBody.referenceBody == context.targetBody)
				return vesselOrbitingBody.orbit.PeA < distance;


			// our main body is not orbiting our target, find a common anchestor
			// f.i. we need a line of sight to venus, but are currently orbiting earth
			// or we're orbiting minmus and need a line of sight to gilly
			CelestialBody targetOrbitingBody;
			if (!TrackToCommonAncestor(vessel.mainBody, context.targetBody, out vesselOrbitingBody, out targetOrbitingBody))
				return false;

			return vesselOrbitingBody.orbit.PeA + targetOrbitingBody.orbit.PeA < distance;
		}

		internal override SubRequirementState VesselMeetsCondition(Vessel vessel, EvaluationContext context)
		{
			ObserveBodyState state = new ObserveBodyState();

			var body = context.targetBody;
			
			// generate ray parameters
			var vesselPosition = context.VesselPosition(vessel);
			var bodyDir = context.BodyPosition(body) - vesselPosition;
			state.distance = bodyDir.magnitude;

			bodyDir /= state.distance;
			state.distance -= body.Radius;

			double distance = maxDistance;
			if (distance == 0 && maxDistanceAU != 0)
				distance = Sim.AU * maxDistanceAU;

			if (distance != 0 && state.distance > distance)
			{
				state.requirementMet = false;
				return state;
			}

			if (minAngularVelocity > 0 || maxAngularVelocity > 0)
			{
				var elevation = AboveWaypoint.GetElevation(vessel, 0, 0, body, context);
				var elevation10s = AboveWaypoint.GetElevation(vessel, 0, 0, body, context, 10);

				state.angularVelocity = Math.Abs((elevation10s - elevation) * 6.0); // radial velocity is in degrees/minute
				state.angularRequirementMet = true;

				if (minAngularVelocity > 0)
					state.angularRequirementMet &= state.angularVelocity >= minAngularVelocity;
				if (maxAngularVelocity > 0)
					state.angularRequirementMet &= state.angularVelocity <= maxAngularVelocity;

				state.requirementMet &= state.angularRequirementMet;
			}

			VesselData vd;
			if (!vessel.TryGetVesselData(out vd))
			{
				state.requirementMet = false;
				return state;
			}

			// check if the ray intersects with an occluder
			foreach (CelestialBody occludingBody in vd.VisibleBodies)
			{
				if (occludingBody == body) continue;
				if (!Sim.RayAvoidBody(vesselPosition, bodyDir, state.distance, occludingBody))
				{
					state.occluder = occludingBody;
					state.requirementMet = false;
					return state;
				}
			}

			state.requirementMet = true;
			return state;
		}

		internal override string GetLabel(Vessel vessel, EvaluationContext context, SubRequirementState state)
		{
			string targetName = context?.targetBody?.displayName.LocalizeRemoveGender() ?? Localizer.Format("#KerCon_ABody");

			ObserveBodyState losState = (ObserveBodyState) state;

			if (!losState.requirementMet)
			{
				if (losState.occluder != null)
					return Localizer.Format("#KerCon_OccludedByX", Lib.Color(losState.occluder.displayName.LocalizeRemoveGender(), Lib.Kolor.Red)); // Occluded by <<1>>
				if (losState.distance > 0)
					return Localizer.Format("#KerCon_DistanceX", Lib.Color(Lib.HumanReadableDistance(losState.distance), Lib.Kolor.Red));

				if(!losState.angularRequirementMet && (minAngularVelocity != 0 || maxAngularVelocity != 0))
				{
					string angularVelocityStr = Lib.Color(losState.angularVelocity.ToString("F1") + " °/m", Lib.Kolor.Red);
					return Localizer.Format("#KerCon_AngularVelX", angularVelocityStr);
				}
				return Localizer.Format("#KerCon_XisY", targetName, Lib.Color(Localizer.Format("#KerCon_NotVisible"), Lib.Kolor.Red));
			}

			string result = Localizer.Format("#KerCon_XisY", targetName, Lib.Color(Localizer.Format("#KerCon_Visible"), Lib.Kolor.Green));
			if (losState.angularRequirementMet)
			{
				string angularVelocityStr = Lib.Color(losState.angularVelocity.ToString("F1") + " °/m", Lib.Kolor.Green);
				result += ", " + Localizer.Format("#KerCon_AngularVelX", angularVelocityStr);
			}
			return result;
		}

		internal override bool VesselsMeetCondition(List<Vessel> vessels, EvaluationContext context, out string statusLabel)
		{
			if (minSurface == 0 || vessels.Count == 0)
			{
				statusLabel = string.Empty;
				return vessels.Count > 0;
			}

			Vector3d bodyPosition = context.BodyPosition(context.targetBody);

			double visible = 100.0 * BodySurfaceObservation.VisibleSurface(vessels, context, bodyPosition, minElevation, Surfaces());
			string observedPercStr = Lib.HumanReadablePerc(visible / 100.0) + " / " + Lib.HumanReadablePerc(minSurface / 100.0);
			observedPercStr = Lib.Color(observedPercStr, visible > minSurface ? Lib.Kolor.Green : Lib.Kolor.Red);
			statusLabel = Localizer.Format("#KerCon_XofSurfaceObserved", observedPercStr);
			return visible > minSurface;
		}

		private static List<Surface> surfaces;
		private List<Surface> Surfaces()
		{
			if(surfaces == null)
				surfaces = BodySurfaceObservation.CreateVisibleSurfaces();
			return surfaces;
		}
	}
}
