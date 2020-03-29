using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;
using KSP;
using System.Linq;
using System.Text;
using UnityEngine;
using Contracts.Parameters;
using FinePrint;
using FinePrint.Utilities;
using ContractConfigurator.Behaviour;


namespace KerbalismContracts
{
	/// <summary> Parameter for global radiation field status: how often has a radiation field been penetrated </summary>
	public class KerbalismContractFactory : ParameterFactory
	{
		protected string id;
		protected ContractConfigurator.Duration duration;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue<ContractConfigurator.Duration>(configNode, "duration", x => duration = x, this, new ContractConfigurator.Duration(0.0));
			valid &= ConfigNodeUtil.ParseValue<string>(configNode, "id", x => id = x, this, "");

			if (Configuration.Requirement(id) == null)
			{
				LoggingUtil.LogError(GetType(), ErrorPrefix() + $"There is no KerbalismContractRequirement with name '{id}'");
				valid = false;
			}

			return valid;
		}

		public override ContractParameter Generate(Contract contract)
		{
			if (Configuration.Requirement(id) == null)
			{
				Utils.Log($"There is no KerbalismContractRequirement with name '{id}'.", LogLevel.Error);
				return null;
			}

			var result = new KerbalismContractParameter(title, id, duration.Value);

			if(KerbalismContracts.Requirements.NeedWaypoint(id))
			{
				if(result.FetchWaypoint(contract) == null)
				{
					return null;
				}
			}
			return result;
		}
	}

	public class KerbalismContractParameter : ContractConfiguratorParameter
	{
		protected string kerbalism_contract_id;
		protected double duration;

		protected double endTime = 0.0;
		protected Waypoint waypoint;

		public KerbalismContractParameter(): base(null) {}

		public KerbalismContractParameter(string title, string kerbalism_contract_id, double duration)
		{
			this.kerbalism_contract_id = kerbalism_contract_id;
			this.duration = duration;
			this.title = title;
		}

		protected override string GetParameterTitle()
		{
			string result = Configuration.Requirement(id).Title;

			if (endTime > 0.01)
			{
				double time = endTime;
				if (time - Planetarium.GetUniversalTime() > 0.0)
				{
					result = Localizer.Format("Time to completion: <<1>>", DurationUtil.StringValue(time - Planetarium.GetUniversalTime()));
				}
				else
				{
					result = "Wait time over";
				}
			}
			else
			{
				result = Localizer.Format("Waiting time required: <<1>>", DurationUtil.StringValue(duration));
			}

			return result;
		}

		protected override string GetNotes()
		{
			return Configuration.Requirement(id).GetNotes();
		}

		protected override void OnParameterSave(ConfigNode node)
		{
			node.AddValue("kerbalism_contract_id", kerbalism_contract_id);
			node.AddValue("duration", duration);
			node.AddValue("title", title);
			node.AddValue("endTime", endTime);
		}

		protected override void OnParameterLoad(ConfigNode node)
		{
			kerbalism_contract_id = ConfigNodeUtil.ParseValue<string>(node, "kerbalism_contract_id", "");
			duration = ConfigNodeUtil.ParseValue<double>(node, "duration", 0);
			title = ConfigNodeUtil.ParseValue<string>(node, "title", "");
			endTime = ConfigNodeUtil.ParseValue<double>(node, "endTime", 0.0);
		}

		protected override void OnRegister()
		{
			base.OnRegister();

			if (waypoint == null && KerbalismContracts.Requirements.NeedWaypoint(id))
			{
				waypoint = FetchWaypoint(Root);
			}

			KerbalismContracts.Requirements.Register(kerbalism_contract_id, Root, Update);
		}

		protected override void OnUnregister()
		{
			base.OnUnregister();
			KerbalismContracts.Requirements.Unregister(kerbalism_contract_id, Root, Update);
		}

		private void Update(List<Vessel> vessels)
		{
			bool completed = vessels.Count > 0;

			if (completed)
			{
				if(endTime == 0.0)
				{
					endTime = Planetarium.GetUniversalTime() + duration;
				}
			}
			else
			{
				endTime = 0.0;
			}

			if (endTime != 0.0 && Planetarium.GetUniversalTime() > endTime)
			{
				SetState(ParameterState.Complete);
			}
		}

		/// <summary>
		/// Goes and finds the waypoint for our parameter.
		/// </summary>
		/// <param name="contract">The contract</param>
		/// <returns>The waypoint used by our parameter.</returns>
		public Waypoint FetchWaypoint(Contract contract)
		{
			// Find the WaypointGenerator behaviours
			IEnumerable<WaypointGenerator> waypointGenerators = ((ConfiguredContract)contract).Behaviours.OfType<WaypointGenerator>();

			if (!waypointGenerators.Any())
			{
				Utils.Log("Could not find WaypointGenerator BEHAVIOUR to couple with ExperimentAboveWaypoint PARAMETER.", LogLevel.Error);
				return null;
			}

			var waypoint = waypointGenerators.SelectMany(wg => wg.Waypoints()).ElementAtOrDefault(0);
			if (waypoint == null)
			{
				LoggingUtil.LogError(this, "Couldn't find waypoint in WaypointGenerator behaviour(s).");
			}

			return waypoint;
		}
	}
}
