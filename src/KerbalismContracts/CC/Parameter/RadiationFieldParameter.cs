using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace KerbalismContracts
{
	/// <summary> Parameter for global radiation field status: how often has a radiation field been penetrated </summary>
	public class RadiationFieldFactory : ParameterFactory
	{
		protected RadiationFieldType field;
		protected int crossings_min, crossings_max;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue<RadiationFieldType>(configNode, "field", x => field = x, this, RadiationFieldType.UNDEFINED, ValidateField);
			valid &= ConfigNodeUtil.ParseValue<int>(configNode, "crossings_min", x => crossings_min = x, this, -1);
			valid &= ConfigNodeUtil.ParseValue<int>(configNode, "crossings_max", x => crossings_max = x, this, -1);

			valid &= ValidateTargetBody(configNode);

			if (crossings_max > 0 && crossings_min > crossings_max)
			{
				LoggingUtil.LogError(GetType(), ErrorPrefix() + ": crossings_min must be <= crossings_max");
				valid = false;
			}

			return valid;
		}

		public override ContractParameter Generate(Contract contract)
		{
			if (!ValidateTargetBody())
				return null;

			return new RadiationFieldParameter(field, crossings_min, crossings_max, targetBody, title);
		}

		private bool ValidateField(RadiationFieldType f)
		{
			if(f == RadiationFieldType.UNDEFINED || f == RadiationFieldType.ANY)
			{
				LoggingUtil.LogError(this, "Missing field. You must specify field = INNER_BELT, OUTER_BELT or MAGNETOPAUSE.");
				return false;
			}
			return true;
		}
	}

	public class RadiationFieldParameter : ContractConfiguratorParameter
	{
		public RadiationFieldType field;
		protected int crossings_min;
		protected int crossings_max;

		protected int crossed_count = 0;

		public RadiationFieldParameter(): base(null) {}

		public RadiationFieldParameter(RadiationFieldType field, int crossings_min, int crossings_max, CelestialBody targetBody, string title)
		{
			this.field = field;
			this.crossings_min = crossings_min;
			this.crossings_max = crossings_max;
			this.targetBody = targetBody;
			this.title = title;
		}

		protected override string GetParameterTitle()
		{
			string bodyName = targetBody?.CleanDisplayName() ?? "a body";
			string fieldName = RadiationField.Name(field);

			string prefix = title;
			if (!string.IsNullOrEmpty(prefix))
				prefix = prefix + ":\n - ";

			if (crossed_count == 0)
			{
				if (crossings_min > 0)
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
			node.AddValue("field", field);
			node.AddValue("targetBody", targetBody.name);
			node.AddValue("crossings_min", crossings_min);
			node.AddValue("crossings_max", crossings_max);
			node.AddValue("title", title);
		}

		protected override void OnParameterLoad(ConfigNode node)
		{
			try
			{
				field = ConfigNodeUtil.ParseValue<RadiationFieldType>(node, "field", RadiationFieldType.UNDEFINED);
				targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody", (CelestialBody)null);
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
			if (state == null || targetBody == null || state.bodyIndex != targetBody.flightGlobalsIndex)
				return;

			var bd = KerbalismContracts.Instance.BodyData(targetBody);
			if (bd == null)
				return;

			switch (field)
			{
				case RadiationFieldType.INNER_BELT: crossed_count = bd.inner_crossings; break;
				case RadiationFieldType.OUTER_BELT: crossed_count = bd.outer_crossings; break;
				case RadiationFieldType.MAGNETOPAUSE: crossed_count = bd.magneto_crossings; break;
			}

			var result = ParameterState.Incomplete;

			if (crossings_min > 0 && crossed_count >= crossings_min)
				result = ParameterState.Complete;
			if (crossings_max > 0 && crossed_count > crossings_max)
				result = ParameterState.Failed;

			GetTitle();

			SetState(result);
		}
	}
}
