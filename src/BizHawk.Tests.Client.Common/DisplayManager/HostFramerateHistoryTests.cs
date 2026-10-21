using System.Globalization;
using System.Linq;

using BizHawk.Client.Common;

namespace BizHawk.Tests.Client.Common.DisplayManager
{
	[TestClass]
	public sealed class HostFramerateHistoryTests
	{
#pragma warning disable MA0136
		[DataRow(33072057529L, """
		33074588684;14;55.3
		33077169269;33;102.0
		33079723059;41;139.5
		33082254924;44;161.3
		33084766767;45;172.6
		33087291462;44;173.7
		""")]
		[DataRow(477369643997L, """
		477372189861;20;78.6
		477374714213;44;139.4
		477377271911;43;157.8
		477379827150;43;164.5
		477382334920;43;168.9
		477384853460;43;170.1
		477387415846;42;166.1
		""")]
		[DataRow(0L, """
		2500000;25;100.0
		5000000;25;100.0
		7500000;25;100.0
		10000000;25;100.0
		12500000;25;100.0
		15000000;25;100.0
		17500000;25;100.0
		20000000;25;100.0
		""")]
		[DataRow(0L, """
		2500000;30;120.0
		5000000;20;94.7
		7500000;30;110.7
		10000000;20;91.3
		12500000;30;109.4
		15000000;20;90.8
		17500000;30;109.3
		20000000;20;90.8
		""")]
#pragma warning restore MA0136
		[TestMethod]
		public void TestCalc(long initTimestamp, string iterations)
		{
			HostFramerateHistory history = new(10_000_000L, initTimestamp);
			foreach (var (i, fields) in iterations.Split('\n').Select(static line => line.Split(';')).Index())
			{
				var numFrames = uint.Parse(fields[1]);
				history.IncrementCounterBy(numFrames);
				var currentTimestamp = long.Parse(fields[0]);
				Assert.IsTrue(history.MaybeRevise(currentTimestamp, out _), $"iter {i}: enough time should have passed to revise");
				var expectedFramerate = double.Parse(fields[2], CultureInfo.InvariantCulture);
				Assert.AreEqual(expectedFramerate, history.Framerate, delta: 0.1, $"iter {i}: new estimate");
			}
		}
	}
}
