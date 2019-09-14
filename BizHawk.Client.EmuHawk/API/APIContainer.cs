using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using BizHawk.Client.ApiHawk;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class APIContainer : IAPIContainer
	{
		public IComm Comm => (IComm)Libraries[typeof(CommAPI)];
		public IEmu Emu => (IEmu)Libraries[typeof(EmuAPI)];
		public IGameInfo GameInfo => (IGameInfo)Libraries[typeof(GameInfoAPI)];
		public IGUI Gui => (IGUI)Libraries[typeof(GUIAPI)];
		public IInput Input => (IInput)Libraries[typeof(InputAPI)];
		public IJoypad Joypad => (IJoypad)Libraries[typeof(JoypadAPI)];
		public IMem Mem => (IMem)Libraries[typeof(MemAPI)];
		public IMemEvents MemEvents => (IMemEvents)Libraries[typeof(MemEventsAPI)];
		public IMemorySaveState MemorySaveState => (IMemorySaveState)Libraries[typeof(MemorySaveStateAPI)];
		public IInputMovie Movie => (IInputMovie)Libraries[typeof(InputMovieAPI)];
		public ISaveState SaveState => (ISaveState)Libraries[typeof(SaveStateAPI)];
		public ISQLite Sql => (ISQLite)Libraries[typeof(SQLiteAPI)];
		public ITool Tool => (ITool)Libraries[typeof(ToolAPI)];
		public IUserData UserData => (IUserData)Libraries[typeof(UserDataAPI)];
		public Dictionary<Type, IExternalAPI> Libraries { get; set; }
		public APIContainer(Dictionary<Type, IExternalAPI> libs)
		{
			Libraries = libs;
		}
	}
}
