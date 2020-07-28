using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64 : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			throw new NotImplementedException();
			return api.SaveSaveram();
		}

		public void StoreSaveRam(byte[] data)
		{
			return;
			api.LoadSaveram(data);
		}

		public bool SaveRamModified => true;
	}
}
