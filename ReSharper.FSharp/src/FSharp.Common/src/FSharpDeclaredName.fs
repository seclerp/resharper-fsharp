namespace JetBrains.ReSharper.Plugins.FSharp

type FSharpDeclaredName =
    | SingleName of sourceName: string
    | AlternativeNames of sourceName: string * compiledName: string

    member this.SourceName =
        match this with
        | SingleName(sourceName = name)
        | AlternativeNames(sourceName = name) -> name

    member this.CompiledName =
        match this with
        | SingleName(sourceName = name)
        | AlternativeNames(compiledName = name) -> name

    /// IAlternativeNameOwner.AlternativeName should return null when there's effectively no alternative name.
    member this.AlternativeName =
        match this with
        | AlternativeNames(source, compiled) when source <> compiled -> source
        | _ -> null
