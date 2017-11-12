﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using SharpRemote.Attributes;
using SharpRemote.Exceptions;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	/// </summary>
	public abstract class AbstractReadValueNotNullMethodCompiler
		: AbstractMethodCompiler
	{
		private readonly CompilationContext _context;

		/// <summary>
		/// </summary>
		protected AbstractReadValueNotNullMethodCompiler(CompilationContext context)
		{
			_context = context;
			Method = context.TypeBuilder.DefineMethod("ReadValueNotNull",
			                                          MethodAttributes.Public | MethodAttributes.Static,
			                                          CallingConventions.Standard,
			                                          context.Type,
			                                          new[]
			                                          {
				                                          context.ReaderType,
				                                          typeof(ISerializer2),
				                                          typeof(IRemotingEndPoint)
			                                          });
		}

		/// <inheritdoc />
		public override MethodBuilder Method { get; }

		/// <inheritdoc />
		public override void Compile(AbstractMethodsCompiler methods, ISerializationMethodStorage<AbstractMethodsCompiler> methodStorage)
		{
			var serializationType = _context.TypeDescription.SerializationType;
			switch (serializationType)
			{
				case SerializationType.ByValue:
					EmitReadByValue();
					break;

				case SerializationType.ByReference:
					EmitReadByReference();
					break;

				case SerializationType.Singleton:
					break;

				case SerializationType.NotSerializable:
					break;

				default:
					throw new InvalidEnumArgumentException("", (int)serializationType, typeof(SerializationType));
			}
		}

		private void EmitReadByValue()
		{
			var gen = Method.GetILGenerator();
			var tmp = gen.DeclareLocal(_context.Type);

			if (_context.Type.IsValueType)
			{
				gen.Emit(OpCodes.Ldloca, tmp);
				gen.Emit(OpCodes.Initobj, _context.Type);
			}
			else
			{
				var ctor = _context.Type.GetConstructor(new Type[0]);
				if (ctor == null)
					throw new ArgumentException(string.Format("Type '{0}' is missing a parameterless constructor", _context.Type));

				gen.Emit(OpCodes.Newobj, ctor);
				gen.Emit(OpCodes.Stloc, tmp);
			}

			// tmp.BeforeDeserializationCallback();
			EmitCallBeforeDeserialization(gen, tmp);

			EmitReadFields(gen, tmp);
			EmitReadProperties(gen, tmp);

			// tmp.AfterDeserializationCallback();
			EmitCallAfterSerialization(gen, tmp);

			// return tmp
			gen.Emit(OpCodes.Ldloc, tmp);
			gen.Emit(OpCodes.Ret);
		}

		private void EmitReadFields(ILGenerator gen, LocalBuilder local)
		{
			foreach (var field in _context.TypeDescription.Fields)
				try
				{
					if (_context.TypeDescription.IsValueType)
					{
						gen.Emit(OpCodes.Ldloca_S, local);
					}
					else
					{
						gen.Emit(OpCodes.Ldloc, local);
					}
					EmitReadValue(gen, field.FieldType, field.Name);
					gen.Emit(OpCodes.Stfld, field.Field);
				}
				catch (SerializationException)
				{
					throw;
				}
				catch (Exception e)
				{
					var message = string.Format("There was a problem generating the code to deserialize field '{0} {1}' of type '{2}' ",
					                            field.FieldType,
					                            field.Name,
					                            _context.Type.FullName
					                           );
					throw new SerializationException(message, e);
				}
		}

		private void EmitReadProperties(ILGenerator gen, LocalBuilder local)
		{
			foreach (var property in _context.TypeDescription.Properties)
				try
				{
					if (_context.TypeDescription.IsValueType)
					{
						gen.Emit(OpCodes.Ldloca_S, local);
					}
					else
					{
						gen.Emit(OpCodes.Ldloc, local);
					}
					EmitReadValue(gen, property.PropertyType, property.Name);
					gen.Emit(OpCodes.Call, property.SetMethod.Method);
				}
				catch (SerializationException)
				{
					throw;
				}
				catch (Exception e)
				{
					var message =
						string.Format("There was a problem generating the code to serialize property '{0} {1}' of type '{2}' ",
						              property.PropertyType,
						              property.Name,
						              _context.Type.FullName
						             );
					throw new SerializationException(message, e);
				}
		}

		private void EmitReadValue(ILGenerator gen, TypeDescription valueType, string name)
		{
			EmitBeginReadFieldOrProperty(gen, valueType, name);
			var type = valueType.Type;
			if (type == typeof(byte))
				EmitReadByte(gen);
			else if (type == typeof(sbyte))
				EmitReadSByte(gen);
			else if (type == typeof(ushort))
				EmitReadUShort(gen);
			else if (type == typeof(short))
				EmitReadShort(gen);
			else if (type == typeof(uint))
				EmitReadUInt(gen);
			else if (type == typeof(int))
				EmitReadInt(gen);
			else if (type == typeof(ulong))
				EmitReadULong(gen);
			else if (type == typeof(long))
				EmitReadLong(gen);
			else if (type == typeof(decimal))
				EmitReadDecimal(gen);
			else if (type == typeof(float))
				EmitReadFloat(gen);
			else if (type == typeof(double))
				EmitReadDouble(gen);
			else if (type == typeof(string))
				EmitReadString(gen);
			else
				throw new NotImplementedException();
			EmitEndReadFieldOrProperty(gen, valueType, name);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gen"></param>
		protected abstract void EmitReadByte(ILGenerator gen);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gen"></param>
		protected abstract void EmitReadSByte(ILGenerator gen);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gen"></param>
		protected abstract void EmitReadUShort(ILGenerator gen);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gen"></param>
		protected abstract void EmitReadShort(ILGenerator gen);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gen"></param>
		protected abstract void EmitReadUInt(ILGenerator gen);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gen"></param>
		protected abstract void EmitReadInt(ILGenerator gen);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gen"></param>
		protected abstract void EmitReadULong(ILGenerator gen);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gen"></param>
		protected abstract void EmitReadLong(ILGenerator gen);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gen"></param>
		protected abstract void EmitReadDecimal(ILGenerator gen);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gen"></param>
		protected abstract void EmitReadFloat(ILGenerator gen);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gen"></param>
		protected abstract void EmitReadDouble(ILGenerator gen);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gen"></param>
		protected abstract void EmitReadString(ILGenerator gen);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="valueType"></param>
		/// <param name="name"></param>
		protected abstract void EmitBeginReadFieldOrProperty(ILGenerator gen, TypeDescription valueType, string name);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="valueType"></param>
		/// <param name="name"></param>
		protected abstract void EmitEndReadFieldOrProperty(ILGenerator gen, TypeDescription valueType, string name);

		private void EmitCallBeforeDeserialization(ILGenerator gen, LocalBuilder tmp)
		{
			var method = _context.Type.GetMethods()
			                     .FirstOrDefault(x => x.GetCustomAttribute<BeforeSerializeAttribute>() != null);
			if (method != null)
			{
				gen.Emit(OpCodes.Ldloc, tmp);
				gen.EmitCall(OpCodes.Call, method, optionalParameterTypes: null);
			}
		}

		private void EmitCallAfterSerialization(ILGenerator gen, LocalBuilder tmp)
		{
			var method = _context.Type.GetMethods().FirstOrDefault(x => x.GetCustomAttribute<AfterSerializeAttribute>() != null);
			if (method != null)
			{
				gen.Emit(OpCodes.Ldloc, tmp);
				gen.EmitCall(OpCodes.Call, method, optionalParameterTypes: null);
			}
		}

		private void EmitReadByReference()
		{
			var gen = Method.GetILGenerator();

			var objectRetrieved = gen.DefineLabel();
			var getOrCreateProxy = gen.DefineLabel();

			var hint = gen.DeclareLocal(typeof(ByReferenceHint));
			var id = gen.DeclareLocal(typeof(ulong));

			// hint, id = ReadHintAndGrain() //< defined in inherited class
			EmitReadHintAndGrainId(gen);
			gen.Emit(OpCodes.Stloc, id);
			gen.Emit(OpCodes.Stloc, hint);

			// If hint != RetrieveSubject { goto getOrCreateProxy; }
			gen.Emit(OpCodes.Ldloc, hint);
			gen.Emit(OpCodes.Ldc_I4, (int)ByReferenceHint.RetrieveSubject);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Brfalse_S, getOrCreateProxy);

			// result = _remotingEndPoint.RetrieveSubject(id)
			var retrieveSubject = typeof(IRemotingEndPoint).GetMethod("RetrieveSubject").MakeGenericMethod(_context.TypeDescription.ByReferenceInterfaceType);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldloc, id);
			gen.Emit(OpCodes.Callvirt, retrieveSubject);
			gen.Emit(OpCodes.Br, objectRetrieved);

			gen.MarkLabel(getOrCreateProxy);
			// result = _remotingEndPoint.GetExistingOrCreateNewProxy<T>(serializer.ReadLong());
			var getOrCreateNewProxy = typeof(IRemotingEndPoint)
				.GetMethod("GetExistingOrCreateNewProxy").MakeGenericMethod(_context.TypeDescription.ByReferenceInterfaceType);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldloc, id);
			gen.Emit(OpCodes.Callvirt, getOrCreateNewProxy);

			gen.MarkLabel(objectRetrieved);
			gen.Emit(OpCodes.Ret);
		}

		/// <summary>
		///     Shall emit code which reads two values from the given writer and pushes them
		///     onto the evaluation stack in the following order:
		///     1. Shall we create a proxy or servant? => <see cref="ByReferenceHint" />
		///     2. GrainId of the proxy/servant => <see cref="ulong"/>
		/// </summary>
		/// <param name="generator"></param>
		protected abstract void EmitReadHintAndGrainId(ILGenerator generator);
	}
}