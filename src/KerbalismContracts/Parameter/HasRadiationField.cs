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
		protected RadiationFieldType field;

		public override bool Load(ConfigNode configNode)
		{
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue<RadiationFieldType>(configNode, "field", x => field = x, this, RadiationFieldType.ANY, ValidateField);
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

		protected bool ValidateField(RadiationFieldType f)
		{
			if (f == RadiationFieldType.UNDEFINED)
			{
				LoggingUtil.LogError(this, "Missing field. You must specify field = INNER_BELT, OUTER_BELT, MAGNETOPAUSE or ANY.");
				return false;
			}
			return true;
		}

	}

	public class HasRadiationFieldParameter : VesselParameter
	{
		public RadiationFieldType field;

		public HasRadiationFieldParameter() : base(null) { }

		public HasRadiationFieldParameter(RadiationFieldType field, CelestialBody targetBody, string title)
		{
			this.field = field;
			this.targetBody = targetBody;
			this.title = title;
		}

		protected override string GetParameterTitle()
		{
			if (!string.IsNullOrEmpty(title)) return title;

			string bodyName = targetBody != null ? targetBody.CleanDisplayName() : "a body";
			return bodyName + " has " + RadiationField.Name(field);
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
			var bd = KerbalismContracts.Instance.BodyData(vessel.mainBody);
			switch (field)
			{
				case RadiationFieldType.INNER_BELT: return bd.has_inner;
				case RadiationFieldType.OUTER_BELT: return bd.has_outer;
				case RadiationFieldType.MAGNETOPAUSE: return bd.has_pause;
				case RadiationFieldType.ANY: return bd.has_inner || bd.has_outer || bd.has_pause;
			}

			return false;
		}

		protected override bool VesselMeetsCondition(Vessel vessel)
		{
			if (vessel == null) return false;

			if (targetBody != null && vessel.mainBody != targetBody) return false;

			return HasField(vessel);
		}
	}
}
