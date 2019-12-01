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
	public abstract class BackgroundVesselParameter : ContractConfiguratorParameter, ParameterDelegateContainer
 	{
		private readonly Dictionary<Guid, bool> validCandidates = new Dictionary<Guid, bool>();
		private readonly Dictionary<Guid, ParameterDelegate<Vessel>> vesselParameters = new Dictionary<Guid, ParameterDelegate<Vessel>>();

		private float lastUpdate = 0.0f;
		private const float UPDATE_FREQUENCY = 1f;

		protected bool condition_met = false;
		protected bool allow_interruption = true;
		protected double duration = -1;
		protected double endTime = double.MaxValue;
		private ParameterDelegate<Vessel> durationParameter;

		public bool ChildChanged { get; set; }

		protected BackgroundVesselParameter() : base()
		{
		}

		protected BackgroundVesselParameter(string title, double duration = 0.0) : base(title)
		{
			this.duration = duration;
			CreateDurationParameter();
		}

		public void SetAllowInterruption(bool value)
		{
			allow_interruption = value;
		}

		protected void CreateDurationParameter()
		{
			if (duration > 0.0 && durationParameter == null)
			{
				durationParameter = new ParameterDelegate<Vessel>("Duration: " + DurationUtil.StringValue(duration), v => false);
				durationParameter.Optional = true;
				durationParameter.fakeOptional = true;
				AddParameter(durationParameter);
			}
		}

		protected void AddVesselParameter(Vessel vessel)
		{
			if (vesselParameters.ContainsKey(vessel.id)) return;

			vesselParameters[vessel.id] = CreateVesselParameter(vessel);
			vesselParameters[vessel.id].Optional = true;
			vesselParameters[vessel.id].fakeOptional = true;
			AddParameter(vesselParameters[vessel.id]);
		}

		protected void OnContractLoaded(ConfiguredContract contract)
		{
			if (contract == Root)
			{
				CreateDurationParameter();
			}
		}

		protected override void OnParameterLoad(ConfigNode node)
		{
			try
			{
				title = ConfigNodeUtil.ParseValue(node, "title", string.Empty);
				condition_met = ConfigNodeUtil.ParseValue(node, "condition_met", false);
				allow_interruption = ConfigNodeUtil.ParseValue(node, "allow_interruption", true);
				duration = Convert.ToDouble(node.GetValue("duration"));
				endTime = Convert.ToDouble(node.GetValue("endTime"));
				validCandidates.Clear();

				CreateDurationParameter();
			}
			finally
			{
				ParameterDelegate<Vessel>.OnDelegateContainerLoad(node);
			}
		}

		protected override void OnParameterSave(ConfigNode node)
		{
			if(!string.IsNullOrEmpty(title)) node.SetValue("title", title);
			node.SetValue("condition_met", condition_met);
			node.SetValue("allow_interruption", allow_interruption);
			node.AddValue("duration", duration);
			node.AddValue("endTime", endTime);
		}

		protected void RemoveOneDeadVessel()
		{
			// remove vessels that no longer exist
			foreach (Guid id in validCandidates.Keys)
			{
				if (FlightGlobals.FindVessel(id) == null)
				{
					vesselParameters.Remove(id);
					validCandidates.Remove(id);

					// do only one per call should be enough
					return;
				}
			}
		}

		protected virtual void BeforeUpdate() { }
		protected virtual void AfterUpdate() { }

		protected override void OnUpdate()
		{
			base.OnUpdate();

			if (state != ParameterState.Incomplete) return;
			if (Time.fixedTime - lastUpdate < UPDATE_FREQUENCY) return;

			RemoveOneDeadVessel();
			BeforeUpdate();

			lastUpdate = Time.fixedTime;

			bool was_condition_met = condition_met;
			condition_met = false;

			foreach (Vessel vessel in FlightGlobals.Vessels)
			{
				if(IsValidCandidate(vessel))
				{
					bool check = UpdateVesselState(vessel, vesselParameters[vessel.id]);
					condition_met |= check;
					vesselParameters[vessel.id].SetState(check ? ParameterState.Complete : ParameterState.Incomplete);
				}
			}

			AfterUpdate();

			double now = Planetarium.GetUniversalTime();
			if (condition_met)
			{
				if(endTime == double.MaxValue)
				{
					endTime = now + duration - 1;
				}

				if(endTime < now)
				{
					SetState(ParameterState.Complete);
				}
			}
			else
			{
				// condition not met
				if (was_condition_met && !allow_interruption)
				{
					// interruption not allowed -> fail
					SetState(ParameterState.Failed);
				}
				else
				{
					// interruptions allowed, reset timer
					endTime = double.MaxValue;
				}
			}

			GetTitle();

			if (durationParameter != null)
			{
				if(condition_met)
					durationParameter.SetTitle("Time Remaining: " + DurationUtil.StringValue(endTime - Planetarium.GetUniversalTime()));
				else
					durationParameter.SetTitle("Duration: " + DurationUtil.StringValue(duration));
			}

			if (ChildChanged)
			{
				ContractConfigurator.ContractConfigurator.OnParameterChange.Fire(this.Root, this);
				ChildChanged = false;
			}
		}

		protected bool IsValidCandidate(Vessel vessel)
		{
			switch(vessel.vesselType)
			{
				case VesselType.Debris:
				case VesselType.Flag:
				case VesselType.SpaceObject:
				case VesselType.Unknown:
					return false;
			}

			if(!validCandidates.ContainsKey(vessel.id))
			{
				validCandidates[vessel.id] = VesselIsCandidate(vessel);

				if(validCandidates[vessel.id])
				{
					AddVesselParameter(vessel);
				}
			}

			return validCandidates[vessel.id];
		}

		/// <summary>
		/// Method to determine if a vessel is a potential candidate for this parameter.
		/// Called on parameter load, so the implementation isn't too performance critical.
		/// If this method returns false, VesselMeetsCondition will not be called for this vessel.
		/// </summary>
		/// <param name="vessel">can be loaded or unloaded</param> 
		protected abstract bool VesselIsCandidate(Vessel vessel);

		protected abstract bool UpdateVesselState(Vessel vessel, ParameterDelegate<Vessel> parameter);

		protected abstract ParameterDelegate<Vessel> CreateVesselParameter(Vessel vessel);
	}
}
