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
		protected double min_elevation;
		protected double min_angle_between;
		protected int max_distance;
		protected string experiment;
		protected bool allow_interruption;
		protected ContractConfigurator.Duration duration;
		protected int min_vessels;

		public override bool Load(ConfigNode configNode)
		{
			// Load base class
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue(configNode, "index", x => index = x, this, 0, x => Validation.GE(x, 0));
			valid &= ConfigNodeUtil.ParseValue(configNode, "min_elevation", x => min_elevation = x, this, 10.0, x => Validation.GE(x, -90.0));
			valid &= ConfigNodeUtil.ParseValue(configNode, "duration", x => duration = x, this, new ContractConfigurator.Duration(0.0));
			valid &= ConfigNodeUtil.ParseValue(configNode, "experiment", x => experiment = x, this, string.Empty);
			valid &= ConfigNodeUtil.ParseValue(configNode, "allow_interruption", x => allow_interruption = x, this, true);
			valid &= ConfigNodeUtil.ParseValue(configNode, "min_vessels", x => min_vessels = x, this, 1, x => Validation.GE(x, 1));
			valid &= ConfigNodeUtil.ParseValue(configNode, "max_distance", x => max_distance = x, this, 1, x => Validation.GE(x, 0));
			valid &= ConfigNodeUtil.ParseValue(configNode, "min_angle_between", x => min_angle_between = x, this, 1, x => Validation.GE(x, 0));

			return valid;
		}

		public override ContractParameter Generate(Contract contract)
		{
			ExperimentAboveWaypoint aw = new ExperimentAboveWaypoint(index, duration.Value, max_distance, min_elevation, min_vessels, min_angle_between, experiment, title);
			aw.SetAllowInterruption(allow_interruption);
			return aw.FetchWaypoint(contract) != null ? aw : null;
		}
	}

	public class ExperimentAboveWaypoint : BackgroundVesselParameter
	{
		protected double min_elevation { get; set; }
		protected int max_distance { get; set; }
		protected double min_angle_between { get; set; }
		protected int waypointIndex { get; set; }
		protected string experiment { get; set; }

		protected Waypoint waypoint { get; set; }

		private Vector3 waypointPosition;
		private List<ResultData> results;

		private class ResultData
		{
			internal VesselData vd;
			internal Vector3 vesselPosition;
			internal string title;
			internal double a = double.MaxValue;

			public ResultData(VesselData vd, Vector3 vesselPosition, string title)
			{
				this.vd = vd;
				this.vesselPosition = vesselPosition;
				this.title = title;
			}
		}

		/// <summary>
		/// Child class for checking waypoints, because completed/disabled parameters don't get events.
		/// Copied from CCs VisitWaypoint parameter.
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

		public ExperimentAboveWaypoint(int waypointIndex, double duration, int max_distance, double min_elevation, int min_vessels, double min_angle_between, string experiment_id, string title)
			: base(title, min_vessels, duration)
		{
			this.min_elevation = min_elevation;
			this.waypointIndex = waypointIndex;
			this.experiment = experiment_id;
			this.min_angle_between = min_angle_between;
			this.max_distance = max_distance;

			if (string.IsNullOrEmpty(title))
			{
				this.title = "Min. elevation " + min_elevation.ToString("F0") + "° above " + (waypoint == null ? "waypoint" : waypoint.name);
			}
		}

		protected override void OnParameterSave(ConfigNode node)
		{
			base.OnParameterSave(node);
			node.AddValue("min_elevation", min_elevation);
			node.AddValue("waypointIndex", waypointIndex);
			node.AddValue("experiment", experiment);
			node.AddValue("min_angle_between", min_angle_between);
			node.AddValue("max_distance", max_distance);
		}

		protected override void OnParameterLoad(ConfigNode node)
		{
			base.OnParameterLoad(node);
			min_elevation = Convert.ToDouble(node.GetValue("min_elevation"));
			min_angle_between = Convert.ToDouble(node.GetValue("min_angle_between"));
			waypointIndex = Convert.ToInt32(node.GetValue("waypointIndex"));
			max_distance = Convert.ToInt32(node.GetValue("max_distance"));
			experiment = ConfigNodeUtil.ParseValue(node, "experiment", string.Empty);
		}

		protected override void BeforeUpdate()
		{
			// Make sure we have a waypoint
			if (waypoint == null && Root != null)
			{
				waypoint = FetchWaypoint(Root, true);
			}
			if (waypoint == null || waypoint.celestialBody == null) return;

			waypointPosition = waypoint.celestialBody.GetWorldSurfacePosition(waypoint.latitude, waypoint.longitude, 0);

			if(NeedAngleBetween())
				results = new List<ResultData>();
		}

		protected override void AfterUpdate()
		{
			if (waypoint == null || !NeedAngleBetween()) return;

			condition_met = false;


			// TODO see if the angle between at least min_vessels is bigger than the min required


			foreach(var rd in results)
			{
				string angStr = rd.a == double.MaxValue ? "n/a" : rd.a.ToString("F1") + "°";
				if(rd.a < double.MaxValue && rd.a > min_angle_between)
				{
					angStr = "<color=green>" + angStr + "</color>";
					rd.vd.pass = true;
				}
				else
				{
					angStr = "<color=red>" + angStr + "</red>";
					rd.vd.pass = false;
				}

				rd.title += " ang. " + angStr + " (min. " + min_angle_between.ToString("F0") + "°";
				rd.vd.parameterDelegate.SetTitle(rd.title);
			}
		}


		private bool NeedAngleBetween()
		{
			return min_vessels > 1 && min_angle_between > 0;
		}

		protected override bool UpdateVesselState(Vessel vessel, VesselData vesselData)
		{
			if (waypoint == null || waypoint.celestialBody == null)
			{
				vesselData.parameterDelegate.SetTitle(vessel.vesselName + ": no target");
				return false;
			}

			if (!string.IsNullOrEmpty(experiment) && !ExperimentStateTracker.IsRunning(vessel, experiment))
			{
				vesselData.parameterDelegate.SetTitle(vessel.vesselName + ": Equipment is off");
				return false;
			}

			var vesselPosition = Lib.VesselPosition(vessel);

			if(max_distance > 0)
			{
				var distance = (vesselPosition - waypointPosition).magnitude;
				if (distance > max_distance)
				{
					vesselData.parameterDelegate.SetTitle(vessel.vesselName + ": too far");
					return false;
				}
			}

			var bodyPosition = waypoint.celestialBody.position;

			var a = Vector3d.Angle(vesselPosition - bodyPosition, waypointPosition - bodyPosition);
			var b = Vector3d.Angle(waypointPosition - vesselPosition, bodyPosition - vesselPosition);

			// a + b + 90 + elevation = 180 degrees
			// elevation = 180 - 90 - a - b = 90 - a - b
			var elevation = 90.0 - a - b;

			// TODO determine line of sight obstruction (there may be an occluding body)

			bool pass = min_elevation <= elevation;

			string elevString = "elevation " + elevation.ToString("F1") + "°";
			if (!pass) elevString = "<color=orange>" + elevString + "</color>";
			else elevString = "<color=green>" + elevString + "</color>";
			string title = vessel.vesselName + ": " + elevString + " (min. " + min_elevation.ToString("F0") + "°)";

			vesselData.parameterDelegate.SetTitle(title);

			if (pass && NeedAngleBetween())
				results.Add(new ResultData(vesselData, vesselPosition, title));

			return pass;
		}

		protected override string GetNotes()
		{
			string waypointName = waypoint != null ? waypoint.name : "waypoint";
			return "Vessels must be over " + waypointName + ", min. elevation above horizon " + min_elevation + "°";
		}

		protected override bool VesselIsCandidate(Vessel vessel)
		{
			Lib.Log("Looking into vessel " + vessel);
			if(!string.IsNullOrEmpty(experiment))
				return Lib.HasExperiment(vessel, experiment);
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

		protected override ParameterDelegate<Vessel> CreateVesselParameter(Vessel vessel)
		{
			return new ParameterDelegate<Vessel>(vessel.vesselName, v => false);
		}
	}
}
