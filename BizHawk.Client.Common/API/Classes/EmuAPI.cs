using System;
using System.Collections.Generic;
using System.ComponentModel;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.WonderSwan;

namespace BizHawk.Client.Common
{
	[Description("A library for interacting with the currently loaded emulator core")]
	public sealed class EmuAPI : IEmu
	{
		private readonly Action<string> LogCallback;

		[RequiredService]
		private IEmulator Emulator { get; set; }

		[OptionalService]
		private IDebuggable DebuggableCore { get; set; }

		[OptionalService]
		private IDisassemblable DisassemblableCore { get; set; }

		[OptionalService]
		private IMemoryDomains MemoryDomains { get; set; }

		[OptionalService]
		private IInputPollable InputPollableCore { get; set; }

		[OptionalService]
		private IRegionable RegionableCore { get; set; }

		[OptionalService]
		private IBoardInfo BoardInfo { get; set; }

		public EmuAPI(Action<string> logCallback)
		{
			LogCallback = logCallback;
		}

		public EmuAPI() : this(Console.WriteLine) {}

		#region Public API (IEmu)

		public Action FrameAdvanceCallback { get; set; }

		public Action YieldCallback { get; set; }

		public object Disassemble(uint pc, string name = "")
		{
			try
			{
				if (DisassemblableCore == null) throw new NotImplementedException();
				int l;
				return new
				{
					disasm = DisassemblableCore.Disassemble(
						string.IsNullOrEmpty(name) ? MemoryDomains.SystemBus : MemoryDomains[name],
						pc,
						out l
					),
					length = l // assigned in call to DisassemblableCore.Disassemble
				};
			}
			catch (NotImplementedException)
			{
				LogCallback($"Error: {Emulator.Attributes().CoreName} does not yet implement {nameof(IDisassemblable.Disassemble)}()");
				return null;
			}
		}

		public void DisplayVsync(bool enabled) => Global.Config.VSync = enabled;

		public void FrameAdvance() => FrameAdvanceCallback();

		public int FrameCount() => Emulator.Frame;

		public string GetBoardName() => BoardInfo?.BoardName ?? string.Empty;

		public string GetDisplayType() => (RegionableCore?.Region)?.ToString() ?? string.Empty;

		public ulong? GetRegister(string name)
		{
			try
			{
				if (DebuggableCore == null) throw new NotImplementedException();
				RegisterValue rv;
				return DebuggableCore.GetCpuFlagsAndRegisters().TryGetValue(name, out rv) ? rv.Value : (ulong?) null;
			}
			catch (NotImplementedException)
			{
				LogCallback($"Error: {Emulator.Attributes().CoreName} does not yet implement {nameof(IDebuggable.GetCpuFlagsAndRegisters)}()");
				return null;
			}
		}

		public Dictionary<string, ulong> GetRegisters()
		{
			try
			{
				if (DebuggableCore == null) throw new NotImplementedException();
				return DebuggableCore.GetCpuFlagsAndRegisters().MapValues(rv => rv.Value);
			}
			catch (NotImplementedException)
			{
				LogCallback($"Error: {Emulator.Attributes().CoreName} does not yet implement {nameof(IDebuggable.GetCpuFlagsAndRegisters)}()");
				return new Dictionary<string, ulong>();
			}
		}

		public object GetSettings()
		{
			if (Emulator is GPGX) return ((GPGX) Emulator).GetSettings();
			if (Emulator is LibsnesCore) return ((LibsnesCore) Emulator).GetSettings();
			if (Emulator is NES) return ((NES) Emulator).GetSettings();
			if (Emulator is PCEngine) return ((PCEngine) Emulator).GetSettings();
			if (Emulator is QuickNES) return ((QuickNES) Emulator).GetSettings();
			if (Emulator is SMS) return ((SMS) Emulator).GetSettings();
			if (Emulator is WonderSwan) return ((WonderSwan) Emulator).GetSettings();
			return false;
		}

		public string GetSystemId() => Global.Game.System;

		public bool IsLagged()
		{
			if (InputPollableCore != null) return InputPollableCore.IsLagFrame;
			LogCallback($"Can not get lag information, {Emulator.Attributes().CoreName} does not implement {nameof(IInputPollable)}");
			return default(bool);
		}

		public int LagCount()
		{
			if (InputPollableCore != null) return InputPollableCore.LagCount;
			LogCallback($"Can not get lag information, {Emulator.Attributes().CoreName} does not implement {nameof(IInputPollable)}");
			return default(int);
		}

		public void LimitFramerate(bool enabled) => Global.Config.ClockThrottle = enabled;

		public void MinimizeFrameskip(bool enabled) => Global.Config.AutoMinimizeSkipping = enabled;

