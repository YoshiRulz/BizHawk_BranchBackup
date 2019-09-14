using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public sealed class APISubsetContainer : IAPIContainer
	{
		public Dictionary<Type, IExternalAPI> Libraries { get; set; }

		public IEmu Emu => (IEmu) Libraries[typeof(EmuAPI)];
		public IGameInfo GameInfo => (IGameInfo) Libraries[typeof(GameInfoAPI)];
		public IJoypad Joypad => (IJoypad) Libraries[typeof(JoypadAPI)];
		public IMem Mem => (IMem) Libraries[typeof(MemAPI)];
		public IMemEvents MemEvents => (IMemEvents) Libraries[typeof(MemEventsAPI)];
		public IMemorySaveState MemorySaveState => (IMemorySaveState) Libraries[typeof(MemorySaveStateAPI)];
		public IInputMovie Movie => (IInputMovie) Libraries[typeof(InputMovieAPI)];
		public ISQLite Sql => (ISQLite) Libraries[typeof(SQLiteAPI)];
		public IUserData UserData => (IUserData) Libraries[typeof(UserDataAPI)];

		public APISubsetContainer(Dictionary<Type, IExternalAPI> libs)
		{
			Libraries = libs;
		}
	}
}
