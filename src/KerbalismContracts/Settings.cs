using System;
using System.Collections.Generic;
using UnityEngine;
using KERBALISM;

namespace KerbalismContracts
{
	public static class Settings
	{
		public static void Parse()
		{
			var cfg = GameDatabase.Instance.GetConfigNode("KerbalismContracts") ?? new ConfigNode();
		}
	}


} // KERBALISM
