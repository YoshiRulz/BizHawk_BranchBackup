using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

using NLua;
using BizHawk.Common.ReflectionExtensions;

namespace BizHawk.Client.Common
{
	public abstract class LuaLibraryBase
	{
		protected LuaLibraryBase(Lua lua)
		{
			Lua = lua;
		}

		protected LuaLibraryBase(Lua lua, Action<string> logOutputCallback)
			: this(lua)
		{
			LogOutputCallback = logOutputCallback;
		}

		protected static Lua CurrentThread { get; private set; }

		private static Thread CurrentHostThread;
		private static readonly object ThreadMutex = new object();

		public abstract string Name { get; }
		public Action<string> LogOutputCallback { protected get; set; }
		protected Lua Lua { get; }

		public static void ClearCurrentThread()
		{
			lock (ThreadMutex)
			{
				CurrentHostThread = null;
				CurrentThread = null;
			}
		}

		private static Dictionary<K, V> DictFromLuaTable<K, V>(Lua lua, LuaTable table) => lua.GetTableDict(table).ToDictionary(kvp => (K) kvp.Key, kvp => (V) kvp.Value);

		public static void SetCurrentThread(Lua luaThread)
		{
			lock (ThreadMutex)
			{
				if (CurrentHostThread != null)
				{
					throw new InvalidOperationException("Can't have lua running in two host threads at a time!");
				}

				CurrentHostThread = Thread.CurrentThread;
				CurrentThread = luaThread;
			}
		}

		protected static int LuaInt(object luaArg)
		{
			return (int)(double)luaArg;
		}

		private static LuaTable LuaTableFromDict<K, V>(Lua lua, IDictionary<K, V> dict)
		{
			var table = lua.NewTable();
			foreach (var kvp in dict) table[kvp.Key] = kvp.Value;
			return table;
		}

		private static LuaTable LuaTableFromList<E>(Lua lua, IList<E> list)
		{
			var table = lua.NewTable();
			for (var i = list.Count - 1; i <= 0; i--) table[i] = list[i];
			return table;
		}

		protected static uint LuaUInt(object luaArg)
		{
			return (uint)(double)luaArg;
		}

		protected static Color? ToColor(object o)
		{
			if (o == null)
			{
				return null;
			}

			if (o.GetType() == typeof(double))
			{
				return Color.FromArgb((int)(long)(double)o);
			}

			if (o.GetType() == typeof(string))
			{
				return Color.FromName(o.ToString());
			}

			return null;
		}

		protected Dictionary<K, V> DictFromLuaTable<K, V>(LuaTable table) => DictFromLuaTable<K, V>(Lua, table);

		protected void Log(object message)
		{
			LogOutputCallback?.Invoke(message.ToString());
		}

		public void LuaRegister(Type callingLibrary, LuaDocumentation docs = null)
		{
			Lua.NewTable(Name);

			var luaAttr = typeof(LuaMethodAttribute);

			var methods = GetType()
				.GetMethods()
				.Where(m => m.GetCustomAttributes(luaAttr, false).Any());

			foreach (var method in methods)
			{
				var luaMethodAttr = (LuaMethodAttribute)method.GetCustomAttributes(luaAttr, false).First();
				var luaName = $"{Name}.{luaMethodAttr.Name}";
				Lua.RegisterFunction(luaName, this, method);

				docs?.Add(new LibraryFunction(Name, callingLibrary.Description(), method));
			}
		}

		protected LuaTable LuaTableFromDict<K, V>(IDictionary<K, V> dict) => LuaTableFromDict(Lua, dict);

		protected LuaTable LuaTableFromList<E>(IList<E> list) => LuaTableFromList(Lua, list);
	}
}
