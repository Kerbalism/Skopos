using System;
using System.Collections.Generic;
using Contracts;
using KSP.Localization;
using ContractConfigurator;
using ContractConfigurator.Parameters;

namespace Kerbalism.Contracts
{
	public class HasRadiationFieldFactory : ParameterFactory
	{
		protected RadiationField field;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue<RadiationField>(configNode, "field", x => field = x, this, RadiationField.ANY, ValidateField);
			valid &= ValidateTargetBody(configNode);

			return valid;
		}

		public override ContractParameter Generate(Contract contract)
		{
			if (!ValidateTargetBody())
			{
				return null;
			}

			return new HasRadiationFieldParameter(field, targetBody, title);
		}

		protected bool ValidateField(RadiationField f)
		{
			if (f == RadiationField.UNDEFINED)
			{
				LoggingUtil.LogError(this, "Missing field. You must specify field = INNER_BELT, OUTER_BELT, MAGNETOPAUSE or ANY.");
				return false;
			}
			return true;
		}

	}

	public class HasRadiationFieldParameter : VesselParameter
	{
		public RadiationField field;

		public HasRadiationFieldParameter() : base(null) { }

		public HasRadiationFieldParameter(RadiationField field, CelestialBody targetBody, string title)
		{
			this.field = field;
			this.targetBody = targetBody;
			this.title = title;
		}

		protected override string GetParameterTitle()
		{
			if (!string.IsNullOrEmpty(title)) return title;

			string bodyName = targetBody != null ? targetBody.CleanDisplayName() : "a body";
			return bodyName + " has " + RadiationFieldParameter.FieldName(field);
		}

		protected override void OnParameterSave(ConfigNode node)
		{
			base.OnParameterSave(node);

			node.AddValue("field", field);
			node.AddValue("targetBody", targetBody.name);
		}

		protected override void OnParameterLoad(ConfigNode node)
		{
			try
			{
				base.OnParameterLoad(node);

				field = ConfigNodeUtil.ParseValue<RadiationField>(node, "field", RadiationField.UNDEFINED);
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
			GameEvents.onVesselSOIChanged.Add(SoiChanged);
		}

		protected override void OnUnregister()
		{
			base.OnUnregister();
			GameEvents.onVesselSOIChanged.Remove(SoiChanged);
		}

		private void SoiChanged(GameEvents.HostedFromToAction<Vessel, CelestialBody> action)
		{
			CheckVessel(action.to.GetVessel());
		}

		protected bool HasField(Vessel vessel)
		{
			switch (field)
			{
				case RadiationField.INNER_BELT:
					return KERBALISM.API.HasInnerBelt(vessel.mainBody);
				case RadiationField.OUTER_BELT:
					return KERBALISM.API.HasOuterBelt(vessel.mainBody);
				case RadiationField.MAGNETOPAUSE:
					return KERBALISM.API.HasMagnetopause(vessel.mainBody);
				case RadiationField.ANY:
					return KERBALISM.API.HasMagneticField(vessel.mainBody);
			}

			return false;
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			if (vessel == null) return false;

			LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);

			if (targetBody != null && vessel.mainBody != targetBody) return false;

			return HasField(vessel);
		}
	}
}
