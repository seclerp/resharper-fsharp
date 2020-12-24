namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Caches.SymbolCache
open JetBrains.ReSharper.Psi.Impl.Reflection2
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest; AbstractClass>]
type FSharpReferencedAssemblyTestBase() =
    inherit BaseTestWithSingleProject()

    abstract DoTest: assemblyModule: IPsiModule -> unit

    member x.ExecuteWithGold(writer) =
        base.ExecuteWithGold(writer)

    member x.DoTest(moduleName: string) =
        use cookie = ReadLockCookie.Create()
        x.WithSingleProject([], fun _ (solution: ISolution) (project: IProject) ->
            let psiModules = solution.PsiModules()
            let psiModule = psiModules.GetModules() |> Seq.find (fun psiModule -> psiModule.Name = moduleName)
            x.DoTest(psiModule)
        )

type FSharpMetadataReaderTest() =
    inherit FSharpReferencedAssemblyTestBase()

    override x.RelativeTestDataPath = "common/metadataReader"

    override x.DoTest(psiModule: IPsiModule) =
        let psiServices = x.Solution.GetPsiServices()

        let isAccessible (typeElement: ITypeElement) =
            match typeElement with
            | :? CompiledTypeElement as typeElement -> typeElement.GetAccessRights() = AccessRights.PUBLIC
            | _ -> false

        x.ExecuteWithGold(fun writer ->
            let symbolCache = psiServices.GetComponent<SymbolCache>()
            symbolCache.TestDumpModule(writer, psiModule, Predicate(isAccessible))
        ) |> ignore

    [<Test; TestPackages(FSharpCorePackage)>]
    member x.FSharpCore() = x.DoTest("FSharp.Core")

    [<Test; TestReferences("TypeInGlobalNamespace.dll")>]
    member x.``Type in global namespace``() = x.DoTest("TypeInGlobalNamespace")

    [<Test; TestReferences("MetadataReading1.dll")>]
    member x.MetadataReading1() = x.DoTest("MetadataReading1")
