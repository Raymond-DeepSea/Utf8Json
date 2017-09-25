﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Utf8Json.Internal;
using Utf8Json.Internal.Emit;

namespace Utf8Json
{
    // NonGeneric API
    public static partial class JsonSerializer
    {
        public static class NonGeneric
        {
            static readonly Func<Type, CompiledMethods> CreateCompiledMethods;
            static readonly ThreadsafeTypeKeyHashTable<CompiledMethods> serializes = new ThreadsafeTypeKeyHashTable<CompiledMethods>(capacity: 64);

            static NonGeneric()
            {
                CreateCompiledMethods = t => new CompiledMethods(t);
            }

            static CompiledMethods GetOrAdd(Type type)
            {
                return serializes.GetOrAdd(type, CreateCompiledMethods);
            }

            /// <summary>
            /// Serialize to binary with default resolver.
            /// </summary>
            public static byte[] Serialize(object value)
            {
                if (value == null) return Serialize<object>(value);
                return Serialize(value.GetType(), value, defaultResolver);
            }

            /// <summary>
            /// Serialize to binary with default resolver.
            /// </summary>
            public static byte[] Serialize(Type type, object value)
            {
                return Serialize(type, value, defaultResolver);
            }

            /// <summary>
            /// Serialize to binary with specified resolver.
            /// </summary>
            public static byte[] Serialize(object value, IJsonFormatterResolver resolver)
            {
                if (value == null) return Serialize<object>(value, resolver);
                return Serialize(value.GetType(), value, resolver);
            }

            /// <summary>
            /// Serialize to binary with specified resolver.
            /// </summary>
            public static byte[] Serialize(Type type, object value, IJsonFormatterResolver resolver)
            {
                return GetOrAdd(type).serialize1.Invoke(value, resolver);
            }

            /// <summary>
            /// Serialize to stream.
            /// </summary>
            public static void Serialize(Stream stream, object value)
            {
                if (value == null) { Serialize<object>(stream, value); return; }
                Serialize(value.GetType(), stream, value, defaultResolver);
            }

            /// <summary>
            /// Serialize to stream.
            /// </summary>
            public static void Serialize(Type type, Stream stream, object value)
            {
                Serialize(type, stream, value, defaultResolver);
            }

            /// <summary>
            /// Serialize to stream with specified resolver.
            /// </summary>
            public static void Serialize(Stream stream, object value, IJsonFormatterResolver resolver)
            {
                if (value == null) { Serialize<object>(stream, value, resolver); return; }
                Serialize(value.GetType(), stream, value, resolver);
            }

            /// <summary>
            /// Serialize to stream with specified resolver.
            /// </summary>
            public static void Serialize(Type type, Stream stream, object value, IJsonFormatterResolver resolver)
            {
                GetOrAdd(type).serialize2.Invoke(stream, value, resolver);
            }

            /// <summary>
            /// Serialize to binary. Get the raw memory pool byte[]. The result can not share across thread and can not hold, so use quickly.
            /// </summary>
            public static ArraySegment<byte> SerializeUnsafe(object value)
            {
                if (value == null) return SerializeUnsafe<object>(value);
                return SerializeUnsafe(value.GetType(), value);
            }

            /// <summary>
            /// Serialize to binary. Get the raw memory pool byte[]. The result can not share across thread and can not hold, so use quickly.
            /// </summary>
            public static ArraySegment<byte> SerializeUnsafe(Type type, object value)
            {
                return SerializeUnsafe(type, value, defaultResolver);
            }

            /// <summary>
            /// Serialize to binary with specified resolver. Get the raw memory pool byte[]. The result can not share across thread and can not hold, so use quickly.
            /// </summary>
            public static ArraySegment<byte> SerializeUnsafe(object value, IJsonFormatterResolver resolver)
            {
                if (value == null) return SerializeUnsafe<object>(value);
                return SerializeUnsafe(value.GetType(), value, resolver);
            }

            /// <summary>
            /// Serialize to binary with specified resolver. Get the raw memory pool byte[]. The result can not share across thread and can not hold, so use quickly.
            /// </summary>
            public static ArraySegment<byte> SerializeUnsafe(Type type, object value, IJsonFormatterResolver resolver)
            {
                return GetOrAdd(type).serializeUnsafe.Invoke(value, resolver);
            }

