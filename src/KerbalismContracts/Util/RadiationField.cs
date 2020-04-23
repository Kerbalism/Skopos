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
				case RadiationFieldType.INNER_BELT: return "#KerCon_innerBelt"; // inner radiation belt
				case RadiationFieldType.OUTER_BELT: return "#KerCon_outerBelt";
				case RadiationFieldType.MAGNETOPAUSE: return "#KerCon_magnetopause"; // magnetopause
				case RadiationFieldType.ANY: return "#KerCon_radiationField"; // radiation field
			}
			return "INVALID FIELD TYPE";
		}
	}
}
