﻿using System;
using System.Net;

namespace SharpRemote
{
	/// <summary>
	///     Represents an endpoint in that can establish a connection to exactly one other
	///     endpoint in the same / different AppDomain, Process or Machine.
	/// </summary>
	/// <remarks>
	///     There can only be one endpoint *per* <see cref="IPEndPoint" /> *per* machine.
	/// </remarks>
	public interface IRemotingEndPoint
		: IDisposable
	{
		/// <summary>
		///     The name of this endpoint, only used for debugging.
		/// </summary>
		string Name { get; }

		/// <summary>
		///     The address of this endpoint.
		/// </summary>
		Uri LocalAddress { get; }

		/// <summary>
		///     The address of the remote endpoint or null when this one is not connected to one.
		/// </summary>
		Uri RemoteAddress { get; }

		/// <summary>
		///     Whether or not this endpoint is connected to another one.
		/// </summary>
		bool IsConnected { get; }

		/// <summary>
		///     Connects this endpoint to the given one.
		/// </summary>
        /// <param name="uri"></param>
		/// <param name="timeout">The amount of time this method should block and await a successful connection from the remote end-point</param>
		/// <exception cref="ArgumentNullException">
		///     When <paramref name="uri" /> is null
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///     When <paramref name="timeout" /> is equal or less than <see cref="TimeSpan.Zero" />
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///     When this endpoint is already connected to another endpoint.
		/// </exception>
		/// <exception cref="NoSuchEndPointException">When no such endpoint could be *found* - it might exist but this one is incapable of establishing a successfuly connection</exception>
		void Connect(Uri uri, TimeSpan timeout);

		/// <summary>
		///     Disconnects this endpoint from its remote endpoint.
		/// </summary>
		/// <remarks>
		///     When this endpoint is not connected to a remot endpoint in the first place, then this method does nothing.
		/// </remarks>
		void Disconnect();

		/// <summary>
		///     Creates and registers an object that implements the given interface <typeparamref name="T" />.
		///     Calls to properties / methods of the given interface are marshalled to connected endpoint, if an appropriate
		///     servant of the same interface an <paramref name="objectId" /> has been created using <see cref="CreateServant{T}" />.
		/// </summary>
		/// <remarks>
		///     A proxy can be created independent from its servant and the order in which both are created is unimportant, for as long
		///     as no interface methods / properties are invoked.
		/// </remarks>
		/// <remarks>
		///     Every method / property on the given object is now capable of throwing an additional set of exceptions, in addition
		///     to whatever exceptions any implementation already throws:
		///     - <see cref="NoSuchServantException" />: There's no servant with the id of the proxy and therefore no subject on which the method could possibly be executed
		///     - <see cref="NotConnectedException" />: At the time of calling the proxy's method, no connection to a remote end point was available
		///     - <see cref="ConnectionLostException" />: The method call was cancelled because the connection between proxy and servant was interrupted / lost / disconnected
		///     - <see cref="UnserializableException" />: The remote method was executed, threw an exception, but the exception could not be serialized
		/// </remarks>
		/// <remarks>
		///     This method is thread-safe.
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectId"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException">
		///     When there already exists a proxy of id <paramref name="objectId" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///     When <typeparamref name="T" /> does not refer to an interface.
		/// </exception>
		T CreateProxy<T>(ulong objectId) where T : class;

		/// <summary>
		/// Returns the proxy that belongs to the given object-id.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectId"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">When there's no proxy with that id</exception>
		/// <exception cref="ArgumentException">When there proxy's type does not match the given <typeparamref name="T"/> type parameter</exception>
		T GetProxy<T>(ulong objectId) where T : class;

		/// <summary>
		///     Creates and registers an object for the given subject <paramref name="subject" /> and invokes its methods, when they
		///     have been called on the corresponding proxy.
		/// </summary>
		/// <remarks>
		///     A servant can be created independent from any proxy and the order in which both are created is unimportant, for as long
		///     as no interface methods / properties are invoked.
		/// </remarks>
		/// <remarks>
		///     This method is thread-safe.
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectId"></param>
		/// <param name="subject"></param>
		/// <returns></returns>
		IServant CreateServant<T>(ulong objectId, T subject) where T : class;

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectId"></param>
		/// <returns></returns>
		T GetExistingOrCreateNewProxy<T>(ulong objectId) where T : class;

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="subject"></param>
		/// <returns></returns>
		IServant GetExistingOrCreateNewServant<T>(T subject) where T : class;
	};
}