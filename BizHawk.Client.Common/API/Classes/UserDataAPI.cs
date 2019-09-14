using System;

namespace BizHawk.Client.Common
{
	public sealed class UserDataAPI : IUserData
	{
		#region Public API (IUserData)

		public void Clear() => Global.UserBag.Clear();

		public bool ContainsKey(string key) => Global.UserBag.ContainsKey(key);

		public object Get(string key)
		{
			object val;
			return Global.UserBag.TryGetValue(key, out val) ? val : null;
		}

		public bool Remove(string key) => Global.UserBag.Remove(key);

		public void Set(string name, object value)
		{
			if (value != null)
			{
				var t = value.GetType();
				if (!t.IsPrimitive && t != typeof(string)) throw new InvalidOperationException("Invalid type for userdata");
			}
			Global.UserBag[name] = value;
		}

		#endregion
	}
}
