using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

using BizHawk.Client.ApiHawk.Classes.Events;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

using Extensions = BizHawk.Emulation.Common.IEmulatorExtensions.Extensions;

namespace BizHawk.Client.ApiHawk
{
	/// <summary>Contains the API for controlling EmuHawk.</summary>
	public static class ClientApi
	{
		private static readonly Assembly ClientAssembly = Assembly.GetEntryAssembly();
		private static readonly dynamic /* = EmuHawk.MainForm */ MainFormInstance = GetGlobal("MainForm");
		private static readonly Type GlobalWinType = ClientAssembly.GetType("BizHawk.Client.EmuHawk.GlobalWin");
		private static readonly Array JoypadButtonsArray = Enum.GetValues(typeof(JoypadButton));
		private static readonly JoypadStringToEnumConverter JoypadConverter = new JoypadStringToEnumConverter();
		private static readonly Action<string> LogCallback = Console.WriteLine;

		internal static readonly BizHawkSystemIdToEnumConverter SystemIdConverter = new BizHawkSystemIdToEnumConverter();

		private static List<Joypad> AllJoypads;
		private static IEmulator Emulator;
		private static IVideoProvider VideoProvider;

		private static dynamic GetGlobal(string name) => GlobalWinType.GetField(name).GetValue(null);

		/// <summary>Occurs before a quickload is done (just after user has pressed the shortcut button or has clicked on the item menu)</summary>
		public static event BeforeQuickLoadEventHandler BeforeQuickLoad;

		/// <summary>Occurs before a quicksave is done (just after user has pressed the shortcut button or has clicked on the item menu)</summary>
		public static event BeforeQuickSaveEventHandler BeforeQuickSave;

		/// <summary>Occurs when a ROM is succesfully loaded</summary>
		public static event EventHandler RomLoaded;

		/// <summary>Occurs when a savestate is sucessfully loaded</summary>
		public static event StateLoadedEventHandler StateLoaded;

		/// <summary>Occurs when a savestate is successfully saved</summary>
		public static event StateSavedEventHandler StateSaved;

		public static void UpdateEmulatorAndVP(IEmulator emu = null)
		{
			Emulator = emu;
			VideoProvider = Extensions.AsVideoProviderOrDefault(emu);
		}

		/// <summary>
		/// THE FrameAdvance stuff
		/// </summary>
		public static void DoFrameAdvance()
		{
			MainFormInstance.FrameAdvance();
			MainFormInstance.StepRunLoop_Throttle();
			MainFormInstance.Render();
		}

		/// <summary>
		/// THE FrameAdvance stuff
		/// Auto unpause emulation
		/// </summary>
		public static void DoFrameAdvanceAndUnpause()
		{
			DoFrameAdvance();
			Unpause();
		}

		/// <summary>
		/// Gets a <see cref="Joypad"/> for specified player
		/// </summary>
		/// <param name="player">Player (one based) you want current inputs</param>
		/// <returns>A <see cref="Joypad"/> populated with current inputs</returns>
		/// <exception cref="IndexOutOfRangeException">Raised when you specify a player less than 1 or greater than maximum allows (see SystemInfo class to get this information)</exception>
		public static Joypad GetInput(int player)
		{
			if (player < 1 || RunningSystem.MaxControllers < player) throw new IndexOutOfRangeException($"{RunningSystem.DisplayName} does not support {player} controller(s)");
			GetAllInputs();
			return AllJoypads[player - 1];
		}

		/// <summary>
		/// Load a savestate specified by its name
		/// </summary>
		/// <param name="name">Savetate friendly name</param>
		public static void LoadState(string name) => MainFormInstance.LoadState(Path.Combine(PathManager.GetSaveStatePath(Global.Game), $"{name}.State"), name, false, false);

		/// <summary>
		/// Raised before a quickload is done (just after pressing shortcut button)
		/// </summary>
		/// <param name="sender">Object who raised the event</param>
		/// <param name="quickSaveSlotName">Slot used for quickload</param>
		/// <param name="eventHandled">A boolean that can be set if users want to handle save themselves; if so, BizHawk won't do anything</param>
		public static void OnBeforeQuickLoad(object sender, string quickSaveSlotName, out bool eventHandled)
		{
			eventHandled = false;
			if (BeforeQuickLoad != null)
			{
				var e = new BeforeQuickLoadEventArgs(quickSaveSlotName);
				BeforeQuickLoad(sender, e);
				eventHandled = e.Handled;
			}
		}

