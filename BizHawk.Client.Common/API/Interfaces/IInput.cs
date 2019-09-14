using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IInput : IExternalAPI
	{
		Dictionary<string, bool> Get();
		Dictionary<string, dynamic> GetMouse();
	}
}
