using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{	
	public abstract class RadiationFieldFactory : ParameterFactory
	{
		protected int crossings;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);
			valid &= targetBody != null;
			valid &= ConfigNodeUtil.ParseValue<int>(configNode, "crossings", x => crossings = x, this, 1);
			return valid;
		}
	}

	public abstract class RadiationFieldParameter : VesselParameter
	{
		protected int crossings = 1;
		protected bool in_field = false;

		private float lastUpdate = 0.0f;
		private const float UPDATE_FREQUENCY = 0.25f;

		protected RadiationFieldParameter(CelestialBody targetBody, int crossings)
		{
			this.targetBody = targetBody;
			this.crossings = crossings;
		}

		protected override void OnParameterLoad(ConfigNode node)
		{
			base.OnParameterLoad(node);
			in_field = Lib.ConfigValue(node, "in_field", false);
			crossings = Lib.ConfigValue(node, "crossings", 1);
		}

		protected override void OnParameterSave(ConfigNode node)
		{
			base.OnParameterSave(node);
			node.AddValue("in_field", in_field);
			node.AddValue("crossings", crossings);
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if (UnityEngine.Time.fixedTime - lastUpdate > UPDATE_FREQUENCY)
			{
				lastUpdate = UnityEngine.Time.fixedTime;
				CheckVessel(FlightGlobals.ActiveVessel);
			}
		}
	}

	public abstract class RevealRadiationFieldFactory : BehaviourFactory
	{
		protected bool visible;
		protected bool requireCompletion = false;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);
			valid &= targetBody != null;
			valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "visible", x => visible = x, this, true);
			valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "require_completion", x => requireCompletion = x, this, false);
			return valid;
		}
	}

	public abstract class RevealRadiationFieldBehaviour: ContractBehaviour
	{
		protected CelestialBody targetBody;
		protected bool visible = true;
		protected bool requireCompletion = false;

		protected RevealRadiationFieldBehaviour(CelestialBody targetBody, bool visible, bool requireCompletion)
		{
			this.targetBody = targetBody;
			this.visible = visible;
			this.requireCompletion = requireCompletion;
		}
	}
}
