namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Components
open JetBrains.Application.Environment
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi
open JetBrains.ReSharper.TestFramework
open JetBrains.TestFramework
open JetBrains.TestFramework.Application.Zones
open NUnit.Framework

[<ZoneDefinition>]
type IFSharpTestsZone = 
    inherit ITestsEnvZone

[<ZoneActivator>]
type PsiFeatureTestZoneActivator() = 
    interface IActivate<PsiFeatureTestZone> with
        member x.ActivatorEnabled() = true

[<ZoneActivator>]
type FSharpZoneActivator() = 
    interface IActivate<ILanguageFSharpZone> with
        member x.ActivatorEnabled() = true

[<SetUpFixture>]
type PsiFeaturesTestEnvironmentAssembly() = 
    inherit TestEnvironmentAssembly<IFSharpTestsZone>() with
    override x.IsRunningTestsWithAsyncBehaviorProhibited = true

