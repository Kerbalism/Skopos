using System.Collections.Generic;
using KERBALISM;

namespace KerbalismContracts
{
	public abstract class SubRequirement
	{
		public string type { get; protected set; }
		public KerbalismContractRequirement parent { get; private set; }

		public abstract string GetTitle(Contracts.Contract contract);

		protected SubRequirement(KerbalismContractRequirement requirement)
		{
			this.parent = requirement;
		}

		/// <summary>
		/// hard test: vessels that do not pass this test will be discarded. Implementation should be quick.
		/// not guaranteed to be called on all vessels (the first failing test will remove the vessel from the
		/// list of candidates)
		/// </summary>
		internal virtual bool CouldBeCandiate(Vessel vessel, Contracts.Contract contract)
		{
			return true;
		}

		/// <summary>
		/// soft filter: runs on ALL vessels that pass the hard filter (CouldBeCandidate),
		/// determine if the vessel currently meets the condition (i.e. currently over location or not)
		/// </summary>
		/// <param name="label">label to add to this vessel in the status display</param>
		internal virtual bool VesselMeetsCondition(Vessel vessel, Contracts.Contract contract, out string label)
		{
			label = string.Empty;
			return true;
		}

		/// <summary>
		/// final filter: looks at the collection of all vessels that passed the hard and soft filters,
		/// use this to check constellations, count vessels etc.
		/// </summary>
		internal virtual bool VesselsMeetCondition(List<Vessel> vessels, int timesConditionMet, Contracts.Contract contract, out string label)
		{
			label = string.Empty;
			return timesConditionMet > 0;
		}

		public static SubRequirement Load(KerbalismContractRequirement requirement, ConfigNode node)
		{
			string type = Lib.ConfigValue(node, "name", "");

			Utils.LogDebug($"Loading sub requirement {type}");

			// TODO doing it like this makes it impossible to dynamically add additional sub reqs
			// in other mods. browse all loaded assemblies for classes that extend SubRequirement
			// and keep them in a static map at runtime...

			switch (type)
			{
				case "AboveWaypoint":
					return new AboveWaypoint(requirement, node);
				case "EquipmentRunning":
					return new EquipmentRunning(requirement, node);
				default:
					return null;
			}
		}

		internal virtual bool NeedsWaypoint()
		{
			return false;
		}
	}
}
