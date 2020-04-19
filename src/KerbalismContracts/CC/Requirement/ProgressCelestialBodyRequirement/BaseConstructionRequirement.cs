using ContractConfigurator;
using KSPAchievements;

namespace KerbalismContracts
{
    /// <summary>
    /// ContractRequirement to provide requirement for player having built a base on a specific CelestialBody.
    /// </summary>
    public class KsmBaseConstructionRequirement : ProgressCelestialBodyRequirement
    {

		protected override ProgressNode GetTypeSpecificProgressNode(CelestialBodySubtree celestialBodySubtree)
		{
            return celestialBodySubtree.baseConstruction;
		}

		public override bool RequirementMet(ConfiguredContract contract)
        {
            return base.RequirementMet(contract) &&
                GetCelestialBodySubtree().IsComplete;
        }

        protected override string RequirementText()
        {
            string output = "Must " + (invertRequirement ? "not " : "") + "have built a " + CheckTypeString() + "base on " + (targetBody == null ? "the target body" : targetBody.CleanDisplayName(true));

            return output;
        }
    }
}
