using System;
using System.ComponentModel;

using BizHawk.Emulation.Common;

using NLua;

namespace BizHawk.Client.Common
{
	[Description("A library for interacting with the currently loaded emulator core")]
	public sealed class EmulatorLuaLibrary : DelegatingLuaLibrary
	{
		public override string Name => "emu";

		[OptionalService]
		private IBoardInfo BoardInfo { get; set; }

		[OptionalService]
		private IDebuggable DebuggableCore { get; set; }

		[OptionalService]
		private IDisassemblable DisassemblableCore { get; set; }

		[RequiredService]
		private IEmulator Emulator { get; set; }

		[OptionalService]
		private IInputPollable InputPollableCore { get; set; }

		[OptionalService]
		private IMemoryDomains MemoryDomains { get; set; }

		[OptionalService]
		private IRegionable RegionableCore { get; set; }

		public EmulatorLuaLibrary(Lua lua) : base(lua) {}

		public EmulatorLuaLibrary(Lua lua, Action<string> logOutputCallback) : base(lua, logOutputCallback) {}

		#region Delegated to ApiHawk

		[LuaMethodExample("local disasm_tuple = emu.disassemble(0x8000)")]
		[LuaMethod("disassemble", "Returns the disassembly object (disasm string and length int) for the given PC address. Uses System Bus domain if no domain name provided")] //TODO docs
		public object Disassemble(uint pc, string name = "") => ApiHawkContainer.Emu.Disassemble(pc, name);

		[LuaMethodExample("emu.displayvsync(true)")]
		[LuaMethod("displayvsync", "Sets the display vsync property of the emulator")] //TODO docs
		public void DisplayVsync(bool enabled) => ApiHawkContainer.Emu.DisplayVsync(enabled);

		[LuaMethodExample("local frame_count = emu.framecount()")]
		[LuaMethod("framecount", "Returns the current frame count")] //TODO docs
		public int FrameCount() => ApiHawkContainer.Emu.FrameCount();

		[LuaMethodExample("local stemuget = emu.getboardname()")] //TODO docs
		[LuaMethod("getboardname", "returns (if available) the board name of the loaded ROM")] //TODO docs
		public string GetBoardName() => ApiHawkContainer.Emu.GetBoardName();

		[LuaMethodExample("local stemuget = emu.getdisplaytype()")] //TODO docs
		[LuaMethod("getdisplaytype", "returns the display type (PAL vs NTSC) that the emulator is currently running in")] //TODO docs
		public string GetDisplayType() => ApiHawkContainer.Emu.GetDisplayType();

		//TODO: what about 64 bit registers?
		[LuaMethodExample("local first_reg_value = emu.getregister(emu.getregisters()[0])")]
		[LuaMethod("getregister", "returns the value of a cpu register or flag specified by name. For a complete list of possible registers or flags for a given core, use getregisters")] //TODO docs
		public int GetRegister(string name) => (int?) ApiHawkContainer.Emu.GetRegister(name) ?? default(int);

		[LuaMethodExample("local first_reg = emu.getregisters()[0]")]
		[LuaMethod("getregisters", "returns the complete set of available flags and registers for a given core")] //TODO docs
		public LuaTable GetRegisters() => LuaTableFromDict(ApiHawkContainer.Emu.GetRegisters());

		[LuaMethodExample("local stemuget = emu.getsystemid()")] //TODO docs
		[LuaMethod("getsystemid", "Returns the ID string of the current core loaded. Note: No ROM loaded will return the string NULL")] //TODO docs
		public string GetSystemId() => ApiHawkContainer.Emu.GetSystemId();

		[LuaMethodExample("if (emu.islagged()) then console.log(\"Returns whether or not the current frame is a lag frame\") end")] //TODO docs
		[LuaMethod("islagged", "Returns whether or not the current frame is a lag frame")] //TODO docs
		public bool IsLagged() => ApiHawkContainer.Emu.IsLagged();

		[LuaMethodExample("local lag_count = emu.lagcount()")]
		[LuaMethod("lagcount", "Returns the current lag count")] //TODO docs
		public int LagCount() => ApiHawkContainer.Emu.LagCount();

		[LuaMethodExample("emu.limitframerate(true)")]
		[LuaMethod("limitframerate", "sets the limit framerate property of the emulator")] //TODO docs
		public void LimitFramerate(bool enabled) => ApiHawkContainer.Emu.LimitFramerate(enabled);

		[LuaMethodExample("emu.minimizeframeskip(true)")]
		[LuaMethod("minimizeframeskip", "Sets the autominimizeframeskip value of the emulator")] //TODO docs
		public void MinimizeFrameskip(bool enabled) => ApiHawkContainer.Emu.MinimizeFrameskip(enabled);

		[LuaMethodExample("emu.setislagged(true)")]
		[LuaMethod("setislagged", "Sets the lag flag for the current frame. If no value is provided, it will default to true")] //TODO docs
		public void SetIsLagged(bool value = true) => ApiHawkContainer.Emu.SetIsLagged(value);

		[LuaMethodExample("emu.setlagcount(50)")]
		[LuaMethod("setlagcount", "Sets the current lag count")] //TODO docs
		public void SetLagCount(int count) => ApiHawkContainer.Emu.SetLagCount(count);

		[LuaMethodExample("emu.setregister(emu.getregisters()[0], -1000)")]
		[LuaMethod("setregister", "sets the given register name to the given value")] //TODO docs
		public void SetRegister(string register, int value) => ApiHawkContainer.Emu.SetRegister(register, value);

		[LuaMethodExample("emu.setrenderplanes(true, false)")]
		[LuaMethod("setrenderplanes", "Toggles the drawing of sprites and background planes. Set to false or nil to disable a pane, anything else will draw them")] //TODO docs
		public void SetRenderPlanes(params bool[] luaParam) => ApiHawkContainer.Emu.SetRenderPlanes(luaParam);

		[LuaMethodExample("local cycle_count = emu.totalexecutedcycles()")]
		[LuaMethod("totalexecutedcycles", "gets the total number of executed cpu cycles")] //TODO docs
		public long TotalExecutedycles() => ApiHawkContainer.Emu.TotalExecutedycles();

		#endregion

		#region Lua-specific

		[LuaMethod("getluacore", "returns the name of the Lua core currently in use")] //TODO docs
		public string GetLuaBackend() => Lua.WhichLua;

		#endregion

		#region Unable to delegate

		public Action FrameAdvanceCallback { get; set; }

		public Action YieldCallback { get; set; }

		[LuaMethodExample("emu.frameadvance()")]
		[LuaMethod("frameadvance", "Signals to the emulator to resume emulation. Necessary for any lua script while loop or else the emulator will freeze!")] //TODO docs
		public void FrameAdvance() => FrameAdvanceCallback();

		[LuaMethodExample("emu.yield()")]
		[LuaMethod("yield", "allows a script to run while emulation is paused and interact with the gui/main window in realtime ")] //TODO docs
		public void Yield() => YieldCallback();

		#endregion
	}
}
