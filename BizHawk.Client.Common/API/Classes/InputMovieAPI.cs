using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Client.Common
{
	public sealed class InputMovieAPI : IInputMovie
	{
		private static IMovie CurrentMovie => Global.MovieSession.Movie;

		private readonly Action<string> LogCallback;

		public InputMovieAPI(Action<string> logCallback)
		{
			LogCallback = logCallback;
		}

		public InputMovieAPI() : this(Console.WriteLine) {}

		#region Public API (IInputMovie)

		public string Filename() => CurrentMovie.Filename;

		public List<string> GetComments() => CurrentMovie.IsActive
			? CurrentMovie.Comments.ShallowCopy()
			: new List<string>(CurrentMovie.Comments.Count); //TODO intended?

		public double GetFps()
		{
			if (!CurrentMovie.IsActive) return 0.0;
			var headers = CurrentMovie.HeaderEntries;
			string palFlag;
			return new PlatformFrameRates()[headers[HeaderKeys.PLATFORM], headers.TryGetValue(HeaderKeys.PAL, out palFlag) && palFlag == "1"];
		}

		public Dictionary<string, string> GetHeader() => CurrentMovie.IsActive
			? CurrentMovie.HeaderEntries.ShallowCopy()
			: new Dictionary<string, string>();

		public Dictionary<string, dynamic> GetInput(int frame)
		{
			if (!CurrentMovie.IsActive)
			{
				LogCallback("No movie loaded");
				return null;
			}
			var adapter = CurrentMovie.GetInputState(frame);
			if (adapter == null)
			{
				LogCallback("Can't get input of the last frame of the movie. Use the previous frame");
				return null;
			}
			var input = new Dictionary<string, dynamic>();
			foreach (var button in adapter.Definition.BoolButtons) input[button] = adapter.IsPressed(button);
			foreach (var button in adapter.Definition.FloatControls) input[button] = adapter.GetFloat(button);
			return input;
		}

		public string GetInputAsMnemonic(int frame)
		{
			if (CurrentMovie.IsActive && frame < CurrentMovie.InputLogLength)
			{
				var lg = Global.MovieSession.LogGeneratorInstance();
				lg.SetSource(CurrentMovie.GetInputState(frame));
				return lg.GenerateLogEntry();
			}
			return string.Empty;
		}

		public bool GetReadOnly() => Global.MovieSession.ReadOnly;

		public ulong GetRerecordCount() => CurrentMovie.Rerecords;

		public bool GetRerecordCounting() => CurrentMovie.IsCountingRerecords;

		public List<string> GetSubtitles() => CurrentMovie.IsActive
			? CurrentMovie.Subtitles.Select(s => s.ToString()).ToList()
			: new List<string>(CurrentMovie.Subtitles.Count); //TODO intended?

		public bool IsLoaded() => CurrentMovie.IsActive;

		public double Length() => CurrentMovie.FrameCount;

		public string Mode() => CurrentMovie.IsFinished ? "FINISHED"
			: CurrentMovie.IsPlaying ? "PLAY"
			: CurrentMovie.IsRecording ? "RECORD"
			: "INACTIVE";

		public void Save(string filename = "")
		{
			if (!CurrentMovie.IsActive) return;
			if (!string.IsNullOrEmpty(filename))
			{
				var filename1 = filename + $".{CurrentMovie.PreferredExtension}";
				if (new FileInfo(filename1).Exists)
				{
					LogCallback($"File {filename1} already exists, will not overwrite");
					return;
				}
				CurrentMovie.Filename = filename1;
			}
			CurrentMovie.Save();
		}

		public void SetReadOnly(bool readOnly) => Global.MovieSession.ReadOnly = readOnly;

		public void SetRerecordCount(ulong count) => CurrentMovie.Rerecords = count;

		public void SetRerecordCounting(bool counting) => CurrentMovie.IsCountingRerecords = counting;

		public bool StartsFromSaveram() => CurrentMovie.IsActive && CurrentMovie.StartsFromSaveRam;

		public bool StartsFromSavestate() => CurrentMovie.IsActive && CurrentMovie.StartsFromSavestate;

		public void Stop() => CurrentMovie.Stop();

		#endregion
	}
}