		/// <summary>
		/// Raised before a quicksave is done (just after pressing shortcut button)
		/// </summary>
		/// <param name="sender">Object who raised the event</param>
		/// <param name="quickSaveSlotName">Slot used for quicksave</param>
		/// <param name="eventHandled">A boolean that can be set if users want to handle save themselves; if so, BizHawk won't do anything</param>
		public static void OnBeforeQuickSave(object sender, string quickSaveSlotName, out bool eventHandled)
		{
			eventHandled = false;
			if (BeforeQuickSave != null)
			{
				var e = new BeforeQuickSaveEventArgs(quickSaveSlotName);
				BeforeQuickSave(sender, e);
				eventHandled = e.Handled;
			}
		}

		/// <summary>
		/// Raise when a state is loaded
		/// </summary>
		/// <param name="sender">Object who raised the event</param>
		/// <param name="stateName">User friendly name for saved state</param>
		public static void OnStateLoaded(object sender, string stateName) => StateLoaded?.Invoke(sender, new StateLoadedEventArgs(stateName));

		/// <summary>
		/// Raise when a state is saved
		/// </summary>
		/// <param name="sender">Object who raised the event</param>
		/// <param name="stateName">User friendly name for saved state</param>
		public static void OnStateSaved(object sender, string stateName) => StateSaved?.Invoke(sender, new StateSavedEventArgs(stateName));

		/// <summary>
		/// Raise when a rom is successfully Loaded
		/// </summary>
		public static void OnRomLoaded(IEmulator emu)
		{
			UpdateEmulatorAndVP(emu);
			RomLoaded?.Invoke(null, EventArgs.Empty);
			AllJoypads = Enumerable.Range(1, RunningSystem.MaxControllers)
				.Select(i => new Joypad(RunningSystem, i))
				.ToList();
		}

		/// <summary>
		/// Save a state with specified name
		/// </summary>
		/// <param name="name">Savetate friendly name</param>
		public static void SaveState(string name) => MainFormInstance.SaveState(Path.Combine(PathManager.GetSaveStatePath(Global.Game), $"{name}.State"), name, false);

		/// <summary>
		/// Set inputs in specified <see cref="Joypad"/> to specified player
		/// </summary>
		/// <param name="player">Player (one based) whom inputs must be set</param>
		/// <param name="joypad"><see cref="Joypad"/> with inputs</param>
		/// <exception cref="IndexOutOfRangeException">Raised when you specify a player less than 1 or greater than maximum allows (see SystemInfo class to get this information)</exception>
		/// <remarks>Still have some strange behaviour with multiple inputs; so this feature is still in beta</remarks>
		public static void SetInput(int player, Joypad joypad)
		{
			if (player < 1 || RunningSystem.MaxControllers < player) throw new IndexOutOfRangeException($"{RunningSystem.DisplayName} does not support {player} controller(s)");

			if (joypad.Inputs == 0)
			{
				var joypadAdaptor = Global.AutofireStickyXORAdapter;
				joypadAdaptor.ClearStickies();
			}
			else
			{
				var prefix = RunningSystem == SystemInfo.GB ? string.Empty : $"P{player} ";
				foreach (JoypadButton button in JoypadButtonsArray) if (joypad.Inputs.HasFlag(button))
					Global.AutofireStickyXORAdapter.SetSticky(prefix + JoypadConverter.ConvertBack(button, RunningSystem), true);
			}

#if false // Using this breaks joypad usage (even in UI)
			if ((RunningSystem.AvailableButtons & JoypadButton.AnalogStick) == JoypadButton.AnalogStick)
			{
				AutoFireStickyXorAdapter joypadAdaptor = Global.AutofireStickyXORAdapter;
				for (int i = 1; i <= RunningSystem.MaxControllers; i++)
				{
					joypadAdaptor.SetFloat($"P{i} X Axis", allJoypads[i - 1].AnalogX);
					joypadAdaptor.SetFloat($"P{i} Y Axis", allJoypads[i - 1].AnalogY);
				}
			}
#endif
		}

		/// <summary>
		/// Gets all current inputs for each joypad and store
		/// them in <see cref="Joypad"/> class collection
		/// </summary>
		private static void GetAllInputs()
		{
			foreach (var j in AllJoypads) j.ClearInputs();
			var joypadAdaptor = Global.AutofireStickyXORAdapter;
			Parallel.ForEach(
				joypadAdaptor.Definition.BoolButtons.Where(joypadAdaptor.IsPressed),
				button =>
				{
					if (RunningSystem == SystemInfo.GB)
					{
						AllJoypads[0].AddInput(JoypadConverter.Convert(button));
					}
					else
					{
						int player;
						if (int.TryParse(button.Substring(1, 2), out player))
							AllJoypads[player - 1].AddInput(JoypadConverter.Convert(button.Substring(3)));
					}
				}
			);
			if (RunningSystem.AvailableButtons.HasFlag(JoypadButton.AnalogStick))
			{
				for (var i = 1; i <= RunningSystem.MaxControllers; i++)
				{
					AllJoypads[i - 1].AnalogX = joypadAdaptor.GetFloat($"P{i} X Axis");
					AllJoypads[i - 1].AnalogY = joypadAdaptor.GetFloat($"P{i} Y Axis");
				}
			}
		}

