﻿/*
  Copyright (C) 2007-2014 Jeroen Frijters

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Jeroen Frijters
  jeroen@frijters.net
  
*/
using System;
using System.Reflection;
#if !NO_REF_EMIT
using System.Reflection.Emit;
#endif
using System.Runtime.Serialization;
using System.Security;

using IKVM.Internal;

namespace IKVM.Java.Externs.sun.reflect
{

    static class ReflectionFactory
	{

#if !FIRST_PASS

		private static object ConvertPrimitive(TypeWrapper tw, object value)
		{
			if (tw == PrimitiveTypeWrapper.BOOLEAN)
			{
				if (value is global::java.lang.Boolean)
				{
					return ((global::java.lang.Boolean)value).booleanValue();
				}
			}
			else if (tw == PrimitiveTypeWrapper.BYTE)
			{
				if (value is global::java.lang.Byte)
				{
					return ((global::java.lang.Byte)value).byteValue();
				}
			}
			else if (tw == PrimitiveTypeWrapper.CHAR)
			{
				if (value is global::java.lang.Character)
				{
					return ((global::java.lang.Character)value).charValue();
				}
			}
			else if (tw == PrimitiveTypeWrapper.SHORT)
			{
				if (value is global::java.lang.Short || value is global::java.lang.Byte)
				{
					return ((global::java.lang.Number)value).shortValue();
				}
			}
			else if (tw == PrimitiveTypeWrapper.INT)
			{
				if (value is global::java.lang.Integer || value is global::java.lang.Short || value is global::java.lang.Byte)
				{
					return ((global::java.lang.Number)value).intValue();
				}
				else if (value is global::java.lang.Character)
				{
					return (int)((global::java.lang.Character)value).charValue();
				}
			}
			else if (tw == PrimitiveTypeWrapper.LONG)
			{
				if (value is global::java.lang.Long || value is global::java.lang.Integer || value is global::java.lang.Short || value is global::java.lang.Byte)
				{
					return ((global::java.lang.Number)value).longValue();
				}
				else if (value is global::java.lang.Character)
				{
					return (long)((global::java.lang.Character)value).charValue();
				}
			}
			else if (tw == PrimitiveTypeWrapper.FLOAT)
			{
				if (value is global::java.lang.Float || value is global::java.lang.Long || value is global::java.lang.Integer || value is global::java.lang.Short || value is global::java.lang.Byte)
				{
					return ((global::java.lang.Number)value).floatValue();
				}
				else if (value is global::java.lang.Character)
				{
					return (float)((global::java.lang.Character)value).charValue();
				}
			}
			else if (tw == PrimitiveTypeWrapper.DOUBLE)
			{
				if (value is global::java.lang.Double || value is global::java.lang.Float || value is global::java.lang.Long || value is global::java.lang.Integer || value is global::java.lang.Short || value is global::java.lang.Byte)
				{
					return ((global::java.lang.Number)value).doubleValue();
				}
				else if (value is global::java.lang.Character)
				{
					return (double)((global::java.lang.Character)value).charValue();
				}
			}
			throw new global::java.lang.IllegalArgumentException();
		}

		private static object[] ConvertArgs(ClassLoaderWrapper loader, TypeWrapper[] argumentTypes, object[] args)
		{
			object[] nargs = new object[args == null ? 0 : args.Length];
			if (nargs.Length != argumentTypes.Length)
			{
				throw new global::java.lang.IllegalArgumentException("wrong number of arguments");
			}
			for (int i = 0; i < nargs.Length; i++)
			{
				if (argumentTypes[i].IsPrimitive)
				{
					nargs[i] = ConvertPrimitive(argumentTypes[i], args[i]);
				}
				else
				{
					if (args[i] != null && !argumentTypes[i].EnsureLoadable(loader).IsInstance(args[i]))
					{
						throw new global::java.lang.IllegalArgumentException();
					}
					nargs[i] = argumentTypes[i].GhostWrap(args[i]);
				}
			}
			return nargs;
		}

		private sealed class MethodAccessorImpl : global::sun.reflect.MethodAccessor
		{
			private readonly MethodWrapper mw;

			internal MethodAccessorImpl(MethodWrapper mw)
			{
				this.mw = mw;
				mw.Link();
				mw.ResolveMethod();
			}

			[IKVM.Attributes.HideFromJava]
			public object invoke(object obj, object[] args, global::ikvm.@internal.CallerID callerID)
			{
				if (!mw.IsStatic && !mw.DeclaringType.IsInstance(obj))
				{
					if (obj == null)
					{
						throw new global::java.lang.NullPointerException();
					}
					throw new global::java.lang.IllegalArgumentException("object is not an instance of declaring class");
				}
				args = ConvertArgs(mw.DeclaringType.GetClassLoader(), mw.GetParameters(), args);
				// if the method is an interface method, we must explicitly run <clinit>,
				// because .NET reflection doesn't
				if (mw.DeclaringType.IsInterface)
				{
					mw.DeclaringType.RunClassInit();
				}
				if (mw.HasCallerID)
				{
					args = ArrayUtil.Concat(args, callerID);
				}
				object retval;
				try
				{
					retval = mw.Invoke(obj, args);
				}
				catch (Exception x)
				{
					throw new global::java.lang.reflect.InvocationTargetException(global::ikvm.runtime.Util.mapException(x));
				}
				if (mw.ReturnType.IsPrimitive && mw.ReturnType != PrimitiveTypeWrapper.VOID)
				{
					retval = JVM.Box(retval);
				}
				else
				{
					retval = mw.ReturnType.GhostUnwrap(retval);
				}
				return retval;
			}
		}

		private sealed class ConstructorAccessorImpl : global::sun.reflect.ConstructorAccessor
		{
			private readonly MethodWrapper mw;

			internal ConstructorAccessorImpl(MethodWrapper mw)
			{
				this.mw = mw;
				mw.Link();
				mw.ResolveMethod();
			}

			[IKVM.Attributes.HideFromJava]
			public object newInstance(object[] args)
			{
				args = ConvertArgs(mw.DeclaringType.GetClassLoader(), mw.GetParameters(), args);
				try
				{
					return mw.CreateInstance(args);
				}
				catch (Exception x)
				{
					throw new global::java.lang.reflect.InvocationTargetException(global::ikvm.runtime.Util.mapException(x));
				}
			}
		}

		private sealed class SerializationConstructorAccessorImpl : global::sun.reflect.ConstructorAccessor
		{
			private readonly MethodWrapper mw;
			private readonly Type type;

			internal SerializationConstructorAccessorImpl(global::java.lang.reflect.Constructor constructorToCall, global::java.lang.Class classToInstantiate)
			{
				this.type = TypeWrapper.FromClass(classToInstantiate).TypeAsBaseType;
				MethodWrapper mw = MethodWrapper.FromExecutable(constructorToCall);
				if (mw.DeclaringType != CoreClasses.java.lang.Object.Wrapper)
				{
					this.mw = mw;
					mw.Link();
					mw.ResolveMethod();
				}
			}

			[IKVM.Attributes.HideFromJava]
			[SecuritySafeCritical]
			public object newInstance(object[] args)
			{
				object obj = FormatterServices.GetUninitializedObject(type);
				if (mw != null)
				{
					mw.Invoke(obj, ConvertArgs(mw.DeclaringType.GetClassLoader(), mw.GetParameters(), args));
				}
				return obj;
			}
		}

#if !NO_REF_EMIT

		private sealed class BoxUtil
		{

			private static readonly MethodInfo valueOfByte = typeof(global::java.lang.Byte).GetMethod("valueOf", new Type[] { typeof(byte) });
			private static readonly MethodInfo valueOfBoolean = typeof(global::java.lang.Boolean).GetMethod("valueOf", new Type[] { typeof(bool) });
			private static readonly MethodInfo valueOfChar = typeof(global::java.lang.Character).GetMethod("valueOf", new Type[] { typeof(char) });
			private static readonly MethodInfo valueOfShort = typeof(global::java.lang.Short).GetMethod("valueOf", new Type[] { typeof(short) });
			private static readonly MethodInfo valueOfInt = typeof(global::java.lang.Integer).GetMethod("valueOf", new Type[] { typeof(int) });
			private static readonly MethodInfo valueOfFloat = typeof(global::java.lang.Float).GetMethod("valueOf", new Type[] { typeof(float) });
			private static readonly MethodInfo valueOfLong = typeof(global::java.lang.Long).GetMethod("valueOf", new Type[] { typeof(long) });
			private static readonly MethodInfo valueOfDouble = typeof(global::java.lang.Double).GetMethod("valueOf", new Type[] { typeof(double) });
			private static readonly MethodInfo byteValue = typeof(global::java.lang.Byte).GetMethod("byteValue", Type.EmptyTypes);
			private static readonly MethodInfo booleanValue = typeof(global::java.lang.Boolean).GetMethod("booleanValue", Type.EmptyTypes);
			private static readonly MethodInfo charValue = typeof(global::java.lang.Character).GetMethod("charValue", Type.EmptyTypes);
			private static readonly MethodInfo shortValue = typeof(global::java.lang.Short).GetMethod("shortValue", Type.EmptyTypes);
			private static readonly MethodInfo intValue = typeof(global::java.lang.Integer).GetMethod("intValue", Type.EmptyTypes);
			private static readonly MethodInfo floatValue = typeof(global::java.lang.Float).GetMethod("floatValue", Type.EmptyTypes);
			private static readonly MethodInfo longValue = typeof(global::java.lang.Long).GetMethod("longValue", Type.EmptyTypes);
			private static readonly MethodInfo doubleValue = typeof(global::java.lang.Double).GetMethod("doubleValue", Type.EmptyTypes);

