﻿namespace ThriftSharp.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open ThriftSharp
open ThriftSharp.Internals

[<ThriftService("Service")>]
type Service6 =
    [<ThriftMethod("noParameters")>]
    abstract NoParameters: unit -> unit

    [<ThriftMethod("oneParameter")>]
    abstract OneParameter: [<ThriftParameter(1s, "p1")>] p1: int -> unit

    [<ThriftMethod("manyParameters")>]
    abstract ManyParameters: [<ThriftParameter(1s, "p1")>] p1: int 
                           * [<ThriftParameter(2s, "p2")>] p2: string 
                           * [<ThriftParameter(10s, "p10")>] p10: string -> unit

    [<ThriftMethod("oneWay", true)>]
    abstract OneWay: unit -> unit

    [<ThriftMethod("withUnixDateParam")>]
    abstract WithUnixDateParam: 
        [<ThriftParameter(1s, "date"); ThriftConverter(typeof<ThriftUnixDateConverter>)>] date: System.DateTime -> unit

[<TestClass>]
type ``Writing service queries``() =
    member x.WriteMsg<'T>(methodName, args) =
        let m = MemoryProtocol([MessageHeader (0, "", ThriftMessageType.Reply)
                                StructHeader ""
                                FieldStop
                                StructEnd
                                MessageEnd])
        let svc = ThriftAttributesParser.ParseService(typeof<'T>)
        Thrift.CallMethod(ThriftCommunication(m), svc, methodName, args) |> ignore
        m

    [<Test>]
    member x.``No parameters``() =
        let m = x.WriteMsg<Service6>("NoParameters", [| |])
        
        m.WrittenValues <===> [MessageHeader (0, "noParameters", ThriftMessageType.Call)
                               StructHeader ""
                               FieldStop
                               StructEnd
                               MessageEnd]

    [<Test>]
    member x.``One parameter``() =
        let m = x.WriteMsg<Service6>("OneParameter", [| 123 |])

        m.WrittenValues <===> [MessageHeader (0, "oneParameter", ThriftMessageType.Call)
                               StructHeader ""
                               FieldHeader (1s, "p1", ThriftType.Int32)
                               Int32 123
                               FieldEnd
                               FieldStop
                               StructEnd
                               MessageEnd]

    [<Test>]
    member x.``Many parameters``() =
        let m = x.WriteMsg<Service6>("ManyParameters", [| 16; "Sayid"; "Jarrah" |])

        m.WrittenValues <===> [MessageHeader (0, "manyParameters", ThriftMessageType.Call)
                               StructHeader ""
                               FieldHeader (1s, "p1", ThriftType.Int32)
                               Int32 16
                               FieldEnd
                               FieldHeader (2s, "p2", ThriftType.String)
                               String "Sayid"
                               FieldEnd
                               FieldHeader (10s, "p10", ThriftType.String)
                               String "Jarrah"
                               FieldEnd
                               FieldStop
                               StructEnd
                               MessageEnd]

    [<Test>]
    member x.``One-way method``() =
        let m = x.WriteMsg<Service6>("OneWay", [| |])

        m.WrittenValues <===> [MessageHeader (0, "oneWay", ThriftMessageType.OneWay)
                               StructHeader ""
                               FieldStop
                               StructEnd
                               MessageEnd]

    [<Test>]
    member x.``UnixDate parameter``() =
        let m = x.WriteMsg<Service6>("WithUnixDateParam", [| System.DateTime(1994, 12, 18) |])

        m.WrittenValues <===> [MessageHeader (0, "withUnixDateParam", ThriftMessageType.Call)
                               StructHeader ""
                               FieldHeader (1s, "date", ThriftType.Int32)
                               Int32 787708800
                               FieldEnd
                               FieldStop
                               StructEnd
                               MessageEnd]

    [<Test>]
    member x.``ArgumentException is thrown when there are too few params``() =
        throws<System.ArgumentException>(fun () -> box (x.WriteMsg<Service6>("OneParameter", [| |]))) |> ignore
        
    [<Test>]
    member x.``ArgumentException is thrown when there are too many params``() =
        throws<System.ArgumentException>(fun () -> box (x.WriteMsg<Service6>("OneParameter", [| 1; 2 |]))) |> ignore