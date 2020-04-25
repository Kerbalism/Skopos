using KSP.Localization;

namespace KerbalismContracts
{
	public enum RadiationFieldType { UNDEFINED, INNER_BELT, OUTER_BELT, MAGNETOPAUSE, ANY }

	public class RadiationField
	{
		public static string Name(RadiationFieldType field)
		{
			switch (field)
			{
				case RadiationFieldType.INNER_BELT: return Localizer.Format("#KerCon_innerBelt"); // inner radiation belt
				case RadiationFieldType.OUTER_BELT: return Localizer.Format("#KerCon_outerBelt"); // outer radiation belt
				case RadiationFieldType.MAGNETOPAUSE: return Localizer.Format("#KerCon_magnetopause"); // magnetopause
				case RadiationFieldType.ANY: return Localizer.Format("#KerCon_radiationField"); // radiation field
			}
			return "INVALID FIELD TYPE";
		}
	}
}
