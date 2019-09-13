using System;
using System.Collections.Generic;

namespace BizHawk.Client.ApiHawk
{
	public interface IAPIContainer
	{
		Dictionary<Type, IExternalAPI> Libraries { get; set; }
	}
}
