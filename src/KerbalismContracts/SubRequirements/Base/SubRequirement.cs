using System.Collections.Generic;
using System.Reflection;
using System;
using Contracts;
using FinePrint;
using System.Text;
using System.Threading.Tasks;
using KERBALISM;

namespace KerbalismContracts
{
	public abstract class SubRequirement
	{
		private static Dictionary<string, ConstructorInfo> subRequirementActivators = new Dictionary<string, ConstructorInfo>();

		public string type { get; private set; }
		public KerbalismContractRequirement parent { get; private set; }

		public abstract string GetTitle(EvaluationContext context);

		protected SubRequirement(string type, KerbalismContractRequirement parent)
		{
			this.type = type;
			this.parent = parent;
		}

		/// <summary>
		/// hard test: vessels that do not pass this test will be discarded. Implementation should be quick.
		/// not guaranteed to be called on all vessels (the first failing test will remove the vessel from the
		/// list of candidates)
		/// </summary>
		internal virtual bool CouldBeCandiate(Vessel vessel, EvaluationContext context)
		{
			return true;
		}

		/// <summary>
		/// soft filter: runs on ALL vessels that pass the hard filter (CouldBeCandidate),
		/// determine if the vessel currently meets the condition (i.e. currently over location or not)
		/// </summary>
		internal virtual SubRequirementState VesselMeetsCondition(Vessel vessel, EvaluationContext context)
		{
			return new Stateless();
		}

		/// <summary> return the text to display in the contract UI as part of the individual vessel state </summary>
		internal virtual string GetLabel(Vessel vessel, EvaluationContext context, SubRequirementState state)
		{
			return string.Empty;
		}

		/// <summary>
		/// final filter: looks at the collection of all vessels that passed the hard and soft filters,
		/// use this to check constellations, count vessels etc.
		/// </summary>
		internal virtual bool VesselsMeetCondition(List<Vessel> vessels, EvaluationContext context, out string statusLabel)
		{
			statusLabel = string.Empty;
			return vessels.Count > 0;
		}

		private static void InitSubRequirementActivators()
		{
			Utils.LogDebug("Initializing sub requirement types...");

			Type[] constructorTypes = new Type[] { typeof(string), typeof(KerbalismContractRequirement), typeof(ConfigNode) };
			Type subRequirementType = typeof(SubRequirement);
			foreach (var a in AssemblyLoader.loadedAssemblies)
			{
				AssemblyName nameObject = new AssemblyName(a.assembly.FullName);
				string assemblyName = nameObject.Name;

				foreach (Type t in a.assembly.GetTypes())
				{
					if (t.IsAbstract || !t.IsClass || !subRequirementType.IsAssignableFrom(t))
						continue;

					ConstructorInfo ctor = t.GetConstructor(constructorTypes);
					if (ctor == null)
					{
						Utils.Log($"No valid constructor found for sub requirement type '{t.Name}' in {assemblyName}", LogLevel.Error);
						continue;
					}

					Utils.Log($"Registering sub requirement type '{t.Name}' from {assemblyName}");
					subRequirementActivators.Add(t.Name, ctor);
				}
			}
		}

		public static SubRequirement Load(KerbalismContractRequirement requirement, ConfigNode node)
		{
			if(subRequirementActivators.Count == 0)
				InitSubRequirementActivators();

			string type = Lib.ConfigValue(node, "name", "");

			if (!subRequirementActivators.ContainsKey(type))
			{
				Utils.Log($"Will ignore unknown sub requirement type {type} in {requirement.name}", LogLevel.Error);
				return null;
			}

			Utils.LogDebug($"Loading sub requirement {type}");

			return (SubRequirement)subRequirementActivators[type].Invoke(new object[] { type, requirement, node });
		}

		internal virtual bool NeedsWaypoint()
		{
			return false;
		}

		/// <summary> The minimum required frequency needed for reliable simulation </summary>
		internal double MinUpdateFrequency()
		{
			return double.MaxValue;
		}
	}
}
