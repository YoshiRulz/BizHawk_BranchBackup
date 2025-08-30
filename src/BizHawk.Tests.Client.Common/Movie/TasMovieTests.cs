using BizHawk.Client.Common;

namespace BizHawk.Tests.Client.Common.Movie
{
	public abstract class TasMovieTests
	{
		[TestClass]
		public sealed class AllOperationsFlagChanges : TasMovieTests
		{
			[TestInitialize]
			public void BeforeEach()
			{
				InitMovie(numberOfFrames: 10);
				Movie.ClearChanges();
			}

			[TestCleanup]
			public void AfterEach()
				=> Assert.IsTrue(Movie.Changes);
		}

		[TestClass]
		public sealed class AllOperationsProduceSingleInvalidation : TasMovieTests
		{
			private int _invalidations;

			[TestInitialize]
			public void BeforeEach()
			{
				InitMovie(numberOfFrames: 10);
				_invalidations = 0;
				Movie.GreenzoneInvalidated = _ => _invalidations++;
			}

			[TestCleanup]
			public void AfterEach()
				=> Assert.AreEqual(1, _invalidations);
		}

		internal static ITasMovie MakeMovie(int numberOfFrames)
		{
			FakeEmulator emu = new FakeEmulator();
			FakeMovieSession session = new(emu) { Movie = null! };
			TasMovie movie = new(session, "/fake/path");
			session.Movie = movie;

			movie.Attach(emu);
			movie.InsertEmptyFrame(0, numberOfFrames);

			return movie;
		}

		private Bk2Controller controllerA = null!;

		private Bk2Controller controllerEmpty = null!;

		protected string entryA = null!;

		private string entryEmpty = null!;

		protected ITasMovie Movie = null!;

		protected void InitMovie(int numberOfFrames)
		{
			Movie = MakeMovie(numberOfFrames);
			controllerEmpty = new(Movie.Emulator.ControllerDefinition);
			entryEmpty = Bk2LogEntryGenerator.GenerateLogEntry(controllerEmpty);
			controllerA = new(Movie.Emulator.ControllerDefinition);
			controllerA.SetBool("A", true);
			entryA = Bk2LogEntryGenerator.GenerateLogEntry(controllerA);
			// Make sure all operations actually do something.
			Movie.SetBoolState(3, "A", true);
		}

		[TestMethod]
		public void TestRecordFrame()
			=> Movie.RecordFrame(1, controllerA);

		[TestMethod]
		public void TestTruncate()
			=> Movie.Truncate(3);

		[TestMethod]
		public void TestPokeFrame()
			=> Movie.PokeFrame(1, controllerA);

		[TestMethod]
		public void TestSetFrame()
			=> Movie.SetFrame(1, entryA);

		[TestMethod]
		public void TestClearFrame()
			=> Movie.ClearFrame(3);

		[TestMethod]
		public void TestInsertInputSingleString()
			=> Movie.InsertInput(2, entryA);

		[TestMethod]
		public void TestInsertInputStrings()
			=> Movie.InsertInput(2, [ entryA, entryEmpty, entryA, entryEmpty ]);

		[TestMethod]
		public void TestInsertInputControllers()
			=> Movie.InsertInput(2, [ controllerA, controllerEmpty, controllerA, controllerEmpty ]);

		[TestMethod]
		public void TestRemoveFrame()
			=> Movie.RemoveFrame(2);

		[TestMethod]
		public void TestRemoveFramesRange()
			=> Movie.RemoveFrames(2, 4);

		[TestMethod]
		public void TestRemoveFramesList()
			=> Movie.RemoveFrames([ 2, 4, 6 ]);

		[TestMethod]
		public void TestCopyOverInputOverwriting()
			=> Movie.CopyOverInput(2, [ controllerA, controllerEmpty ]);

		[TestMethod]
		public void TestInsertEmptyFrame()
			=> Movie.InsertEmptyFrame(2, 2);

		[TestMethod]
		public void TestToggleBoolStateOverwriting()
			=> Movie.ToggleBoolState(2, "B");

		[TestMethod]
		public void TestSetBoolStateSingleOverwriting()
			=> Movie.SetBoolState(3, "B", true);

		[TestMethod]
		public void TestSetBoolStatesOverwriting()
			=> Movie.SetBoolStates(3, 2, "B", true);

		[TestMethod]
		public void TestSetAxisStateSingleOverwriting()
			=> Movie.SetAxisState(2, "Stick", 10);

		[TestMethod]
		public void TestSetAxisStatesOverwriting()
			=> Movie.SetAxisStates(3, 2, "Stick", 20);

		[TestMethod]
		public void TestCopyOverInputAppending()
			=> Movie.CopyOverInput(9, [ controllerA, controllerEmpty, controllerA ]);

		[TestMethod]
		public void TestToggleBoolStateAppending()
			=> Movie.ToggleBoolState(15, "B");

		[TestMethod]
		public void TestSetBoolStateSingleAppending()
			=> Movie.SetBoolState(15, "B", true);

		[TestMethod]
		public void TestSetBoolStatesAppending()
			=> Movie.SetBoolStates(15, 2, "B", true);

		[TestMethod]
		public void TestSetAxisStateSingleAppending()
			=> Movie.SetAxisState(15, "Stick", 10);

		[TestMethod]
		public void TestSetAxisStatesAppending()
			=> Movie.SetAxisStates(15, 2, "Stick", 20);
	}
}
