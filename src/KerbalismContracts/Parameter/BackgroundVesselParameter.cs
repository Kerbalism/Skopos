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
		protected class VesselData
		{
			internal bool valid;
			internal double expiration;
			internal ParameterDelegate<Vessel> parameterDelegate;
			private Vessel vessel;

			public VesselData(Vessel vessel, bool valid)
			{
				this.vessel = vessel;
				this.valid = valid;
				this.expiration = Time.time + 10;
			}
		}

		private readonly Dictionary<Guid, VesselData> vesselData = new Dictionary<Guid, VesselData>();

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
			if (!vesselData.ContainsKey(vessel.id)) return;
			if (vesselData[vessel.id].parameterDelegate != null) return;

			vesselData[vessel.id].parameterDelegate = CreateVesselParameter(vessel);
			vesselData[vessel.id].parameterDelegate.Optional = true;
			vesselData[vessel.id].parameterDelegate.fakeOptional = true;
			AddParameter(vesselData[vessel.id].parameterDelegate);
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
				vesselData.Clear();

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

		protected void AddVessel(Vessel vessel, bool valid)
		{
			if (vesselData.ContainsKey(vessel.id)) return;

			vesselData[vessel.id] = new VesselData(vessel, valid);
			if(valid && vesselData[vessel.id].parameterDelegate == null)
			{
				vesselData[vessel.id].parameterDelegate = CreateVesselParameter(vessel);
				vesselData[vessel.id].parameterDelegate.Optional = true;
				vesselData[vessel.id].parameterDelegate.fakeOptional = true;
				AddParameter(vesselData[vessel.id].parameterDelegate);
				ContractConfigurator.ContractConfigurator.OnParameterChange.Fire(this.Root, this);
			}
		}

		/// <summary>
		/// Remove vessels that are missing / no longer valid, add vessels that are now valid but were not before
		/// </summary>
		protected void RefreshVesselData()
		{
			foreach (Guid id in vesselData.Keys)
			{
				var vessel = FlightGlobals.FindVessel(id);
				if (vessel == null)
				{
					// vessel no longer exists -> remove
					if(vesselData[id].parameterDelegate != null)
					{
						RemoveParameter(vesselData[id].parameterDelegate);
						ContractConfigurator.ContractConfigurator.OnParameterChange.Fire(this.Root, this);
					}
					vesselData.Remove(id);
					return;
				}

				if (vesselData[id].expiration < Time.time)
				{
					bool isCandidate = IsValidCandidate(vessel, true);
					vesselData[id].expiration = Time.time + 30;

					if (isCandidate != vesselData[id].valid)
					{
						if(!isCandidate)
						{
							// remove vessel that no longer is a candidate
							if (vesselData[id].parameterDelegate != null)
							{
								RemoveParameter(vesselData[id].parameterDelegate);
								ContractConfigurator.ContractConfigurator.OnParameterChange.Fire(this.Root, this);
							}

							vesselData[id].parameterDelegate = null;
							vesselData[id].valid = false;
						}
						else
						{
							// add vessel that now is a candidate
							vesselData[id].valid = true;
							vesselData[id].expiration = Time.time + 30;
							vesselData[id].parameterDelegate = CreateVesselParameter(vessel);
							vesselData[id].parameterDelegate.Optional = true;
							vesselData[id].parameterDelegate.fakeOptional = true;
							AddParameter(vesselData[id].parameterDelegate);
							ContractConfigurator.ContractConfigurator.OnParameterChange.Fire(this.Root, this);
						}
					}

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

			RefreshVesselData();
			BeforeUpdate();

			lastUpdate = Time.fixedTime;

			bool was_condition_met = condition_met;
			condition_met = false;

			foreach (Vessel vessel in FlightGlobals.Vessels)
			{
				if(IsValidCandidate(vessel))
				{
					AddVessel(vessel, true);

					bool check = UpdateVesselState(vessel, vesselData[vessel.id]);
					condition_met |= check;
					vesselData[vessel.id].parameterDelegate.SetState(check ? ParameterState.Complete : ParameterState.Incomplete);
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

		protected bool IsValidCandidate(Vessel vessel, bool forceCheck = false)
		{
			switch(vessel.vesselType)
			{
				case VesselType.Debris:
				case VesselType.Flag:
				case VesselType.SpaceObject:
				case VesselType.Unknown:
					return false;
			}

			if(forceCheck || !vesselData.ContainsKey(vessel.id))
			{
				return VesselIsCandidate(vessel);
			}

			return vesselData[vessel.id].valid;
		}

		/// <summary>
		/// Method to determine if a vessel is a potential candidate for this parameter.
		/// Called on parameter load, so the implementation isn't too performance critical.
		/// If this method returns false, VesselMeetsCondition will not be called for this vessel.
		/// </summary>
		/// <param name="vessel">can be loaded or unloaded</param> 
		protected abstract bool VesselIsCandidate(Vessel vessel);

		protected abstract bool UpdateVesselState(Vessel vessel, VesselData vesselData);

		protected abstract ParameterDelegate<Vessel> CreateVesselParameter(Vessel vessel);
	}
}
