﻿using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading;
using log4net;

namespace SharpRemote.Hosting
{
	/// <summary>
	///     Used in conjunction with a <see cref="ProcessSiloClient" /> to host objects in a remote process.
	/// </summary>
	/// <example>
	///     public static void main(string[] arguments)
	///     {
	///         // Put any additional/required initialization here.
	///         using (var silo = new ProcessSiloServer(arguments))
	///         {
	///             // This is the place to register any additional interfaces with this silo
	///             // silo.CreateServant(id, (IMyCustomInterface)new MyCustomImplementation());
	///             silo.Run();
	///         }
	///     }
	/// </example>
	public sealed class ProcessSiloServer
		: IRemotingEndPoint
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private readonly SocketRemotingEndPoint _endPoint;

		private readonly Process _parentProcess;
		private readonly int? _parentProcessId;
		private readonly ManualResetEvent _waitHandle;

		/// <summary>
		///     Initializes a new silo server.
		/// </summary>
		/// <param name="args">The command line arguments given to the Main() method</param>
		public ProcessSiloServer(string[] args)
		{
			int pid;
			if (args.Length >= 1 && int.TryParse(args[0], out pid))
			{
				_parentProcessId = pid;
				_parentProcess = Process.GetProcessById(pid);
				_parentProcess.EnableRaisingEvents = true;
				_parentProcess.Exited += ParentProcessOnExited;
			}

			_waitHandle = new ManualResetEvent(false);

			_endPoint = new SocketRemotingEndPoint();
		}

		/// <summary>
		///     The process id of the parent process, as specified in the command line arguments or null
		///     when no id was specified.
		/// </summary>
		public int? ParentProcessId
		{
			get { return _parentProcessId; }
		}

		public void Dispose()
		{
			_waitHandle.Dispose();
		}

		public string Name
		{
			get { return _endPoint.Name; }
		}

		public bool IsConnected
		{
			get { return _endPoint.IsConnected; }
		}

		public void Disconnect()
		{
			_endPoint.Disconnect();
		}

		public T CreateProxy<T>(ulong objectId) where T : class
		{
			return _endPoint.CreateProxy<T>(objectId);
		}

		public T GetProxy<T>(ulong objectId) where T : class
		{
			return _endPoint.GetProxy<T>(objectId);
		}

		public IServant CreateServant<T>(ulong objectId, T subject) where T : class
		{
			return _endPoint.CreateServant(objectId, subject);
		}

		public T GetExistingOrCreateNewProxy<T>(ulong objectId) where T : class
		{
			return _endPoint.GetExistingOrCreateNewProxy<T>(objectId);
		}

		public IServant GetExistingOrCreateNewServant<T>(T subject) where T : class
		{
			return _endPoint.GetExistingOrCreateNewServant(subject);
		}

		/// <summary>
		///     Runs the server and blocks until a shutdown command is received because the
		///     <see cref="ProcessSiloClient" /> is being disposed of or because the parent process
		///     quits unexpectedly.
		/// </summary>
		public void Run()
		{
			Console.WriteLine(ProcessSiloClient.Constants.BootingMessage);

			const ulong subjectHostId = ProcessSiloClient.Constants.SubjectHostId;
			const ulong firstServantId = subjectHostId + 1;

			try
			{
				using (_endPoint)
				using (var host = new SubjectHost(_endPoint, firstServantId, OnSubjectHostDisposed))
				{
					_endPoint.CreateServant(subjectHostId, (ISubjectHost) host);

					_endPoint.Bind(IPAddress.Loopback);
					Console.WriteLine(_endPoint.LocalEndPoint.Port);
					Console.WriteLine(ProcessSiloClient.Constants.ReadyMessage);

					_waitHandle.WaitOne();
					Console.WriteLine(ProcessSiloClient.Constants.ShutdownMessage);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Exception: {0}", e.Message);
			}
		}

		private void ParentProcessOnExited(object sender, EventArgs eventArgs)
		{
			Log.InfoFormat("Parent process terminated unexpectedly (exit code: {0}), shutting down...",
			               _parentProcess.ExitCode
				);

			Shutdown();
		}

		private void Shutdown()
		{
			OnSubjectHostDisposed();
		}

		private void OnSubjectHostDisposed()
		{
			_waitHandle.Set();
		}
	}
}