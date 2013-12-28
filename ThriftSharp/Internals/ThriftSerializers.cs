﻿// Copyright (c) 2013 Solal Pirelli
// This code is licensed under the MIT License (see Licence.txt for details).
// Redistributions of this source code must retain the above copyright notice.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThriftSharp.Protocols;
using ThriftSharp.Utilities;

namespace ThriftSharp.Internals
{
    /// <summary>
    /// Thrift serializers.
    /// </summary>
    internal abstract class ThriftSerializer
    {
        // Order matters; FromType uses MatchesType on all, in order.
        private static readonly ThriftSerializer[] Serializers = new ThriftSerializer[]
        {
            new PrimitiveSerializer<bool>( p => p.ReadBoolean, p => p.WriteBoolean , ThriftType.Bool ),
            new PrimitiveSerializer<sbyte>( p => p.ReadSByte, p => p.WriteSByte , ThriftType.Byte ),
            new PrimitiveSerializer<double>( p => p.ReadDouble, p => p.WriteDouble , ThriftType.Double ),
            new PrimitiveSerializer<short>( p => p.ReadInt16, p => p.WriteInt16 , ThriftType.Int16 ),
            new PrimitiveSerializer<int>( p => p.ReadInt32, p => p.WriteInt32 , ThriftType.Int32 ),
            new PrimitiveSerializer<long>( p => p.ReadInt64, p => p.WriteInt64 , ThriftType.Int64 ),
            new PrimitiveSerializer<string>( p => p.ReadString, p => p.WriteString , ThriftType.String ),
            new PrimitiveSerializer<sbyte[]>( p => p.ReadBinary, p => p.WriteBinary , ThriftType.String ),
            new EnumSerializer(),
            new MapSerializer(),
            new CollectionSerializer( typeof( ISet<> ), 
                                      p => p.ReadSetHeader, p => p.ReadSetEnd, 
                                      p => p.WriteSetHeader, p => p.WriteSetEnd, 
                                      ThriftType.Set ),
            new CollectionSerializer( typeof( ICollection<> ), 
                                      p => p.ReadListHeader, p => p.ReadListEnd, 
                                      p => p.WriteListHeader, p => p.WriteListEnd, 
                                      ThriftType.List ),
            new ArraySerializer(),
            new NullableSerializer(),
            new StructSerializer()
        };

        /// <summary>
        /// Gets a Thrift type from a .NET type.
        /// </summary>
        public static ThriftSerializer FromType( Type type )
        {
            return Serializers.First( tt => tt.MatchesType( type ) );
        }

        /// <summary>
        /// Gets the Thrift type with the specified ID.
        /// </summary>
        public static ThriftSerializer FromThriftType( ThriftType id )
        {
            return Serializers.First( s => s.GetThriftType( null ) == id );
        }


        /// <summary>
        /// Indicates whether the Thrift type matches the specified .NET type.
        /// </summary>
        internal abstract bool MatchesType( Type type );

        /// <summary>
        /// Gets the Thrift type associated with the specified type, which may be null.
        /// The return value can be null if the argument is null.
        /// </summary>
        internal abstract ThriftType? GetThriftType( Type type );

        /// <summary>
        /// Reads an instance of the specified .NET type from the specified protocol.
        /// </summary>
        internal abstract object Read( IThriftProtocol protocol, Type targetType );

        /// <summary>
        /// Skips an instance of the Thrift type from the specified protocol.
        /// </summary>
        internal abstract void Skip( IThriftProtocol protocol );

        /// <summary>
        /// Writes the specified object as an instance of the Thrift type to the specified protocol.
        /// </summary>
        internal abstract void Write( IThriftProtocol protocol, object obj );

        /// <summary>
        /// Reads the fields of an already created instance of the specified Thrift struct on the specified protocol.
        /// </summary>
        internal static object ReadStruct( IThriftProtocol protocol, ThriftStruct st, object instance )
        {
            var readIds = new List<short>();

            protocol.ReadStructHeader();
            while ( true )
            {
                var header = protocol.ReadFieldHeader();
                if ( header == null )
                {
                    break;
                }

                var field = st.Fields.FirstOrDefault( f => f.Header.Id == header.Id );
                if ( field == null )
                {
                    ThriftSerializer.FromThriftType( header.FieldType ).Skip( protocol );
                }
                else
                {
                    var value = ThriftSerializer.FromType( field.UnderlyingType ).Read( protocol, field.UnderlyingType );
                    field.Setter( instance, value );

                    readIds.Add( field.Header.Id );
                }
                protocol.ReadFieldEnd();
            }
            protocol.ReadStructEnd();

