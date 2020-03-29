using System;
using System.Collections.Generic;
using FinePrint;
using Contracts;

namespace KerbalismContracts
{
	public class RequirementTracker
	{
		internal readonly List<Action<List<Vessel>>> listeners = new List<Action<List<Vessel>>>();

		internal void Register(string id, Contract contract, Action<List<Vessel>> listener)
		{
			Utils.LogDebug($"AddListener {id} {contract.ContractID}");
			if (!listeners.Contains(listener)) listeners.Add(listener);
		}

		internal void Unregister(string id, Contract contract, Action<List<Vessel>> listener)
		{
			Utils.LogDebug($"RemoveListener {id} {contract.ContractID}");
			if (listeners.Contains(listener)) listeners.Remove(listener);
		}

		internal bool NeedWaypoint(string id)
		{
			var waypointRequirement = Configuration.Requirement(id).SubRequirements.Find(sr => sr is AboveWaypoint);
			return waypointRequirement != null;
		}

		internal void Update()
		{
			
		}
	}
}
