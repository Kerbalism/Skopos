using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{
	public enum RadiationFieldType { UNDEFINED, INNER_BELT, OUTER_BELT, MAGNETOPAUSE, ANY }

	public class RadiationField
	{
		public static String Name(RadiationFieldType field)
		{
			switch (field)
			{
				case RadiationFieldType.INNER_BELT: return "inner radiation belt";
				case RadiationFieldType.OUTER_BELT: return "outer radiation belt";
				case RadiationFieldType.MAGNETOPAUSE: return "magnetopause";
				case RadiationFieldType.ANY: return "radiation field";
			}
			return "INVALID FIELD TYPE";
		}
	}

	public class RadiationFieldFactory : ParameterFactory
	{
		protected RadiationFieldType field;
		protected int crossings_min, crossings_max;
		protected bool stay_in, stay_out;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue<RadiationFieldType>(configNode, "field", x => field = x, this, RadiationFieldType.UNDEFINED, ValidateField);
			valid &= ConfigNodeUtil.ParseValue<int>(configNode, "crossings_min", x => crossings_min = x, this, -1);
			valid &= ConfigNodeUtil.ParseValue<int>(configNode, "crossings_max", x => crossings_max = x, this, -1);
			valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "stay_in", x => stay_in = x, this, false);
			valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "stay_out", x => stay_out = x, this, false);

			valid &= ValidateTargetBody(configNode);
			valid &= ConfigNodeUtil.MutuallyExclusive(configNode, new string[] { "stay_in", "stay_out" }, new string[] { "crossings_min", "crossings_max" }, this);

			if (crossings_max > 0 && crossings_min > crossings_max)
			{
				LoggingUtil.LogError(GetType(), ErrorPrefix() + ": crossings_min must be <= crossings_max");
				valid = false;
			}

			if (stay_in && stay_out)
			{
				LoggingUtil.LogError(GetType(), ErrorPrefix() + ": cannot be both stay_in AND stay_out");
				valid = false;
			}

			return valid;
		}

		public override ContractParameter Generate(Contract contract)
		{
			if (!ValidateTargetBody())
			{
				return null;
			}

			return new RadiationFieldParameter(field, crossings_min, crossings_max, stay_in, stay_out, targetBody, title);
		}

		private bool ValidateField(RadiationFieldType f)
		{
			if(f == RadiationFieldType.UNDEFINED)
			{
				LoggingUtil.LogError(this, "Missing field. You must specify field = INNER_BELT, OUTER_BELT, MAGNETOPAUSE or ANY.");
				return false;
			}
			return true;
		}

	}

	public class RadiationFieldParameter : VesselParameter
	{
		public RadiationFieldType field;
		protected int crossings_min;
		protected int crossings_max;
		protected bool stay_in;
		protected bool stay_out;

		protected int crossed_count = 0;
		protected bool currently_in_field = false;

		public RadiationFieldParameter(): base(null) {}

		public RadiationFieldParameter(RadiationFieldType field, int crossings_min, int crossings_max, bool stay_in, bool stay_out, CelestialBody targetBody, string title)
		{
			this.field = field;
			this.crossings_min = crossings_min;
			this.crossings_max = crossings_max;
			this.stay_in = stay_in;
			this.stay_out = stay_out;
			this.targetBody = targetBody;
			this.title = title;
		}

		protected override string GetParameterTitle()
		{
			if (!string.IsNullOrEmpty(title)) return title;

			string bodyName = targetBody != null ? targetBody.CleanDisplayName() : "a body";
			return "Find " + RadiationField.Name(field) + " of " + bodyName;
		}

		protected static bool InField(RadiationFieldType f, RadiationFieldState state)
		{
			switch(f)
			{
				case RadiationFieldType.INNER_BELT: return state.inner_belt;
				case RadiationFieldType.OUTER_BELT: return state.outer_belt;
				case RadiationFieldType.MAGNETOPAUSE: return state.magnetosphere;
				case RadiationFieldType.ANY:
					return state.inner_belt || state.outer_belt || state.magnetosphere;
			}
			return false;
		}

		protected override void OnParameterSave(ConfigNode node)
		{
			base.OnParameterSave(node);

			node.AddValue("field", field);
			node.AddValue("crossed_count", crossed_count);
			node.AddValue("currently_in_field", currently_in_field);
			node.AddValue("targetBody", targetBody.name);
			node.AddValue("stay_in", stay_in);
			node.AddValue("stay_out", stay_out);
			node.AddValue("crossings_min", crossings_min);
			node.AddValue("crossings_max", crossings_max);
			node.AddValue("title", title);
		}

		protected override void OnParameterLoad(ConfigNode node)
		{
			try
			{
				base.OnParameterLoad(node);

				field = ConfigNodeUtil.ParseValue<RadiationFieldType>(node, "field", RadiationFieldType.UNDEFINED);
				crossed_count = ConfigNodeUtil.ParseValue<int>(node, "crossed_count", 0);
				currently_in_field = ConfigNodeUtil.ParseValue<bool>(node, "currently_in_field", false);
				targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody", (CelestialBody)null);
				stay_in = ConfigNodeUtil.ParseValue<bool>(node, "stay_in", false);
				stay_out = ConfigNodeUtil.ParseValue<bool>(node, "stay_out", false);
				crossings_min = ConfigNodeUtil.ParseValue<int>(node, "crossings_min", 0);
				crossings_max = ConfigNodeUtil.ParseValue<int>(node, "crossings_max", 0);
				title = ConfigNodeUtil.ParseValue(node, "title", string.Empty);
			}
			finally
			{
				ParameterDelegate<Vessel>.OnDelegateContainerLoad(node);
			}
		}

		protected override void OnRegister()
		{
			base.OnRegister();
			RadiationFieldTracker.AddListener(RunCheck);
		}

		protected override void OnUnregister()
		{
			base.OnUnregister();
			RadiationFieldTracker.RemoveListener(RunCheck);
		}

		private void RunCheck(Vessel v, RadiationFieldState state)
		{
			CheckVessel(v);
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			var states = RadiationFieldTracker.RadiationFieldStates(vessel);
			if (states == null) return false;

			var state = states.Find(s => s.bodyIndex == targetBody.flightGlobalsIndex);
			if (state == null) return false;

			bool in_field = InField(field, state);

			if (stay_in && !in_field) return false;
			if (stay_out && in_field) return false;
			if (stay_in || stay_out) return true;

			Lib.Log("RadiationFieldParameter " + field + " / " + vessel + ": in_field " + in_field + " currently " + currently_in_field + " crossed " + crossed_count);

			if (in_field != currently_in_field) crossed_count++;
			currently_in_field = in_field;

			if (crossings_min >= 0 && crossed_count < crossings_min) return false;
			if (crossings_max >= 0 && crossed_count > crossings_max) return false;
			return true;
		}
	}

	public class RadiationFieldVisibleFactory : ParameterFactory
	{
		protected RadiationFieldType field;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue<RadiationFieldType>(configNode, "field", x => field = x, this, RadiationFieldType.ANY);
			valid &= ValidateTargetBody(configNode);

			return valid;
		}

		public override ContractParameter Generate(Contract contract)
		{
			if (!ValidateTargetBody())
			{
				return null;
			}

			return new RadiationFieldVisibleParameter(field, targetBody, title);
		}
	}

	public class RadiationFieldVisibleParameter : ContractConfiguratorParameter
	{
		public RadiationFieldType field;

		public RadiationFieldVisibleParameter() : base(null) { }

		public RadiationFieldVisibleParameter(RadiationFieldType field, CelestialBody targetBody, string title)
		{
			this.field = field;
			this.targetBody = targetBody;
			this.title = title;
		}

		protected override string GetParameterTitle()
		{
			if (!string.IsNullOrEmpty(title)) return title;

			string bodyName = targetBody != null ? targetBody.CleanDisplayName() : "a body";
			return RadiationField.Name(field) + " of " + bodyName + " is researched";
		}

		protected override void OnParameterSave(ConfigNode node)
		{
			node.AddValue("field", field);
			node.AddValue("targetBody", targetBody.name);
		}

		protected override void OnParameterLoad(ConfigNode node)
		{
			try
			{
				field = ConfigNodeUtil.ParseValue<RadiationFieldType>(node, "field", RadiationFieldType.UNDEFINED);
				targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody", (CelestialBody)null);
			}
			finally
			{
				ParameterDelegate<Vessel>.OnDelegateContainerLoad(node);
			}
		}

		protected override void OnRegister()
		{
			base.OnRegister();
			RadiationFieldTracker.AddListener(RunCheck);
		}

		protected override void OnUnregister()
		{
			base.OnUnregister();
			RadiationFieldTracker.RemoveListener(RunCheck);
		}

		private void RunCheck(Vessel v, RadiationFieldState newState)
		{
			var bd = KerbalismContracts.Instance.BodyData(targetBody);

			switch(field)
			{
				case RadiationFieldType.INNER_BELT:
					SetState(bd.inner_visible || !bd.has_inner ? ParameterState.Complete : ParameterState.Incomplete);
					break;
				case RadiationFieldType.OUTER_BELT:
					SetState(bd.outer_visible || !bd.has_outer ? ParameterState.Complete : ParameterState.Incomplete);
					break;
				case RadiationFieldType.MAGNETOPAUSE:
					SetState(bd.pause_visible || !bd.has_pause ? ParameterState.Complete : ParameterState.Incomplete);
					break;
				case RadiationFieldType.ANY:
					bool hasNone = !bd.has_inner && !bd.has_outer && !bd.has_pause;
					SetState(hasNone || bd.inner_visible || bd.outer_visible || bd.pause_visible ? ParameterState.Complete : ParameterState.Incomplete);
					break;
			}
		}
	}
}
