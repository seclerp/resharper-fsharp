namespace JetBrains.ReSharper.Plugins.FSharp.Metadata

open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.Util.Extension

module FSharpMetadata =
    [<CompiledName("GetCompiledModuleName")>]
    let getCompiledModuleName (entityKind: EntityKind) (logicalName: string) =
        match entityKind with
        | EntityKind.ModuleWithSuffix -> AlternativeNames(logicalName.SubstringBeforeLast("Module"), logicalName)
        | _ -> SingleName(logicalName)

    [<CompiledName("GetCompiledTypeName")>]
    let getCompiledTypeName logicalName compiledName =
        match compiledName with
        | Some(name) -> AlternativeNames(logicalName, name)
        | _ -> SingleName(logicalName)


type FSharpCompiledUnionCase =
    { Name: string }

type FSharpCompiledRecordField =
    { Name: string
      IsMutable: bool }

type FSharpCompiledTypeRepresentation =
    | Enum of cases: string[]
    | Union of cases: FSharpCompiledUnionCase[]
    | Record of fields: FSharpCompiledRecordField[]
    | ObjectModel of kind: ObjectModelTypeKind

type FSharpCompiledType =
    { Name: FSharpDeclaredName
      Representation: FSharpCompiledTypeRepresentation option }
