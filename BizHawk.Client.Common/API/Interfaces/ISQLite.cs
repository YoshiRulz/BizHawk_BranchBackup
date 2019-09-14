namespace BizHawk.Client.Common
{
	public interface ISQLite : IExternalAPI
	{
		string CreateDatabase(string name);
		string OpenDatabase(string name);
		string WriteCommand(string query = "");
		dynamic ReadCommand(string query = "");
	}
}
