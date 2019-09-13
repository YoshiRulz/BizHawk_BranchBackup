namespace BizHawk.Client.ApiHawk
{
	interface IPlugin
	{
		void PreFrameCallback();
		void PostFrameCallback();
		void SaveStateCallback(string name);
		void LoadStateCallback(string name);
		void InputPollCallback();
		void Init(IAPIContainer api);
	}
}
