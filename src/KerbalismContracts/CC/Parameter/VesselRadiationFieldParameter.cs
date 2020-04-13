using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace KerbalismContracts
{
	/// <summary> Parameter for radiation field shenanigans that are related to a vessel (like max. crossings of inner belt, or avoid belt to moon type of contracts) </summary>
	public class VesselRadiationFieldFactory : ParameterFactory
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

			return new VesselRadiationFieldParameter(field, crossings_min, crossings_max, stay_in, stay_out, targetBody, title);
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

	public class VesselRadiationFieldParameter : VesselParameter
	{
		public RadiationFieldType field;
		protected int crossings_min;
		protected int crossings_max;
		protected bool stay_in;
		protected bool stay_out;

		protected int crossed_count = 0;
		protected bool currently_in_field = false;

		public VesselRadiationFieldParameter(): base(null) {}

		public VesselRadiationFieldParameter(RadiationFieldType field, int crossings_min, int crossings_max, bool stay_in, bool stay_out, CelestialBody targetBody, string title)
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
			string bodyName = targetBody?.CleanDisplayName() ?? "a body";
			string fieldName = RadiationField.Name(field);

			string prefix = title;
			if (!string.IsNullOrEmpty(prefix))
				prefix = prefix + ": ";

			if (stay_in)
				return prefix + Localizer.Format("Stay in <<1>> of <<2>>", fieldName, bodyName);
			if(stay_out)
				return prefix + Localizer.Format("Do not enter <<1>> of <<2>>", fieldName, bodyName);

			if(crossed_count == 0)
			{
				if(crossings_min > 0)
					return prefix + Localizer.Format("Cross <<1>> of <<2>> at least <<3>> times", fieldName, bodyName, crossings_min);
				if (crossings_max > 0)
					return prefix + Localizer.Format("Cross <<1>> of <<2>> no more than <<3>> times", fieldName, bodyName, crossings_max);
			}
			else
			{
				if (crossings_min > 0)
					return prefix + Localizer.Format("Cross <<1>> of <<2>> at least <<3>> times (<<4>>/<<3>>)", fieldName, bodyName, crossings_min, crossed_count);
				if (crossings_max > 0)
					return prefix + Localizer.Format("Cross <<1>> of <<2>> no more than <<3>> times (<<4>>/<<3>>)", fieldName, bodyName, crossings_max, crossed_count);
			}

			return prefix + Localizer.Format("Find <<1>> of <<2>>", fieldName, bodyName);
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

		private void RunCheck(Vessel v, VesselRadiationFieldStatus state)
		{
			CheckVessel(v);
		}

		internal static bool InField(RadiationFieldType f, VesselRadiationFieldStatus state)
		{
			switch (f)
			{
				case RadiationFieldType.INNER_BELT: return state.inner_belt;
				case RadiationFieldType.OUTER_BELT: return state.outer_belt;
				case RadiationFieldType.MAGNETOPAUSE: return state.magnetosphere;
				case RadiationFieldType.ANY:
					return state.inner_belt || state.outer_belt || state.magnetosphere;
			}
			return false;
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			var states = RadiationFieldTracker.RadiationFieldStates(vessel);
			if (states == null)
				return false;

			var body = targetBody != null ? targetBody : vessel.mainBody;
			var state = states.Find(s => s.bodyIndex == body.flightGlobalsIndex);
			if (state == null)
				return false;

			bool in_field = InField(field, state);

			if (stay_in && !in_field) return false;
			if (stay_out && in_field) return false;
			if (stay_in || stay_out) return true;

			// Utils.LogDebug($"VesselRadiationFieldParameter {field} / {vessel}: in_field { in_field} currently {currently_in_field} crossed {crossed_count}");

			if (in_field != currently_in_field)
			{
				crossed_count++;
				GetTitle();
			}
			currently_in_field = in_field;
			
			if (crossings_min >= 0 && crossed_count < crossings_min)
				return false;
			if (crossings_max >= 0 && crossed_count > crossings_max)
				return false;
			return true;
		}
	}
}
