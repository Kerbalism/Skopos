using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{	
	public abstract class AbstractRadiationFieldFactory : ParameterFactory
	{
		protected int crossings_min;
		protected int crossings_max;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);
			valid &= targetBody != null;
			valid &= ConfigNodeUtil.ParseValue<int>(configNode, "crossings_min", x => crossings_min = x, this, 1);
			valid &= ConfigNodeUtil.ParseValue<int>(configNode, "crossings_max", x => crossings_max = x, this, -1);

			if(crossings_min < 0) {
				LoggingUtil.LogError(GetType(), ErrorPrefix() + ": crossings_min must be >= 0, but is " + crossings_min);
				valid = false;
			}

			if (crossings_max > 0 && crossings_min > crossings_max) {
				LoggingUtil.LogError(GetType(), ErrorPrefix() + ": crossings_min must be < crossings_max (min " + crossings_min + " > max " + crossings_max + ")");
				valid = false;
			}

			return valid;
		}
	}

	public abstract class AbstractRadiationFieldParameter : VesselParameter
	{
		protected int crossings_min = 1;
		protected int crossings_max = -1;
		protected int crossed_count = 0;
		protected bool in_field = false;

		private float lastUpdate = 0.0f;
		private const float UPDATE_FREQUENCY = 0.25f;

		protected AbstractRadiationFieldParameter(CelestialBody targetBody, int crossings_min, int crossings_max)
		{
			this.targetBody = targetBody;
			this.crossings_min = crossings_min;
			this.crossings_max = crossings_max;
		}

		protected override void OnParameterLoad(ConfigNode node)
		{
			base.OnParameterLoad(node);
			in_field = Lib.ConfigValue(node, "in_field", false);
			crossings_min = Lib.ConfigValue(node, "crossings_min", 1);
			crossings_max = Lib.ConfigValue(node, "crossings_max", -1);
			crossed_count = Lib.ConfigValue(node, "crossed_count", 0);
		}

		protected override void OnParameterSave(ConfigNode node)
		{
			base.OnParameterSave(node);
			node.AddValue("in_field", in_field);
			node.AddValue("crossings_min", crossings_min);
			node.AddValue("crossings_max", crossings_max);
			node.AddValue("crossed_count", crossed_count);
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

		protected bool UpdateCrossingCount(bool current_condition) {
			if (current_condition != in_field) crossed_count++;
			in_field = current_condition;

			bool result = crossed_count >= crossings_min;
			if (crossings_max > 0)
				result &= crossed_count <= crossings_max;
			
			return result;
		}
	}

	public abstract class AbstractRevealRadiationFieldFactory : BehaviourFactory
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

	public abstract class AbstractRevealRadiationFieldBehaviour: ContractBehaviour
	{
		protected CelestialBody targetBody;
		protected bool visible = true;
		protected bool requireCompletion = false;

		protected AbstractRevealRadiationFieldBehaviour(CelestialBody targetBody, bool visible, bool requireCompletion)
		{
			this.targetBody = targetBody;
			this.visible = visible;
			this.requireCompletion = requireCompletion;
		}
	}
}
