﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace SharpRemote
{
	/// <summary>
	/// Handles remote method calls that are not completed yet.
	/// </summary>
	internal sealed class PendingMethodsQueue
		: IDisposable
	{
		private readonly object _syncRoot;
		private readonly Queue<PendingMethodCall> _recycledMessages;
		private readonly BlockingCollection<PendingMethodCall> _pendingWrites;
		private readonly Dictionary<long, PendingMethodCall> _pendingCalls;

		private const int MaxConcurrency = 1000;

		public PendingMethodsQueue()
		{
			_syncRoot = new object();
			_recycledMessages = new Queue<PendingMethodCall>();
			_pendingWrites = new BlockingCollection<PendingMethodCall>(MaxConcurrency);
			_pendingCalls = new Dictionary<long, PendingMethodCall>();
		}

		/// <summary>
		/// Retrieves the next message for writing from the queue.
		/// </summary>
		/// <param name="token"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		public byte[] TakePendingWrite(CancellationToken token, out int length)
		{
			var message = _pendingWrites.Take(token);
			return message.GetMessage(out length);
		}

		/// <summary>
		/// Shall be called once an answer to a pending call has been received.
		/// Causes the <see cref="WaitHandle"/> of the pending call to be set.
		/// </summary>
		/// <param name="rpcId"></param>
		/// <param name="messageType"></param>
		/// <param name="reader"></param>
		public bool HandleResponse(long rpcId, MessageType messageType, BinaryReader reader)
		{
			lock (_syncRoot)
			{
				PendingMethodCall methodCall;
				if (_pendingCalls.TryGetValue(rpcId, out methodCall))
				{
					methodCall.HandleResponse(messageType, reader);
					return true;
				}
			}

			return false;
		}

		public void CancelAllCalls()
		{
			lock (_syncRoot)
			{
				if (_pendingCalls.Count > 0)
				{
					byte[] exceptionMessage;
					int exceptionLength;

					using (var stream = new MemoryStream())
					using (var writer = new BinaryWriter(stream, Encoding.UTF8))
					{
						AbstractEndPoint.WriteException(writer, new ConnectionLostException());
						exceptionMessage = stream.GetBuffer();
						exceptionLength = (int)stream.Length;
					}

					foreach (var call in _pendingCalls.Values)
					{
						var stream = new MemoryStream(exceptionMessage, 0, exceptionLength);
						var reader = new BinaryReader(stream, Encoding.UTF8);
						call.HandleResponse(MessageType.Return | MessageType.Exception, reader);
					}
					_pendingCalls.Clear();
				}
			}
		}

		/// <summary>
		/// Shall be called once a pending call has been completely handled.
		/// </summary>
		/// <param name="methodCall"></param>
		public void Recycle(PendingMethodCall methodCall)
		{
			lock (_syncRoot)
			{
				_pendingCalls.Remove(methodCall.RpcId);
				_recycledMessages.Enqueue(methodCall);
			}
		}

		public PendingMethodCall Enqueue(ulong servantId,
		                    string interfaceType,
		                    string methodName,
		                    MemoryStream arguments,
		                    long rpcId)
		{
			PendingMethodCall message;

			lock (_syncRoot)
			{
				message = _recycledMessages.Count > 0
					          ? _recycledMessages.Dequeue()
							  : new PendingMethodCall();
				_pendingCalls.Add(rpcId, message);
			}

			message.Reset(servantId, interfaceType, methodName, arguments, rpcId);

			lock (_pendingWrites)
			{
				_pendingWrites.Add(message);
			}

			return message;
		}

		public void Dispose()
		{
			_pendingWrites.Dispose();
		}
	}
}