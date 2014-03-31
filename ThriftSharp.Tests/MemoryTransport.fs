﻿// Copyright (c) 2013 Solal Pirelli
// This code is licensed under the MIT License (see Licence.txt for details).
// Redistributions of this source code must retain the above copyright notice.

namespace ThriftSharp.Tests

open System.Collections.Generic
open System.Threading.Tasks
open ThriftSharp.Transport

type MemoryTransport(toRead: byte list) =
    let mutable writtenVals = []
    let mutable hasRead = false
    let toRead = Queue(toRead)

    let write values = for value in values do writtenVals <- value::writtenVals
    let read len = [| for x = 1 to len do
                          if toRead.Count > 0 then yield toRead.Dequeue()
                          else failwith "Not enough bytes were read." |]

    member x.WrittenValues with get() = List.rev writtenVals
    member x.IsEmpty with get() = toRead.Count = 0

    new() = MemoryTransport([])

    interface IThriftTransport with
        member x.WriteByte(b) =
            (x :> IThriftTransport).WriteBytes([| b |])

        member x.WriteBytes(bs) =
            if hasRead then failwith "Cannot write after a read. Close the transport first."
            write bs

        member x.ReadByteAsync() =
            Task.FromResult((x :> IThriftTransport).ReadBytesAsync(1).Result.[0])
            
        member x.ReadBytesAsync(len) =
            if not hasRead then
                hasRead <- true
            Task.FromResult(read len)

        member x.FlushAsync() =
            hasRead <- true
            Task.FromResult(0) :> Task

        member x.Dispose() =
            hasRead <- false