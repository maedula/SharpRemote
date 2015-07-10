﻿using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// This exception is thrown when a thrown exception should be marshalled (because it crosses the proxy / servant threshold)
	/// but cannot, for example because it is missing the <see cref="SerializableAttribute"/> or a proper constructor.
	/// 
	/// It preserves a lot of information about the original exception to help document the original problem as well as
	/// to find it, if necessary.
	/// </summary>
	[Serializable]
	public class UnserializableException
		: RemotingException
	{
		private readonly string _originalMessage;
		private readonly string _originalSource;
		private readonly string _originalStacktrace;

#if !WINDOWS_PHONE_APP
		private readonly string _originalTargetSite;
#endif

		private readonly string _originalTypename;

		/// <summary>
		/// Creates a new UnserializableException that tries to capture
		/// as much information about the original (unserializable) exception
		/// as possible to ease debugging.
		/// </summary>
		/// <param name="originalException"></param>
		public UnserializableException(Exception originalException)
		{
			_originalMessage = originalException.Message;
			_originalStacktrace = originalException.StackTrace;
			_originalTypename = originalException.GetType().AssemblyQualifiedName;
			_originalSource = originalException.Source;

#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
			_originalTargetSite = originalException.TargetSite.Name;
#endif
#endif

			HResult = originalException.HResult;
		}

#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT

		/// <summary>
		/// Restores an UnserializableException from the given stream.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public UnserializableException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			_originalMessage = info.GetString("OriginalMessage");
			_originalStacktrace = info.GetString("OriginalStacktrace");
			_originalTypename = info.GetString("OriginalExceptionType");
			_originalSource = info.GetString("OriginalSource");
			_originalTargetSite = info.GetString("OriginalTargetSite");
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("OriginalMessage", _originalMessage);
			info.AddValue("OriginalStacktrace", _originalStacktrace);
			info.AddValue("OriginalExceptionType", _originalTypename);
			info.AddValue("OriginalSource", _originalSource);
			info.AddValue("OriginalTargetSite", _originalTargetSite);
		}
#endif
#endif
		/// <summary>
		/// The <see cref="Exception.Message"/> of the
		/// original exception that could not be serialized.
		/// </summary>
		public string OriginalMessage
		{
			get { return _originalMessage; }
		}

		/// <summary>
		/// The <see cref="Exception.StackTrace"/> of the
		/// original exception that could not be serialized.
		/// </summary>
		public string OriginalStacktrace
		{
			get { return _originalStacktrace; }
		}

		/// <summary>
		/// The <see cref="Exception.Source"/> of the
		/// original exception that could not be serialized.
		/// </summary>
		public string OriginalSource
		{
			get { return _originalSource; }
		}

		/// <summary>
		/// The fully qualified typename of the original
		/// exception that could not be serialized.
		/// </summary>
		public string OriginalTypename
		{
			get { return _originalTypename; }
		}

#if !WINDOWS_PHONE_APP

		/// <summary>
		/// The name of the <see cref="Exception.TargetSite"/> of the
		/// original exception that could not be serialized.
		/// </summary>
		public string OriginalTargetSite
		{
			get { return _originalTargetSite; }
		}
#endif
	}
}