			internal static void EmitUnboxArg(CodeEmitter ilgen, TypeWrapper type)
			{
				if (type == PrimitiveTypeWrapper.BYTE)
				{
					ilgen.Emit(OpCodes.Castclass, typeof(global::java.lang.Byte));
					ilgen.Emit(OpCodes.Call, byteValue);
				}
				else if (type == PrimitiveTypeWrapper.BOOLEAN)
				{
					ilgen.Emit(OpCodes.Castclass, typeof(global::java.lang.Boolean));
					ilgen.Emit(OpCodes.Call, booleanValue);
				}
				else if (type == PrimitiveTypeWrapper.CHAR)
				{
					ilgen.Emit(OpCodes.Castclass, typeof(global::java.lang.Character));
					ilgen.Emit(OpCodes.Call, charValue);
				}
				else if (type == PrimitiveTypeWrapper.SHORT
					|| type == PrimitiveTypeWrapper.INT
					|| type == PrimitiveTypeWrapper.FLOAT
					|| type == PrimitiveTypeWrapper.LONG
					|| type == PrimitiveTypeWrapper.DOUBLE)
				{
					ilgen.Emit(OpCodes.Dup);
					ilgen.Emit(OpCodes.Isinst, typeof(global::java.lang.Byte));
					CodeEmitterLabel next = ilgen.DefineLabel();
					ilgen.EmitBrfalse(next);
					ilgen.Emit(OpCodes.Castclass, typeof(global::java.lang.Byte));
					ilgen.Emit(OpCodes.Call, byteValue);
					ilgen.Emit(OpCodes.Conv_I1);
					Expand(ilgen, type);
					CodeEmitterLabel done = ilgen.DefineLabel();
					ilgen.EmitBr(done);
					ilgen.MarkLabel(next);
					if (type == PrimitiveTypeWrapper.SHORT)
					{
						ilgen.Emit(OpCodes.Castclass, typeof(global::java.lang.Short));
						ilgen.Emit(OpCodes.Call, shortValue);
					}
					else
					{
						ilgen.Emit(OpCodes.Dup);
						ilgen.Emit(OpCodes.Isinst, typeof(global::java.lang.Short));
						next = ilgen.DefineLabel();
						ilgen.EmitBrfalse(next);
						ilgen.Emit(OpCodes.Castclass, typeof(global::java.lang.Short));
						ilgen.Emit(OpCodes.Call, shortValue);
						Expand(ilgen, type);
						ilgen.EmitBr(done);
						ilgen.MarkLabel(next);
						ilgen.Emit(OpCodes.Dup);
						ilgen.Emit(OpCodes.Isinst, typeof(global::java.lang.Character));
						next = ilgen.DefineLabel();
						ilgen.EmitBrfalse(next);
						ilgen.Emit(OpCodes.Castclass, typeof(global::java.lang.Character));
						ilgen.Emit(OpCodes.Call, charValue);
						Expand(ilgen, type);
						ilgen.EmitBr(done);
						ilgen.MarkLabel(next);
						if (type == PrimitiveTypeWrapper.INT)
						{
							ilgen.Emit(OpCodes.Castclass, typeof(global::java.lang.Integer));
							ilgen.Emit(OpCodes.Call, intValue);
						}
						else
						{
							ilgen.Emit(OpCodes.Dup);
							ilgen.Emit(OpCodes.Isinst, typeof(global::java.lang.Integer));
							next = ilgen.DefineLabel();
							ilgen.EmitBrfalse(next);
							ilgen.Emit(OpCodes.Castclass, typeof(global::java.lang.Integer));
							ilgen.Emit(OpCodes.Call, intValue);
							Expand(ilgen, type);
							ilgen.EmitBr(done);
							ilgen.MarkLabel(next);
							if (type == PrimitiveTypeWrapper.LONG)
							{
								ilgen.Emit(OpCodes.Castclass, typeof(global::java.lang.Long));
								ilgen.Emit(OpCodes.Call, longValue);
							}
							else
							{
								ilgen.Emit(OpCodes.Dup);
								ilgen.Emit(OpCodes.Isinst, typeof(global::java.lang.Long));
								next = ilgen.DefineLabel();
								ilgen.EmitBrfalse(next);
								ilgen.Emit(OpCodes.Castclass, typeof(global::java.lang.Long));
								ilgen.Emit(OpCodes.Call, longValue);
								Expand(ilgen, type);
								ilgen.EmitBr(done);
								ilgen.MarkLabel(next);
								if (type == PrimitiveTypeWrapper.FLOAT)
								{
									ilgen.Emit(OpCodes.Castclass, typeof(global::java.lang.Float));
									ilgen.Emit(OpCodes.Call, floatValue);
								}
								else if (type == PrimitiveTypeWrapper.DOUBLE)
								{
									ilgen.Emit(OpCodes.Dup);
									ilgen.Emit(OpCodes.Isinst, typeof(global::java.lang.Float));
									next = ilgen.DefineLabel();
									ilgen.EmitBrfalse(next);
									ilgen.Emit(OpCodes.Castclass, typeof(global::java.lang.Float));
									ilgen.Emit(OpCodes.Call, floatValue);
									ilgen.EmitBr(done);
									ilgen.MarkLabel(next);
									ilgen.Emit(OpCodes.Castclass, typeof(global::java.lang.Double));
									ilgen.Emit(OpCodes.Call, doubleValue);
								}
								else
								{
									throw new InvalidOperationException();
								}
							}
						}
					}
					ilgen.MarkLabel(done);
				}
				else
				{
					type.EmitCheckcast(ilgen);
				}
			}

			internal static void BoxReturnValue(CodeEmitter ilgen, TypeWrapper type)
			{
				if (type == PrimitiveTypeWrapper.VOID)
				{
					ilgen.Emit(OpCodes.Ldnull);
				}
				else if (type == PrimitiveTypeWrapper.BYTE)
				{
					ilgen.Emit(OpCodes.Call, valueOfByte);
				}
				else if (type == PrimitiveTypeWrapper.BOOLEAN)
				{
					ilgen.Emit(OpCodes.Call, valueOfBoolean);
				}
				else if (type == PrimitiveTypeWrapper.CHAR)
				{
					ilgen.Emit(OpCodes.Call, valueOfChar);
				}
				else if (type == PrimitiveTypeWrapper.SHORT)
				{
					ilgen.Emit(OpCodes.Call, valueOfShort);
				}
				else if (type == PrimitiveTypeWrapper.INT)
				{
					ilgen.Emit(OpCodes.Call, valueOfInt);
				}
				else if (type == PrimitiveTypeWrapper.FLOAT)
				{
					ilgen.Emit(OpCodes.Call, valueOfFloat);
				}
				else if (type == PrimitiveTypeWrapper.LONG)
				{
					ilgen.Emit(OpCodes.Call, valueOfLong);
				}
				else if (type == PrimitiveTypeWrapper.DOUBLE)
				{
					ilgen.Emit(OpCodes.Call, valueOfDouble);
				}
			}

			private static void Expand(CodeEmitter ilgen, TypeWrapper type)
			{
				if (type == PrimitiveTypeWrapper.FLOAT)
				{
					ilgen.Emit(OpCodes.Conv_R4);
				}
				else if (type == PrimitiveTypeWrapper.LONG)
				{
					ilgen.Emit(OpCodes.Conv_I8);
				}
				else if (type == PrimitiveTypeWrapper.DOUBLE)
				{
					ilgen.Emit(OpCodes.Conv_R8);
				}
			}
		}

