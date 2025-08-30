using BizHawk.Client.Common;

namespace BizHawk.Tests.Client.Common.Movie
{
	[TestClass]
	public class MovieUndoTests
	{
		// Our test looks for the first actually different frame and compares with the value returned by Undo.
		// Some operations (e.g. RemoveFrames) don't check for which frames were actually edited. (Should they? Would that give bad performance?)
		// So we should ensure the first frame we touch is actually changed.
		internal static void ValidateActionCanUndoAndRedo(
			ITasMovie movie,
			Action action,
			int expectedUndoItems = 1,
			bool skipUndoIndexCheck = false)
		{
			IStringLog originalLog = movie.GetLogEntries().Clone();
			int originalUndoLength = movie.ChangeLog.UndoIndex;
			action();
			DoAsserts(
				movie,
				originalLog,
				skipUndoIndexCheck: skipUndoIndexCheck,
				originalUndoLength: originalUndoLength,
				expectedUndoItems: expectedUndoItems);
		}

		private static void DoAsserts(
			ITasMovie movie,
			IStringLog originalLog,
			bool skipUndoIndexCheck,
			int originalUndoLength,
			int expectedUndoItems)
		{
			IStringLog changedLog = movie.GetLogEntries().Clone();
			int changedUndoLength = movie.ChangeLog.UndoIndex;
			int firstEditedFrame = originalLog.DivergentPoint(changedLog) ?? movie.InputLogLength;

			if (!skipUndoIndexCheck)
				Assert.AreEqual(originalUndoLength + expectedUndoItems, changedUndoLength);

			Action<int>? oldInvalidated = movie.GreenzoneInvalidated;
			int invalidateFrame = int.MaxValue;
			movie.GreenzoneInvalidated = (f) =>
			{
				oldInvalidated?.Invoke(f);
				invalidateFrame = Math.Min(invalidateFrame, f);
			};

			// undo
			for (int i = 0; i < expectedUndoItems; i++)
				movie.ChangeLog.Undo();
			Assert.AreEqual(firstEditedFrame, invalidateFrame);
			Assert.IsNull(originalLog.DivergentPoint(movie.GetLogEntries()));

			// redo
			invalidateFrame = int.MaxValue;
			for (int i = 0; i < expectedUndoItems; i++)
				movie.ChangeLog.Redo();
			Assert.AreEqual(firstEditedFrame, invalidateFrame);
			Assert.IsNull(changedLog.DivergentPoint(movie.GetLogEntries()));

			movie.GreenzoneInvalidated = oldInvalidated;
		}

		private int _expectedUndoItems;

		private ITasMovie _movie = null!;

		private IStringLog _originalLog = null!;

		private int _originalUndoLength;

		private ITasMovie Movie
		{
			get => _movie;
			set
			{
				_movie = value;
				_originalLog = value.GetLogEntries().Clone();
				_originalUndoLength = value.ChangeLog.UndoIndex;
			}
		}

		[TestInitialize]
		public void BeforeEach()
			=> _expectedUndoItems = 1;

		[TestCleanup]
		public void AfterEach()
			=> DoAsserts(
				_movie,
				_originalLog,
				skipUndoIndexCheck: false,
				originalUndoLength: _originalUndoLength,
				expectedUndoItems: _expectedUndoItems);

		[TestMethod]
		public void SetBool()
		{
			Movie = TasMovieTests.MakeMovie(5);
			Movie.SetBoolState(2, "A", true);
		}

		[TestMethod]
		public void ExtendsMovieBoolStateSingle()
		{
			Movie = TasMovieTests.MakeMovie(5);
			Movie.SetBoolState(8, "A", true);
		}

		[TestMethod]
		public void ExtendsMovieBoolStates()
		{
			Movie = TasMovieTests.MakeMovie(5);
			Movie.SetBoolStates(8, 2, "A", true);
		}

		[TestMethod]
		public void ExtendsMovieAxisStateSingle()
		{
			Movie = TasMovieTests.MakeMovie(5);
			Movie.SetAxisState(8, "Stick", 10);
		}

		[TestMethod]
		public void ExtendsMovieAxisStates()
		{
			Movie = TasMovieTests.MakeMovie(5);
			Movie.SetAxisStates(8, 2, "Stick", 10);
		}

		[TestMethod]
		public void SetAxis()
		{
			Movie = TasMovieTests.MakeMovie(5);
			Movie.SetAxisState(2, "Stick", 20);
		}

#pragma warning disable CA2245 // assigning property to itself
		[TestMethod]
		public void InsertFrame()
		{
			Movie = TasMovieTests.MakeMovie(5);
			Movie.SetBoolState(2, "A", true);
			Movie.SetBoolState(3, "B", true);
			Movie = Movie; // reset baseline
			Movie.InsertEmptyFrame(3);
		}

