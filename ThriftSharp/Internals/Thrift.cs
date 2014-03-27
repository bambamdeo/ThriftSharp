﻿// Copyright (c) 2013 Solal Pirelli
// This code is licensed under the MIT License (see Licence.txt for details).
// Redistributions of this source code must retain the above copyright notice.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThriftSharp.Protocols;
using ThriftSharp.Utilities;

namespace ThriftSharp.Internals
{
    /// <summary>
    /// Single entry point to Thrift#'s internals.
    /// Translates Thrift interface definitions into protocol calls.
    /// </summary>
    internal static class Thrift
    {
        /// <summary>
        /// Creates a read-only ThriftField with the specified header and value.
        /// </summary>
        private static ThriftField ReadOnlyField( short id, string name, Type underlyingType, object value )
        {
            return new ThriftField( id, name, true, new Option<object>(), underlyingType, _ => value, null );
        }

        /// <summary>
        /// Creates a ThriftStruct representing the specified parameters of the specified method.
        /// </summary>
        private static ThriftStruct MakeParametersStruct( ThriftMethod method, object[] args )
        {
            if ( args.Length != method.Parameters.Count )
            {
                throw new ArgumentException( string.Format( "Parameter count mismatch. Expected {0}, got {1}.", method.Parameters.Count, args.Length ) );
            }

            var paramFields = new ThriftField[method.Parameters.Count];
            for ( int n = 0; n < method.Parameters.Count; n++ )
            {
                var param = method.Parameters[n];
                var type = ThriftSerializer.FromType( param.UnderlyingType );

                if ( param.Converter == null )
                {
                    paramFields[n] = ReadOnlyField( param.Id, param.Name, param.UnderlyingType, args[n] );
                }
                else
                {
                    paramFields[n] = ReadOnlyField( param.Id, param.Name, param.Converter.FromType, param.Converter.ConvertBack( args[n] ) );
                }
            }

            return new ThriftStruct( new ThriftStructHeader( "" ), paramFields );
        }

        /// <summary>
        /// Calls the specified ThriftMethod on the specified protocol with the specified arguments.
        /// </summary>
        private static void CallMethod( IThriftProtocol protocol, ThriftMethod method, object[] args )
        {
            var msg = new ThriftMessageHeader( 0, method.Name, method.IsOneWay ? ThriftMessageType.OneWay : ThriftMessageType.Call );
            var paramSt = MakeParametersStruct( method, args );

            protocol.WriteMessageHeader( msg );
            ThriftSerializer.WriteStruct( protocol, paramSt, null );
            protocol.WriteMessageEnd();
        }


        /// <summary>
        /// Creates a set-only ThriftField with the specified header and values.
        /// </summary>
        private static ThriftField SetOnlyField( short id, string name, bool isRequired, Type underlyingType, Action<object> setter )
        {
            return new ThriftField( id, name, isRequired, new Option<object>(), underlyingType, null, ( _, v ) => setter( v ) );
        }

        /// <summary>
        /// Creates a ThriftStruct representing the return type of the specified ThriftMethod.
        /// </summary>
        private static Tuple<ThriftStruct, Container> MakeReturnStruct( ThriftMethod method )
        {
            var retFields = new List<ThriftField>();
            var retValContainer = new Container();

            if ( method.ReturnType != typeof( void ) )
            {
                if ( method.ReturnValueConverter == null )
                {
                    var retType = ThriftSerializer.FromType( method.ReturnType );
                    retFields.Add( SetOnlyField( 0, "", true, method.ReturnType, v => retValContainer.Value = v ) );
                }
                else
                {
                    var retType = ThriftSerializer.FromType( method.ReturnValueConverter.FromType );
                    retFields.Add( SetOnlyField( 0, "", true, method.ReturnValueConverter.FromType,
                                                 v => retValContainer.Value = method.ReturnValueConverter.Convert( v ) ) );
                }
            }

            foreach ( var e in method.Exceptions )
            {
                retFields.Add( SetOnlyField( e.Id, e.Name, false, e.ExceptionType, v => { throw (Exception) v; } ) );
            }

            return Tuple.Create( new ThriftStruct( new ThriftStructHeader( "" ), retFields ), retValContainer );
        }

        /// <summary>
        /// Reads a protocol exception from the specified protocol.
        /// </summary>
        private static ThriftProtocolException ReadException( IThriftProtocol protocol )
        {
            // Server exception (not a declared one)
            var exceptionType = typeof( ThriftProtocolException );
            var exception = ThriftSerializer.FromType( exceptionType ).Read( protocol, exceptionType );
            protocol.ReadMessageEnd();
            return (ThriftProtocolException) exception;
        }

        /// <summary>
        /// Reads a ThriftMessage returned by the specified ThriftMethod on the specified ThriftProtocol.
        /// </summary>
        private static object ReadMessage( IThriftProtocol protocol, ThriftMethod method )
        {
            var header = protocol.ReadMessageHeader();

            if ( !EnumEx.GetValues<ThriftMessageType>().Contains( header.MessageType ) )
            {
                throw new ThriftProtocolException( ThriftProtocolExceptionType.InvalidMessageType, "The returned Thrift message type is invalid." );
            }
            if ( header.MessageType == ThriftMessageType.Exception )
            {
                throw ReadException( protocol );
            }

            var retStAndVal = MakeReturnStruct( method );
            ThriftSerializer.ReadStruct( protocol, retStAndVal.Item1, null );
            protocol.ReadMessageEnd();
            // Dispose of it now that we have finished reading and writing
            // using() is quite dangerous in this case because of async stuff happening
            protocol.Dispose();

            return retStAndVal.Item2.Value;
        }


        /// <summary>
        /// Sends a Thrift message representing the specified method call with the specified arguments on the specified protocol.
        /// </summary>
        private static object SendMessage( IThriftProtocol protocol, ThriftMethod method, params object[] args )
        {
            Func<object> action = () =>
            {
                CallMethod( protocol, method, args );
                return ReadMessage( protocol, method );
            };

            if ( method.IsAsync )
            {
                return Task.Factory.StartNew( action );
            }
            return action();
        }


        /// <summary>
        /// Calls a ThriftMethod specified by its name with the specified arguments using the specified means of communication.
        /// </summary>
        /// <param name="communication">The means of communication with the server.</param>
        /// <param name="service">The Thrift service containing the method.</param>
        /// <param name="methodName">The underlying method name.</param>
        /// <param name="args">The method arguments.</param>
        /// <returns>The method result.</returns>
        public static object CallMethod( ThriftCommunication communication, ThriftService service, string methodName, params object[] args )
        {
            var protocol = communication.CreateProtocol();
            var method = service.Methods.FirstOrDefault( m => m.UnderlyingName == methodName );
            if ( method == null )
            {
                throw new ArgumentException( string.Format( "Invalid method name ({0})", methodName ) );
            }
            return SendMessage( protocol, method, args );
        }


        /// <summary>
        /// Utility class that holds a single value.
        /// </summary>
        private sealed class Container
        {
            public object Value { get; set; }
        }
    }
}