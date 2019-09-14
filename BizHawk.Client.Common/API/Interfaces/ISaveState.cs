namespace BizHawk.Client.Common
{
	public interface ISaveState : IExternalAPI
	{
		void Load(string path);
		void LoadSlot(int slotNum);
		void Save(string path);
		void SaveSlot(int slotNum);
	}
}
