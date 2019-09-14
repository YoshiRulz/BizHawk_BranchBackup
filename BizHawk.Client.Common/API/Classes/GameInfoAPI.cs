using System.Collections.Generic;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class GameInfoAPI : IGameInfo
	{
		[OptionalService]
		private IBoardInfo BoardInfo { get; set; }

		#region Public API (IGameInfo)

		public string GetBoardType() => BoardInfo?.BoardName ?? string.Empty;

		public Dictionary<string, string> GetOptions() => Global.Game?.GetOptionsDict()?.ShallowCopy() ?? new Dictionary<string, string>();

		public string GetRomHash() => Global.Game?.Hash ?? string.Empty;

		public string GetRomName() => Global.Game?.Name ?? string.Empty;

		public string GetStatus() => (Global.Game?.Status)?.ToString() ?? string.Empty;

		public bool InDatabase() => Global.Game != null && !Global.Game.NotInDatabase;

		public bool IsStatusBad() => Global.Game == null || Global.Game.IsRomStatusBad();

		#endregion
	}
}