            foreach ( var field in st.Fields )
            {
                if ( !readIds.Contains( field.Header.Id ) )
                {
                    if ( field.IsRequired )
                    {
                        throw new ThriftProtocolException( ThriftProtocolExceptionType.MissingResult, string.Format( "Field '{0}' of type '{1}' is required but was not received.", field.Header.Name, st.Header.Name ) );
                    }
                    if ( field.DefaultValue.HasValue )
                    {
                        field.Setter( instance, field.DefaultValue.Value );
                    }
                }
            }

            return instance;
        }

        /// <summary>
        /// Writes the fields of the specified instance of the specified Thrift struct on the specified protocol.
        /// </summary>
        internal static void WriteStruct( IThriftProtocol protocol, ThriftStruct st, object instance )
        {
            protocol.WriteStructHeader( st.Header );
            foreach ( var field in st.Fields )
            {
                var fieldValue = field.Getter( instance );
                if ( fieldValue != null )
                {
                    protocol.WriteFieldHeader( field.Header );
                    ThriftSerializer.FromType( field.UnderlyingType ).Write( protocol, fieldValue );
                    protocol.WriteFieldEnd();
                }
            }
            protocol.WriteFieldStop();
            protocol.WriteStructEnd();
        }


        /// <summary>
        /// A Thrift primitive parser. Super simple.
        /// </summary>
        private sealed class PrimitiveSerializer<T> : ThriftSerializer
        {
            private readonly Func<IThriftProtocol, Func<T>> _read;
            private readonly Func<IThriftProtocol, Action<T>> _write;
            private readonly ThriftType _thriftType;

            /// <summary>
            /// Creates a new instance of the PrimitiveType class from the specified reading and writing methods.
            /// </summary>
            public PrimitiveSerializer( Func<IThriftProtocol, Func<T>> read, Func<IThriftProtocol, Action<T>> write, ThriftType thriftType )
            {
                _read = read;
                _write = write;
                _thriftType = thriftType;
            }

            internal override bool MatchesType( Type type )
            {
                return type == typeof( T );
            }

            internal override ThriftType? GetThriftType( Type type )
            {
                return _thriftType;
            }

            internal override object Read( IThriftProtocol protocol, Type targetType )
            {
                return _read( protocol )();
            }

            internal override void Skip( IThriftProtocol protocol )
            {
                _read( protocol )();
            }

            internal override void Write( IThriftProtocol protocol, object obj )
            {
                _write( protocol )( (T) obj );
            }
        }

        /// <summary>
        /// The Thrift enum serializer, which converts Int32s to enums.
        /// </summary>
        private sealed class EnumSerializer : ThriftSerializer
        {
            internal override bool MatchesType( Type type )
            {
                return type.IsEnum;
            }

            internal override ThriftType? GetThriftType( Type type )
            {
                return ThriftType.Int32;
            }

            internal override object Read( IThriftProtocol protocol, Type targetType )
            {
                var enm = ThriftAttributesParser.ParseEnum( targetType );
                int value = protocol.ReadInt32();

                var member = enm.Members.FirstOrDefault( f => f.Value == value );
                if ( member == null )
                {
                    return 0;
                }

                return member.UnderlyingField.GetValue( null );
            }

            internal override void Skip( IThriftProtocol protocol )
            {
                protocol.ReadInt32();
            }

            internal override void Write( IThriftProtocol protocol, object obj )
            {
                var enm = ThriftAttributesParser.ParseEnum( obj.GetType() );
                var member = enm.Members.FirstOrDefault( m => (int) m.UnderlyingField.GetValue( null ) == (int) obj );
                if ( member == null )
                {
                    throw new ThriftProtocolException( ThriftProtocolExceptionType.InternalError, "Cannot write an undeclared enum value." );
                }
                protocol.WriteInt32( member.Value );
            }
        }

        /// <summary>
        /// The Thrift array serializer, for lists as arrays.
        /// </summary>
        private sealed class ArraySerializer : ThriftSerializer
        {
            internal override bool MatchesType( Type type )
            {
                return type.IsArray;
            }

            internal override ThriftType? GetThriftType( Type type )
            {
                return ThriftType.List;
            }

            internal override object Read( IThriftProtocol protocol, Type targetType )
            {
                var elemType = targetType.GetElementType();
                var elemSerializer = ThriftSerializer.FromType( elemType );

                var header = protocol.ReadListHeader();
                var array = Array.CreateInstance( elemType, header.Count );
                for ( int n = 0; n < array.Length; n++ )
                {
                    array.SetValue( elemSerializer.Read( protocol, elemType ), n );
                }
                protocol.ReadListEnd();
                return array;
            }

