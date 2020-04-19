using ContractConfigurator;
using KSPAchievements;

namespace KerbalismContracts
{
    /// <summary>
    /// ContractRequirement to provide requirement for player having rendezvoued with a specific CelestialBody.
    /// </summary>
    public class KsmRendezvousRequirement : ProgressCelestialBodyRequirement
    {
        protected override ProgressNode GetTypeSpecificProgressNode(CelestialBodySubtree celestialBodySubtree)
        {
            return celestialBodySubtree.rendezvous;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            return base.RequirementMet(contract) &&
                GetCelestialBodySubtree().IsComplete;
        }

        protected override string RequirementText()
        {
            string output = "Must " + (invertRequirement ? "not " : "") + "have performed " + ACheckTypeString() + "rendezvous near " + (targetBody == null ? "the target body" : targetBody.CleanDisplayName(true));

            return output;
        }
    }
}
