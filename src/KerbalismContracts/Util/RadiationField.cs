using System;

namespace KerbalismContracts
{
	public enum RadiationFieldType { UNDEFINED, INNER_BELT, OUTER_BELT, MAGNETOPAUSE, ANY }

	public class RadiationField
	{
		public static string Name(RadiationFieldType field)
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
}
