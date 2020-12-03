using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;

using MoonSharp.Interpreter;

namespace BizHawk.Client.Common
{
	/// <remarks>
	/// use <c>_MOONSHARP.banner</c> in the REPL
	/// </remarks>
	public sealed class MoonConsole
	{
		private static readonly Lazy<string> _expectedGlobals = new Lazy<string>(() =>
		{
			var session = new Script();
			session.Globals["moonhawk_loader"] = new Dictionary<string, object>();
			return string.Join(";;", session.Globals.Keys.OrderBy(k => k.ToPrintString()));
		});

		private static readonly uint _prevReleaseVer = VersionInfo.VersionStrToInt(VersionInfo.PreviousReleaseVersion);

		private static readonly uint _runningHawkVer = VersionInfo.VersionStrToInt(VersionInfo.MainVersion);

//		private readonly IDictionary<>;

		public string DoAThing()
		{
			var session = new Script();
			session.Globals["moonhawk_loader"] = new MoonHawkLoader();

			var scriptLoadResult = session.DoString(@"
				local every_frame = function(frame_emu_flags)
					print();
				end

				local on_load = function()
					print(""loaded"");
					local factorial = function(n)
						if (n == 0) then
							return 1;
						else
							return n * fact(n - 1);
						end
					end
					print(""factorial(6) returned ""..factorial(6).."" (expected 720)"");
				end

				if (moonhawk_loader ~= nil) then
					i_understand_this_is_experimental = true; -- required because Yoshi doesn't want MoonHawk's release to be mired by misinformation

					local metadata = moonhawk_loader.script_metadata or {};
					metadata.arrays_start_at = 0; -- allowed values are 0 and 1; def. 1
					metadata.roll_forward_function = nil; -- function called when roll_forward_policy is ""function""; any return value other than true indicates failure, and will be coerced to a string and displayed to the user; the function will have all of the current version's libs and the compat layer, where applicable, in the global scope; def. nil
					metadata.roll_forward_on_dev_build = false; -- iff true, dev builds are assumed to have breaking changes and the chosen roll-forward policy will be applied when loading e.g. a script for 2.5.2 on 2.5.3 dev; def. true
					metadata.roll_forward_policy = ""compat_layer""; -- ""fail""/""compat_layer""/""function""/""naive""; def. ""compat_layer""
					metadata.strict_validation = true; -- iff true, declaring globals is a load error, and the returned script_metadata is scrutinised more than necessary; def. false
					metadata.target_emu = ""snes9x""; -- allowed values are nil, ""emuhawk"" (equivalent to nil), and ""snes9x""; def. nil
					metadata.target_version = ""2.5.3""; -- allowed values (when target_emu is nil/""emuhawk"") are ""2.5.2"" (for triggering compat layer) and ""2.5.3""; def. nil
					if (moonhawk_loader.version_at_least(""2.5.3"")) then
						metadata.exports = { -- dict-like table (.NET: IDict<string, IDict<string, object>>), outer keys are module names, inner keys are category names, inner values are objects of any type, usually functions; def. nil
							[""emuhawk""] = {
								[""dormant_post_frame_handler""] = nil, -- like ""post_frame_handler"", but runs when dormant (loaded and running but not applicable to current rom); def. nil
								--TODO when moving to unloaded, to inactive, to dormant, and to active
								[""load_error_handler""] = nil, -- def. nil
								[""load_error_handler""] = nil, -- def. nil
								[""only_applies_to_roms""] = nil, -- array-like table (.NET: IList<string>), each string is a hash incl. cipher; def. nil
								[""only_applies_to_system""] = ""GEN"", -- def. nil
								[""post_frame_handler""] = every_frame, -- def. nil
								[""post_frame_includes_loadstate""] = true, -- def. true
								[""post_frame_includes_rewind""] = true, -- def. false
								[""post_frame_includes_turbo""] = false, -- def. false
								[""post_load_handler""] = on_load, -- def. nil
								[""pre_unload_handler""] = nil, -- def. nil
							},
						};
					end
					moonhawk_loader.script_metadata = metadata;
					return;
				elseif (bizstring ~= nil and tastudio ~= nil) then -- legacy EmuHawk
					on_load();
					while true do
						every_frame(client.isturbo); --TODO
						emu.frameadvance();
					end
					return;
				end
			");

			var luaFactorialFunction = session.Globals.Get("factorial");
			var d = session.Call(luaFactorialFunction, DynValue.NewNumber(4)).Number;

			// Assert(string.Join(";;", session.Globals.Keys.OrderBy(k => k.ToPrintString())) == _expectedGlobals.Value)

			return string.Join("\n",
				scriptLoadResult.IsNotNil() ? $"script load returned {scriptLoadResult}" : "script load succeeded",
				$"factorial(4) returned {d} (expected 24)"
			);
		}
	}
}
