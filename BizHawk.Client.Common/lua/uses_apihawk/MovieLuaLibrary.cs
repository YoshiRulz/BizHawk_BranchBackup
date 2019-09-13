using System;

using NLua;

namespace BizHawk.Client.Common
{
	public sealed class MovieLuaLibrary : DelegatingLuaLibrary
	{
		public override string Name => "movie";

		public MovieLuaLibrary(Lua lua) : base(lua) {}

		public MovieLuaLibrary(Lua lua, Action<string> logOutputCallback) : base(lua, logOutputCallback) {}

		#region Delegated to ApiHawk

		[LuaMethodExample("local movie_filename = movie.filename()")]
		[LuaMethod("filename", "Returns the file name including path of the currently loaded movie")] //TODO docs
		public string Filename() => ApiHawkContainer.Movie.Filename();

		[LuaMethodExample("local movie_comments = movie.getcomments()")]
		[LuaMethod("getcomments", "If a movie is active, will return the movie comments as a lua table")] //TODO docs
		public LuaTable GetComments() => LuaTableFromList(ApiHawkContainer.Movie.GetComments());

		[LuaMethodExample("local movie_framerate = movie.getfps()")]
		[LuaMethod("getfps", "If a movie is loaded, gets the frames per second used by the movie to determine the movie length time")] //TODO docs
		public double GetFps() => ApiHawkContainer.Movie.GetFps();

		[LuaMethodExample("local movie_header = movie.getheader()")]
		[LuaMethod("getheader", "If a movie is active, will return the movie header as a lua table")] //TODO docs
		public LuaTable GetHeader() => LuaTableFromDict(ApiHawkContainer.Movie.GetHeader());

		[LuaMethodExample("local nlmovget = movie.getinput(500)")] //TODO docs
		[LuaMethod("getinput", "Returns a table of buttons pressed on a given frame of the loaded movie")] //TODO docs
		public LuaTable GetInput(int frame) => LuaTableFromDict(ApiHawkContainer.Movie.GetInput(frame));

		[LuaMethodExample("local stmovget = movie.getinputasmnemonic(500)")] //TODO docs
		[LuaMethod("getinputasmnemonic", "Returns the input of a given frame of the loaded movie in a raw inputlog string")] //TODO docs
		public string GetInputAsMnemonic(int frame) => ApiHawkContainer.Movie.GetInputAsMnemonic(frame);

		[LuaMethodExample("if (movie.getreadonly()) then console.log(\"Returns true if the movie is in read-only mode, false if in read+write\") end")] //TODO docs
		[LuaMethod("getreadonly", "Returns true if the movie is in read-only mode, false if in read+write")] //TODO docs
		public bool GetReadOnly() => ApiHawkContainer.Movie.GetReadOnly();

		[LuaMethodExample("local ulmovget = movie.getrerecordcount()")] //TODO docs
		[LuaMethod("getrerecordcount", "Gets the rerecord count of the current movie.")] //TODO docs
		public ulong GetRerecordCount() => ApiHawkContainer.Movie.GetRerecordCount();

		[LuaMethodExample("if (movie.getrerecordcounting()) then console.log(\"Returns whether or not the current movie is incrementing rerecords on loadstate\") end")] //TODO docs
		[LuaMethod("getrerecordcounting", "Returns whether or not the current movie is incrementing rerecords on loadstate")] //TODO docs
		public bool GetRerecordCounting() => ApiHawkContainer.Movie.GetRerecordCounting();

		[LuaMethodExample("local nlmovget = movie.getsubtitles()")] //TODO docs
		[LuaMethod("getsubtitles", "If a movie is active, will return the movie subtitles as a lua table")] //TODO docs
		public LuaTable GetSubtitles() => LuaTableFromList(ApiHawkContainer.Movie.GetSubtitles());

		[LuaMethodExample("if (movie.isloaded()) then console.log(\"Returns true if a movie is loaded in memory ( play, record, or finished modes ), false if not ( inactive mode )\") end")] //TODO docs
		[LuaMethod("isloaded", "Returns true if a movie is loaded in memory (play, record, or finished modes), false if not (inactive mode)")] //TODO docs
		public bool IsLoaded() => ApiHawkContainer.Movie.IsLoaded();

		[LuaMethodExample("local movie_frame_count = movie.length()")]
		[LuaMethod("length", "Returns the total number of frames of the loaded movie")] //TODO docs
		public double Length() => ApiHawkContainer.Movie.Length();

		[LuaMethodExample("local movie_mode = movie.mode()")]
		[LuaMethod("mode", "Returns the mode of the current movie. Possible modes: \"PLAY\", \"RECORD\", \"FINISHED\", \"INACTIVE\"")] //TODO docs
		public string Mode() => ApiHawkContainer.Movie.Mode();

		[LuaMethodExample("movie.save(\"C:\\moviename.ext\")")]
		[LuaMethod("save", "Saves the current movie to the disc. If the filename is provided (no extension or path needed), the movie is saved under the specified name to the current movie directory. The filename may contain a subdirectory, it will be created if it doesn't exist. Existing files won't get overwritten.")] //TODO docs
		public void Save(string filename = "") => ApiHawkContainer.Movie.Save(filename);

		[LuaMethodExample("movie.setreadonly(false)")]
		[LuaMethod("setreadonly", "Sets the read-only state to the given value. true for read only, false for read+write")] //TODO docs
		public void SetReadOnly(bool readOnly) => ApiHawkContainer.Movie.SetReadOnly(readOnly);

		[LuaMethodExample("movie.setrerecordcount(20.0)")]
		[LuaMethod("setrerecordcount", "Sets the rerecord count of the current movie.")] //TODO docs
		public void SetRerecordCount(double count)
		{
			// Lua numbers are always double, integer precision holds up to 53 bits, so throw an error if it's bigger than that
			const double PrecisionLimit = 9007199254740992d;
			if (count < PrecisionLimit) ApiHawkContainer.Movie.SetRerecordCount((ulong) count);
			else throw new ArgumentOutOfRangeException(nameof(count), "Rerecord count exceeds Lua integer precision.");
		}

		[LuaMethodExample("movie.setrerecordcounting(true)")]
		[LuaMethod("setrerecordcounting", "Sets whether or not the current movie will increment the rerecord counter on loadstate")] //TODO docs
		public void SetRerecordCounting(bool counting) => ApiHawkContainer.Movie.SetRerecordCounting(counting);

		[LuaMethodExample("if (movie.startsfromsaveram()) then console.log(\"Returns whether or not the movie is a saveram-anchored movie\") end")] //TODO docs
		[LuaMethod("startsfromsaveram", "Returns whether or not the movie is a saveram-anchored movie")] //TODO docs
		public bool StartsFromSaveram() => ApiHawkContainer.Movie.StartsFromSaveram();

		[LuaMethodExample("if (movie.startsfromsavestate()) then console.log(\"Returns whether or not the movie is a savestate-anchored movie\") end")] //TODO docs
		[LuaMethod("startsfromsavestate", "Returns whether or not the movie is a savestate-anchored movie")] //TODO docs
		public bool StartsFromSavestate() => ApiHawkContainer.Movie.StartsFromSavestate();

		[LuaMethodExample("movie.stop()")]
		[LuaMethod("stop", "Stops the current movie")] //TODO docs
		public void Stop() => ApiHawkContainer.Movie.Stop();

		#endregion
	}
}