            /// <summary>
            /// Serialize to JsonString.
            /// </summary>
            public static string ToJsonString(object value)
            {
                if (value == null) return "null";
                return ToJsonString(value.GetType(), value);
            }

            /// <summary>
            /// Serialize to JsonString.
            /// </summary>
            public static string ToJsonString(Type type, object value)
            {
                return ToJsonString(type, value, defaultResolver);
            }

            /// <summary>
            /// Serialize to JsonString with specified resolver.
            /// </summary>
            public static string ToJsonString(object value, IJsonFormatterResolver resolver)
            {
                if (value == null) return "null";
                return ToJsonString(value.GetType(), value, resolver);
            }

            /// <summary>
            /// Serialize to JsonString with specified resolver.
            /// </summary>
            public static string ToJsonString(Type type, object value, IJsonFormatterResolver resolver)
            {
                return GetOrAdd(type).toJsonString.Invoke(value, resolver);
            }

            public static object Deserialize(Type type, string json)
            {
                return Deserialize(type, json, defaultResolver);
            }

            public static object Deserialize(Type type, string json, IJsonFormatterResolver resolver)
            {
                return GetOrAdd(type).deserialize1.Invoke(json, resolver);
            }

            public static object Deserialize(Type type, byte[] bytes)
            {
                return Deserialize(type, bytes, defaultResolver);
            }

            public static object Deserialize(Type type, byte[] bytes, IJsonFormatterResolver resolver)
            {
                return Deserialize(type, bytes, 0, defaultResolver);
            }

            public static object Deserialize(Type type, byte[] bytes, int offset)
            {
                return Deserialize(type, bytes, offset, defaultResolver);
            }

            public static object Deserialize(Type type, byte[] bytes, int offset, IJsonFormatterResolver resolver)
            {
                return GetOrAdd(type).deserialize2.Invoke(bytes, offset, resolver);
            }

            public static object Deserialize(Type type, Stream stream)
            {
                return Deserialize(type, stream, defaultResolver);
            }

            public static object Deserialize(Type type, Stream stream, IJsonFormatterResolver resolver)
            {
                return GetOrAdd(type).deserialize3.Invoke(stream, resolver);
            }

            class CompiledMethods
            {
                public readonly Func<object, IJsonFormatterResolver, byte[]> serialize1;
                public readonly Action<Stream, object, IJsonFormatterResolver> serialize2;
                public readonly Func<object, IJsonFormatterResolver, ArraySegment<byte>> serializeUnsafe;
                public readonly Func<object, IJsonFormatterResolver, string> toJsonString;
                public readonly Func<string, IJsonFormatterResolver, object> deserialize1;
                public readonly Func<byte[], int, IJsonFormatterResolver, object> deserialize2;
                public readonly Func<Stream, IJsonFormatterResolver, object> deserialize3;

