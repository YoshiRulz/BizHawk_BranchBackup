using System;
using System.Collections.Generic;
using System.ComponentModel;

using NLua;

namespace BizHawk.Client.Common
{
	[Description("A library for performing SQLite operations.")]
	public sealed class SQLiteLuaLibrary : DelegatingLuaLibrary
	{
		public override string Name => "SQL";

		public SQLiteLuaLibrary(Lua lua) : base(lua) {}

		public SQLiteLuaLibrary(Lua lua, Action<string> logOutputCallback) : base(lua, logOutputCallback) {}

		#region Delegated to ApiHawk

		[LuaMethodExample("local stSQLcre = SQL.createdatabase( \"eg_db\" );")] //TODO docs
		[LuaMethod("createdatabase", "Creates a SQLite Database. Name should end with .db")] //TODO docs
		public string CreateDatabase(string name) => ApiHawkContainer.Sql.CreateDatabase(name);

		[LuaMethodExample("local stSQLope = SQL.opendatabase( \"eg_db\" );")] //TODO docs
		[LuaMethod("opendatabase", "Opens a SQLite database. Name should end with .db")] //TODO docs
		public string OpenDatabase(string name) => ApiHawkContainer.Sql.OpenDatabase(name);

		[LuaMethodExample("local stSQLwri = SQL.writecommand( \"CREATE TABLE eg_tab ( eg_tab_id integer PRIMARY KEY, eg_tab_row_name text NOT NULL ); INSERT INTO eg_tab ( eg_tab_id, eg_tab_row_name ) VALUES ( 1, 'Example table row' );\" );")] //TODO docs
		[LuaMethod("writecommand", "Runs a SQLite write command which includes CREATE,INSERT, UPDATE. Ex: create TABLE rewards (ID integer  PRIMARY KEY, action VARCHAR(20)) ")] //TODO docs
		public string WriteCommand(string query = "") => ApiHawkContainer.Sql.WriteCommand(query);

		[LuaMethodExample("local obSQLrea = SQL.readcommand( \"SELECT * FROM eg_tab WHERE eg_tab_id = 1;\" );")] //TODO docs
		[LuaMethod("readcommand", "Run a SQLite read command which includes Select. Returns all rows into a LuaTable. Ex: select * from rewards")] //TODO docs
		public dynamic ReadCommand(string query = "")
		{
			var result = ApiHawkContainer.Sql.ReadCommand(query);
			return result is Dictionary<string, object> ? LuaTableFromDict(result) : result;
		}

		#endregion
	}
}