		public bool PutSettings(object settings)
		{
			if (Emulator is GPGX) return ((GPGX) Emulator).PutSettings((GPGX.GPGXSettings) settings);
			if (Emulator is LibsnesCore) return ((LibsnesCore) Emulator).PutSettings((LibsnesCore.SnesSettings) settings);
			if (Emulator is NES) return ((NES) Emulator).PutSettings((NES.NESSettings) settings);
			if (Emulator is PCEngine) return ((PCEngine) Emulator).PutSettings((PCEngine.PCESettings) settings);
			if (Emulator is QuickNES) return ((QuickNES) Emulator).PutSettings((QuickNES.QuickNESSettings) settings);
			if (Emulator is SMS) return ((SMS) Emulator).PutSettings((SMS.SMSSettings) settings);
			if (Emulator is WonderSwan) return ((WonderSwan) Emulator).PutSettings((WonderSwan.Settings) settings);
			return false;
		}

		public void SetIsLagged(bool value = true)
		{
			if (InputPollableCore != null) InputPollableCore.IsLagFrame = value;
			else LogCallback($"Can not set lag information, {Emulator.Attributes().CoreName} does not implement {nameof(IInputPollable)}");
		}

		public void SetLagCount(int count)
		{
			if (InputPollableCore != null) InputPollableCore.LagCount = count;
			else LogCallback($"Can not set lag information, {Emulator.Attributes().CoreName} does not implement {nameof(IInputPollable)}");
		}

		public void SetRegister(string register, int value)
		{
			try
			{
				if (DebuggableCore == null) throw new NotImplementedException();
				DebuggableCore.SetCpuRegister(register, value);
			}
			catch (NotImplementedException)
			{
				LogCallback($"Error: {Emulator.Attributes().CoreName} does not yet implement {nameof(IDebuggable.SetCpuRegister)}()");
			}
		}

		public void SetRenderPlanes(params bool[] param)
		{
			Func<int, bool> getParamN = i => i < param.Length ? param[i] : true;
			if (Emulator is GPGX)
			{
				var gpgx = (GPGX) Emulator;
				var s = gpgx.GetSettings();
				s.DrawBGA = getParamN(0);
				s.DrawBGB = getParamN(1);
				s.DrawBGW = getParamN(2);
				s.DrawObj = getParamN(3);
				gpgx.PutSettings(s);
			}
			else if (Emulator is LibsnesCore)
			{
				var snes = (LibsnesCore) Emulator;
				var s = snes.GetSettings();
				s.ShowBG1_0 = s.ShowBG1_1 = getParamN(0);
				s.ShowBG2_0 = s.ShowBG2_1 = getParamN(1);
				s.ShowBG3_0 = s.ShowBG3_1 = getParamN(2);
				s.ShowBG4_0 = s.ShowBG4_1 = getParamN(3);
				s.ShowOBJ_0 = getParamN(4);
				s.ShowOBJ_1 = getParamN(5);
				s.ShowOBJ_2 = getParamN(6);
				s.ShowOBJ_3 = getParamN(7);
				snes.PutSettings(s);
			}
			else if (Emulator is NES)
			{
				// in the future, we could do something more arbitrary here.
				// but this isn't any worse than the old system
				var nes = (NES) Emulator;
				var s = nes.GetSettings();
				s.DispSprites = getParamN(0);
				s.DispBackground = getParamN(1);
				nes.PutSettings(s);
			}
			else if (Emulator is PCEngine)
			{
				var pce = (PCEngine) Emulator;
				var s = pce.GetSettings();
				s.ShowOBJ1 = getParamN(0);
				s.ShowBG1 = getParamN(1);
				if (param.Length > 2)
				{
					s.ShowOBJ2 = getParamN(2);
					s.ShowBG2 = getParamN(3);
				}
				pce.PutSettings(s);
			}
			else if (Emulator is QuickNES)
			{
				var quicknes = (QuickNES) Emulator;
				var s = quicknes.GetSettings();
				// this core doesn't support disabling BG
				var showsp = getParamN(0);
				if (showsp && s.NumSprites == 0) s.NumSprites = 8;
				else if (!showsp && s.NumSprites > 0) s.NumSprites = 0;
				quicknes.PutSettings(s);
			}
			else if (Emulator is SMS)
			{
				var sms = (SMS) Emulator;
				var s = sms.GetSettings();
				s.DispOBJ = getParamN(0);
				s.DispBG = getParamN(1);
				sms.PutSettings(s);
			}
			else if (Emulator is WonderSwan)
			{
				var ws = (WonderSwan) Emulator;
				var s = ws.GetSettings();
				s.EnableSprites = getParamN(0);
				s.EnableFG = getParamN(1);
				s.EnableBG = getParamN(2);
				ws.PutSettings(s);
			}
		}

		public long TotalExecutedycles()
		{
			try
			{
				if (DebuggableCore == null) throw new NotImplementedException();
				return DebuggableCore.TotalExecutedCycles;
			}
			catch (NotImplementedException)
			{
				LogCallback($"Error: {Emulator.Attributes().CoreName} does not yet implement {nameof(IDebuggable.TotalExecutedCycles)}()");
				return default(long);
			}
		}

		public void Yield() => YieldCallback();

		#endregion
	}
}