		private sealed class FastMethodAccessorImpl : global::sun.reflect.MethodAccessor
		{
			internal static readonly ConstructorInfo invocationTargetExceptionCtor;
			internal static readonly ConstructorInfo illegalArgumentExceptionCtor;
			internal static readonly MethodInfo get_TargetSite;
			internal static readonly MethodInfo GetCurrentMethod;

			private delegate object Invoker(object obj, object[] args, global::ikvm.@internal.CallerID callerID);
			private Invoker invoker;

			static FastMethodAccessorImpl()
			{
				invocationTargetExceptionCtor = typeof(global::java.lang.reflect.InvocationTargetException).GetConstructor(new Type[] { typeof(Exception) });
				illegalArgumentExceptionCtor = typeof(global::java.lang.IllegalArgumentException).GetConstructor(Type.EmptyTypes);
				get_TargetSite = typeof(Exception).GetMethod("get_TargetSite");
				GetCurrentMethod = typeof(MethodBase).GetMethod("GetCurrentMethod");
			}

			private sealed class RunClassInit
			{
				private FastMethodAccessorImpl outer;
				private TypeWrapper tw;
				private Invoker invoker;

				internal RunClassInit(FastMethodAccessorImpl outer, TypeWrapper tw, Invoker invoker)
				{
					this.outer = outer;
					this.tw = tw;
					this.invoker = invoker;
				}

				[IKVM.Attributes.HideFromJava]
				internal object invoke(object obj, object[] args, global::ikvm.@internal.CallerID callerID)
				{
					// FXBUG pre-SP1 a DynamicMethod that calls a static method doesn't trigger the cctor, so we do that explicitly.
					// even on .NET 2.0 SP2, interface method invocations don't run the interface cctor
					// NOTE when testing, please test both the x86 and x64 CLR JIT, because they have different bugs (even on .NET 2.0 SP2)
					tw.RunClassInit();
					outer.invoker = invoker;
					return invoker(obj, args, callerID);
				}
			}

			internal FastMethodAccessorImpl(MethodWrapper mw)
			{
				TypeWrapper[] parameters;
				try
				{
					mw.DeclaringType.Finish();
					parameters = mw.GetParameters();
					for (int i = 0; i < parameters.Length; i++)
					{
						// the EnsureLoadable shouldn't fail, because we don't allow a global::java.lang.reflect.Method
						// to "escape" if it has an unloadable type in the signature
						parameters[i] = parameters[i].EnsureLoadable(mw.DeclaringType.GetClassLoader());
						parameters[i].Finish();
					}
				}
				catch (RetargetableJavaException x)
				{
					throw x.ToJava();
				}
				mw.ResolveMethod();
				DynamicMethod dm = DynamicMethodUtils.Create("___Invoker_", mw.DeclaringType.TypeAsBaseType, !mw.IsPublic || !mw.DeclaringType.IsPublic, typeof(object), new Type[] { typeof(object), typeof(object[]), typeof(global::ikvm.@internal.CallerID) });
				CodeEmitter ilgen = CodeEmitter.Create(dm);
				CodeEmitterLocal ret = ilgen.DeclareLocal(typeof(object));
				if (!mw.IsStatic)
				{
					// check target for null
					ilgen.Emit(OpCodes.Ldarg_0);
					ilgen.EmitNullCheck();
				}

				// check args length
				CodeEmitterLabel argsLengthOK = ilgen.DefineLabel();
				if (parameters.Length == 0)
				{
					// zero length array may be null
					ilgen.Emit(OpCodes.Ldarg_1);
					ilgen.EmitBrfalse(argsLengthOK);
				}
				ilgen.Emit(OpCodes.Ldarg_1);
				ilgen.Emit(OpCodes.Ldlen);
				ilgen.EmitLdc_I4(parameters.Length);
				ilgen.EmitBeq(argsLengthOK);
				ilgen.Emit(OpCodes.Newobj, illegalArgumentExceptionCtor);
				ilgen.Emit(OpCodes.Throw);
				ilgen.MarkLabel(argsLengthOK);

				int thisCount = mw.IsStatic ? 0 : 1;
				CodeEmitterLocal[] args = new CodeEmitterLocal[parameters.Length + thisCount];
				if (!mw.IsStatic)
				{
					args[0] = ilgen.DeclareLocal(mw.DeclaringType.TypeAsSignatureType);
				}
				for (int i = thisCount; i < args.Length; i++)
				{
					args[i] = ilgen.DeclareLocal(parameters[i - thisCount].TypeAsSignatureType);
				}
				ilgen.BeginExceptionBlock();
				if (!mw.IsStatic)
				{
					ilgen.Emit(OpCodes.Ldarg_0);
					mw.DeclaringType.EmitCheckcast(ilgen);
					mw.DeclaringType.EmitConvStackTypeToSignatureType(ilgen, null);
					ilgen.Emit(OpCodes.Stloc, args[0]);
				}
				for (int i = thisCount; i < args.Length; i++)
				{
					ilgen.Emit(OpCodes.Ldarg_1);
					ilgen.EmitLdc_I4(i - thisCount);
					ilgen.Emit(OpCodes.Ldelem_Ref);
					TypeWrapper tw = parameters[i - thisCount];
					BoxUtil.EmitUnboxArg(ilgen, tw);
					tw.EmitConvStackTypeToSignatureType(ilgen, null);
					ilgen.Emit(OpCodes.Stloc, args[i]);
				}
				CodeEmitterLabel label1 = ilgen.DefineLabel();
				ilgen.EmitLeave(label1);
				ilgen.BeginCatchBlock(typeof(InvalidCastException));
				ilgen.Emit(OpCodes.Newobj, illegalArgumentExceptionCtor);
				ilgen.Emit(OpCodes.Throw);
				ilgen.BeginCatchBlock(typeof(NullReferenceException));
				ilgen.Emit(OpCodes.Newobj, illegalArgumentExceptionCtor);
				ilgen.Emit(OpCodes.Throw);
				ilgen.EndExceptionBlock();

				// this is the actual call
				ilgen.MarkLabel(label1);
				ilgen.BeginExceptionBlock();
				for (int i = 0; i < args.Length; i++)
				{
					if (i == 0 && !mw.IsStatic && (mw.DeclaringType.IsNonPrimitiveValueType || mw.DeclaringType.IsGhost))
					{
						ilgen.Emit(OpCodes.Ldloca, args[i]);
					}
					else
					{
						ilgen.Emit(OpCodes.Ldloc, args[i]);
					}
				}
				if (mw.HasCallerID)
				{
					ilgen.Emit(OpCodes.Ldarg_2);
				}
				if (mw.IsStatic)
				{
					mw.EmitCall(ilgen);
				}
				else
				{
					mw.EmitCallvirtReflect(ilgen);
				}
				mw.ReturnType.EmitConvSignatureTypeToStackType(ilgen);
				BoxUtil.BoxReturnValue(ilgen, mw.ReturnType);
				ilgen.Emit(OpCodes.Stloc, ret);
				CodeEmitterLabel label2 = ilgen.DefineLabel();
				ilgen.EmitLeave(label2);
				ilgen.BeginCatchBlock(typeof(Exception));
				CodeEmitterLabel label = ilgen.DefineLabel();
				CodeEmitterLabel labelWrap = ilgen.DefineLabel();
				// If the exception we caught is a global::java.lang.reflect.InvocationTargetException, we know it must be
				// wrapped, because .NET won't throw that exception and we also cannot check the target site,
				// because it may be the same as us if a method is recursively invoking itself.
				ilgen.Emit(OpCodes.Dup);
				ilgen.Emit(OpCodes.Isinst, typeof(global::java.lang.reflect.InvocationTargetException));
				ilgen.EmitBrtrue(labelWrap);
				ilgen.Emit(OpCodes.Dup);
				ilgen.Emit(OpCodes.Callvirt, get_TargetSite);
				ilgen.Emit(OpCodes.Call, GetCurrentMethod);
				ilgen.Emit(OpCodes.Ceq);
				ilgen.EmitBrtrue(label);
				ilgen.MarkLabel(labelWrap);
				ilgen.Emit(OpCodes.Ldc_I4_0);
				ilgen.Emit(OpCodes.Call, ByteCodeHelperMethods.mapException.MakeGenericMethod(Types.Exception));
				ilgen.Emit(OpCodes.Newobj, invocationTargetExceptionCtor);
				ilgen.MarkLabel(label);
				ilgen.Emit(OpCodes.Throw);
				ilgen.EndExceptionBlock();

				ilgen.MarkLabel(label2);
				ilgen.Emit(OpCodes.Ldloc, ret);
				ilgen.Emit(OpCodes.Ret);
				ilgen.DoEmit();
				invoker = (Invoker)dm.CreateDelegate(typeof(Invoker));
				if ((mw.IsStatic || mw.DeclaringType.IsInterface) && mw.DeclaringType.HasStaticInitializer)
				{
					invoker = new Invoker(new RunClassInit(this, mw.DeclaringType, invoker).invoke);
				}
			}

