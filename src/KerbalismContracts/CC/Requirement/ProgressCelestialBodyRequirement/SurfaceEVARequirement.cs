using ContractConfigurator;
using KSPAchievements;

namespace KerbalismContracts
{
    /// <summary>
    /// ContractRequirement to provide requirement for player having performed a surface EVA on a specific CelestialBody.
    /// </summary>
    public class KsmSurfaceEVARequirement : ProgressCelestialBodyRequirement
    {
        protected override ProgressNode GetTypeSpecificProgressNode(CelestialBodySubtree celestialBodySubtree)
        {
            return celestialBodySubtree.surfaceEVA;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            return base.RequirementMet(contract) &&
                GetCelestialBodySubtree().IsComplete;
        }

        protected override string RequirementText()
        {
            string output = "Must " + (invertRequirement ? "not " : "") + "have performed a surface EVA on " + (targetBody == null ? "the target body" : targetBody.CleanDisplayName(true));
            return output;
        }
    }
}