                public CompiledMethods(Type type)
                {
                    var ti = type.GetTypeInfo();
                    {
                        var dm = new DynamicMethod("serialize1", typeof(byte[]), new[] { typeof(object), typeof(IJsonFormatterResolver) });
                        var il = dm.GetILGenerator();

                        il.EmitLdarg(0); // obj
                        il.EmitUnboxOrCast(type);
                        il.EmitLdarg(1);
                        il.EmitCall(GetMethod(type, "Serialize", new[] { null, typeof(IJsonFormatterResolver) }));
                        il.Emit(OpCodes.Ret);

                        serialize1 = CreateDelegate<Func<object, IJsonFormatterResolver, byte[]>>(dm);
                    }
                    {
                        var dm = new DynamicMethod("serialize2", null, new[] { typeof(Stream), typeof(object), typeof(IJsonFormatterResolver) });
                        var il = dm.GetILGenerator();

                        il.EmitLdarg(0); // stream
                        il.EmitLdarg(1);
                        il.EmitUnboxOrCast(type);
                        il.EmitLdarg(2);
                        il.EmitCall(GetMethod(type, "Serialize", new[] { typeof(Stream), null, typeof(IJsonFormatterResolver) }));
                        il.Emit(OpCodes.Ret);

                        serialize2 = CreateDelegate<Action<Stream, object, IJsonFormatterResolver>>(dm);
                    }
                    {
                        var dm = new DynamicMethod("serializeUnsafe", typeof(ArraySegment<byte>), new[] { typeof(object), typeof(IJsonFormatterResolver) });
                        var il = dm.GetILGenerator();

                        il.EmitLdarg(0); // obj
                        il.EmitUnboxOrCast(type);
                        il.EmitLdarg(1);
                        il.EmitCall(GetMethod(type, "SerializeUnsafe", new[] { null, typeof(IJsonFormatterResolver) }));
                        il.Emit(OpCodes.Ret);

                        serializeUnsafe = CreateDelegate<Func<object, IJsonFormatterResolver, ArraySegment<byte>>>(dm);
                    }
                    {
                        var dm = new DynamicMethod("toJsonString", typeof(string), new[] { typeof(object), typeof(IJsonFormatterResolver) });
                        var il = dm.GetILGenerator();

                        il.EmitLdarg(0); // obj
                        il.EmitUnboxOrCast(type);
                        il.EmitLdarg(1);
                        il.EmitCall(GetMethod(type, "ToJsonString", new[] { null, typeof(IJsonFormatterResolver) }));
                        il.Emit(OpCodes.Ret);

                        toJsonString = CreateDelegate<Func<object, IJsonFormatterResolver, string>>(dm);
                    }
                    {
                        var dm = new DynamicMethod("Deserialize", typeof(object), new[] { typeof(string), typeof(IJsonFormatterResolver) });
                        var il = dm.GetILGenerator();

                        il.EmitLdarg(0);
                        il.EmitLdarg(1);
                        il.EmitCall(GetMethod(type, "Deserialize", new[] { typeof(string), typeof(IJsonFormatterResolver) }));
                        il.EmitBoxOrDoNothing(type);
                        il.Emit(OpCodes.Ret);

                        deserialize1 = CreateDelegate<Func<string, IJsonFormatterResolver, object>>(dm);
                    }
                    {
                        var dm = new DynamicMethod("Deserialize", typeof(object), new[] { typeof(byte[]), typeof(int), typeof(IJsonFormatterResolver) });
                        var il = dm.GetILGenerator();

                        il.EmitLdarg(0);
                        il.EmitLdarg(1);
                        il.EmitLdarg(2);
                        il.EmitCall(GetMethod(type, "Deserialize", new[] { typeof(byte[]), typeof(int), typeof(IJsonFormatterResolver) }));
                        il.EmitBoxOrDoNothing(type);
                        il.Emit(OpCodes.Ret);

                        deserialize2 = CreateDelegate<Func<byte[], int, IJsonFormatterResolver, object>>(dm);
                    }
                    {
                        var dm = new DynamicMethod("Deserialize", typeof(object), new[] { typeof(Stream), typeof(IJsonFormatterResolver) });
                        var il = dm.GetILGenerator();

                        il.EmitLdarg(0);
                        il.EmitLdarg(1);
                        il.EmitCall(GetMethod(type, "Deserialize", new[] { typeof(Stream), typeof(IJsonFormatterResolver) }));
                        il.EmitBoxOrDoNothing(type);
                        il.Emit(OpCodes.Ret);

                        deserialize3 = CreateDelegate<Func<Stream, IJsonFormatterResolver, object>>(dm);
                    }
                }

                static T CreateDelegate<T>(DynamicMethod dm)
                {
                    return (T)(object)dm.CreateDelegate(typeof(T));
                }

                static MethodInfo GetMethod(Type type, string name, Type[] arguments)
                {
                    return typeof(JsonSerializer).GetMethods(BindingFlags.Static | BindingFlags.Public)
                        .Where(x => x.Name == name)
                        .Single(x =>
                        {
                            var ps = x.GetParameters();
                            if (ps.Length != arguments.Length) return false;
                            for (int i = 0; i < ps.Length; i++)
                            {
                                // null for <T>.
                                if (arguments[i] == null && ps[i].ParameterType.IsGenericParameter) continue;
                                if (ps[i].ParameterType != arguments[i]) return false;
                            }
                            return true;
                        })
                        .MakeGenericMethod(type);
                }
            }
        }
    }
}