			[IKVM.Attributes.HideFromJava]
			public object invoke(object obj, object[] args, global::ikvm.@internal.CallerID callerID)
			{
				try
				{
					return invoker(obj, args, callerID);
				}
				catch (MethodAccessException x)
				{
					// this can happen if we're calling a non-public method and the call stack doesn't have ReflectionPermission.MemberAccess
					throw new global::java.lang.IllegalAccessException().initCause(x);
				}
			}
		}

		private sealed class FastConstructorAccessorImpl : global::sun.reflect.ConstructorAccessor
		{
			private delegate object Invoker(object[] args);
			private Invoker invoker;

			internal FastConstructorAccessorImpl(global::java.lang.reflect.Constructor constructor)
			{
				MethodWrapper mw = MethodWrapper.FromExecutable(constructor);
				TypeWrapper[] parameters;
				try
				{
					mw.DeclaringType.Finish();
					parameters = mw.GetParameters();
					for (int i = 0; i < parameters.Length; i++)
					{
						// the EnsureLoadable shouldn't fail, because we don't allow a global::java.lang.reflect.Method
						// to "escape" if it has an unloadable type in the signature
						parameters[i] = parameters[i].EnsureLoadable(mw.DeclaringType.GetClassLoader());
						parameters[i].Finish();
					}
				}
				catch (RetargetableJavaException x)
				{
					throw x.ToJava();
				}
				mw.ResolveMethod();
				DynamicMethod dm = DynamicMethodUtils.Create("___Invoker_", mw.DeclaringType.TypeAsTBD, !mw.IsPublic || !mw.DeclaringType.IsPublic, typeof(object), new Type[] { typeof(object[]) });
				CodeEmitter ilgen = CodeEmitter.Create(dm);
				CodeEmitterLocal ret = ilgen.DeclareLocal(typeof(object));

				// check args length
				CodeEmitterLabel argsLengthOK = ilgen.DefineLabel();
				if (parameters.Length == 0)
				{
					// zero length array may be null
					ilgen.Emit(OpCodes.Ldarg_0);
					ilgen.EmitBrfalse(argsLengthOK);
				}
				ilgen.Emit(OpCodes.Ldarg_0);
				ilgen.Emit(OpCodes.Ldlen);
				ilgen.EmitLdc_I4(parameters.Length);
				ilgen.EmitBeq(argsLengthOK);
				ilgen.Emit(OpCodes.Newobj, FastMethodAccessorImpl.illegalArgumentExceptionCtor);
				ilgen.Emit(OpCodes.Throw);
				ilgen.MarkLabel(argsLengthOK);

				CodeEmitterLocal[] args = new CodeEmitterLocal[parameters.Length];
				for (int i = 0; i < args.Length; i++)
				{
					args[i] = ilgen.DeclareLocal(parameters[i].TypeAsSignatureType);
				}
				ilgen.BeginExceptionBlock();
				for (int i = 0; i < args.Length; i++)
				{
					ilgen.Emit(OpCodes.Ldarg_0);
					ilgen.EmitLdc_I4(i);
					ilgen.Emit(OpCodes.Ldelem_Ref);
					TypeWrapper tw = parameters[i];
					BoxUtil.EmitUnboxArg(ilgen, tw);
					tw.EmitConvStackTypeToSignatureType(ilgen, null);
					ilgen.Emit(OpCodes.Stloc, args[i]);
				}
				CodeEmitterLabel label1 = ilgen.DefineLabel();
				ilgen.EmitLeave(label1);
				ilgen.BeginCatchBlock(typeof(InvalidCastException));
				ilgen.Emit(OpCodes.Newobj, FastMethodAccessorImpl.illegalArgumentExceptionCtor);
				ilgen.Emit(OpCodes.Throw);
				ilgen.BeginCatchBlock(typeof(NullReferenceException));
				ilgen.Emit(OpCodes.Newobj, FastMethodAccessorImpl.illegalArgumentExceptionCtor);
				ilgen.Emit(OpCodes.Throw);
				ilgen.EndExceptionBlock();

				// this is the actual call
				ilgen.MarkLabel(label1);
				ilgen.BeginExceptionBlock();
				for (int i = 0; i < args.Length; i++)
				{
					ilgen.Emit(OpCodes.Ldloc, args[i]);
				}
				mw.EmitNewobj(ilgen);
				ilgen.Emit(OpCodes.Stloc, ret);
				CodeEmitterLabel label2 = ilgen.DefineLabel();
				ilgen.EmitLeave(label2);
				ilgen.BeginCatchBlock(typeof(Exception));
				ilgen.Emit(OpCodes.Dup);
				ilgen.Emit(OpCodes.Callvirt, FastMethodAccessorImpl.get_TargetSite);
				ilgen.Emit(OpCodes.Call, FastMethodAccessorImpl.GetCurrentMethod);
				ilgen.Emit(OpCodes.Ceq);
				CodeEmitterLabel label = ilgen.DefineLabel();
				ilgen.EmitBrtrue(label);
				ilgen.Emit(OpCodes.Ldc_I4_0);
				ilgen.Emit(OpCodes.Call, ByteCodeHelperMethods.mapException.MakeGenericMethod(Types.Exception));
				ilgen.Emit(OpCodes.Newobj, FastMethodAccessorImpl.invocationTargetExceptionCtor);
				ilgen.MarkLabel(label);
				ilgen.Emit(OpCodes.Throw);
				ilgen.EndExceptionBlock();

				ilgen.MarkLabel(label2);
				ilgen.Emit(OpCodes.Ldloc, ret);
				ilgen.Emit(OpCodes.Ret);
				ilgen.DoEmit();
				invoker = (Invoker)dm.CreateDelegate(typeof(Invoker));
			}

			[IKVM.Attributes.HideFromJava]
			public object newInstance(object[] args)
			{
				try
				{
					return invoker(args);
				}
				catch (MethodAccessException x)
				{
					// this can happen if we're calling a non-public method and the call stack doesn't have ReflectionPermission.MemberAccess
					throw new global::java.lang.IllegalAccessException().initCause(x);
				}
			}
		}

		private sealed class FastSerializationConstructorAccessorImpl : global::sun.reflect.ConstructorAccessor
		{
			private static readonly MethodInfo GetTypeFromHandleMethod = typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });
			private static readonly MethodInfo GetUninitializedObjectMethod = typeof(FormatterServices).GetMethod("GetUninitializedObject", new Type[] { typeof(Type) });
			private delegate object InvokeCtor();
			private InvokeCtor invoker;

			internal FastSerializationConstructorAccessorImpl(global::java.lang.reflect.Constructor constructorToCall, global::java.lang.Class classToInstantiate)
			{
				MethodWrapper constructor = MethodWrapper.FromExecutable(constructorToCall);
				if (constructor.GetParameters().Length != 0)
				{
					throw new NotImplementedException("Serialization constructor cannot have parameters");
				}
				constructor.Link();
				constructor.ResolveMethod();
				Type type;
				try
				{
					TypeWrapper wrapper = TypeWrapper.FromClass(classToInstantiate);
					wrapper.Finish();
					type = wrapper.TypeAsBaseType;
				}
				catch (RetargetableJavaException x)
				{
					throw x.ToJava();
				}
				DynamicMethod dm = DynamicMethodUtils.Create("___SerializationCtor_", constructor.DeclaringType.TypeAsBaseType, true, typeof(object), null);
				CodeEmitter ilgen = CodeEmitter.Create(dm);
				ilgen.Emit(OpCodes.Ldtoken, type);
				ilgen.Emit(OpCodes.Call, GetTypeFromHandleMethod);
				ilgen.Emit(OpCodes.Call, GetUninitializedObjectMethod);
				ilgen.Emit(OpCodes.Dup);
				constructor.EmitCall(ilgen);
				ilgen.Emit(OpCodes.Ret);
				ilgen.DoEmit();
				invoker = (InvokeCtor)dm.CreateDelegate(typeof(InvokeCtor));
			}

			[IKVM.Attributes.HideFromJava]
			public object newInstance(object[] args)
			{
				try
				{
					return invoker();
				}
				catch (MethodAccessException x)
				{
					// this can happen if we're calling a non-public method and the call stack doesn't have ReflectionPermission.MemberAccess
					throw new global::java.lang.IllegalAccessException().initCause(x);
				}
			}
		}
