using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public sealed class JoypadAPI : IJoypad
	{
		private readonly Action<string> LogCallback;

		public JoypadAPI(Action<string> logCallback)
		{
			LogCallback = logCallback;
		}

		public JoypadAPI() : this(Console.WriteLine) {}

		#region Public API (IJoypad)

		public Dictionary<string, dynamic> Get(int? controller = null)
		{
			var dict = new Dictionary<string, dynamic> { ["clear"] = null, ["getluafunctionslist"] = null, ["output"] = null };
			var adapter = Global.AutofireStickyXORAdapter;
			foreach (var button in adapter.Source.Definition.BoolButtons)
			{
				if (controller == null)
					dict[button] = adapter.IsPressed(button);
				else if (button.Length >= 3 && button.Substring(0, 2) == $"P{controller}")
					dict[button.Substring(3)] = adapter.IsPressed($"P{controller} {button.Substring(3)}");
			}
			foreach (var button in adapter.Source.Definition.FloatControls)
			{
				if (controller == null)
					dict[button] = adapter.GetFloat(button);
				else if (button.Length >= 3 && button.Substring(0, 2) == $"P{controller}")
					dict[button.Substring(3)] = adapter.GetFloat($"P{controller} {button.Substring(3)}");
			}
			return dict;
		}

		//TODO: what about float controls for Lua?
		public Dictionary<string, dynamic> GetImmediate()
		{
			var dict = new Dictionary<string, dynamic>();
			var adapter = Global.ActiveController;
			foreach (var button in adapter.Definition.BoolButtons) dict[button] = adapter.IsPressed(button);
			foreach (var button in adapter.Definition.FloatControls) dict[button] = adapter.GetFloat(button);
			return dict;
		}

		public void Set(string button, bool? state = null, int? controller = null)
		{
			try
			{
				var toPress = controller == null ? button : $"P{controller} {button}";
				if (state.HasValue) Global.LuaAndAdaptor.SetButton(toPress, state.Value);
				else Global.LuaAndAdaptor.UnSet(toPress);
				Global.ActiveController.Overrides(Global.LuaAndAdaptor);
			}
			catch
			{
				// ignored
			}
		}

		public void Set(Dictionary<string, bool> buttons, int? controller = null)
		{
			try
			{
				foreach (var kvp in buttons) Set(kvp.Key, kvp.Value, controller);
			}
			catch
			{
				// ignored
			}
		}

		public void SetAnalog(string control, float? value = null, object controller = null)
		{
			try
			{
				Global.StickyXORAdapter.SetFloat(controller == null ? control : $"P{controller} {control}", value);
			}
			catch
			{
				// ignored
			}
		}

		public void SetAnalog(Dictionary<string, float> controls, object controller = null)
		{
			try
			{
				foreach (var control in controls) SetAnalog(control.Key, control.Value, controller);
			}
			catch
			{
				// ignored
			}
		}

		public void SetFromMnemonicStr(string inputLogEntry)
		{
			try
			{
				var controller = Global.MovieSession.MovieControllerInstance();
				controller.SetControllersAsMnemonic(inputLogEntry);
				foreach (var button in controller.Definition.BoolButtons)
					Global.LuaAndAdaptor.SetButton(button, controller.IsPressed(button));
				foreach (var floatButton in controller.Definition.FloatControls)
					Global.LuaAndAdaptor.SetFloat(floatButton, controller.GetFloat(floatButton));
			}
			catch (Exception)
			{
				LogCallback($"invalid mnemonic string: {inputLogEntry}");
			}
		}

		#endregion
	}
}
