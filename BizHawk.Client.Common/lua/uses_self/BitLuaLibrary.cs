using System;
using System.ComponentModel;

using NLua;

namespace BizHawk.Client.Common
{
	[Description("A library for performing standard bitwise operations.")]
	public sealed class BitLuaLibrary : LuaLibraryBase
	{
		public override string Name => "bit";

		public BitLuaLibrary(Lua lua) : base(lua) {}

		public BitLuaLibrary(Lua lua, Action<string> logOutputCallback) : base(lua, logOutputCallback) {}

		#region Lua-specific

		[LuaMethodExample("local shifted = bit.arshift(-1000, 4)")] //TODO docs
		[LuaMethod("arshift", "Computes the arithmetic right-shift of signed 32-bit integer `i` by `s` places.")]
		public static int Arshift(int i, int s) => i >> s;

		[LuaMethodExample("local intersection = bit.band(43, 15) -- = 11")]
		[LuaMethod("band", "Computes the bitwise AND (C-like: `a & b`) of unsigned 32-bit integers `a` and `b`.")]
		public static uint Band(uint a, uint b) => a & b;

		[LuaMethodExample("local complement = bit.bnot(0xA2BCAF9B) -- = 0x5D435064")]
		[LuaMethod("bnot", "Computes the bitwise NOT (C-like: `~b`) of unsigned 32-bit integer `b`.")]
		public static uint Bnot(uint b) => ~b;

		[LuaMethodExample("local union = bit.bor(12, 25) -- = 29")]
		[LuaMethod("bor", "Computes the bitwise OR (C-like: `a | b`) of unsigned 32-bit integers `a` and `b`.")]
		public static uint Bor(uint a, uint b) => a | b;

		[LuaMethodExample("local sym_difference = bit.bxor(24, 9) -- = 17")]
		[LuaMethod("bxor", "Computes the bitwise XOR (C-like: `a ^ b`) of unsigned 32-bit integers `a` and `b`.")]
		public static uint Bxor(uint a, uint b) => a ^ b;

		[LuaMethodExample("local octet_reversed = bit.byteswap_16(0xABCD) -- = 0xCDAB")]
		[LuaMethod("byteswap_16", "Returns a copy of unsigned 16-bit integer `b` with its octets reversed.")]
		public static ushort Byteswap16(ushort b) => (ushort) ((b & 0x00FFU) << 8 | (b & 0xFF00U) >> 8);

		[LuaMethodExample("local octet_reversed = bit.byteswap_32(0xABCD1234) -- = 0x3412CDAB")]
		[LuaMethod("byteswap_32", "Returns a copy of unsigned 32-bit integer `b` with its octets reversed.")]
		public static uint Byteswap32(uint b) => 0U
			| (b & 0x000000FFU) << 24
			| (b & 0x0000FF00U) << 8
			| (b & 0x00FF0000U) >> 8
			| (b & 0xFF000000U) >> 24;

		[LuaMethodExample("local octet_reversed = bit.byteswap_64(0x0123456789ABCDEF) -- = 0xEFCDAB8967452301")]
		[LuaMethod("byteswap_64", "Returns a copy of unsigned 64-bit integer `b` with its octets reversed.")]
		public static ulong Byteswap64(ulong b) => 0U
			| (b & 0x00000000000000FFUL) << 56
			| (b & 0x000000000000FF00UL) << 40
			| (b & 0x0000000000FF0000UL) << 24
			| (b & 0x00000000FF000000UL) << 8
			| (b & 0x000000FF00000000UL) >> 8
			| (b & 0x0000FF0000000000UL) >> 24
			| (b & 0x00FF000000000000UL) >> 40
			| (b & 0xFF00000000000000UL) >> 56;

		[LuaMethodExample("console.log(bit.check(4861 / 3, 0) and \"odd\" or \"even\")")]
		[LuaMethod("check", "Returns true iff the unsigned 64-bit integer `b` has the bit at position `p` set (to 1).")]
		public static bool Check(ulong b, int p) => (b & (1U << p)) != 0U;

		[LuaMethodExample("local without_bit3 = bit.clear(25, 3) -- = 17")]
		[LuaMethod("clear", "Returns a copy of the unsigned 32-bit integer `b` with the bit at `p` unset (set to 0).")]
		public static uint Clear(uint b, int p) => b & ~(1U << p);

		[LuaMethodExample("local shifted = bit.lshift(1000, 4)")] //TODO docs
		[LuaMethod("lshift", "Computes the left-shift of unsigned 32-bit integer `b` by `s` places.")]
		public static uint Lshift(uint b, int s) => b << s;

		[LuaMethodExample("local rotated = bit.rol(1000, 4)")] //TODO docs
		[LuaMethod("rol", "Computes the leftward rotation of unsigned 32-bit integer `b` by `s` places.")]
		public static uint Rol(uint b, int s) => (b << s) | (b >> 32 - s);

		[LuaMethodExample("local rotated = bit.ror(1000, 4)")] //TODO docs
		[LuaMethod("ror", "Computes the rightward rotation of unsigned 32-bit integer `b` by `s` places.")]
		public static uint Ror(uint b, int s) => (b >> s) | (b << 32 - s);

		[LuaMethodExample("local shifted = bit.rshift(1000, 4)")] //TODO docs
		[LuaMethod("rshift", "Computes the (logical) right-shift of unsigned 32-bit integer `b` by `s` places.")]
		public static uint Rshift(uint b, int s) => b >> s;

		[LuaMethodExample("local with_bit25 = bit.set(35, 25)")] //TODO docs
		[LuaMethod("set", "Returns a copy of the unsigned 32-bit integer `b` with the bit at `p` set (to 1).")]
		public static uint Set(uint b, int p) => b | (1U << p);

		#endregion
	}
}
