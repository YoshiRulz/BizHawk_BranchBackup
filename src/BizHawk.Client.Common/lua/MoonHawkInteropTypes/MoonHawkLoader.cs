using System.Collections.Generic;

using BizHawk.Common;

using MoonSharp.Interpreter;

namespace BizHawk.Client.Common
{
	public sealed class MoonHawkLoader
	{
		private static readonly uint _runningHawkVer = VersionInfo.VersionStrToInt(VersionInfo.MainVersion);

		public IDictionary<string, DynValue> script_metadata;

		public bool version_at_least(string versionStr)
			=> _runningHawkVer >= VersionInfo.VersionStrToInt(versionStr);
	}
}
