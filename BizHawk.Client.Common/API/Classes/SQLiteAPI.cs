using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace BizHawk.Client.Common
{
	public sealed class SQLiteAPI : ISQLite
	{
		private SQLiteConnection connection;

		#region Public API (ISQLite)

		public string CreateDatabase(string name)
		{
			try
			{
				SQLiteConnection.CreateFile(name);
				return "Database Created Successfully";
			}
			catch (SQLiteException sqlEX)
			{
				return sqlEX.Message;
			}
		}

		public string OpenDatabase(string name)
		{
			try
			{
				connection = new SQLiteConnection(new SQLiteConnectionStringBuilder {
						DataSource = name,
						Version = 3, // SQLite version
						JournalMode = SQLiteJournalModeEnum.Wal, // Allows for reads and writes to happen at the same time
						DefaultIsolationLevel = IsolationLevel.ReadCommitted, // This only helps make the database lock left. May be pointless now
						SyncMode = SynchronizationModes.Off // This shortens the delay for do synchronous calls.
					}.ToString()
				);
				connection.Open();
				connection.Close();
				return "Database Opened Successfully";
			}
			catch (SQLiteException sqlEX)
			{
				return sqlEX.Message;
			}
		}

		public dynamic ReadCommand(string query)
		{
			if (string.IsNullOrWhiteSpace(query)) return "query is empty";
			try
			{
				connection.Open();
				var reader = new SQLiteCommand($"PRAGMA read_uncommitted =1;{query}", connection).ExecuteReader();
				if (reader.HasRows)
				{
					var columnNames = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
					var rowCount = 0L;
					var table = new Dictionary<string, object>();
					while (reader.Read())
						foreach (var i in Enumerable.Range(0, reader.FieldCount))
							table[$"{columnNames[i]} {rowCount++}"] = reader.GetValue(i);
					reader.Close();
					return table;
				}
				else
				{
					reader.Close();
					return "No rows found";
				}
			}
			catch (NullReferenceException)
			{
				return "Database not opened.";
			}
			catch (SQLiteException sqlEX)
			{
				return sqlEX.Message;
			}
			finally
			{
				connection?.Close();
			}
		}

		public string WriteCommand(string query)
		{
			if (string.IsNullOrWhiteSpace(query)) return "query is empty";
			try
			{
				connection.Open();
				new SQLiteCommand(query, connection).ExecuteNonQuery();
				return "Command ran successfully";
			}
			catch (NullReferenceException)
			{
				return "Database not open.";
			}
			catch (SQLiteException sqlEx)
			{
				return sqlEx.Message;
			}
			finally
			{
				connection?.Close();
			}
		}

		#endregion
	}
}