            internal override void Skip( IThriftProtocol protocol )
            {
                var header = protocol.ReadListHeader();
                var ttype = ThriftSerializer.FromThriftType( header.ElementType );
                for ( int n = 0; n < header.Count; n++ )
                {
                    ttype.Skip( protocol );
                }
                protocol.ReadListEnd();
            }

            internal override void Write( IThriftProtocol protocol, object obj )
            {
                var elemType = obj.GetType().GetElementType();
                var elemSerializer = ThriftSerializer.FromType( elemType );

                var array = (Array) obj;
                protocol.WriteListHeader( new ThriftCollectionHeader( array.Length, elemSerializer.GetThriftType( elemType ).Value ) );
                for ( int n = 0; n < array.Length; n++ )
                {
                    elemSerializer.Write( protocol, array.GetValue( n ) );
                }
                protocol.WriteListEnd();
            }
        }

        /// <summary>
        /// The Thrift collections parameterized serializer.
        /// </summary>
        private sealed class CollectionSerializer : ThriftSerializer
        {
            private readonly Type _collectionGenericType;
            private readonly Func<IThriftProtocol, Func<ThriftCollectionHeader>> _readHeader;
            private readonly Func<IThriftProtocol, Action> _readEnd;
            private readonly Func<IThriftProtocol, Action<ThriftCollectionHeader>> _writeHeader;
            private readonly Func<IThriftProtocol, Action> _writeEnd;
            private readonly ThriftType _thriftType;

            /// <summary>
            /// Creates a new instance of the CollectionType class from the specified collection type and reading/writing methods.
            /// </summary>
            public CollectionSerializer( Type collectionGenericType,
                                         Func<IThriftProtocol, Func<ThriftCollectionHeader>> readHeader, Func<IThriftProtocol, Action> readEnd,
                                         Func<IThriftProtocol, Action<ThriftCollectionHeader>> writeHeader, Func<IThriftProtocol, Action> writeEnd,
                                         ThriftType thriftType )
            {
                _collectionGenericType = collectionGenericType;
                _readHeader = readHeader;
                _readEnd = readEnd;
                _writeHeader = writeHeader;
                _writeEnd = writeEnd;
                _thriftType = thriftType;
            }

            internal override bool MatchesType( Type type )
            {
                return !type.IsArray && type.GetGenericInterface( _collectionGenericType ) != null;
            }

            internal override ThriftType? GetThriftType( Type type )
            {
                return _thriftType;
            }

            internal override object Read( IThriftProtocol protocol, Type targetType )
            {
                var elemType = targetType.GetGenericInterface( _collectionGenericType ).GetGenericArguments()[0];
                var elemSerializer = ThriftSerializer.FromType( elemType );
                var addMethod = ReflectionEx.GetAddMethod( targetType, _collectionGenericType );

                var inst = ReflectionEx.Create( targetType );

                var header = _readHeader( protocol )();
                for ( int n = 0; n < header.Count; n++ )
                {
                    addMethod.Invoke( inst, new[] { elemSerializer.Read( protocol, elemType ) } );
                }
                _readEnd( protocol )();

                return inst;
            }

            internal override void Skip( IThriftProtocol protocol )
            {
                var header = _readHeader( protocol )();
                var ttype = ThriftSerializer.FromThriftType( header.ElementType );
                for ( int n = 0; n < header.Count; n++ )
                {
                    ttype.Skip( protocol );
                }
                _readEnd( protocol )();
            }

            internal override void Write( IThriftProtocol protocol, object obj )
            {
                var elemType = obj.GetType().GetGenericInterface( _collectionGenericType ).GetGenericArguments()[0];
                var elemSerializer = ThriftSerializer.FromType( elemType );
                var arr = ( (IEnumerable) obj ).Cast<object>().ToArray();

                _writeHeader( protocol )( new ThriftCollectionHeader( arr.Length, elemSerializer.GetThriftType( elemType ).Value ) );
                foreach ( var elem in arr )
                {
                    elemSerializer.Write( protocol, elem );
                }
                _writeEnd( protocol )();
            }
        }

        /// <summary>
        /// The Thrift map serializer.
        /// </summary>
        private sealed class MapSerializer : ThriftSerializer
        {
            internal override bool MatchesType( Type type )
            {
                return type.GetGenericInterface( typeof( IDictionary<,> ) ) != null;
            }

            internal override ThriftType? GetThriftType( Type type )
            {
                return ThriftType.Map;
            }

