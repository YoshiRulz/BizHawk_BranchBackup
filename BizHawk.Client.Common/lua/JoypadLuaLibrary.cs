using System;

using NLua;

namespace BizHawk.Client.Common
{
	public sealed class JoypadLuaLibrary : DelegatingLuaLibrary
	{
		public override string Name => "joypad";

		public JoypadLuaLibrary(Lua lua) : base(lua) {}

		public JoypadLuaLibrary(Lua lua, Action<string> logOutputCallback) : base(lua, logOutputCallback) {}

		#region Delegated to ApiHawk

		[LuaMethodExample("local nljoyget = joypad.get(1)")] //TODO docs
		[LuaMethod("get", "returns a lua table of the controller buttons pressed. If supplied, it will only return a table of buttons for the given controller")] //TODO docs
		public LuaTable Get(int? controller = null) => LuaTableFromDict(ApiHawkContainer.Joypad.Get(controller));

		[LuaMethodExample("joypad.setanalog({ [\"Tilt X\"] = true, [\"Tilt Y\"] = false })")]
		[LuaMethod("setanalog", "sets the given analog controls to their provided values for the current frame. Note that unlike set() there is only the logic of overriding with the given value.")] //TODO docs
		public void SetAnalog(LuaTable controls, object controller = null) => ApiHawkContainer.Joypad.SetAnalog(DictFromLuaTable<string, float>(controls), controller);

		[LuaMethodExample("joypad.setfrommnemonicstr(\"|    0,    0,    0,  100,...R..B....|\")")]
		[LuaMethod("setfrommnemonicstr", "sets the given buttons to their provided values for the current frame, string will be interpretted the same way an entry from a movie input log would be")] //TODO docs
		public void SetFromMnemonicStr(string inputLogEntry) => ApiHawkContainer.Joypad.SetFromMnemonicStr(inputLogEntry);

		#endregion

		#region Unable to delegate

		//TODO: what about float controls?
		[LuaMethodExample("local nljoyget = joypad.getimmediate()")] //TODO docs
		[LuaMethod("getimmediate", "returns a lua table of any controller buttons currently pressed by the user")] //TODO docs
		public LuaTable GetImmediate()
		{
			var buttons = Lua.NewTable();
			foreach (var button in Global.ActiveController.Definition.BoolButtons)
				buttons[button] = Global.ActiveController.IsPressed(button);
			return buttons;
		}

		[LuaMethodExample("joypad.set({ [\"Left\"] = true, [\"A\"] = true, [\"B\"] = true })")]
		[LuaMethod("set", "sets the given buttons to their provided values for the current frame")] //TODO docs
		public void Set(LuaTable buttons, int? controller = null)
		{
			try
			{
				foreach (var button in buttons.Keys)
				{
					var toPress = controller == null ? button.ToString() : $"P{controller} {button}";
					var theValueStr = buttons[button].ToString();
					if (string.IsNullOrWhiteSpace(theValueStr))
					{
						// Unset
						Global.LuaAndAdaptor.UnSet(toPress);
						Global.ActiveController.Overrides(Global.LuaAndAdaptor); //TODO needs to be called every loop?
						continue;
					}

					bool parsed;
					if (bool.TryParse(theValueStr.ToLowerInvariant(), out parsed))
					{
						// Force
						Global.LuaAndAdaptor.SetButton(toPress, parsed);
						Global.ActiveController.Overrides(Global.LuaAndAdaptor);
						continue;
					}

					// Inverse
					Global.LuaAndAdaptor.SetInverse(toPress);
					Global.ActiveController.Overrides(Global.LuaAndAdaptor);
				}
			}
			catch
			{
				// ignored
			}
		}

		#endregion
	}
}
