﻿namespace FsCoreSerializer

    open System
    open System.IO
    open System.Collections.Generic
    open System.Runtime.Serialization

    // pluggable type serialization

    /// <summary>Provides facility for implementing a custom type serialization scheme.
    /// This is particularly useful in cases where bridging mono/.NET runtimes or
    /// dynamic/static assemblies is required.</summary>
    type ITypeNameConverter =
        abstract ToQualifiedName : Type -> string
        abstract OfQualifiedName : string -> Type

    and internal TypeFormatter private () =
        static let mutable conv = DefaultTypeNameConverter () :> ITypeNameConverter

        static member TypeNameConverter 
            with get () = conv
            and set converter = conv <- converter

        static member Write (bw : BinaryWriter) (t : Type) =
            if t.IsGenericParameter then
                bw.Write (conv.ToQualifiedName t.ReflectedType)
                bw.Write true
                bw.Write t.Name
            else
                bw.Write (conv.ToQualifiedName t)
                bw.Write false

        static member Read (br : BinaryReader) =
            let aqn = br.ReadString()
            let t = conv.OfQualifiedName aqn
            if br.ReadBoolean() then
                // is generic parameter
                let pname = br.ReadString()
                try t.GetGenericArguments() |> Array.find(fun a -> a.Name = pname)
                with :? KeyNotFoundException -> 
                    raise <| new SerializationException(sprintf "cannot deserialize type '%s'" pname)
            else
                t

    /// provides standard type serialization
    and DefaultTypeNameConverter () =
        interface ITypeNameConverter with
            member __.ToQualifiedName (t : Type) = t.AssemblyQualifiedName
            member __.OfQualifiedName (aqn : string) = Type.GetType(aqn, true)