		#region only needed because Lua also calls these and there can't be an instance field for the callback (being a static class)

		public static void LuaHelper_SetFrameSkip(int numFrames, Action<string> logCallback)
		{
			if (numFrames >= 0)
			{
				Global.Config.FrameSkip = numFrames;
				MainFormInstance.FrameSkipMessage();
			}
			else
			{
				logCallback("Invalid frame skip value");
			}
		}

		public static void LuaHelper_SetSpeedMode(int percent, Action<string> logCallback)
		{
			if (0 < percent && percent < 6400) MainFormInstance.ClickSpeedItem(percent);
			else logCallback("Invalid speed value");
		}

		public static void LuaHelper_SetZoomFactor(int size, Action<string> logCallback)
		{
			switch (size)
			{
				case 1:
				case 2:
				case 3:
				case 4:
				case 5:
				case 10:
					Global.Config.TargetZoomFactors[Emulator.SystemId] = size;
					MainFormInstance.FrameBufferResized();
					GetGlobal("OSD").AddMessage($"Window size set to {size}x");
					break;
				default:
					logCallback("Invalid window size");
					break;
			}
		}

		#endregion

		#region Public API

		#region Properties

		public static int BorderHeight => TransformPoint(Point.Empty).Y;

		public static int BorderWidth => TransformPoint(Point.Empty).X;

		/// <value>the visible height of the 'emu' display surface (the core's video output, does not include <see cref="GameExtraPadding"/>)</value>
		public static int BufferHeight => VideoProvider.BufferHeight;

		/// <value>the visible width of the 'emu' display surface (the core's video output, does not include <see cref="GameExtraPadding"/>)</value>
		public static int BufferWidth => VideoProvider.BufferWidth;

		/// <value>how much extra padding to put on the 'native' surface to create space for drawing</value>
		public static Padding ClientExtraPadding
		{
			set
			{
				GetGlobal("DisplayManager").ClientExtraPadding = value;
				MainFormInstance.FrameBufferResized();
			}
		}

		/// <value>true iff screenshots will capture OSD graphics</value>
		public static bool DoesScreenshotIncludeOSD
		{
			set { Global.Config.Screenshot_CaptureOSD = value; }
		}

		/// <value>how many frames to skip (set to 0 to disable frame skip)</value>
		public static int FrameSkip
		{
			set { LuaHelper_SetFrameSkip(value, LogCallback); }
		}

		/// <value>how much extra padding to put on the 'emu' surface to create space for drawing</value>
		public static Padding GameExtraPadding
		{
			set
			{
				GetGlobal("DisplayManager").GameExtraPadding = value;
				MainFormInstance.FrameBufferResized();
			}
		}

		/// <value>true iff AV recording is paused</value>
		public static bool IsAVRecordingPaused
		{
			set { MainFormInstance.PauseAvi = value; }
		}

		/// <value>true iff on-screen messages are being displayed</value>
		public static bool IsDisplayingMessages
		{
			set { Global.Config.DisplayMessages = value; }
		}

		/// <value>true iff emulation is paused</value>
		public static bool IsPaused => MainFormInstance.EmulatorPaused;

		/// <value>true iff rewinding is enabled (separate from <c>Config</c> > <c>Rewind &amp; States...</c>)</value>
		public static bool IsRewindEnabled
		{
			set { MainFormInstance.EnableRewind(value); }
		}

		/// <value>true iff the emulator core is seeking</value>
		public static bool IsSeeking => MainFormInstance.IsSeeking;

		/// <value>true iff sound is enabled (equivalent to the <c>Config</c> > <c>Sound...</c> > <c>Sound Master Enable</c> checkbox)</value>
		public static bool IsSoundEnabled
		{
			get { return Global.Config.SoundEnabled; }
			set
			{
				Global.Config.SoundEnabled = value;
				var sound = GetGlobal("Sound");
				sound.StopSound();
				sound.StartSound();
			}
		}

		/// <value>true iff the emulator core is running in "turbo"</value>
		public static bool IsTurboing => MainFormInstance.IsTurboing;

