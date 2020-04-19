using ContractConfigurator;
using KSPAchievements;

namespace KerbalismContracts
{
    /// <summary>
    /// ContractRequirement to provide requirement for player having escaped from a specific CelestialBody.
    /// </summary>
    public class KsmEscapeRequirement : ProgressCelestialBodyRequirement
    {
        protected override ProgressNode GetTypeSpecificProgressNode(CelestialBodySubtree celestialBodySubtree)
        {
            return celestialBodySubtree.escape;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            return base.RequirementMet(contract) &&
                GetCelestialBodySubtree().IsComplete;
        }

        protected override string RequirementText()
        {
            string output = "Must " + (invertRequirement ? "not " : "") + "have performed " + AnCheckTypeString() + "escape from " + (targetBody == null ? "the target body" : targetBody.CleanDisplayName(true));

            return output;
        }
    }
}