#endif // !NO_REF_EMIT

		sealed class ActivatorConstructorAccessor : global::sun.reflect.ConstructorAccessor
		{
			private readonly Type type;

			internal ActivatorConstructorAccessor(MethodWrapper mw)
			{
				this.type = mw.DeclaringType.TypeAsBaseType;
			}

			public object newInstance(object[] objarr)
			{
				if (objarr != null && objarr.Length != 0)
				{
					throw new global::java.lang.IllegalArgumentException();
				}
				try
				{
					return Activator.CreateInstance(type);
				}
				catch (TargetInvocationException x)
				{
					throw new global::java.lang.reflect.InvocationTargetException(global::ikvm.runtime.Util.mapException(x.InnerException));
				}
			}

			internal static bool IsSuitable(MethodWrapper mw)
			{
				MethodBase mb = mw.GetMethod();
				return mb != null
					&& mb.IsConstructor
					&& mb.IsPublic
					&& mb.DeclaringType.IsPublic
					&& mb.DeclaringType == mw.DeclaringType.TypeAsBaseType
					&& mb.GetParameters().Length == 0;
			}
		}

		private abstract class FieldAccessorImplBase : global::sun.reflect.FieldAccessor, IReflectionException
		{
			protected static readonly ushort inflationThreshold = 15;
			protected readonly FieldWrapper fw;
			protected readonly bool isFinal;
			protected ushort numInvocations;

			static FieldAccessorImplBase()
			{
				string str = global::java.lang.Props.props.getProperty("ikvm.reflect.field.inflationThreshold");
				int value;
				if (str != null && int.TryParse(str, out value))
				{
					if (value >= ushort.MinValue && value <= ushort.MaxValue)
					{
						inflationThreshold = (ushort)value;
					}
				}
			}

			private FieldAccessorImplBase(FieldWrapper fw, bool isFinal)
			{
				this.fw = fw;
				this.isFinal = isFinal;
			}

			private string GetQualifiedFieldName()
			{
				return fw.DeclaringType.Name + "." + fw.Name;
			}

			private string GetFieldTypeName()
			{
				return fw.FieldTypeWrapper.IsPrimitive
					? fw.FieldTypeWrapper.ClassObject.getName()
					: fw.FieldTypeWrapper.Name;
			}

			public global::java.lang.IllegalArgumentException GetIllegalArgumentException(object obj)
			{
				// LAME like JDK 6 we return the wrong exception message (talking about setting the field, instead of getting)
				return SetIllegalArgumentException(obj);
			}

			public global::java.lang.IllegalArgumentException SetIllegalArgumentException(object obj)
			{
				// LAME like JDK 6 we return the wrong exception message (when obj is the object, instead of the value)
				return SetIllegalArgumentException(obj != null ? global::ikvm.runtime.Util.getClassFromObject(obj).getName() : "", "");
			}

			private global::java.lang.IllegalArgumentException SetIllegalArgumentException(string attemptedType, string attemptedValue)
			{
				return new global::java.lang.IllegalArgumentException(GetSetMessage(attemptedType, attemptedValue));
			}

			protected global::java.lang.IllegalAccessException FinalFieldIllegalAccessException(object obj)
			{
				return FinalFieldIllegalAccessException(obj != null ? global::ikvm.runtime.Util.getClassFromObject(obj).getName() : "", "");
			}

			private global::java.lang.IllegalAccessException FinalFieldIllegalAccessException(string attemptedType, string attemptedValue)
			{
				return new global::java.lang.IllegalAccessException(GetSetMessage(attemptedType, attemptedValue));
			}

			private global::java.lang.IllegalArgumentException GetIllegalArgumentException(string type)
			{
				return new global::java.lang.IllegalArgumentException("Attempt to get " + GetFieldTypeName() + " field \"" + GetQualifiedFieldName() + "\" with illegal data type conversion to " + type);
			}

			// this message comes from global::sun.reflect.UnsafeFieldAccessorImpl
			private string GetSetMessage(String attemptedType, String attemptedValue)
			{
				String err = "Can not set";
				if (fw.IsStatic)
					err += " static";
				if (isFinal)
					err += " final";
				err += " " + GetFieldTypeName() + " field " + GetQualifiedFieldName() + " to ";
				if (attemptedValue.Length > 0)
				{
					err += "(" + attemptedType + ")" + attemptedValue;
				}
				else
				{
					if (attemptedType.Length > 0)
						err += attemptedType;
					else
						err += "null value";
				}
				return err;
			}

			public virtual bool getBoolean(object obj)
			{
				throw GetIllegalArgumentException("boolean");
			}

			public virtual byte getByte(object obj)
			{
				throw GetIllegalArgumentException("byte");
			}

			public virtual char getChar(object obj)
			{
				throw GetIllegalArgumentException("char");
			}

			public virtual short getShort(object obj)
			{
				throw GetIllegalArgumentException("short");
			}

			public virtual int getInt(object obj)
			{
				throw GetIllegalArgumentException("int");
			}

			public virtual long getLong(object obj)
			{
				throw GetIllegalArgumentException("long");
			}

			public virtual float getFloat(object obj)
			{
				throw GetIllegalArgumentException("float");
			}

			public virtual double getDouble(object obj)
			{
				throw GetIllegalArgumentException("double");
			}

			public virtual void setBoolean(object obj, bool z)
			{
				throw SetIllegalArgumentException("boolean", global::java.lang.Boolean.toString(z));
			}

			public virtual void setByte(object obj, byte b)
			{
				throw SetIllegalArgumentException("byte", global::java.lang.Byte.toString(b));
			}

			public virtual void setChar(object obj, char c)
			{
				throw SetIllegalArgumentException("char", global::java.lang.Character.toString(c));
			}

			public virtual void setShort(object obj, short s)
			{
				throw SetIllegalArgumentException("short", global::java.lang.Short.toString(s));
			}

			public virtual void setInt(object obj, int i)
			{
				throw SetIllegalArgumentException("int", global::java.lang.Integer.toString(i));
			}

			public virtual void setLong(object obj, long l)
			{
				throw SetIllegalArgumentException("long", global::java.lang.Long.toString(l));
			}

			public virtual void setFloat(object obj, float f)
			{
				throw SetIllegalArgumentException("float", global::java.lang.Float.toString(f));
			}

			public virtual void setDouble(object obj, double d)
			{
				throw SetIllegalArgumentException("double", global::java.lang.Double.toString(d));
			}

			public abstract object get(object obj);
			public abstract void set(object obj, object value);

			private abstract class FieldAccessor<T> : FieldAccessorImplBase
			{
				protected delegate void Setter(object obj, T value, FieldAccessor<T> acc);
				protected delegate T Getter(object obj, FieldAccessor<T> acc);
				private static readonly Setter initialSetter = lazySet;
				private static readonly Getter initialGetter = lazyGet;
				protected Setter setter = initialSetter;
				protected Getter getter = initialGetter;

				internal FieldAccessor(FieldWrapper fw, bool isFinal)
					: base(fw, isFinal)
				{
					if (!IsSlowPathCompatible(fw))
					{
						// prevent slow path
						numInvocations = inflationThreshold;
					}
				}

				private bool IsSlowPathCompatible(FieldWrapper fw)
				{
#if !NO_REF_EMIT
					if (fw.IsVolatile && (fw.FieldTypeWrapper == PrimitiveTypeWrapper.LONG || fw.FieldTypeWrapper == PrimitiveTypeWrapper.DOUBLE))
					{
						return false;
					}
#endif
					fw.Link();
					return true;
				}

				private static T lazyGet(object obj, FieldAccessor<T> acc)
				{
					return acc.lazyGet(obj);
				}

				private static void lazySet(object obj, T value, FieldAccessor<T> acc)
				{
					acc.lazySet(obj, value);
				}

				private T lazyGet(object obj)
				{
#if !NO_REF_EMIT
					if (numInvocations >= inflationThreshold)
					{
						// FXBUG it appears that a ldsfld/stsfld in a DynamicMethod doesn't trigger the class constructor
						// and if we didn't use the slow path, we haven't yet initialized the class
						fw.DeclaringType.RunClassInit();
						getter = (Getter)GenerateFastGetter(typeof(Getter), typeof(T), fw);
						return getter(obj, this);
					}
#endif // !NO_REF_EMIT
					if (fw.IsStatic)
					{
						obj = null;
					}
					else if (obj == null)
					{
						throw new global::java.lang.NullPointerException();
					}
					else if (!fw.DeclaringType.IsInstance(obj))
					{
						throw GetIllegalArgumentException(obj);
					}
					else if (fw.DeclaringType.IsRemapped && !fw.DeclaringType.TypeAsBaseType.IsInstanceOfType(obj))
					{
						throw GetUnsupportedRemappedFieldException(obj);
					}
					if (numInvocations == 0)
					{
						fw.DeclaringType.RunClassInit();
						fw.DeclaringType.Finish();
						fw.ResolveField();
					}
					numInvocations++;
					return (T)fw.FieldTypeWrapper.GhostUnwrap(fw.GetValue(obj));
				}

				private void lazySet(object obj, T value)
				{
					if (isFinal)
					{
						// for some reason Java runs class initialization before checking if the field is final
						fw.DeclaringType.RunClassInit();
						throw FinalFieldIllegalAccessException(JavaBox(value));
					}
#if !NO_REF_EMIT
					if (numInvocations >= inflationThreshold)
					{
						// FXBUG it appears that a ldsfld/stsfld in a DynamicMethod doesn't trigger the class constructor
						// and if we didn't use the slow path, we haven't yet initialized the class
						fw.DeclaringType.RunClassInit();
						setter = (Setter)GenerateFastSetter(typeof(Setter), typeof(T), fw);
						setter(obj, value, this);
						return;
					}
#endif // !NO_REF_EMIT
					if (fw.IsStatic)
					{
						obj = null;
					}
					else if (obj == null)
					{
						throw new global::java.lang.NullPointerException();
					}
					else if (!fw.DeclaringType.IsInstance(obj))
					{
						throw SetIllegalArgumentException(obj);
					}
					else if (fw.DeclaringType.IsRemapped && !fw.DeclaringType.TypeAsBaseType.IsInstanceOfType(obj))
					{
						throw GetUnsupportedRemappedFieldException(obj);
					}
					CheckValue(value);
					if (numInvocations == 0)
					{
						fw.DeclaringType.RunClassInit();
						fw.DeclaringType.Finish();
						fw.ResolveField();
					}
					numInvocations++;
					fw.SetValue(obj, fw.FieldTypeWrapper.GhostWrap(value));
				}

				private Exception GetUnsupportedRemappedFieldException(object obj)
				{
					return new global::java.lang.IllegalAccessException("Accessing field " + fw.DeclaringType.Name + "." + fw.Name + " in an object of type " + global::ikvm.runtime.Util.getClassFromObject(obj).getName() + " is not supported");
				}

				protected virtual void CheckValue(T value)
				{
				}

				protected abstract object JavaBox(T value);
			}

			private sealed class ByteField : FieldAccessor<byte>
			{
				internal ByteField(FieldWrapper field, bool isFinal)
					: base(field, isFinal)
				{
				}

				public sealed override short getShort(object obj)
				{
					return (sbyte)getByte(obj);
				}

				public sealed override int getInt(object obj)
				{
					return (sbyte)getByte(obj);
				}

				public sealed override long getLong(object obj)
				{
					return (sbyte)getByte(obj);
				}

				public sealed override float getFloat(object obj)
				{
					return (sbyte)getByte(obj);
				}

				public sealed override double getDouble(object obj)
				{
					return (sbyte)getByte(obj);
				}

				public sealed override object get(object obj)
				{
					return global::java.lang.Byte.valueOf(getByte(obj));
				}

				public sealed override void set(object obj, object val)
				{
					if (!(val is global::java.lang.Byte))
					{
						throw SetIllegalArgumentException(val);
					}
					setByte(obj, ((global::java.lang.Byte)val).byteValue());
				}

				public sealed override byte getByte(object obj)
				{
					try
					{
						return getter(obj, this);
					}
					catch (FieldAccessException x)
					{
						throw new global::java.lang.IllegalAccessException().initCause(x);
					}
				}

				public sealed override void setByte(object obj, byte value)
				{
					try
					{
						setter(obj, value, this);
					}
					catch (FieldAccessException x)
					{
						throw new global::java.lang.IllegalAccessException().initCause(x);
					}
				}

				protected sealed override object JavaBox(byte value)
				{
					return global::java.lang.Byte.valueOf(value);
				}
			}

			private sealed class BooleanField : FieldAccessor<bool>
			{
				internal BooleanField(FieldWrapper field, bool isFinal)
					: base(field, isFinal)
				{
				}

				public sealed override object get(object obj)
				{
					return global::java.lang.Boolean.valueOf(getBoolean(obj));
				}

				public sealed override void set(object obj, object val)
				{
					if (!(val is global::java.lang.Boolean))
					{
						throw SetIllegalArgumentException(val);
					}
					setBoolean(obj, ((global::java.lang.Boolean)val).booleanValue());
				}

				public sealed override bool getBoolean(object obj)
				{
					try
					{
						return getter(obj, this);
					}
					catch (FieldAccessException x)
					{
						throw new global::java.lang.IllegalAccessException().initCause(x);
					}
				}

				public sealed override void setBoolean(object obj, bool value)
				{
					try
					{
						setter(obj, value, this);
					}
					catch (FieldAccessException x)
					{
						throw new global::java.lang.IllegalAccessException().initCause(x);
					}
				}

				protected sealed override object JavaBox(bool value)
				{
					return global::java.lang.Boolean.valueOf(value);
				}
			}

			private sealed class CharField : FieldAccessor<char>
			{
				internal CharField(FieldWrapper field, bool isFinal)
					: base(field, isFinal)
				{
				}

				public sealed override int getInt(object obj)
				{
					return getChar(obj);
				}

				public sealed override long getLong(object obj)
				{
					return getChar(obj);
				}

				public sealed override float getFloat(object obj)
				{
					return getChar(obj);
				}

				public sealed override double getDouble(object obj)
				{
					return getChar(obj);
				}

				public sealed override object get(object obj)
				{
					return global::java.lang.Character.valueOf(getChar(obj));
				}

				public sealed override void set(object obj, object val)
				{
					if (val is global::java.lang.Character)
						setChar(obj, ((global::java.lang.Character)val).charValue());
					else
						throw SetIllegalArgumentException(val);
				}

				public sealed override char getChar(object obj)
				{
					try
					{
						return getter(obj, this);
					}
					catch (FieldAccessException x)
					{
						throw new global::java.lang.IllegalAccessException().initCause(x);
					}
				}

				public sealed override void setChar(object obj, char value)
				{
					try
					{
						setter(obj, value, this);
					}
					catch (FieldAccessException x)
					{
						throw new global::java.lang.IllegalAccessException().initCause(x);
					}
				}

				protected sealed override object JavaBox(char value)
				{
					return global::java.lang.Character.valueOf(value);
				}
			}

			private sealed class ShortField : FieldAccessor<short>
			{
				internal ShortField(FieldWrapper field, bool isFinal)
					: base(field, isFinal)
				{
				}

				public sealed override int getInt(object obj)
				{
					return getShort(obj);
				}

				public sealed override long getLong(object obj)
				{
					return getShort(obj);
				}

				public sealed override float getFloat(object obj)
				{
					return getShort(obj);
				}

				public sealed override double getDouble(object obj)
				{
					return getShort(obj);
				}

				public sealed override object get(object obj)
				{
					return global::java.lang.Short.valueOf(getShort(obj));
				}

				public sealed override void set(object obj, object val)
				{
					if (val is global::java.lang.Byte
						|| val is global::java.lang.Short)
						setShort(obj, ((global::java.lang.Number)val).shortValue());
					else
						throw SetIllegalArgumentException(val);
				}

				public sealed override void setByte(object obj, byte b)
				{
					setShort(obj, (sbyte)b);
				}

				public sealed override short getShort(object obj)
				{
					try
					{
						return getter(obj, this);
					}
					catch (FieldAccessException x)
					{
						throw new global::java.lang.IllegalAccessException().initCause(x);
					}
				}

				public sealed override void setShort(object obj, short value)
				{
					try
					{
						setter(obj, value, this);
					}
					catch (FieldAccessException x)
					{
						throw new global::java.lang.IllegalAccessException().initCause(x);
					}
				}

				protected sealed override object JavaBox(short value)
				{
					return global::java.lang.Short.valueOf(value);
				}
			}

			private sealed class IntField : FieldAccessor<int>
			{
				internal IntField(FieldWrapper field, bool isFinal)
					: base(field, isFinal)
				{
				}

				public sealed override long getLong(object obj)
				{
					return getInt(obj);
				}

				public sealed override float getFloat(object obj)
				{
					return getInt(obj);
				}

				public sealed override double getDouble(object obj)
				{
					return getInt(obj);
				}

				public sealed override object get(object obj)
				{
					return global::java.lang.Integer.valueOf(getInt(obj));
				}

				public sealed override void set(object obj, object val)
				{
					if (val is global::java.lang.Byte
						|| val is global::java.lang.Short
						|| val is global::java.lang.Integer)
						setInt(obj, ((global::java.lang.Number)val).intValue());
					else if (val is global::java.lang.Character)
						setInt(obj, ((global::java.lang.Character)val).charValue());
					else
						throw SetIllegalArgumentException(val);
				}

				public sealed override void setByte(object obj, byte b)
				{
					setInt(obj, (sbyte)b);
				}

				public sealed override void setChar(object obj, char c)
				{
					setInt(obj, c);
				}

				public sealed override void setShort(object obj, short s)
				{
					setInt(obj, s);
				}

				public sealed override int getInt(object obj)
				{
					try
					{
						return getter(obj, this);
					}
					catch (FieldAccessException x)
					{
						throw new global::java.lang.IllegalAccessException().initCause(x);
					}
				}

				public sealed override void setInt(object obj, int value)
				{
					try
					{
						setter(obj, value, this);
					}
					catch (FieldAccessException x)
					{
						throw new global::java.lang.IllegalAccessException().initCause(x);
					}
				}

				protected sealed override object JavaBox(int value)
				{
					return global::java.lang.Integer.valueOf(value);
				}
			}

			private sealed class FloatField : FieldAccessor<float>
			{
				internal FloatField(FieldWrapper field, bool isFinal)
					: base(field, isFinal)
				{
				}

				public sealed override double getDouble(object obj)
				{
					return getFloat(obj);
				}

				public sealed override object get(object obj)
				{
					return global::java.lang.Float.valueOf(getFloat(obj));
				}

				public sealed override void set(object obj, object val)
				{
					if (val is global::java.lang.Float
						|| val is global::java.lang.Byte
						|| val is global::java.lang.Short
						|| val is global::java.lang.Integer
						|| val is global::java.lang.Long)
						setFloat(obj, ((global::java.lang.Number)val).floatValue());
					else if (val is global::java.lang.Character)
						setFloat(obj, ((global::java.lang.Character)val).charValue());
					else
						throw SetIllegalArgumentException(val);
				}

				public sealed override void setByte(object obj, byte b)
				{
					setFloat(obj, (sbyte)b);
				}

				public sealed override void setChar(object obj, char c)
				{
					setFloat(obj, c);
				}

				public sealed override void setShort(object obj, short s)
				{
					setFloat(obj, s);
				}

				public sealed override void setInt(object obj, int i)
				{
					setFloat(obj, i);
				}

				public sealed override void setLong(object obj, long l)
				{
					setFloat(obj, l);
				}

				public sealed override float getFloat(object obj)
				{
					try
					{
						return getter(obj, this);
					}
					catch (FieldAccessException x)
					{
						throw new global::java.lang.IllegalAccessException().initCause(x);
					}
				}

				public sealed override void setFloat(object obj, float value)
				{
					try
					{
						setter(obj, value, this);
					}
					catch (FieldAccessException x)
					{
						throw new global::java.lang.IllegalAccessException().initCause(x);
					}
				}

				protected sealed override object JavaBox(float value)
				{
					return global::java.lang.Float.valueOf(value);
				}
			}

			private sealed class LongField : FieldAccessor<long>
			{
				internal LongField(FieldWrapper field, bool isFinal)
					: base(field, isFinal)
				{
				}

				public sealed override float getFloat(object obj)
				{
					return getLong(obj);
				}

				public sealed override double getDouble(object obj)
				{
					return getLong(obj);
				}

				public sealed override object get(object obj)
				{
					return global::java.lang.Long.valueOf(getLong(obj));
				}

				public sealed override void set(object obj, object val)
				{
					if (val is global::java.lang.Long
						|| val is global::java.lang.Byte
						|| val is global::java.lang.Short
						|| val is global::java.lang.Integer)
						setLong(obj, ((global::java.lang.Number)val).longValue());
					else if (val is global::java.lang.Character)
						setLong(obj, ((global::java.lang.Character)val).charValue());
					else
						throw SetIllegalArgumentException(val);
				}

				public sealed override void setByte(object obj, byte b)
				{
					setLong(obj, (sbyte)b);
				}

				public sealed override void setChar(object obj, char c)
				{
					setLong(obj, c);
				}

				public sealed override void setShort(object obj, short s)
				{
					setLong(obj, s);
				}

				public sealed override void setInt(object obj, int i)
				{
					setLong(obj, i);
				}

				public sealed override long getLong(object obj)
				{
					try
					{
						return getter(obj, this);
					}
					catch (FieldAccessException x)
					{
						throw new global::java.lang.IllegalAccessException().initCause(x);
					}
				}

				public sealed override void setLong(object obj, long value)
				{
					try
					{
						setter(obj, value, this);
					}
					catch (FieldAccessException x)
					{
						throw new global::java.lang.IllegalAccessException().initCause(x);
					}
				}

				protected sealed override object JavaBox(long value)
				{
					return global::java.lang.Long.valueOf(value);
				}
			}

			private sealed class DoubleField : FieldAccessor<double>
			{
				internal DoubleField(FieldWrapper field, bool isFinal)
					: base(field, isFinal)
				{
				}

				public sealed override object get(object obj)
				{
					return global::java.lang.Double.valueOf(getDouble(obj));
				}

				public sealed override void set(object obj, object val)
				{
					if (val is global::java.lang.Double
						|| val is global::java.lang.Float
						|| val is global::java.lang.Byte
						|| val is global::java.lang.Short
						|| val is global::java.lang.Integer
						|| val is global::java.lang.Long)
						setDouble(obj, ((global::java.lang.Number)val).doubleValue());
					else if (val is global::java.lang.Character)
						setDouble(obj, ((global::java.lang.Character)val).charValue());
					else
						throw SetIllegalArgumentException(val);
				}

				public sealed override void setByte(object obj, byte b)
				{
					setDouble(obj, (sbyte)b);
				}

				public sealed override void setChar(object obj, char c)
				{
					setDouble(obj, c);
				}

				public sealed override void setShort(object obj, short s)
				{
					setDouble(obj, s);
				}

				public sealed override void setInt(object obj, int i)
				{
					setDouble(obj, i);
				}

				public sealed override void setLong(object obj, long l)
				{
					setDouble(obj, l);
				}

				public sealed override void setFloat(object obj, float f)
				{
					setDouble(obj, f);
				}

				public sealed override double getDouble(object obj)
				{
					try
					{
						return getter(obj, this);
					}
					catch (FieldAccessException x)
					{
						throw new global::java.lang.IllegalAccessException().initCause(x);
					}
				}

				public sealed override void setDouble(object obj, double value)
				{
					try
					{
						setter(obj, value, this);
					}
					catch (FieldAccessException x)
					{
						throw new global::java.lang.IllegalAccessException().initCause(x);
					}
				}

				protected sealed override object JavaBox(double value)
				{
					return global::java.lang.Double.valueOf(value);
				}
			}

			private sealed class ObjectField : FieldAccessor<object>
			{
				internal ObjectField(FieldWrapper field, bool isFinal)
					: base(field, isFinal)
				{
				}

				protected sealed override void CheckValue(object value)
				{
					if (value != null && !fw.FieldTypeWrapper.EnsureLoadable(fw.DeclaringType.GetClassLoader()).IsInstance(value))
					{
						throw SetIllegalArgumentException(value);
					}
				}

				public sealed override object get(object obj)
				{
					try
					{
						return getter(obj, this);
					}
					catch (FieldAccessException x)
					{
						throw new global::java.lang.IllegalAccessException().initCause(x);
					}
				}

				public sealed override void set(object obj, object value)
				{
					try
					{
						setter(obj, value, this);
					}
					catch (FieldAccessException x)
					{
						throw new global::java.lang.IllegalAccessException().initCause(x);
					}
				}

				protected sealed override object JavaBox(object value)
				{
					return value;
				}
			}

#if !NO_REF_EMIT
			private Delegate GenerateFastGetter(Type delegateType, Type fieldType, FieldWrapper fw)
			{
				TypeWrapper fieldTypeWrapper;
				try
				{
					fieldTypeWrapper = fw.FieldTypeWrapper.EnsureLoadable(fw.DeclaringType.GetClassLoader());
					fieldTypeWrapper.Finish();
					fw.DeclaringType.Finish();
				}
				catch (RetargetableJavaException x)
				{
					throw x.ToJava();
				}
				fw.ResolveField();
				DynamicMethod dm = DynamicMethodUtils.Create("___Getter_", fw.DeclaringType.TypeAsBaseType, !fw.IsPublic || !fw.DeclaringType.IsPublic, fieldType, new Type[] { typeof(IReflectionException), typeof(object), typeof(object) });
				CodeEmitter ilgen = CodeEmitter.Create(dm);
				if (fw.IsStatic)
				{
					fw.EmitGet(ilgen);
					fieldTypeWrapper.EmitConvSignatureTypeToStackType(ilgen);
				}
				else
				{
					ilgen.BeginExceptionBlock();
					ilgen.Emit(OpCodes.Ldarg_1);
					ilgen.Emit(OpCodes.Castclass, fw.DeclaringType.TypeAsBaseType);
					fw.EmitGet(ilgen);
					fieldTypeWrapper.EmitConvSignatureTypeToStackType(ilgen);
					CodeEmitterLocal local = ilgen.DeclareLocal(fieldType);
					ilgen.Emit(OpCodes.Stloc, local);
					CodeEmitterLabel label = ilgen.DefineLabel();
					ilgen.EmitLeave(label);
					ilgen.BeginCatchBlock(typeof(InvalidCastException));
					ilgen.Emit(OpCodes.Ldarg_0);
					ilgen.Emit(OpCodes.Ldarg_1);
					ilgen.Emit(OpCodes.Callvirt, typeof(IReflectionException).GetMethod("GetIllegalArgumentException"));
					ilgen.Emit(OpCodes.Throw);
					ilgen.EndExceptionBlock();
					ilgen.MarkLabel(label);
					ilgen.Emit(OpCodes.Ldloc, local);
				}
				ilgen.Emit(OpCodes.Ret);
				ilgen.DoEmit();
				return dm.CreateDelegate(delegateType, this);
			}

			private Delegate GenerateFastSetter(Type delegateType, Type fieldType, FieldWrapper fw)
			{
				TypeWrapper fieldTypeWrapper;
				try
				{
					fieldTypeWrapper = fw.FieldTypeWrapper.EnsureLoadable(fw.DeclaringType.GetClassLoader());
					fieldTypeWrapper.Finish();
					fw.DeclaringType.Finish();
				}
				catch (RetargetableJavaException x)
				{
					throw x.ToJava();
				}
				fw.ResolveField();
				DynamicMethod dm = DynamicMethodUtils.Create("___Setter_", fw.DeclaringType.TypeAsBaseType, !fw.IsPublic || !fw.DeclaringType.IsPublic, null, new Type[] { typeof(IReflectionException), typeof(object), fieldType, typeof(object) });
				CodeEmitter ilgen = CodeEmitter.Create(dm);
				if (fw.IsStatic)
				{
					if (fieldType == typeof(object))
					{
						ilgen.BeginExceptionBlock();
						ilgen.Emit(OpCodes.Ldarg_2);
						fieldTypeWrapper.EmitCheckcast(ilgen);
						fieldTypeWrapper.EmitConvStackTypeToSignatureType(ilgen, null);
						fw.EmitSet(ilgen);
						CodeEmitterLabel label = ilgen.DefineLabel();
						ilgen.EmitLeave(label);
						ilgen.BeginCatchBlock(typeof(InvalidCastException));
						ilgen.Emit(OpCodes.Ldarg_0);
						ilgen.Emit(OpCodes.Ldarg_1);
						ilgen.Emit(OpCodes.Callvirt, typeof(IReflectionException).GetMethod("SetIllegalArgumentException"));
						ilgen.Emit(OpCodes.Throw);
						ilgen.EndExceptionBlock();
						ilgen.MarkLabel(label);
					}
					else
					{
						ilgen.Emit(OpCodes.Ldarg_2);
						fw.EmitSet(ilgen);
					}
				}
				else
				{
					ilgen.BeginExceptionBlock();
					ilgen.Emit(OpCodes.Ldarg_1);
					ilgen.Emit(OpCodes.Castclass, fw.DeclaringType.TypeAsBaseType);
					ilgen.Emit(OpCodes.Ldarg_2);
					if (fieldType == typeof(object))
					{
						fieldTypeWrapper.EmitCheckcast(ilgen);
					}
					fieldTypeWrapper.EmitConvStackTypeToSignatureType(ilgen, null);
					fw.EmitSet(ilgen);
					CodeEmitterLabel label = ilgen.DefineLabel();
					ilgen.EmitLeave(label);
					ilgen.BeginCatchBlock(typeof(InvalidCastException));
					ilgen.Emit(OpCodes.Ldarg_0);
					ilgen.Emit(OpCodes.Ldarg_1);
					ilgen.Emit(OpCodes.Callvirt, typeof(IReflectionException).GetMethod("SetIllegalArgumentException"));
					ilgen.Emit(OpCodes.Throw);
					ilgen.EndExceptionBlock();
					ilgen.MarkLabel(label);
				}
				ilgen.Emit(OpCodes.Ret);
				ilgen.DoEmit();
				return dm.CreateDelegate(delegateType, this);
			}
#endif // !NO_REF_EMIT

			internal static FieldAccessorImplBase Create(FieldWrapper field, bool isFinal)
			{
				TypeWrapper type = field.FieldTypeWrapper;
				if (type.IsPrimitive)
				{
					if (type == PrimitiveTypeWrapper.BYTE)
					{
						return new ByteField(field, isFinal);
					}
					if (type == PrimitiveTypeWrapper.BOOLEAN)
					{
						return new BooleanField(field, isFinal);
					}
					if (type == PrimitiveTypeWrapper.CHAR)
					{
						return new CharField(field, isFinal);
					}
					if (type == PrimitiveTypeWrapper.SHORT)
					{
						return new ShortField(field, isFinal);
					}
					if (type == PrimitiveTypeWrapper.INT)
					{
						return new IntField(field, isFinal);
					}
					if (type == PrimitiveTypeWrapper.FLOAT)
					{
						return new FloatField(field, isFinal);
					}
					if (type == PrimitiveTypeWrapper.LONG)
					{
						return new LongField(field, isFinal);
					}
					if (type == PrimitiveTypeWrapper.DOUBLE)
					{
						return new DoubleField(field, isFinal);
					}
					throw new InvalidOperationException("field type: " + type);
				}
				else
				{
					return new ObjectField(field, isFinal);
				}
			}
		}
