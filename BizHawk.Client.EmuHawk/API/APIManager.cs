using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Client.ApiHawk;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk

{
	public static class APIManager
	{
		private static APIContainer container;
		private static BasicApiProvider Register(IEmulatorServiceProvider serviceProvider)
		{
			// Register external apis
			var apis = Assembly
				.Load("BizHawk.Client.ApiHawk")
				.GetTypes()
				.Where(t => typeof(IExternalAPI).IsAssignableFrom(t))
				.Where(t => t.IsSealed)
				.Where(t => ServiceInjector.IsAvailable(serviceProvider, t))
				.ToList();

			apis.AddRange(
				Assembly
				.GetAssembly(typeof(APIContainer))
				.GetTypes()
				.Where(t => typeof(IExternalAPI).IsAssignableFrom(t))
				.Where(t => t.IsSealed)
				.Where(t => ServiceInjector.IsAvailable(serviceProvider, t)));

			foreach (var api in apis)
			{
				var instance = (IExternalAPI)Activator.CreateInstance(api);
				ServiceInjector.UpdateServices(serviceProvider, instance);
				Libraries.Add(api, instance);
			}
			container = new APIContainer(Libraries);
			return new BasicApiProvider(container);
		}
		private static readonly Dictionary<Type, IExternalAPI> Libraries = new Dictionary<Type, IExternalAPI>();
		public static BasicApiProvider Restart(IEmulatorServiceProvider newServiceProvider)
		{
			Libraries.Clear();
			return Register(newServiceProvider);
		}
	}
}
