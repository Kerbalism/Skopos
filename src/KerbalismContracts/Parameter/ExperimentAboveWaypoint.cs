using System;
using System.Collections.Generic;
using Contracts;
using KSP;
using ContractConfigurator;
using ContractConfigurator.Parameters;
using System.Linq;
using System.Text;
using UnityEngine;
using Contracts.Parameters;
using FinePrint;
using FinePrint.Utilities;
using ContractConfigurator.Behaviour;


namespace Kerbalism.Contracts
{
	public class ExperimentAboveWaypointFactory : ParameterFactory
	{
		protected int index;
		protected double elevation_min;
		protected double elevation_max;
		protected string experiment;
		protected bool allow_interruption;
		protected ContractConfigurator.Duration duration;

		public override bool Load(ConfigNode configNode)
		{
			// Load base class
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue(configNode, "index", x => index = x, this, 0, x => Validation.GE(x, 0));
			valid &= ConfigNodeUtil.ParseValue(configNode, "elevation_min", x => elevation_min = x, this, 10.0, x => Validation.GE(x, -90.0));
			valid &= ConfigNodeUtil.ParseValue(configNode, "elevation_max", x => elevation_max = x, this, 90.0, x => Validation.GE(x, -90.0));
			valid &= ConfigNodeUtil.ParseValue(configNode, "duration", x => duration = x, this, new ContractConfigurator.Duration(0.0));
			valid &= ConfigNodeUtil.ParseValue(configNode, "experiment", x => experiment = x, this, string.Empty);
			valid &= ConfigNodeUtil.ParseValue(configNode, "allow_interruption", x => allow_interruption = x, this, true);

			return valid;
		}

		public override ContractParameter Generate(Contract contract)
		{
			ExperimentAboveWaypoint aw = new ExperimentAboveWaypoint(index, elevation_min, elevation_max, experiment, title);
			aw.SetDuration(duration.Value);
			aw.SetAllowInterruption(allow_interruption);
			return aw.FetchWaypoint(contract) != null ? aw : null;
		}
	}

	public class ExperimentAboveWaypoint : BackgroundVesselParameter
	{
		protected double elevation_min { get; set; }
		protected double elevation_max { get; set; }
		protected int waypointIndex { get; set; }
		protected string experiment { get; set; }

		protected Waypoint waypoint { get; set; }

		/// <summary>
		/// Child class for checking waypoints, because completed/disabled parameters don't get events.
		/// </summary>
		public class WaypointChecker
		{
			VisitWaypoint visitWaypoint;
			public WaypointChecker(VisitWaypoint vw)
			{
				visitWaypoint = vw;
				ContractConfigurator.ContractConfigurator.OnParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
			}

			~WaypointChecker()
			{
				ContractConfigurator.ContractConfigurator.OnParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
			}

			protected void OnParameterChange(Contract c, ContractParameter p)
			{
				visitWaypoint.OnParameterChange(c, p);
			}
		}

		public ExperimentAboveWaypoint() { }

		public ExperimentAboveWaypoint(int waypointIndex, double elevation_min, double elevation_max, string experiment_id, string title)
			: base(string.IsNullOrEmpty(title) ? "Above waypoint" : title)
		{
			this.elevation_min = elevation_min;
			this.elevation_max = elevation_max;
			this.waypointIndex = waypointIndex;
			this.experiment = experiment_id;

			if (string.IsNullOrEmpty(title) && waypoint != null)
			{
				this.title = "Above " + waypoint.name;
			}
		}

		protected override void OnParameterSave(ConfigNode node)
		{
			base.OnParameterSave(node);
			node.AddValue("elevation_min", elevation_min);
			node.AddValue("elevation_max", elevation_max);
			node.AddValue("waypointIndex", waypointIndex);
			node.AddValue("experiment", experiment);
		}

		protected override void OnParameterLoad(ConfigNode node)
		{
			base.OnParameterLoad(node);
			elevation_min = Convert.ToDouble(node.GetValue("elevation_min"));
			elevation_max = Convert.ToDouble(node.GetValue("elevation_max"));
			waypointIndex = Convert.ToInt32(node.GetValue("waypointIndex"));
			experiment = ConfigNodeUtil.ParseValue(node, "experiment", string.Empty);
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			// Make sure we have a waypoint
			if (waypoint == null && Root != null)
			{
				waypoint = FetchWaypoint(Root, true);
			}
			if (waypoint == null)
			{
				return false;
			}

			// Not even close
			if (vessel.mainBody.name != waypoint.celestialName)
			{
				return false;
			}

			double r = vessel.mainBody.Radius;
			double surfaceDistance = WaypointUtil.GetDistance(vessel.latitude, vessel.longitude, waypoint.latitude, waypoint.longitude, r);
			double elevation = 90.0 - (surfaceDistance / r) * (180.0 / Math.PI);

			bool pass = elevation_min <= elevation && elevation <= elevation_max;
			if (!pass) return false;

			if (!string.IsNullOrEmpty(experiment))
			{
				return ExperimentStateTracker.IsRunning(vessel, experiment);
			}

			return true;
		}

		protected override bool VesselIsCandidate(Vessel vessel)
		{
			return true;
		}

		/// <summary>
		/// Goes and finds the waypoint for our parameter.
		/// </summary>
		/// <returns>The waypoint used by our parameter.</returns>
		protected Waypoint FetchWaypoint()
		{
			return FetchWaypoint(Root);
		}

		/// <summary>
		/// Goes and finds the waypoint for our parameter.
		/// </summary>
		/// <param name="c">The contract</param>
		/// <returns>The waypoint used by our parameter.</returns>
		public Waypoint FetchWaypoint(Contract c, bool silent = false)
		{
			// Find the WaypointGenerator behaviours
			IEnumerable<WaypointGenerator> waypointGenerators = ((ConfiguredContract)c).Behaviours.OfType<WaypointGenerator>();

			if (!waypointGenerators.Any())
			{
				LoggingUtil.LogError(this, "Could not find WaypointGenerator BEHAVIOUR to couple with ExperimentAboveWaypoint PARAMETER.");
				return null;
			}

			waypoint = waypointGenerators.SelectMany(wg => wg.Waypoints()).ElementAtOrDefault(waypointIndex);
			if (waypoint == null)
			{
				LoggingUtil.LogError(this, "Couldn't find waypoint in WaypointGenerator behaviour(s) with index " + waypointIndex + ".");
			}

			return waypoint;
		}
	}
}