		[TestMethod]
		public void DeleteFrame()
		{
			Movie = TasMovieTests.MakeMovie(5);
			Movie.SetBoolState(2, "A", true);
			Movie.SetBoolState(4, "B", true);
			Movie = Movie; // reset baseline
			Movie.RemoveFrame(3);
		}

		[TestMethod]
		public void DeleteFramesRange()
		{
			Movie = TasMovieTests.MakeMovie(10);
			Movie.SetBoolState(2, "A", true);
			Movie.SetBoolState(4, "B", true);
			Movie = Movie; // reset baseline
			Movie.RemoveFrames(1, 4);
		}

		[TestMethod]
		public void DeleteFramesList()
		{
			Movie = TasMovieTests.MakeMovie(10);
			Movie.SetBoolState(2, "A", true);
			Movie.SetBoolState(4, "B", true);
			Movie = Movie; // reset baseline
			Movie.RemoveFrames([ 1, 2, 3, 5 ]);
		}

		[TestMethod]
		public void CloneFrame()
		{
			Movie = TasMovieTests.MakeMovie(5);
			Movie.SetBoolState(2, "A", true);
			Movie.SetBoolState(3, "B", true);
			Movie = Movie; // reset baseline
			Movie.InsertInput(2, Movie.GetInputLogEntry(3));
		}
#pragma warning restore CA2245

		[TestMethod]
		public void MultipleEdits()
		{
			_expectedUndoItems = 2;
			Movie = TasMovieTests.MakeMovie(5);
			Movie.SetBoolState(2, "A", true);
			Movie.SetBoolState(3, "B", true);
		}

		[TestMethod]
		public void BatchedEdit()
		{
			Movie = TasMovieTests.MakeMovie(5);
			Movie.ChangeLog.BeginNewBatch();
			Movie.SetBoolState(2, "A", true);
			Movie.SetBoolState(3, "B", true);
			Movie.ChangeLog.EndBatch();
		}

		[DataRow(0)]
		[DataRow(2)]
		[DataRow(5)]
		[TestMethod]
		public void RecordFrameAtEnd(int frame)
		{
			Movie = TasMovieTests.MakeMovie(5);
			Bk2Controller controller = new Bk2Controller(Movie.Emulator.ControllerDefinition);
			controller.SetBool("A", true);
			Movie.RecordFrame(frame, controller);
		}
	}

	[TestClass]
	public sealed class AllOperationsRespectBatching : TasMovieTests
	{
		private int _beginIndex;

		[TestInitialize]
		public void BeforeEach()
		{
			InitMovie(numberOfFrames: 10);
			// Some actions can move markers.
			Movie.Markers.Add(9, "");
			Movie.BindMarkersToInput = true;
			_beginIndex = Movie.ChangeLog.UndoIndex;
			Movie.ChangeLog.BeginNewBatch();
		}

		[TestCleanup]
		public void AfterEach()
		{
			Movie.SetFrame(0, entryA);
			Movie.ChangeLog.EndBatch();
			Assert.AreEqual(1, Movie.ChangeLog.UndoIndex - _beginIndex);
		}
	}

	[TestClass]
	public sealed class AllOperationsGiveOneUndo : TasMovieTests
	{
		private int _beginIndex;

		[TestInitialize]
		public void BeforeEach()
		{
			InitMovie(numberOfFrames: 10);
			// Some actions can move markers.
			Movie.Markers.Add(9, "");
			Movie.BindMarkersToInput = true;
			_beginIndex = Movie.ChangeLog.UndoIndex;
		}

		[TestCleanup]
		public void AfterEach()
			=> Assert.AreEqual(1, Movie.ChangeLog.UndoIndex - _beginIndex);
	}

	[TestClass]
	public sealed class ExtraMovieUndoTests
	{
		[TestMethod]
		public void MarkersGetMoved()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(5);
			movie.BindMarkersToInput = true;
			movie.Markers.Add(3, "a");
			movie.InsertEmptyFrame(2);

			movie.ChangeLog.Undo();
			Assert.AreEqual(3, movie.Markers[0].Frame);

			movie.ChangeLog.Redo();
			Assert.AreEqual(4, movie.Markers[0].Frame);
		}

		[TestMethod]
		public void MarkersGetUndeleted()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(5);
			movie.BindMarkersToInput = true;
			movie.Markers.Add(3, "a");
			movie.RemoveFrame(3);

			movie.ChangeLog.Undo();
			Assert.AreEqual(1, movie.Markers.Count);
			Assert.AreEqual(3, movie.Markers[0].Frame);
			Assert.AreEqual("a", movie.Markers[0].Message);

