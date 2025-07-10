using System.IO;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// From https://archive.codeplex.com/?p=zxmak2
	/// </summary>
	public class WavHeader
	{
		// RIFF chunk (12 bytes)
		/// <remarks><c>"RIFF"u8</c></remarks>
		public int chunkID;

		public int fileSize;

		/// <remarks><c>"WAVE"u8</c></remarks>
		public int riffType;

		// Format chunk (24 bytes)

		/// <remarks><c>"fmt "u8</c></remarks>
		public int fmtID;
		public int fmtSize;
		public short fmtCode;
		public short channels;
		public int sampleRate;
		public int fmtAvgBPS;
		public short fmtBlockAlign;
		public short bitDepth;
		public short fmtExtraSize;

		// Data chunk

		/// <remarks><c>"data"u8</c></remarks>
		public int dataID;

		/// <summary>should be file size less 36 bytes</summary>
		public int dataSize;


		public void Deserialize(Stream stream)
		{
			StreamHelper.Read(stream, out chunkID);
			StreamHelper.Read(stream, out fileSize);
			StreamHelper.Read(stream, out riffType);
			if (chunkID != BitConverter.ToInt32(Encoding.ASCII.GetBytes("RIFF"), 0))
			{
				throw new FormatException("Invalid WAV file header");
			}
			if (riffType != BitConverter.ToInt32(Encoding.ASCII.GetBytes("WAVE"), 0))
			{
				throw new FormatException($"Not supported RIFF type: '{Encoding.ASCII.GetString(BitConverter.GetBytes(riffType))}'");
			}
			while (stream.Position < stream.Length)
			{
				StreamHelper.Read(stream, out int chunkId);
				StreamHelper.Read(stream, out int chunkSize);
				string strChunkId = Encoding.ASCII.GetString(
					BitConverter.GetBytes(chunkId));
				if (strChunkId == "fmt ")
				{
					read_fmt(stream, chunkId, chunkSize);
				}
				else if (strChunkId == "data")
				{
					read_data(stream, chunkId, chunkSize);
					break;
				}
				else
				{
					stream.Seek(chunkSize, SeekOrigin.Current);
				}
			}
			if (fmtID != BitConverter.ToInt32(Encoding.ASCII.GetBytes("fmt "), 0))
			{
				throw new FormatException("WAV format chunk not found");
			}
			if (dataID != BitConverter.ToInt32(Encoding.ASCII.GetBytes("data"), 0))
			{
				throw new FormatException("WAV data chunk not found");
			}
		}

		private void read_data(Stream stream, int chunkId, int chunkSize)
		{
			dataID = chunkId;
			dataSize = chunkSize;
		}

		private void read_fmt(Stream stream, int chunkId, int chunkSize)
		{
			fmtID = chunkId;
			fmtSize = chunkSize;
			StreamHelper.Read(stream, out fmtCode);
			StreamHelper.Read(stream, out channels);
			StreamHelper.Read(stream, out sampleRate);
			StreamHelper.Read(stream, out fmtAvgBPS);
			StreamHelper.Read(stream, out fmtBlockAlign);
			StreamHelper.Read(stream, out bitDepth);
			if (fmtSize == 18)
			{
				// Read any extra values
				StreamHelper.Read(stream, out fmtExtraSize);
				stream.Seek(fmtExtraSize, SeekOrigin.Current);
			}
		}
	}
}