		/// <value>the virtual system currently being emulated</value>
		public static SystemInfo RunningSystem
		{
			get
			{
				//TODO isn't there already a method for this somewhere? one that works properly?
				switch (Global.Emulator.SystemId)
				{
					case "PCE":
						var necSystemType = ((PCEngine) Global.Emulator).Type;
						switch (necSystemType)
						{
							default:
							case NecSystemType.TurboGrafx: return SystemInfo.PCE;
							case NecSystemType.TurboCD: return SystemInfo.PCECD;
							case NecSystemType.SuperGrafx: return SystemInfo.SGX;
						}
					case "SMS":
						var castEmu = (SMS) Global.Emulator;
						if (castEmu.IsSG1000) return SystemInfo.SG;
						else if (castEmu.IsGameGear) return SystemInfo.GG;
						else return SystemInfo.SMS;
					case "GB":
						if (Global.Emulator is Gameboy) return SystemInfo.GB;
						else if (Global.Emulator is GBColors) return SystemInfo.GBC;
						else return SystemInfo.DualGB;
					default:
						return SystemInfo.FindByCoreSystem(SystemIdConverter.Convert(Global.Emulator.SystemId));
				}
			}
		}

		/// <value>the intensity setting of the scanline display filter</value>
		public static int ScanlineFilterIntensity
		{
			get { return Global.Config.TargetScanlineFilterIntensity; }
			set { Global.Config.TargetScanlineFilterIntensity = value; }
		}

		/// <value>desired emulation speed (as a percentage of full speed)</value>
		public static int SpeedMode
		{
			set { LuaHelper_SetSpeedMode(value, LogCallback); }
		}

		/// <value>the size of the area available to draw in</value>
		public static Size WindowInnerSize => MainFormInstance.PresentaionPanel.NativeSize;

		/// <value>EmuHawk's position on the screen</value>
		public static Point WindowPosition => MainFormInstance.DesktopLocation;

		/// <value>the current system's zoom factor (possible values are { 1, 2, 3, 4, 5, 10 })</value>
		public static int ZoomFactor
		{
			get { return Global.Config.TargetZoomFactors[Emulator.SystemId]; }
			set { LuaHelper_SetZoomFactor(value, LogCallback); }
		}

		#endregion

		#region Methods

		/// <summary>Clears all keys from the autohold list.</summary>
		public static void ClearAutohold() => MainFormInstance.ClearHolds();

		/// <summary>Closes EmuHawk and sets the exit code (to be read by the shell or BATCH script that opened it).</summary>
		public static void CloseEmulator(int exitCode = 0) => MainFormInstance.CloseEmulator(exitCode);

		/// <summary>Stops emulation of the loaded ROM, gracefully* ending the running core. (*: see TASVideos/BizHawk#1019)</summary>
		public static void CloseRom() => MainFormInstance.CloseRom();

		public static void OpenRom(string path)
		{
			dynamic args = Activator.CreateInstance(ClientAssembly.GetType("BizHawk.Client.EmuHawk.MainForm.LoadRomArgs"));
			args.OpenAdvanced = OpenAdvancedSerializer.ParseWithLegacy(path);
			MainFormInstance.LoadRom(path, args);
		}

		/// <summary>Pauses emulation.</summary>
		public static void Pause() => MainFormInstance.PauseEmulator();

		/// <summary>Gracefully stops emulation and then tries to load the current ROM again.</summary>
		public static void RebootCore() => MainFormInstance.RebootCore();

		/// <summary>Flushes SaveRAM to disk.</summary>
		public static void SaveRam() => MainFormInstance.FlushSaveRAM();

		public static void Screenshot(string path = null)
		{
			if (path == null) MainFormInstance.TakeScreenshot();
			else MainFormInstance.TakeScreenshot(path);
		}

		/// <summary>Takes a screenshot and writes the image to the clipboard (equivalent to <c>File</c> > <c>Screenshot</c> > <c>Screenshot (raw) -> Clipboard</c>).</summary>
		public static void ScreenshotToClipboard() => MainFormInstance.TakeScreenshotToClipboard();

		/// <summary>Pauses or unpauses emulation.</summary>
		public static void TogglePause() => MainFormInstance.TogglePause();

		/// <summary>Using the current filter program, turn a emulator screen space coordinate to a window coordinate (suitable for lua layer drawing)</summary>
		public static Point TransformPoint(Point p) => GetGlobal("DisplayManager").TransformPoint(p);

		/// <summary>Resumes emulation.</summary>
		public static void Unpause() => MainFormInstance.UnpauseEmulator();

		#endregion

		#endregion
	}
}
