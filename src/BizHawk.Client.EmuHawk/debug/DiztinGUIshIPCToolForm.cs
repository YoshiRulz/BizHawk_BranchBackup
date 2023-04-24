#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;

using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Common;
using BizHawk.WinForms.Controls;

using SharpCompress.Compressors.Deflate;

namespace BizHawk.Client.EmuHawk.ForDebugging
{
	internal sealed class DiztinGUIshIPCToolForm : ToolFormBase
	{
		public enum ChunkType : byte
		{
			Abridged = 0xEE,
			Unabridged = 0xEF,
		}

		/// <remarks>https://github.com/DizTools/bsnes-plus/blob/e30dfc784f3c40c0db0a09124db4ec83189c575c/bsnes/snes/cpu/core/registers.hpp#L5</remarks>
		[Flags]
		public enum CPUFlag : byte
		{
			FlagN = 0x80,
			FlagV = 0x40,
			FlagM = 0x20,
			FlagX = 0x10,
			FlagD = 0x08,
			FlagI = 0x04,
			FlagZ = 0x02,
			FlagC = 0x01,
		}

		public readonly struct AbridgedItem
		{
			public const int LEN_FIXED = 8;

			public readonly uint Addr;

			public readonly byte DataBank;

			public readonly ushort DirectPage;

			public readonly CPUFlag Flags;

			public readonly byte OpcodeLength;

			public AbridgedItem(in ReadOnlySpan<byte> contents)
			{
				if (contents.Length is not LEN_FIXED) throw new ArgumentException(paramName: nameof(contents), message: "AAAAAAAAAAAAAAA");
				Addr = unchecked((uint) ((contents[2] << 16) | (contents[1] << 8) | contents[0]));
				OpcodeLength = contents[3];
				DirectPage = unchecked((ushort) ((contents[5] << 8) | contents[4]));
				DataBank = contents[6];
				Flags = (CPUFlag) contents[7];
			}

			public override readonly string ToString()
			{
				static string FlagsMnemonic(in CPUFlag p)
					=> new(new[]
					{
						(p & CPUFlag.FlagN) is not 0 ? 'N' : '.',
						(p & CPUFlag.FlagV) is not 0 ? 'V' : '.',
						(p & CPUFlag.FlagM) is not 0 ? 'M' : '.',
						(p & CPUFlag.FlagX) is not 0 ? 'X' : '.',
						(p & CPUFlag.FlagD) is not 0 ? 'D' : '.',
						(p & CPUFlag.FlagI) is not 0 ? 'I' : '.',
						(p & CPUFlag.FlagZ) is not 0 ? 'Z' : '.',
						(p & CPUFlag.FlagC) is not 0 ? 'C' : '.',
					});
				return $"at 0x{Addr:X6}: {OpcodeLength}-byte opcode; A:???? X:???? Y:???? S:???? D:{DirectPage:X4} DB:{DataBank:X2} {FlagsMnemonic(in Flags)}";
			}

			public readonly void WriteTo(Stream stream)
			{
				stream.WriteByte((byte) ChunkType.Abridged);
				stream.WriteByte(LEN_FIXED);
				stream.WriteByte(unchecked((byte) Addr));
				stream.WriteByte(unchecked((byte) (Addr >> 8)));
				stream.WriteByte(unchecked((byte) (Addr >> 16)));
				stream.WriteByte(OpcodeLength);
				stream.WriteByte(unchecked((byte) DirectPage));
				stream.WriteByte(unchecked((byte) (DirectPage >> 8)));
				stream.WriteByte(DataBank);
				stream.WriteByte((byte) Flags);
			}
		}

