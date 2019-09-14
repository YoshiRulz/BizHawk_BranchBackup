using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using BizHawk.Client.ApiHawk;
using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;

using NLua;

// ReSharper disable StringLiteralTypo
// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.EmuHawk
{
	[Description("A library for manipulating the EmuHawk client UI")]
	public sealed class EmuHawkLuaLibrary : LuaLibraryBase
	{
		private readonly Dictionary<int, string> _filterMappings = new Dictionary<int, string>
		{
			{ 0, "None" },
			{ 1, "x2SAI" },
			{ 2, "SuperX2SAI" },
			{ 3, "SuperEagle" },
			{ 4, "Scanlines" },
		};

		public override string Name => "client";

		[RequiredService]
		private IEmulator Emulator { get; set; }

		[RequiredService]
		private IVideoProvider VideoProvider { get; set; }

		public EmuHawkLuaLibrary(Lua lua) : base(lua) {}

		public EmuHawkLuaLibrary(Lua lua, Action<string> logOutputCallback) : base(lua, logOutputCallback) {}

		#region Delegated to ApiHawk

		[LuaMethodExample("local border_top = client.borderheight()")]
		[LuaMethod("borderheight", "Gets the current height in pixels of the letter/pillarbox area (top side only) around the emu display surface, not including padding set by `client.SetGameExtraPadding`. This function (the whole lot of them) should be renamed or refactored since the padding areas have got more complex.")] //TODO docs
		public int BorderHeight() => ClientApi.BorderHeight;

		[LuaMethodExample("local border_left = client.borderwidth()")]
		[LuaMethod("borderwidth", "Gets the current width in pixels of the letter/pillarbox area (left side only) around the emu display surface, not including padding set by `client.SetGameExtraPadding`. This function (the whole lot of them) should be renamed or refactored since the padding areas have got more complex.")] //TODO docs
		public int BorderWidth() => ClientApi.BorderWidth;

		[LuaMethodExample("local game_h = client.bufferheight()")]
		[LuaMethod("bufferheight", "Returns the visible height of the 'emu' display surface (the core's video output, does not include padding set by `client.SetGameExtraPadding`).")]
		public int BufferHeight() => ClientApi.BufferHeight;

		[LuaMethodExample("local game_w = client.bufferwidth()")]
		[LuaMethod("bufferwidth", "Returns the visible width of the 'emu' display surface (the core's video output, does not include padding set by `client.SetGameExtraPadding`).")]
		public int BufferWidth() => ClientApi.BufferWidth;

		[LuaMethodExample("client.clearautohold()")]
		[LuaMethod("clearautohold", "Clears all keys from the autohold list.")]
		public void ClearAutohold() => ClientApi.ClearAutohold();

		[LuaMethodExample("client.exit()")]
		[LuaMethod("exit", "Closes EmuHawk.")]
		public void CloseEmulator() => ClientApi.CloseEmulator();

		[LuaMethodExample("client.exitCode(32)")]
		[LuaMethod("exitCode", "Closes EmuHawk and sets the exit code (to be read by the shell or BATCH script that opened it).")]
		public void CloseEmulatorWithCode(int exitCode) => ClientApi.CloseEmulator(exitCode);

		[LuaMethodExample("client.closerom()")]
		[LuaMethod("closerom", "Stops emulation of the loaded ROM, gracefully* ending the running core. (*: see TASVideos/BizHawk#1019)")]
		public void CloseRom() => ClientApi.CloseRom();

		[LuaMethodExample("client.displaymessages(true)")]
		[LuaMethod("displaymessages", "Enables or disables on-screen messages.")]
		public void DisplayMessages(bool value) => ClientApi.IsDisplayingMessages = value;

		[LuaMethodExample("client.enablerewind(true)")]
		[LuaMethod("enablerewind", "Enables or disables rewinding (separate from `Config` > `Rewind & States...`).")]
		public void EnableRewind(bool enabled) => ClientApi.IsRewindEnabled = enabled;

		[LuaMethodExample("client.frameskip(3)")]
		[LuaMethod("frameskip", "Sets how many frames to skip (pass 0 to disable frame skip).")]
		public void FrameSkip(int numFrames) => ClientApi.LuaHelper_SetFrameSkip(numFrames, Log);

		[LuaMethodExample("if (not client.GetSoundOn()) then gui.text(0, 8, \"(M)\", 0xFFFFFFFF, \"bottomleft\") end")]
		[LuaMethod("GetSoundOn", "Returns true iff sound is enabled (equivalent to the checked state of `Config` > `Sound...` > `Sound Master Enable`).")]
		public bool GetSoundOn() => ClientApi.IsSoundEnabled;

		[LuaMethodExample("local intensity = client.gettargetscanlineintensity()")]
		[LuaMethod("gettargetscanlineintensity", "Returns the intensity setting of the scanline display filter.")]
		public int GetTargetScanlineIntensity() => ClientApi.ScanlineFilterIntensity;

		[LuaMethodExample("local zoom = client.getwindowsize()")]
		[LuaMethod("getwindowsize", "Returns the current system's zoom factor (possible values are { 1, 2, 3, 4, 5, 10 }).")]
		public int GetWindowSize() => ClientApi.ZoomFactor;

		[LuaMethodExample("if (not client.ispaused()) then gui.text(0, 8, memory.read_u8(0xABCD), 0xFFFFFFFF, \"bottomleft\") end")]
		[LuaMethod("ispaused", "Returns true iff emulation is paused.")]
		public bool IsPaused() => ClientApi.IsPaused;

		[LuaMethodExample("if (not client.isseeking()) then gui.text(0, 8, memory.read_u8(0xABCD), 0xFFFFFFFF, \"bottomleft\") end")]
		[LuaMethod("isseeking", "Returns true iff the emulator core is seeking.")]
		public bool IsSeeking() => ClientApi.IsSeeking;

		[LuaMethodExample("if (not client.isturbo()) then gui.text(0, 8, memory.read_u8(0xABCD), 0xFFFFFFFF, \"bottomleft\") end")]
		[LuaMethod("isturbo", "Returns true iff the emulator core is running in \"turbo\".")]
		public bool IsTurbo() => ClientApi.IsTurboing;

		[LuaMethodExample("client.openrom(\"C:\\\")")]
		[LuaMethod("openrom", "opens the Open ROM dialog")] //TODO docs
		public void OpenRom(string path) => ClientApi.OpenRom(path);

		[LuaMethodExample("client.pause()")]
		[LuaMethod("pause", "Pauses emulation.")]
		public void Pause() => ClientApi.Pause();

		[LuaMethodExample("client.pause_av()")]
		[LuaMethod("pause_av", "Pauses active AV recording (frames will not be captured until the next call to `client.unpause_av()`).")]
		public void PauseAv() => ClientApi.IsAVRecordingPaused = true;

		[LuaMethodExample("client.reboot_core()")]
		[LuaMethod("reboot_core", "Gracefully stops emulation and then tries to load the current ROM again.")]
		public void RebootCore()
		{
			((LuaConsole)GlobalWin.Tools.Get<LuaConsole>()).LuaImp.IsRebootingCore = true;
			ClientApi.RebootCore();
			((LuaConsole)GlobalWin.Tools.Get<LuaConsole>()).LuaImp.IsRebootingCore = false;
		}

		[LuaMethodExample("client.saveram()")]
		[LuaMethod("saveram", "Flushes SaveRAM to disk.")]
		public void SaveRam() => ClientApi.SaveRam();

		[LuaMethodExample("local window_h = client.screenheight()")]
		[LuaMethod("screenheight", "Returns the available height to draw in.")]
		public int ScreenHeight() => ClientApi.WindowInnerSize.Height;

		[LuaMethodExample("client.screenshot(\"C:\\\")")]
		[LuaMethod("screenshot", "if a parameter is passed it will function as the Screenshot As menu item of EmuHawk, else it will function as the Screenshot menu item")] //TODO docs
		public void Screenshot(string path = null) => ClientApi.Screenshot(path);

		[LuaMethodExample("client.screenshottoclipboard()")]
		[LuaMethod("screenshottoclipboard", "Takes a screenshot and writes the image to the clipboard (equivalent to `File` > `Screenshot` > `Screenshot (raw) -> Clipboard`).")]
		public void ScreenshotToClipboard() => ClientApi.ScreenshotToClipboard();

		[LuaMethodExample("local window_w = client.screenwidth()")]
		[LuaMethod("screenwidth", "Returns the available width to draw in.")]
		public int ScreenWidth() => ClientApi.WindowInnerSize.Width;

		[LuaMethodExample("client.SetClientExtraPadding(5, 10, 15, 20)")]
		[LuaMethod("SetClientExtraPadding", "Sets extra padding on the 'native' surface to create space for drawing. Parameters are clockwise from left.")]
		public void SetClientExtraPadding(int left, int top, int right, int bottom) => ClientApi.ClientExtraPadding = new Padding(left, top, right, bottom);

		[LuaMethodExample("client.SetGameExtraPadding(5, 10, 15, 20)")]
		[LuaMethod("SetGameExtraPadding", "Sets extra padding on the 'emu' surface to create space for drawing. Parameters are clockwise from left.")]
		public void SetGameExtraPadding(int left, int top, int right, int bottom) => ClientApi.GameExtraPadding = new Padding(left, top, right, bottom);

		[LuaMethodExample("client.setscreenshotosd(false)")]
		[LuaMethod("setscreenshotosd", "Enables or disables capturing OSD graphics in screenshots.")]
		public void SetScreenshotOSD(bool value) => ClientApi.DoesScreenshotIncludeOSD = value;

		[LuaMethodExample("client.SetSoundOn(false)")]
		[LuaMethod("SetSoundOn", "Enables or disables sound (equivalent to checking or unchecking `Config` > `Sound...` > `Sound Master Enable`).")]
		public void SetSoundOn(bool enable) => ClientApi.IsSoundEnabled = enable;

		[LuaMethodExample("client.settargetscanlineintensity(192)")]
		[LuaMethod("settargetscanlineintensity", "Sets the intensity setting of the scanline display filter.")]
		public void SetTargetScanlineIntensity(int val) => ClientApi.ScanlineFilterIntensity = val;

		[LuaMethodExample("client.setwindowsize(100)")]
		[LuaMethod("setwindowsize", "Sets the zoom factor for the current system (possible values are { 1, 2, 3, 4, 5, 10 }).")]
		public void SetWindowSize(int size) => ClientApi.LuaHelper_SetZoomFactor(size, Log);

		[LuaMethodExample("client.speedmode(75)")]
		[LuaMethod("speedmode", "Sets desired emulation speed (as a percentage of full speed).")]
		public void SpeedMode(int percent) => ClientApi.LuaHelper_SetSpeedMode(percent, Log);

		[LuaMethodExample("client.togglepause()")]
		[LuaMethod("togglepause", "Pauses or unpauses emulation.")]
		public void TogglePause() => ClientApi.TogglePause();

		[LuaMethodExample("local textbox_x = client.transformPointX(16)")]
		[LuaMethod("transformPointX", "Transforms an x co-ordinate in emulator-space to client-space.")]
		public int TransformPointX(int x) => ClientApi.TransformPoint(new Point(x, 0)).X;

		[LuaMethodExample("local textbox_y = client.transformPointY(32)")]
		[LuaMethod("transformPointY", "Transforms an y co-ordinate in emulator-space to client-space.")]
		public int TransformPointY(int y) => ClientApi.TransformPoint(new Point(0, y)).Y;

		[LuaMethodExample("client.unpause()")]
		[LuaMethod("unpause", "Resumes emulation.")]
		public void Unpause() => ClientApi.Unpause();

		[LuaMethodExample("client.unpause_av()")]
		[LuaMethod("unpause_av", "Resumes a AV recording previously paused by a call to `client.pause_av()`.")]
		public void UnpauseAv() => ClientApi.IsAVRecordingPaused = false;

		[LuaMethodExample("local window_x = client.xpos()")]
		[LuaMethod("xpos", "Returns EmuHawk's horizontal position on the screen.")]
		public int Xpos() => ClientApi.WindowPosition.X;

		[LuaMethodExample("local window_y = client.ypos()")]
		[LuaMethod("ypos", "Returns EmuHawk's vertical position on the screen.")]
		public int Ypos() => ClientApi.WindowPosition.Y;

		#endregion

		#region Lua-specific

		[LuaMethodExample("local inst = client.createinstance(\"objectname\")")]
		[LuaMethod("createinstance", "returns a default instance of the given type of object if it exists (not case sensitive). Note: This will only work on objects which have a parameterless constructor.  If no suitable type is found, or the type does not have a parameterless constructor, then nil is returned")] //TODO docs
		public LuaTable CreateInstance(string name)
		{
			var type = ReflectionUtil.GetTypeByName(name).FirstOrDefault();
			return type == null ? null : LuaHelper.ToLuaTable(Lua, Activator.CreateInstance(type));
		}

		[LuaMethodExample("client.exactsleep(245)")]
		[LuaMethod("exactsleep", "Makes the Lua thread sleep for n milliseconds, as precisely as possible.")]
		public void ExactSleep(int targetMilliseconds)
		{
			var stopwatch = Stopwatch.StartNew();

			// bisect the duration to get close to the target
			for (var nextTarget = targetMilliseconds / 2; nextTarget > 50; nextTarget = (targetMilliseconds - (int) stopwatch.ElapsedMilliseconds) / 2)
				Thread.Sleep(nextTarget);

			// busy loop until the actual target
			while (stopwatch.ElapsedMilliseconds < targetMilliseconds) {}

			stopwatch.Stop();
		}

		[LuaMethodExample("client.sleep(300)")]
		[LuaMethod("sleep", "Makes the Lua thread sleep for about n milliseconds.")]
		public void Sleep(int millis) => Thread.Sleep(millis);

		#endregion

		[LuaMethodExample("local tools = client.getavailabletools()")]
		[LuaMethod("getavailabletools", "Returns a list of the tools currently open")] //TODO docs
		public LuaTable GetAvailableTools()
		{
			var toolNames = GlobalWin.Tools.AvailableTools.Select(tool => tool.Name.ToLowerInvariant()).ToList();
			var t = Lua.NewTable();
			for (var i = 0; i < toolNames.Count; i++) t[i] = toolNames[i];
			return t;
		}

		[LuaMethodExample("local speed = client.getconfig().SpeedPercent")]
		[LuaMethod("getconfig", "gets the current config settings object")] //TODO docs
		public object GetConfig() => Global.Config;

		[LuaMethodExample("local tool = client.gettool(\"Tool name\")")]
		[LuaMethod("gettool", "Returns an object that represents a tool of the given name (not case sensitive). If the tool is not open, it will be loaded if available. Use gettools to get a list of names")] //TODO docs
		public LuaTable GetTool(string name)
		{
			var toolType = ReflectionUtil.GetTypeByName(name).FirstOrDefault(type => !type.IsInterface && typeof(IToolForm).IsAssignableFrom(type));
			if (toolType != null) GlobalWin.Tools.Load(toolType);

			var selectedTool = GlobalWin.Tools.AvailableTools.FirstOrDefault(tool => string.Equals(tool.Name, name, StringComparison.InvariantCultureIgnoreCase));
			return selectedTool == null ? null : LuaHelper.ToLuaTable(Lua, selectedTool);
		}

		[LuaMethodExample("local version = client.getversion()")]
		[LuaMethod("getversion", "Returns the current stable BizHawk version")] //TODO docs
		public string GetVersion() => VersionInfo.Mainversion;

		[LuaMethodExample("client.opencheats()")]
		[LuaMethod("opencheats", "opens the Cheats dialog")] //TODO docs
		public void OpenCheats() => GlobalWin.Tools.Load<Cheats>();

		[LuaMethodExample("client.openhexeditor()")]
		[LuaMethod("openhexeditor", "opens the Hex Editor dialog")] //TODO docs
		public void OpenHexEditor() => GlobalWin.Tools.Load<HexEditor>();

		[LuaMethodExample("client.openramsearch()")]
		[LuaMethod("openramsearch", "opens the RAM Search dialog")] //TODO docs
		public void OpenRamSearch() => GlobalWin.Tools.Load<RamSearch>();

		[LuaMethodExample("client.openramwatch()")]
		[LuaMethod("openramwatch", "opens the RAM Watch dialog")] //TODO docs
		public void OpenRamWatch() => GlobalWin.Tools.LoadRamWatch(loadDialog: true);

		[LuaMethodExample("client.opentasstudio()")]
		[LuaMethod("opentasstudio", "opens the TAStudio dialog")] //TODO docs
		public void OpenTasStudio() => GlobalWin.Tools.Load<TAStudio>();

		[LuaMethodExample("client.opentoolbox()")]
		[LuaMethod("opentoolbox", "opens the Toolbox Dialog")] //TODO docs
		public void OpenToolBox() => GlobalWin.Tools.Load<ToolBox>();

		[LuaMethodExample("client.opentracelogger()")]
		[LuaMethod("opentracelogger", "opens the tracelogger if it is available for the given core")] //TODO docs
		public void OpenTraceLogger() => GlobalWin.Tools.Load<TraceLogger>();
	}
}
