namespace BizHawk.Client.Common
{
	public interface IMemorySaveState : IExternalAPI
	{
		string SaveCoreStateToMemory();
		void LoadCoreStateFromMemory(string identifier);
		void DeleteState(string identifier);
		void ClearInMemoryStates();
	}
}
