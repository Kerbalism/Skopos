using System;
namespace KerbalismContracts
{
	public class SubRequirementState
	{
		public bool requirementMet;
	}

	public class Stateless : SubRequirementState
    {
		public Stateless()
		{
			requirementMet = true;
		}
	}
}
