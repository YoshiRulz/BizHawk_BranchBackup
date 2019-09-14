using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IAPIContainer
	{
		Dictionary<Type, IExternalAPI> Libraries { get; set; }
	}
}
