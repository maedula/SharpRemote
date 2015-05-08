﻿using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using SharpRemote.Hosting;

namespace SharpRemote.Host
{
	internal class Program
		: IDisposable
	{
		private readonly int? _parentProcessId;
		private readonly ManualResetEvent _waitHandle;
		private readonly Process _parentProcess;

		private Program(string[] args)
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
		}

		private void ParentProcessOnExited(object sender, EventArgs eventArgs)
		{
			Shutdown();
		}

		private void Shutdown()
		{
			OnSubjectHostDisposed();
		}

		private static void Main(string[] args)
		{
			using (var program = new Program(args))
			{
				program.Run();
			}
		}

		public void Run()
		{
			Console.WriteLine(ProcessSilo.Constants.BootingMessage);

			const ulong subjectHostId = ProcessSilo.Constants.SubjectHostId;
			const ulong firstServantId = subjectHostId + 1;

			using (var endpoint = new LidgrenEndPoint(IPAddress.Loopback))
			using (var host = new SubjectHost(endpoint, firstServantId, OnSubjectHostDisposed))
			{
				var servant = endpoint.CreateServant(subjectHostId, (ISubjectHost) host);

				Console.WriteLine(endpoint.LocalEndPoint.Port);
				Console.WriteLine(ProcessSilo.Constants.ReadyMessage);

				_waitHandle.WaitOne();
				Console.WriteLine(ProcessSilo.Constants.ShutdownMessage);
			}
		}

		public void Dispose()
		{
			_waitHandle.Dispose();
		}

		private void OnSubjectHostDisposed()
		{
			_waitHandle.Set();
		}
	}
}