		public readonly struct ItemWithHeader
		{
			public const int HEADER_LEN = 9;

			private const byte MAGIC_BYTE = (byte) 'Z';

			public static readonly IReadOnlyList<ItemWithHeader> DummyList = ItemWithHeader.Parse(new byte[] { 0xEE, 8, 0x01, 0x23, 0x45, 2, 0x89, 0xAB, 0xCD, 0xEF });

			public static byte[] CompressItemsToBytes(IReadOnlyList<ItemWithHeader> list)
			{
				using MemoryStream ms = new();
				foreach (var item in list) item.WriteTo(ms);
				using DeflateStream deflater = new(ms, SharpCompress.Compressors.CompressionMode.Compress); // other side uses SharpZipLib so the exact compression method shouldn't matter
				var compressed = deflater.ReadAllBytes();
				var final = new byte[HEADER_LEN + compressed.Length];
				final[0] = MAGIC_BYTE;
				var temp = ms.Length;
				MemoryMarshal.Write(final.AsSpan(start: 1, length: sizeof(int)), ref temp);
				temp = compressed.Length;
				MemoryMarshal.Write(final.AsSpan(start: 5, length: sizeof(int)), ref temp);
				Array.Copy(sourceArray: compressed, sourceIndex: 0, destinationArray: final, destinationIndex: HEADER_LEN, length: compressed.Length);
				return final;
			}

			public static IReadOnlyList<ItemWithHeader> DecompressAndParse(ReadOnlySpan<byte> compressedDataWithHeader)
			{
				if (compressedDataWithHeader.Length < HEADER_LEN) throw new ArgumentException(paramName: nameof(compressedDataWithHeader), message: "buffer way too small");
				if (compressedDataWithHeader[0] is not MAGIC_BYTE) throw new ArgumentException(paramName: nameof(compressedDataWithHeader), message: "missing magic byte");
				var compressedLength = MemoryMarshal.Read<int>(compressedDataWithHeader.Slice(start: 5, length: sizeof(int))); // inb4 this is the wrong endianness
				if (compressedDataWithHeader.Length != HEADER_LEN + compressedLength) throw new ArgumentException(paramName: nameof(compressedDataWithHeader), message: "body length does not match");
				var decompressedLength = MemoryMarshal.Read<int>(compressedDataWithHeader.Slice(start: 1, length: sizeof(int)));
				var decompressedData = new DeflateStream(new MemoryStream(compressedDataWithHeader.Slice(start: HEADER_LEN).ToArray()), SharpCompress.Compressors.CompressionMode.Decompress).ReadAllBytes();
				if (decompressedData.Length != decompressedLength) throw new ArgumentException(paramName: nameof(compressedDataWithHeader), message: "decompressed body length does not match, possibly corrupted");
				return Parse(decompressedData);
			}

			public static IReadOnlyList<ItemWithHeader> Parse(in ReadOnlySpan<byte> data)
			{
				List<ItemWithHeader> list = new();
				var pos = 0;
				while (pos < data.Length) list.Add(ReadNext(in data, ref pos));
				return list;
			}

			public static ItemWithHeader ReadNext(in ReadOnlySpan<byte> dataWithoutHeader, ref int pos)
			{
				const int MIN_LENGTH = 2 + AbridgedItem.LEN_FIXED;
				if (dataWithoutHeader.Length < MIN_LENGTH) throw new ArgumentException(paramName: nameof(dataWithoutHeader), message: "AAAAAAAAAAAAAAA");
				switch (dataWithoutHeader[0])
				{
					case (byte) ChunkType.Abridged: //TODO are these cast at runtime?
						if (dataWithoutHeader[1] is not AbridgedItem.LEN_FIXED) throw new ArgumentException(paramName: nameof(dataWithoutHeader), message: "AAAAAAAAAAAAAAA");
						pos += AbridgedItem.LEN_FIXED;
						return new(new(dataWithoutHeader.Slice(start: 2, length: AbridgedItem.LEN_FIXED)));
					case (byte) ChunkType.Unabridged:
						throw new NotImplementedException();
					default:
						throw new NotSupportedException();
				}
			}

			public readonly AbridgedItem Item;

			private ItemWithHeader(AbridgedItem item)
				=> Item = item;

			public readonly void WriteTo(Stream stream)
				=> Item.WriteTo(stream);
		}

		private const int PORT = 27015;

		public const string TOOL_NAME = "DiztinGUIsh IPC Test";

		private static void CleanupSocket(ref Socket? socket)
		{
			if (socket is null) return;
			socket.Shutdown(SocketShutdown.Both);
			socket.Dispose();
		}

#if false
		[RequiredService]
		private IDebuggable DebuggableCore { get; set; }
#endif

		protected override string WindowTitleStatic
			=> TOOL_NAME;

		public DiztinGUIshIPCToolForm()
		{
			SzButtonEx btnSend = new() { Text = "send -->" };
			btnSend.Click += (_, _) =>
			{
				using Socket soc = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				soc.Connect("127.0.0.1", PORT);
				soc.Send(ItemWithHeader.CompressItemsToBytes(ItemWithHeader.DummyList));
				// event flushing in main loop throws at some point
			};
			SzButtonEx btnRecv = new() { Text = "recv <--" };
			btnRecv.Click += (_, _) =>
			{
				using TcpClient client = new();
				client.Connect("127.0.0.1", PORT); // SocketException: No connection could be made because the target machine actively refused it 127.0.0.1:27015 // DiztinGUIsh has the same error if bsnes isn't holding the socket open
//				var items = ItemWithHeader.DummyList;
				var items = ItemWithHeader.DecompressAndParse(client.GetStream().ReadAllBytes());
				Console.WriteLine(items.Count);
				foreach (var item in items) Console.WriteLine(item);
			};
			Controls.Add(new SingleRowFLP { Controls = { btnSend, btnRecv } });
		}

#if false
		public override void Restart()
		{
			DebuggableCore.MemoryCallbacks.Add(new MemoryCallback("System Bus", MemoryCallbackType.Execute, "Lua Hook", (addr, val, flags) => {}, null, null));
		}
#endif
	}
}