			movie.ChangeLog.Redo();
			Assert.AreEqual(0, movie.Markers.Count);
		}

		[TestMethod]
		public void MarkersUnaffectedByMovieExtension()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(5);
			movie.BindMarkersToInput = true;
			movie.Markers.Add(5, "a");
			movie.Markers.Add(8, "b");
			movie.SetBoolState(10, "A", true);

			movie.ChangeLog.Undo();
			Assert.AreEqual(2, movie.Markers.Count);
			Assert.AreEqual(5, movie.Markers[0].Frame);
			Assert.AreEqual(8, movie.Markers[1].Frame);

			movie.ChangeLog.Redo();
			Assert.AreEqual(2, movie.Markers.Count);
			Assert.AreEqual(5, movie.Markers[0].Frame);
			Assert.AreEqual(8, movie.Markers[1].Frame);
		}

		[TestMethod]
		public void WorkWithFullUndoHistory()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(5);
			movie.ChangeLog.MaxSteps = 3;

			movie.SetBoolState(0, "A", true);
			movie.SetBoolState(1, "A", true);
			movie.SetBoolState(2, "A", true);

			MovieUndoTests.ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.SetBoolState(10, "A", true);
			}, 1, true);
		}

		[TestMethod]
		public void UndoingMidBatchRetainsBatchState()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(8);
			movie.ChangeLog.BeginNewBatch();

			movie.SetBoolState(1, "A", true);
			movie.SetBoolState(2, "A", true);
			movie.ChangeLog.Undo();

			MovieUndoTests.ValidateActionCanUndoAndRedo(movie, () =>
			{
				movie.SetBoolState(3, "A", true);
				movie.SetBoolState(4, "A", true);

				movie.ChangeLog.EndBatch();
			}, 1, true);

			Assert.AreEqual(1, movie.ChangeLog.UndoIndex);
		}

		[TestMethod]
		public void UndoRedoProduceSingleInvalidation()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(5);
			int invalidations = 0;
			movie.GreenzoneInvalidated = (_) => invalidations++;

			movie.ChangeLog.BeginNewBatch();
			movie.SetBoolState(1, "A", true);
			movie.SetBoolState(2, "B", true);
			movie.ChangeLog.EndBatch();

			invalidations = 0;
			movie.ChangeLog.Undo();
			Assert.AreEqual(1, invalidations);

			invalidations = 0;
			movie.ChangeLog.Redo();
			Assert.AreEqual(1, invalidations);
		}

		[TestMethod]
		public void InsertRespectsMarkerBinding()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(5);
			movie.Markers.Add(3, "a");

			movie.BindMarkersToInput = true;
			movie.InsertEmptyFrame(1);
			movie.BindMarkersToInput = false;

			movie.ChangeLog.Undo();
			Assert.AreEqual(3, movie.Markers[0].Frame);
			Assert.IsFalse(movie.BindMarkersToInput);

			movie.ChangeLog.Redo();
			Assert.AreEqual(4, movie.Markers[0].Frame);
			Assert.IsFalse(movie.BindMarkersToInput);
		}

		[TestMethod]
		public void DeleteRespectsMarkerBinding()
		{
			ITasMovie movie = TasMovieTests.MakeMovie(5);
			movie.Markers.Add(3, "a");

			movie.BindMarkersToInput = true;
			movie.RemoveFrame(1);
			movie.BindMarkersToInput = false;

			movie.ChangeLog.Undo();
			Assert.AreEqual(3, movie.Markers[0].Frame);
			Assert.IsFalse(movie.BindMarkersToInput);

			movie.ChangeLog.Redo();
			Assert.AreEqual(2, movie.Markers[0].Frame);
			Assert.IsFalse(movie.BindMarkersToInput);
		}

		[TestMethod]
		public void GeneralRespectsMarkerBinding()
		{
			// This was just a silly bug.
			ITasMovie movie = TasMovieTests.MakeMovie(5);
			movie.Markers.Add(3, "a");

			Bk2Controller controllerA = new Bk2Controller(movie.Emulator.ControllerDefinition);
			controllerA.SetBool("A", true);

			movie.BindMarkersToInput = true;
			movie.PokeFrame(1, controllerA);
			movie.BindMarkersToInput = false;
			movie.InsertEmptyFrame(1);

			movie.ChangeLog.Undo();
			movie.ChangeLog.Undo();
			Assert.AreEqual(3, movie.Markers[0].Frame);
			Assert.IsFalse(movie.BindMarkersToInput);

			movie.ChangeLog.Redo();
			movie.ChangeLog.Redo();
			Assert.AreEqual(3, movie.Markers[0].Frame);
			Assert.IsFalse(movie.BindMarkersToInput);
		}
	}
}
