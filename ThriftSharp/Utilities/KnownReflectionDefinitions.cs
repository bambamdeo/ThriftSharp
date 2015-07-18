﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThriftSharp.Internals;
using ThriftSharp.Protocols;

namespace ThriftSharp.Utilities
{
    internal static class TypeInfos
    {
        public static readonly TypeInfo
            Void = typeof( void ).GetTypeInfo(),
            String = typeof( string ).GetTypeInfo(),
            HashSetOfShort = typeof( HashSet<short> ).GetTypeInfo(),
            IThriftProtocol = typeof( IThriftProtocol ).GetTypeInfo(),
            ThriftStructReader = typeof( ThriftStructReader ).GetTypeInfo(),
            ThriftStructHeader = typeof( ThriftStructHeader ).GetTypeInfo(),
            ThriftFieldHeader = typeof( ThriftFieldHeader ).GetTypeInfo(),
            ThriftMapHeader = typeof( ThriftMapHeader ).GetTypeInfo(),
            ThriftCollectionHeader = typeof( ThriftCollectionHeader ).GetTypeInfo(),
            ThriftSerializationException = typeof( ThriftSerializationException ).GetTypeInfo();
    }


    internal static class Constructors
    {
        public static readonly ConstructorInfo
            ThriftCollectionHeader = TypeInfos.ThriftCollectionHeader.DeclaredConstructors.Single(),
            ThriftMapHeader = TypeInfos.ThriftMapHeader.DeclaredConstructors.Single(),
            ThriftFieldHeader = TypeInfos.ThriftFieldHeader.DeclaredConstructors.Single(),
            ThriftStructHeader = TypeInfos.ThriftStructHeader.DeclaredConstructors.Single(),
            ThriftMessageHeader = typeof( ThriftMessageHeader ).GetTypeInfo().DeclaredConstructors.Single(),
            ThriftProtocolException = typeof( ThriftProtocolException ).GetTypeInfo().DeclaredConstructors.Single( c => c.GetParameters().Length == 1 );
    }

    internal static class Methods
    {
        public static readonly MethodInfo
            IEnumerator_MoveNext = typeof( IEnumerator ).GetTypeInfo().GetDeclaredMethod( "MoveNext" ),
            IDisposable_Dispose = typeof( IDisposable ).GetTypeInfo().GetDeclaredMethod( "Dispose" ),
            Enum_IsDefined = typeof( Enum ).GetTypeInfo().GetDeclaredMethod( "IsDefined" ),
            HashSetOfShort_Add = TypeInfos.HashSetOfShort.GetDeclaredMethod( "Add" ),
            HashSetOfShort_Contains = TypeInfos.HashSetOfShort.GetDeclaredMethod( "Contains" ),
            ThriftStructReader_Read = TypeInfos.ThriftStructReader.GetDeclaredMethod( "Read" ),
            ThriftStructReader_Skip = TypeInfos.ThriftStructReader.GetDeclaredMethod( "Skip" ),
            ThriftSerializationException_TypeIdMismatch = TypeInfos.ThriftSerializationException.GetDeclaredMethod( "TypeIdMismatch" ),
            ThriftSerializationException_MissingRequiredField = TypeInfos.ThriftSerializationException.GetDeclaredMethod( "MissingRequiredField" ),
            IThriftProtocol_ReadBoolean = TypeInfos.IThriftProtocol.GetDeclaredMethod( "ReadBoolean" ),
            IThriftProtocol_ReadSByte = TypeInfos.IThriftProtocol.GetDeclaredMethod( "ReadSByte" ),
            IThriftProtocol_ReadDouble = TypeInfos.IThriftProtocol.GetDeclaredMethod( "ReadDouble" ),
            IThriftProtocol_ReadInt16 = TypeInfos.IThriftProtocol.GetDeclaredMethod( "ReadInt16" ),
            IThriftProtocol_ReadInt32 = TypeInfos.IThriftProtocol.GetDeclaredMethod( "ReadInt32" ),
            IThriftProtocol_ReadInt64 = TypeInfos.IThriftProtocol.GetDeclaredMethod( "ReadInt64" ),
            IThriftProtocol_ReadString = TypeInfos.IThriftProtocol.GetDeclaredMethod( "ReadString" ),
            IThriftProtocol_ReadBinary = TypeInfos.IThriftProtocol.GetDeclaredMethod( "ReadBinary" ),
            IThriftProtocol_ReadStructHeader = TypeInfos.IThriftProtocol.GetDeclaredMethod( "ReadStructHeader" ),
            IThriftProtocol_ReadStructEnd = TypeInfos.IThriftProtocol.GetDeclaredMethod( "ReadStructEnd" ),
            IThriftProtocol_ReadFieldHeader = TypeInfos.IThriftProtocol.GetDeclaredMethod( "ReadFieldHeader" ),
            IThriftProtocol_ReadFieldEnd = TypeInfos.IThriftProtocol.GetDeclaredMethod( "ReadFieldEnd" ),
            IThriftProtocol_ReadMapHeader = TypeInfos.IThriftProtocol.GetDeclaredMethod( "ReadMapHeader" ),
            IThriftProtocol_ReadMapEnd = TypeInfos.IThriftProtocol.GetDeclaredMethod( "ReadMapEnd" ),
            IThriftProtocol_ReadListHeader = TypeInfos.IThriftProtocol.GetDeclaredMethod( "ReadListHeader" ),
            IThriftProtocol_ReadListEnd = TypeInfos.IThriftProtocol.GetDeclaredMethod( "ReadListEnd" ),
            IThriftProtocol_ReadSetHeader = TypeInfos.IThriftProtocol.GetDeclaredMethod( "ReadSetHeader" ),
            IThriftProtocol_ReadSetEnd = TypeInfos.IThriftProtocol.GetDeclaredMethod( "ReadSetEnd" );
    }

    internal static class Fields
    {
        public static readonly FieldInfo
            ThriftMapHeader_KeyTypeId = TypeInfos.ThriftMapHeader.GetDeclaredField( "KeyTypeId" ),
            ThriftMapHeader_ValueTypeId = TypeInfos.ThriftMapHeader.GetDeclaredField( "ValueTypeId" ),
            ThriftMapHeader_Count = TypeInfos.ThriftMapHeader.GetDeclaredField( "Count" ),
            ThriftCollectionHeader_ElementTypeId = TypeInfos.ThriftCollectionHeader.GetDeclaredField( "ElementTypeId" ),
            ThriftCollectionHeader_Count = TypeInfos.ThriftCollectionHeader.GetDeclaredField( "Count" ),
            ThriftFieldHeader_Id = TypeInfos.ThriftFieldHeader.GetDeclaredField( "Id" ),
            ThriftFieldHeader_TypeId = TypeInfos.ThriftFieldHeader.GetDeclaredField( "TypeId" );
    }

    internal static class Types
    {
        public static readonly Type[] EmptyTypes = new Type[0];
    }
}