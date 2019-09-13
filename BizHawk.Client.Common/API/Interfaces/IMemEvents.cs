using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.ApiHawk
{
	public interface IMemEvents : IExternalAPI
	{
		void AddReadCallback(MemoryCallbackDelegate cb, uint? address, string domain);
		void AddWriteCallback(MemoryCallbackDelegate cb, uint? address, string domain);
		void AddExecCallback(MemoryCallbackDelegate cb, uint? address, string domain);
		void RemoveMemoryCallback(MemoryCallbackDelegate cb);
	}
}
