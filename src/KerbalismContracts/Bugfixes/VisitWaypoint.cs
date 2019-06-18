using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using FinePrint;
using FinePrint.Utilities;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;
using ContractConfigurator.Behaviour;

/* This contains the bugfix from https://github.com/jrossignol/ContractConfigurator/pull/678
 * This file can be removed when that PR is merged into contract configurator
 */

namespace Kerbalism.Contracts
{
	/// <summary>
	/// ParameterFactory for VisitWaypoint.
	/// </summary>
	public class VisitWaypointFactory : ParameterFactory
	{
		protected int index;
		protected double distance;
		protected double horizontalDistance;
		protected bool hideOnCompletion;
		protected bool showMessages;

		public override bool Load(ConfigNode configNode)
		{
			// Load base class
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue<int>(configNode, "index", x => index = x, this, 0, x => Validation.GE(x, 0));
			valid &= ConfigNodeUtil.ParseValue<double>(configNode, "distance", x => distance = x, this, 0.0, x => Validation.GE(x, 0.0));
			valid &= ConfigNodeUtil.ParseValue<double>(configNode, "horizontalDistance", x => horizontalDistance = x, this, 0.0, x => Validation.GE(x, 0.0));
			valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "hideOnCompletion", x => hideOnCompletion = x, this, true);
			valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "showMessages", x => showMessages = x, this, false);