#endif

		public static global::sun.reflect.FieldAccessor newFieldAccessor(object thisFactory, global::java.lang.reflect.Field field, bool overrideAccessCheck)
		{
#if FIRST_PASS
		return null;
#else
			// we look at the modifiers of the Field object to allow Unsafe to give us a fake Field take doesn't have the final flag set
			int modifiers = field.getModifiers();
			bool isStatic = global::java.lang.reflect.Modifier.isStatic(modifiers);
			bool isFinal = global::java.lang.reflect.Modifier.isFinal(modifiers);
			return FieldAccessorImplBase.Create(FieldWrapper.FromField(field), isFinal && (!overrideAccessCheck || isStatic));
#endif
		}

#if !FIRST_PASS
		internal static global::sun.reflect.FieldAccessor NewFieldAccessorJNI(FieldWrapper field)
		{
			return FieldAccessorImplBase.Create(field, false);
		}
#endif

		public static global::sun.reflect.MethodAccessor newMethodAccessor(object thisFactory, global::java.lang.reflect.Method method)
		{
#if FIRST_PASS
		return null;
#else
			MethodWrapper mw = MethodWrapper.FromExecutable(method);
#if !NO_REF_EMIT
			if (!mw.IsDynamicOnly)
			{
				return new FastMethodAccessorImpl(mw);
			}
#endif
			return new MethodAccessorImpl(mw);
#endif
		}

		public static global::sun.reflect.ConstructorAccessor newConstructorAccessor0(object thisFactory, global::java.lang.reflect.Constructor constructor)
		{
#if FIRST_PASS
		return null;
#else
			MethodWrapper mw = MethodWrapper.FromExecutable(constructor);
			if (ActivatorConstructorAccessor.IsSuitable(mw))
			{
				// we special case public default constructors, because in that case using Activator.CreateInstance()
				// is almost as fast as FastConstructorAccessorImpl, but it saves us significantly in working set and
				// startup time (because often during startup a sun.nio.cs.* encoder is instantiated using reflection)
				return new ActivatorConstructorAccessor(mw);
			}
			else
			{
#if NO_REF_EMIT
			return new ConstructorAccessorImpl(mw);
#else
				return new FastConstructorAccessorImpl(constructor);
#endif
			}
#endif
		}

		public static global::sun.reflect.ConstructorAccessor newConstructorAccessorForSerialization(global::java.lang.Class classToInstantiate, global::java.lang.reflect.Constructor constructorToCall)
		{
#if FIRST_PASS
		return null;
#else
			try
			{
#if NO_REF_EMIT
			return new SerializationConstructorAccessorImpl(constructorToCall, classToInstantiate);
#else
				return new FastSerializationConstructorAccessorImpl(constructorToCall, classToInstantiate);
#endif
			}
			catch (SecurityException x)
			{
				throw new global::java.lang.SecurityException(x.Message, global::ikvm.runtime.Util.mapException(x));
			}
#endif
		}

	}

}
