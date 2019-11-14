using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{
	public class HasResourceFactory : ContractConfigurator.HasResourceFactory
	{
		public override ContractParameter Generate(Contract contract)
		{
			return new KerbalismHasResource(filters, false, title);
		}
	}

	public class HasResourceCapacityFactory : ContractConfigurator.HasResourceCapacityFactory
	{
		public override ContractParameter Generate(Contract contract)
		{
			return new KerbalismHasResource(filters, true, title);
		}
	}

	/// <summary>
	/// This is an adapted copy of the original CC HasResource that also looks at unloaded vessels
	/// </summary>
	public class KerbalismHasResource : VesselParameter
	{
		protected List<HasResource.Filter> filters = new List<HasResource.Filter>();
		protected bool capacity = false;

		private float lastLoadedUpdate = 0.0f;
		private const float UPDATE_FREQUENCY_LOADED = 0.25f;

		private float lastUnloadedUpdate = 0.0f;
		private const float UPDATE_FREQUENCY_UNLOADED = 5f;

		public KerbalismHasResource()
			: base(null)
		{
		}

		public KerbalismHasResource(List<HasResource.Filter> filters, bool capacity, string title = null)
			: base(title)
		{
			this.filters = filters;
			this.capacity = capacity;

			CreateDelegates();
		}

		protected override string GetParameterTitle()
		{
			string output = null;
			if (string.IsNullOrEmpty(title))
			{
				if (state == ParameterState.Complete || ParameterCount == 1)
				{
					if (ParameterCount == 1)
					{
						hideChildren = true;
					}

					output = ParameterDelegate<Vessel>.GetDelegateText(this);
				}
			}
			else
			{
				output = title;
			}
			return output;
		}

		protected void CreateDelegates()
		{
			foreach (HasResource.Filter filter in filters)
			{
				string output = (capacity ? "Resource Capacity: " : "Resource: ") + filter.resource.name + ": ";
				if (filter.maxQuantity == 0)
				{
					output += "None";
				}
				else if (filter.maxQuantity == double.MaxValue && (filter.minQuantity > 0.0 && filter.minQuantity <= 0.01))
				{
					output += "Not zero units";
				}
				else if (filter.maxQuantity == double.MaxValue)
				{
					output += "At least " + filter.minQuantity + " units";
				}
				else if (filter.minQuantity == 0)
				{
					output += "At most " + filter.maxQuantity + " units";
				}
				else
				{
					output += "Between " + filter.minQuantity + " and " + filter.maxQuantity + " units";
				}

				AddParameter(new ParameterDelegate<Vessel>(output, v => VesselHasResource(v, filter.resource, capacity, filter.minQuantity, filter.maxQuantity),
					ParameterDelegateMatchType.VALIDATE));
			}
		}

		protected static bool VesselHasResource(Vessel vessel, PartResourceDefinition resource, bool capacity, double minQuantity, double maxQuantity)
		{
			if (vessel == null)
				return false;

			double quantity = 0;

			if (vessel.loaded)
			{
				quantity = capacity ? vessel.ResourceCapacity(resource) : vessel.ResourceQuantity(resource);
				return quantity >= minQuantity && quantity <= maxQuantity;
			}

			foreach (ProtoPartSnapshot p in vessel.protoVessel.protoPartSnapshots)
			{
				foreach (ProtoPartResourceSnapshot r in p.resources)
				{
					if (r.flowState && r.resourceName == resource.name)
					{
						quantity += capacity ? r.maxAmount : r.amount;
					}
				}
			}
			return quantity >= minQuantity && quantity <= maxQuantity;
		}

		protected override void OnParameterSave(ConfigNode node)
		{
			base.OnParameterSave(node);

			node.AddValue("capacity", capacity);

			foreach (HasResource.Filter filter in filters)
			{
				ConfigNode childNode = new ConfigNode("RESOURCE");
				node.AddNode(childNode);

				childNode.AddValue("resource", filter.resource.name);
				childNode.AddValue("minQuantity", filter.minQuantity);
				if (filter.maxQuantity != double.MaxValue)
				{
					childNode.AddValue("maxQuantity", filter.maxQuantity);
				}
			}
		}

		protected override void OnParameterLoad(ConfigNode node)
		{
			try
			{
				base.OnParameterLoad(node);

				capacity = ConfigNodeUtil.ParseValue<bool?>(node, "capacity", (bool?)false).Value;

				foreach (ConfigNode childNode in node.GetNodes("RESOURCE"))
				{
					HasResource.Filter filter = new HasResource.Filter();

					filter.resource = ConfigNodeUtil.ParseValue<PartResourceDefinition>(childNode, "resource");
					filter.minQuantity = ConfigNodeUtil.ParseValue<double>(childNode, "minQuantity");
					filter.maxQuantity = ConfigNodeUtil.ParseValue<double>(childNode, "maxQuantity", double.MaxValue);

					filters.Add(filter);
				}

				// Legacy
				if (node.HasValue("resource"))
				{
					HasResource.Filter filter = new HasResource.Filter();

					filter.resource = ConfigNodeUtil.ParseValue<PartResourceDefinition>(node, "resource");
					filter.minQuantity = ConfigNodeUtil.ParseValue<double>(node, "minQuantity");
					filter.maxQuantity = ConfigNodeUtil.ParseValue<double>(node, "maxQuantity", double.MaxValue);

					filters.Add(filter);
				}

				CreateDelegates();
			}
			finally
			{
				ParameterDelegate<Part>.OnDelegateContainerLoad(node);
			}
		}

		protected override void OnRegister()
		{
			base.OnRegister();
		}

		protected override void OnUnregister()
		{
			base.OnUnregister();
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			if (UnityEngine.Time.fixedTime - lastLoadedUpdate > UPDATE_FREQUENCY_LOADED)
			{
				lastLoadedUpdate = UnityEngine.Time.fixedTime;
				CheckVessel(FlightGlobals.ActiveVessel);
			}


			if (UnityEngine.Time.fixedTime - lastUnloadedUpdate > UPDATE_FREQUENCY_UNLOADED)
			{
				lastUnloadedUpdate = UnityEngine.Time.fixedTime;

				foreach(Vessel vessel in FlightGlobals.Vessels)
				{
					if (vessel == FlightGlobals.ActiveVessel)
						continue;

					switch (vessel.vesselType)
					{
						case VesselType.Unknown:
						case VesselType.Debris:
						case VesselType.Flag:
						case VesselType.SpaceObject:
							continue;
					}

					CheckVessel(vessel);
				}
			}
		}

		/// <summary>
		/// Whether this vessel meets the parameter condition.
		/// </summary>
		/// <param name="vessel">The vessel to check</param>
		/// <returns>Whether the vessel meets the condition</returns>
		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);

			return ParameterDelegate<Vessel>.CheckChildConditions(this, vessel);
		}
	}
}