			return valid;
		}

		public override ContractParameter Generate(Contract contract)
		{
			VisitWaypoint vw = new VisitWaypoint(index, distance, horizontalDistance, hideOnCompletion, showMessages, title);
			return vw.FetchWaypoint(contract) != null ? vw : null;
		}
	}


	/// <summary>
	/// Parameter for requiring a Kerbal to visit a waypoint.
	/// </summary>
	public class VisitWaypoint : VesselParameter
	{
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

		protected int waypointIndex { get; set; }
		protected Waypoint waypoint { get; set; }
		protected double distance { get; set; }
		protected double horizontalDistance { get; set; }
		protected bool hideOnCompletion { get; set; }
		protected bool showMessages { get; set; }

		private double height = double.MaxValue;
		private bool nearWaypoint = false;

		private float lastUpdate = 0.0f;
		private const float UPDATE_FREQUENCY = 0.25f;

		private WaypointChecker waypointChecker;

		public VisitWaypoint()
			: base()
		{
			waypointChecker = new WaypointChecker(this);
		}

		public VisitWaypoint(int waypointIndex, double distance, double horizontalDistance, bool hideOnCompletion, bool showMessages, string title)
			: base(title)
		{
			waypointChecker = new WaypointChecker(this);

			this.distance = distance;
			this.horizontalDistance = horizontalDistance;
			this.waypointIndex = waypointIndex;
			this.hideOnCompletion = hideOnCompletion;
			this.showMessages = showMessages;
		}

		protected override string GetParameterTitle()
		{
			if (waypoint == null && Root != null)
			{
				waypoint = FetchWaypoint(Root, true);
			}

			string output = title;
			if (string.IsNullOrEmpty(title) && waypoint != null)
			{
				if (waypoint.isOnSurface)
				{
					output = "Location: " + waypoint.name;
				}
				else
				{
					output = "Location: " + waypoint.altitude.ToString("N0") + "meters above " + waypoint.name;
				}
			}
			return output;
		}

		protected override void OnParameterSave(ConfigNode node)
		{
			base.OnParameterSave(node);
			node.AddValue("distance", distance);
			node.AddValue("horizontalDistance", horizontalDistance);
			node.AddValue("waypointIndex", waypointIndex);
			node.AddValue("hideOnCompletion", hideOnCompletion);
			node.AddValue("showMessages", showMessages);
		}

		protected override void OnParameterLoad(ConfigNode node)
		{
			base.OnParameterLoad(node);
			distance = Convert.ToDouble(node.GetValue("distance"));
			horizontalDistance = ConfigNodeUtil.ParseValue<double>(node, "horizontalDistance", 0.0);
			waypointIndex = Convert.ToInt32(node.GetValue("waypointIndex"));
			hideOnCompletion = ConfigNodeUtil.ParseValue<bool?>(node, "hideOnCompletion", (bool?)true).Value;
			showMessages = ConfigNodeUtil.ParseValue<bool?>(node, "showMessages", (bool?)false).Value;
		}

		public void OnParameterChange(Contract c, ContractParameter p)
		{
			if (c != Root)
			{
				return;
			}

			// Hide the waypoint if we are done with it
			if (hideOnCompletion && waypoint != null && waypoint.visible)
			{
				for (IContractParameterHost paramHost = this; paramHost != Root; paramHost = paramHost.Parent)
				{
					if (state == ParameterState.Complete)
					{
						ContractParameter param = paramHost as ContractParameter;
						if (param != null && !param.Enabled)
						{
							waypoint.visible = false;
							NavWaypoint navPoint = NavWaypoint.fetch;
							if (navPoint != null
							    && NavWaypoint.fetch.IsActive && Math.Abs(navPoint.Latitude - waypoint.latitude) < double.Epsilon 
							    && Math.Abs(navPoint.Longitude - waypoint.longitude) < double.Epsilon)
							{
								NavWaypoint.fetch.Clear();
								NavWaypoint.fetch.Deactivate();
							}
							break;
						}
					}
					else
					{
						break;
					}
				}
			}
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
				LoggingUtil.LogError(this, "Could not find WaypointGenerator BEHAVIOUR to couple with VisitWaypoint PARAMETER.");
				return null;
			}

			waypoint = waypointGenerators.SelectMany(wg => wg.Waypoints()).ElementAtOrDefault(waypointIndex);
			if (waypoint == null)
			{
				LoggingUtil.LogError(this, "Couldn't find waypoint in WaypointGenerator behaviour(s) with index " + waypointIndex + ".");
			}

			return waypoint;
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			// We don't do this on load because parameters load before behaviours (due to stock logic)
			if (waypoint == null)
			{
				FetchWaypoint();
			}

			if (UnityEngine.Time.fixedTime - lastUpdate > UPDATE_FREQUENCY)
			{
				lastUpdate = UnityEngine.Time.fixedTime;
				CheckVessel(FlightGlobals.ActiveVessel);
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

			// Default distance
			if (distance < double.Epsilon && horizontalDistance < double.Epsilon)
			{
				// Close to the surface
				if (waypoint.altitude < 25.0)
				{
					distance = 500.0;
				}
				else
				{
					distance = Math.Max(1000.0, waypoint.altitude / 5.0);
				}
			}

			// Calculate the distance
			bool check = false;
			if (distance > double.Epsilon)
			{
				double actualDistance = WaypointUtil.GetDistanceToWaypoint(vessel, waypoint, ref height);
				LoggingUtil.LogVerbose(this, "Distance to waypoint '" + waypoint.name + "': " + actualDistance);
				check = actualDistance <= distance;
			}
			else
			{
				double actualDistance = WaypointUtil.GetDistance(vessel.latitude, vessel.longitude, waypoint.latitude, waypoint.longitude, vessel.mainBody.Radius);
				LoggingUtil.LogVerbose(this, "Horizontal distance to waypoint : '" + horizontalDistance + "': " + actualDistance);
				check = actualDistance <= horizontalDistance;
			}

			// Output the message for entering/leaving the waypoint area.
			if (showMessages)
			{
				if (check ^ nearWaypoint)
				{
					nearWaypoint = check;
					string waypointName = waypoint.name + (waypoint.isClustered ? " " + StringUtilities.IntegerToGreek(waypoint.index) : "");
					string msg = "You are " + (nearWaypoint ? "entering " : "leaving ") + waypointName + ".";
					ScreenMessages.PostScreenMessage(msg, 5.0f, ScreenMessageStyle.UPPER_CENTER);
				}

				NavWaypoint navWaypoint = NavWaypoint.fetch;
				if (navWaypoint != null 
				    && Math.Abs(navWaypoint.Latitude - waypoint.latitude) < double.Epsilon 
				    && Math.Abs(navWaypoint.Longitude - waypoint.longitude) < double.Epsilon)
				{
					navWaypoint.IsBlinking = nearWaypoint;
				}
			}

			return check;
		}
	}


	public static class WaypointUtil
	{
		/// <summary>
		/// Gets the  distance in meters from the activeVessel to the given waypoint.
		/// </summary>
		/// <returns>Distance in meters</returns>
		public static double GetDistanceToWaypoint(Vessel vessel, Waypoint waypoint, ref double height)
		{
			CelestialBody celestialBody = vessel.mainBody;

			// Figure out the terrain height
			if (Math.Abs(height - double.MaxValue) < double.Epsilon)
			{
				double latRads = Math.PI / 180.0 * waypoint.latitude;
				double lonRads = Math.PI / 180.0 * waypoint.longitude;
				Vector3d radialVector = new Vector3d(Math.Cos(latRads) * Math.Cos(lonRads), Math.Sin(latRads), Math.Cos(latRads) * Math.Sin(lonRads));
				height = celestialBody.pqsController.GetSurfaceHeight(radialVector) - celestialBody.pqsController.radius;

				// Clamp to zero for ocean worlds
				if (celestialBody.ocean)
				{
					height = Math.Max(height, 0.0);
				}
			}

			// Use the haversine formula to calculate great circle distance.
			double sin1 = Math.Sin(Math.PI / 180.0 * (vessel.latitude - waypoint.latitude) / 2);
			double sin2 = Math.Sin(Math.PI / 180.0 * (vessel.longitude - waypoint.longitude) / 2);
			double cos1 = Math.Cos(Math.PI / 180.0 * waypoint.latitude);
			double cos2 = Math.Cos(Math.PI / 180.0 * vessel.latitude);

			double lateralDist = 2 * (celestialBody.Radius + height + waypoint.altitude) *
				Math.Asin(Math.Sqrt(sin1 * sin1 + cos1 * cos2 * sin2 * sin2));
			double heightDist = Math.Abs(waypoint.altitude + height - vessel.altitude);

			if (heightDist <= lateralDist / 2.0)
			{
				return lateralDist;
			}
			else
			{
				// Get the ratio to use in our formula
				double x = (heightDist - lateralDist / 2.0) / lateralDist;

				// x / (x + 1) starts at 0 when x = 0, and increases to 1
				return (x / (x + 1)) * heightDist + lateralDist;
			}
		}

		/// <summary>
		/// Gets the  distance in meters between two points
		/// </summary>
		/// <param name="lat1">First latitude</param>
		/// <param name="lon1">First longitude</param>
		/// <param name="lat2">Second latitude</param>
		/// <param name="lon2">Second longitude</param>
		/// <returns>the distance</returns>
		public static double GetDistance(double lat1, double lon1, double lat2, double lon2, double radius)
		{
			// Use the haversine formula to calculate great circle distance.
			double R = radius / 1000;
			double dLat = (Math.PI / 180.0) * (lat2 - lat1);
			double dLon = (Math.PI / 180.0) * (lon2 - lon1);
			lat1 = Math.PI / 180.0 * lat1;
			lat2 = Math.PI / 180.0 * lat2;

			double a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Pow(Math.Sin(dLon / 2), 2) * Math.Cos(lat1) * Math.Cos(lat2);
			double c = 2 * Math.Asin(Math.Sqrt(a));

			return R * c;

			// This is the old code, instead of using the radius of the planet, it is using the radius of the orbit (it would seem). This stopped the test from being accurate.
			//double sin1 = Math.Sin(Math.PI / 180.0 * (lat1 - lat2) / 2);
			//double sin2 = Math.Sin(Math.PI / 180.0 * (lon1 - lon2) / 2);
			//double cos1 = Math.Cos(Math.PI / 180.0 * lat2);
			//double cos2 = Math.Cos(Math.PI / 180.0 * lat1);
			//return  2 * (radius) * Math.Asin(Math.Sqrt(sin1 * sin1 + cos1 * cos2 * sin2 * sin2));
		}
	}

}
