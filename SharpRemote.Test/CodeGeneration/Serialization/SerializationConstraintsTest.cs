﻿using System;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.CodeGeneration;
using SharpRemote.CodeGeneration.Serialization;
using SharpRemote.Test.CodeGeneration.Types.Structs;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public sealed class SerializationConstraintsTest
	{
		private Serializer _serializer;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			_serializer = new Serializer();
		}

		[Test]
		[Description("Verifies that registering a type without a [DataContract] attribute is not allowed")]
		public void TestNoDataContractStruct()
		{
			TestFailRegister<MissingDataContractStruct>("The type 'SharpRemote.Test.CodeGeneration.Types.Structs.MissingDataContractStruct' is missing the [DataContract] attribute - this is not supported");
		}

		[Test]
		[Description("Verifies that registering a type that contains a [DataMember] readonly field is not allowed")]
		public void TestReadOnlyDataMemberFieldStruct()
		{
			TestFailRegister<ReadOnlyDataMemberFieldStruct>("The field 'SharpRemote.Test.CodeGeneration.Types.Structs.ReadOnlyDataMemberFieldStruct.Value' is marked with the [DataMember] attribute but is readonly - this is not supported");
		}

		[Test]
		[Description("Verifies that registering a type that contains a [DataMember] static field is not allowed")]
		public void TestStaticDataMemberFieldStruct()
		{
			TestFailRegister<StaticDataMemberFieldStruct>("The field 'SharpRemote.Test.CodeGeneration.Types.Structs.StaticDataMemberFieldStruct.Value' is marked with the [DataMember] attribute but is static - this is not supported");
		}

		private void TestFailRegister<T>(string reason)
		{
			new Action(() => _serializer.RegisterType<T>())
				.ShouldThrow<ArgumentException>()
				.WithMessage(reason);
			_serializer.IsTypeRegistered<T>().Should().BeFalse();
		}
	}
}