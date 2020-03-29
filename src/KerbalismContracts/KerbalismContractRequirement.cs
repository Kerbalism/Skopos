using System;
using System.Collections.Generic;
using UnityEngine;
using KERBALISM;
using System.Text;

namespace KerbalismContracts
{
	public class KerbalismContractRequirement
	{
		public string Id { get; private set; }
		public string Title { get; private set; }
		public List<SubRequirement> SubRequirements { get; private set; }

		private string notes;

		public KerbalismContractRequirement(ConfigNode node)
		{
			Id = Lib.ConfigValue(node, "name", "");
			Title = Lib.ConfigValue(node, "title", "");

			SubRequirements = new List<SubRequirement>();

			foreach(var requirementNode in node.GetNodes("Requirement"))
			{
				SubRequirement sr = LoadSubRequirement(requirementNode);

				if (sr == null)
					Utils.Log($"Unknown requirement type '{Lib.ConfigValue(requirementNode, "name", "")}'");
				else
					SubRequirements.Add(sr);
			}
		}

		private static SubRequirement LoadSubRequirement(ConfigNode node)
		{
			string type = Lib.ConfigValue(node, "name", "");

			switch (type)
			{
				case "AboveWaypoint":
					return new AboveWaypoint(node);
				case "EquipmentRunning":
					return new EquipmentRunning(node);
				case "VesselCount":
				default:
					return null;
			}
		}

		internal string GetNotes()
		{
			if (!string.IsNullOrEmpty(notes))
				return notes;

			StringBuilder sb = new StringBuilder();

			foreach(var sr in SubRequirements)
			{
				var n = sr.GetNotes();
				if (!string.IsNullOrEmpty(n))
				{
					sb.Append(n);
					sb.Append("\n");
				}
			}

			notes = sb.ToString();
			return notes;
		}
	}

	public abstract class SubRequirement
	{
		public string type { get; protected set; }

		public abstract string GetNotes();
	}

	public class AboveWaypoint: SubRequirement
	{
		private double min_elevation;

		public AboveWaypoint(ConfigNode node)
		{
			type = "AboveWaypoint";
			min_elevation = Lib.ConfigValue(node, "min_elevation", 0.0);
		}

		public override string GetNotes()
		{
			return $"Be above waypoint, min. elevation {min_elevation}°";
		}
	}

	public class EquipmentRunning : SubRequirement
	{
		private string equipment;
		private string description;

		public EquipmentRunning(ConfigNode node)
		{
			type = "EquipmentRunning";
			equipment = Lib.ConfigValue(node, "equipment", "");
			description = Lib.ConfigValue(node, "description", "");
		}

		public override string GetNotes()
		{
			return description ?? $"Have equipment running: {equipment}";
		}
	}

	public class VesselCount : SubRequirement
	{
		private int min_vessels;

		public VesselCount(ConfigNode node)
		{
			type = "VesselCount";
			min_vessels = Lib.ConfigValue(node, "min_vessels", 1);
		}

		public override string GetNotes()
		{
			return $"On at least {min_vessels} vessels at the same time";
		}
	}

}
