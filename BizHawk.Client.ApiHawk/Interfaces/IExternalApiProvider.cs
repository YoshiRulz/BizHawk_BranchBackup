using System;
using System.Collections.Generic;

namespace BizHawk.Client.ApiHawk
{
	/// <summary>
	/// This interface defines the mechanism by which External tools can retrieve <seealso cref="IExternalAPI" />
	/// from a client implementation
	/// An implementation should collect all available IExternalAPI instances.
	/// This interface defines only the external interaction.  This interface does not specify the means
	/// by which a api provider will be populated with available apis.  However, an implementation
	/// by design must provide this mechanism
	/// </summary>
	/// <seealso cref="IExternalAPI"/> 
	public interface IExternalApiProvider
	{
		/// <summary>e
		/// Returns whether or not T is available
		/// </summary>
		/// <typeparam name="T">The <seealso cref="IExternalAPI" /> to check</typeparam>
		bool HasApi<T>() where T : IExternalAPI;

		/// <summary>
		/// Returns whether or not t is available
		/// </summary>
		bool HasApi(Type t);

		/// <summary>
		/// Returns an instance of T if T is available
		/// Else returns null
		/// </summary>
		/// <typeparam name="T">The requested <seealso cref="IExternalAPI" /></typeparam>
		T GetApi<T>() where T : IExternalAPI;

		/// <summary>
		/// Returns an instance of t if t is available
		/// Else returns null
		/// </summary>
		object GetApi(Type t);

		/// <summary>
		/// Gets a list of all currently registered Apis available to be retrieved
		/// </summary>
		IEnumerable<Type> AvailableApis { get; }
	}
}
