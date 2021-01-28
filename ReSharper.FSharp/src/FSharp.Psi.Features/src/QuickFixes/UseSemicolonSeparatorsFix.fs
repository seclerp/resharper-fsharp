namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System.Text.RegularExpressions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

type UseSemicolonSeparatorsFix(warning: AddTypeEquationError) =
    inherit FSharpQuickFixBase()

    let regex = Regex("'\w\d*( \* '\w\d*)+")

    let tupleExpr = warning.Expr.As<ITupleExpr>()
    let contextExpr = tupleExpr.IgnoreParentParens()

    let arrayOrListExpr = ArrayOrListExprNavigator.GetByExpression(contextExpr)

    override x.Text = "Use ';' separators"

    override x.IsAvailable _ =
        let actualType = warning.ActualType
        isValid tupleExpr && isNotNull arrayOrListExpr && regex.Match(actualType).Value = actualType

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(tupleExpr.IsPhysical())

        for comma in tupleExpr.Commas do
            replaceWithToken comma FSharpTokenType.SEMICOLON

        LowLevelModificationUtil.AddChildBefore(contextExpr, tupleExpr.Children().AsArray())
        LowLevelModificationUtil.DeleteChild(contextExpr)
