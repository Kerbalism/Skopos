using System.Collections.Generic;
using KERBALISM;
using ContractConfigurator;
using KSP.Localization;

namespace KerbalismContracts
{
	public class LosState : SubRequirementState
	{
		internal double distance;
		internal CelestialBody occluder;
	}

	public class LineOfSight : SubRequirement
	{
		internal double maxDistance;
		internal double maxDistanceAU;

		public LineOfSight(string type, KerbalismContractRequirement requirement, ConfigNode node) : base(type, requirement)
		{
			maxDistance = Lib.ConfigValue(node, "maxDistance", 0.0);
			maxDistanceAU = Lib.ConfigValue(node, "maxDistanceAU", 0.0);

			if(maxDistance == 0 && maxDistanceAU != 0)
				maxDistance = Sim.AU * maxDistanceAU;
		}

		public override string GetTitle(EvaluationContext context)
		{
			string targetName = context?.targetBody?.displayName ?? "target";

			if(maxDistance == 0)
				return Localizer.Format("Line of sight to <<1>>", targetName);
			else
				return Localizer.Format("Line of sight to <<1>> (max. distance <<2>>)",
					targetName, Lib.HumanReadableDistance(maxDistance));
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

			// if distance is not a limiting factor, every vessel is a candidate
			if (maxDistance == 0)
				return true;

			// if vessel is orbiting target body...
			if(vessel.mainBody == context.targetBody)
				return vessel.orbit.PeA < maxDistance;

			// look for a parent body that is orbiting our target body (i.e. moon -> earth -> sun = target)
			var vesselOrbitingBody = vessel.mainBody;
			while (vesselOrbitingBody.referenceBody != null && vesselOrbitingBody.referenceBody != context.targetBody)
				vesselOrbitingBody = vesselOrbitingBody.referenceBody;

			if(vesselOrbitingBody.referenceBody == context.targetBody)
				return vesselOrbitingBody.orbit.PeA < maxDistance;


			// our main body is not orbiting our target, find a common anchestor
			// f.i. we need a line of sight to venus, but are currently orbiting earth
			// or we're orbiting minmus and need a line of sight to gilly
			CelestialBody targetOrbitingBody;
			if (!TrackToCommonAncestor(vessel.mainBody, context.targetBody, out vesselOrbitingBody, out targetOrbitingBody))
				return false;

			return vesselOrbitingBody.orbit.PeA + targetOrbitingBody.orbit.PeA < maxDistance;
		}

		internal override SubRequirementState VesselMeetsCondition(Vessel vessel, EvaluationContext context)
		{
			LosState state = new LosState();

			var body = context.targetBody;

			// generate ray parameters
			var vesselPosition = context.VesselPosition(vessel);
			var bodyDir = context.BodyPosition(body) - vesselPosition;
			state.distance = bodyDir.magnitude;

			bodyDir /= state.distance;
			state.distance -= body.Radius;

			if (state.distance > maxDistance)
			{
				state.requirementMet = false;
				return state;
			}

			VesselData vd;
			if (!vessel.TryGetVesselData(out vd))
			{
				state.requirementMet = false;
				return state;
			}

			// check if the ray intersects with an occluder
			foreach (CelestialBody occludingBody in vd.EnvVisibleBodies)
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
			string targetName = context?.targetBody?.displayName ?? "target";

			LosState losState = (LosState) state;

			if (!losState.requirementMet)
			{
				if (losState.occluder != null)
					return Localizer.Format("Occluded by <<1>>", Lib.Color(losState.occluder.displayName, Lib.Kolor.Red));
				if (losState.distance > 0)
					return Localizer.Format("Distance <<1>>", Lib.Color(Lib.HumanReadableDistance(losState.distance), Lib.Kolor.Red));
				return Localizer.Format("<<1>> is <<2>>", targetName, Lib.Color("not visible", Lib.Kolor.Red));
			}
			return Localizer.Format("<<1>> is <<2>>", targetName, Lib.Color("visible", Lib.Kolor.Green));
		}
	}
}