            internal override object Read( IThriftProtocol protocol, Type targetType )
            {
                var typeArgs = targetType.GetGenericArguments();
                var keyType = typeArgs[0];
                var valueType = typeArgs[1];
                var keySerializer = ThriftSerializer.FromType( keyType );
                var valueSerializer = ThriftSerializer.FromType( valueType );
                var addMethod = ReflectionEx.GetAddMethod( targetType, typeof( IDictionary<,> ) );

                var inst = ReflectionEx.Create( targetType );

                var header = protocol.ReadMapHeader();
                for ( int n = 0; n < header.Count; n++ )
                {
                    addMethod.Invoke( inst, new[] { keySerializer.Read( protocol, keyType ), valueSerializer.Read( protocol, valueType ) } );
                }
                protocol.ReadMapEnd();

                return inst;
            }

            internal override void Skip( IThriftProtocol protocol )
            {
                var header = protocol.ReadMapHeader();
                var keySerializer = ThriftSerializer.FromThriftType( header.KeyType );
                var valueSerializer = ThriftSerializer.FromThriftType( header.ValueType );
                for ( int n = 0; n < header.Count; n++ )
                {
                    keySerializer.Skip( protocol );
                    valueSerializer.Skip( protocol );
                }
                protocol.ReadMapEnd();
            }

            internal override void Write( IThriftProtocol protocol, object obj )
            {
                var typeArgs = obj.GetType().GetGenericArguments();
                var keyType = typeArgs[0];
                var valueType = typeArgs[1];
                var keySerializer = ThriftSerializer.FromType( keyType );
                var valueSerializer = ThriftSerializer.FromType( valueType );
                var map = (IDictionary) obj;

                protocol.WriteMapHeader( new ThriftMapHeader( map.Count, keySerializer.GetThriftType( keyType ).Value, valueSerializer.GetThriftType( valueType ).Value ) );
                var enumerator = map.GetEnumerator();
                while ( enumerator.MoveNext() )
                {
                    keySerializer.Write( protocol, enumerator.Key );
                    valueSerializer.Write( protocol, enumerator.Value );
                }
                protocol.WriteMapEnd();
            }
        }

        /// <summary>
        /// The Thrift serializer for nullable value types, which converts "null" to and from an unset value.
        /// </summary>
        private sealed class NullableSerializer : ThriftSerializer
        {
            internal override bool MatchesType( Type type )
            {
                return type.IsGenericType && type.GetGenericTypeDefinition() == typeof( Nullable<> );
            }

            internal override ThriftType? GetThriftType( Type type )
            {
                if ( type == null )
                {
                    return null;
                }
                var wrappedType = type.GetGenericArguments()[0];
                return ThriftSerializer.FromType( wrappedType ).GetThriftType( wrappedType );
            }

            internal override object Read( IThriftProtocol protocol, Type targetType )
            {
                var wrappedType = targetType.GetGenericArguments()[0];
                var targetCtor = targetType.GetConstructor( new[] { wrappedType } );
                var value = ThriftSerializer.FromType( wrappedType )
                                            .Read( protocol, wrappedType );
                return targetCtor.Invoke( new[] { value } );
            }

            internal override void Skip( IThriftProtocol protocol )
            {
                throw new InvalidOperationException( "NullableSerializer.Skip should never be called." );
            }

            internal override void Write( IThriftProtocol protocol, object obj )
            {
                ThriftSerializer.FromType( obj.GetType() ).Write( protocol, obj );
            }
        }

        /// <summary>
        /// The Thrift serializer for structs, which is the most complex one.
        /// </summary>
        private sealed class StructSerializer : ThriftSerializer
        {
            internal override bool MatchesType( Type type )
            {
                return true;
            }

            internal override ThriftType? GetThriftType( Type type )
            {
                return ThriftType.Struct;
            }

            internal override object Read( IThriftProtocol protocol, Type targetType )
            {
                var st = ThriftAttributesParser.ParseStruct( targetType );
                var inst = ReflectionEx.Create( targetType );
                return ThriftSerializer.ReadStruct( protocol, st, inst );
            }

            internal override void Skip( IThriftProtocol protocol )
            {
                protocol.ReadStructHeader();
                while ( true )
                {
                    var header = protocol.ReadFieldHeader();
                    if ( header == null )
                    {
                        break;
                    }

                    ThriftSerializer.FromThriftType( header.FieldType ).Skip( protocol );
                    protocol.ReadFieldEnd();
                }
                protocol.ReadStructEnd();
            }

            internal override void Write( IThriftProtocol protocol, object obj )
            {
                var st = ThriftAttributesParser.ParseStruct( obj.GetType() );
                ThriftSerializer.WriteStruct( protocol, st, obj );
            }
        }
    }
}