using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace KerbalismContracts
{
	/// <summary> Parameter for radiation field visibility </summary>
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
			return Localizer.Format("<<1>> of <<2>> is researched", RadiationField.Name(field), bodyName);
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

		private void RunCheck(Vessel v, VesselRadiationFieldStatus newState)
		{
			var bd = KerbalismContracts.Instance.BodyData(targetBody);

			switch (field)
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
