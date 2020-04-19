using ContractConfigurator;
using KSPAchievements;

namespace KerbalismContracts
{
    /// <summary>
    /// ContractRequirement to provide requirement for player having performed returned from the
    /// surface of a specific CelestialBody.
    /// </summary>
    public class KsmReturnFromSurfaceRequirement : ProgressCelestialBodyRequirement
    {
        protected override ProgressNode GetTypeSpecificProgressNode(CelestialBodySubtree celestialBodySubtree)
        {
            return celestialBodySubtree.returnFromSurface;
        }

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            // This appears bugged - returnFromSurface is null
            return base.RequirementMet(contract) &&
                GetCelestialBodySubtree().IsComplete;
        }

        protected override string RequirementText()
        {
            string output = "Must " + (invertRequirement ? "not " : "") + "have returned from " + ACheckTypeString() + "landing on " + (targetBody == null ? "the target body" : targetBody.CleanDisplayName(true));

            return output;
        }
    }
}
