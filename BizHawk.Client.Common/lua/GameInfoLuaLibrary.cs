using System;

using BizHawk.Emulation.Common;

using NLua;

namespace BizHawk.Client.Common
{
	public sealed class GameInfoLuaLibrary : DelegatingLuaLibrary
	{
		public override string Name => "gameinfo";

		[OptionalService]
		private IBoardInfo BoardInfo { get; set; }

		public GameInfoLuaLibrary(Lua lua) : base(lua) {}

		public GameInfoLuaLibrary(Lua lua, Action<string> logOutputCallback) : base(lua, logOutputCallback) {}

		#region Delegated to ApiHawk

		[LuaMethodExample("local rom_mapper = gameinfo.getboardtype()")]
		[LuaMethod("getboardtype", "returns identifying information about the 'mapper' or similar capability used for this game.  empty if no such useful distinction can be drawn")] //TODO docs
		public string GetBoardType() => ApiHawkContainer.GameInfo.GetBoardType();

		[LuaMethodExample("local rom_loading_options = gameinfo.getoptions()")]
		[LuaMethod("getoptions", "returns the game options for the currently loaded rom. Options vary per platform")] //TODO docs
		public LuaTable GetOptions() => LuaTableFromDict(ApiHawkContainer.GameInfo.GetOptions());

		[LuaMethodExample("local rom_hash = gameinfo.getromhash()")]
		[LuaMethod("getromhash", "returns the hash of the currently loaded rom, if a rom is loaded")] //TODO docs
		public string GetRomHash() => ApiHawkContainer.GameInfo.GetRomHash();

		[LuaMethodExample("local rom_name = gameinfo.getromname()")]
		[LuaMethod("getromname", "returns the name of the currently loaded rom, if a rom is loaded")] //TODO docs
		public string GetRomName() => ApiHawkContainer.GameInfo.GetRomName();

		[LuaMethodExample("local rom_db_flag = gameinfo.getstatus()")]
		[LuaMethod("getstatus", "returns the game database status of the currently loaded rom. Statuses are for example: GoodDump, BadDump, Hack, Unknown, NotInDatabase")] //TODO docs
		public string GetStatus() => ApiHawkContainer.GameInfo.GetStatus();

		[LuaMethodExample("if (gameinfo.indatabase()) then console.log(\"returns whether or not the currently loaded rom is in the game database\") end")] //TODO docs
		[LuaMethod("indatabase", "returns whether or not the currently loaded rom is in the game database")] //TODO docs
		public bool InDatabase() => ApiHawkContainer.GameInfo.InDatabase();

		[LuaMethodExample("if (gameinfo.isstatusbad()) then console.log(\"returns the currently loaded rom's game database status is considered 'bad'\") end")] //TODO docs
		[LuaMethod("isstatusbad", "returns the currently loaded rom's game database status is considered 'bad'")] //TODO docs
		public bool IsStatusBad() => ApiHawkContainer.GameInfo.IsStatusBad();

		#endregion
	}
}
