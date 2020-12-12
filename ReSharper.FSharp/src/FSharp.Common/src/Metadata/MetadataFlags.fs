namespace JetBrains.ReSharper.Plugins.FSharp.Metadata

open System

type Option =
    | None = 0
    | Some = 1

type EntityKind =
    | ModuleWithSuffix = 0
    | ModuleOrType = 1
    | Namespace = 2

type ObjectModelTypeKind =
    | Class = 0
    | Interface = 1
    | Struct = 2
    | Delegate = 3
    | Enum = 4

type TypeOrMeasureKind =
    | Type = 0
    | Measure = 1

type TypeKind =
    | Tuple = 0
    | SimpleType = 1
    | TypeApp = 2
    | Function = 3
    | TypeReference = 4
    | ForAll = 5
    | Measure = 6
    | UnionCase = 7
    | StructTuple = 8
    | AnonRecord = 9

type TypeRepresentationKind =
    | Record = 0
    | Union = 1
    | MetadataType = 2
    | ObjectModel = 3
    | Measure = 4

type ExceptionRepresentationKind =
    | Abbreviation = 0
    | MetadataType = 1
    | Fresh = 2
    | None = 3

type IlType =
    | Void = 0
    | Array = 1
    | Value = 2
    | Boxed = 3 // reference?
    | Pointer = 4
    | Byref = 5
    | FunctionPointer = 6
    | TypeParameter = 7
    | Modified = 8 // what's this?

type IlScopeRef =
    | Local = 0
    | Module = 1
    | Assembly = 2

type ReferenceKind =
    | Local = 0
    | NonLocal = 1

type AttributeKind =
    | Metadata = 0
    | FSharp = 1

type MeasureExpression =
    | Constant = 0
    | Inverse = 1
    | Product = 2
    | Variable = 3
    | One = 4
    | RationalPower = 5

type StaticOptimization =
    | TypeEquality = 0
    | Struct = 1


[<Flags>]
type EntityFlags =
    | IsModuleOrNamespace = 0b000000000000001L
    | IsStruct            = 0b000000000100000L
    | ReservedBit         = 0b000000000010000L

[<Flags>]
type ValFlags =
    | IsCompilerGenerated = 0b00000000000000001000L
