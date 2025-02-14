namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.PostfixTemplates

open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

module FSharpPostfixTemplates =
    let isApplicableTypeUsage (typeUsage: ITypeUsage) =
        let typeUsage = skipIntermediateParentsOfSameType<ITypeUsage> typeUsage
        let typedExpr = TypedLikeExprNavigator.GetByTypeUsage(typeUsage)
        isNotNull typedExpr


[<AllowNullLiteral>]
type FSharpPostfixTemplateContext(node: ITreeNode, executionContext: PostfixTemplateExecutionContext) =
    inherit PostfixTemplateContext(node, executionContext)

    // todo: override IsSemanticallyMakeSense to disable on namespaces/modules?

    override this.Language = FSharpLanguage.Instance :> _

    override this.GetAllExpressionContexts() =
        [| FSharpPostfixExpressionContext(this, node) :> PostfixExpressionContext |] :> _


and FSharpPostfixExpressionContext(postfixContext, expression) =
    inherit PostfixExpressionContext(postfixContext, expression)


[<Language(typeof<FSharpLanguage>)>]
type FSharpPostfixTemplateContextFactory() =
    interface IPostfixTemplateContextFactory with
        member this.GetReparseStrings() = EmptyArray.Instance

        member this.TryCreate(node, executionContext) =
            let node = FSharpReparsedCodeCompletionContext.FixReferenceOwnerUnderTransaction(node)
            let fsIdentifier = node.As<IFSharpIdentifier>()

            let referenceExpr = ReferenceExprNavigator.GetByIdentifier(fsIdentifier)
            if isNotNull referenceExpr then FSharpPostfixTemplateContext(referenceExpr, executionContext) :> _ else

            let referenceName = TypeReferenceNameNavigator.GetByIdentifier(fsIdentifier)
            if isNotNull referenceName then FSharpPostfixTemplateContext(referenceName, executionContext) :> _ else

            null


[<Language(typeof<FSharpLanguage>)>]
type FSharpPostfixTemplatesProvider(templatesManager, sessionExecutor, usageStatistics) =
    inherit PostfixTemplatesItemProviderBase<FSharpCodeCompletionContext, FSharpPostfixTemplateContext>(
        templatesManager, sessionExecutor, usageStatistics)

    override this.TryCreatePostfixContext(fsCompletionContext) =
        if fsCompletionContext.NodeInFile.IsFSharpSigFile() then null else

        let reparsedContext = fsCompletionContext.ReparsedContext
        let reference = reparsedContext.Reference.As<FSharpSymbolReference>()
        if isNull reference || not reference.IsQualified then null else

        let node = reference.GetTreeNode().NotNull()
        let context = fsCompletionContext.BasicContext
        let settings = context.ContextBoundSettingsStore
        let executionContext = PostfixTemplateExecutionContext(context.Solution, context.TextControl, settings, "__")
        FSharpPostfixTemplateContext(node, executionContext)


[<AbstractClass>]
type FSharpPostfixTemplateBehaviorBase(info) =
    inherit PostfixTemplateBehavior(info)

    let rec getContainingArgExpr (expr: IFSharpExpression) =
        match PrefixAppExprNavigator.GetByArgumentExpression(expr) with
        | null ->
            match BinaryAppExprNavigator.GetByRightArgument(expr) with
            | null -> expr
            | binaryAppExpr -> binaryAppExpr :> _
        | appExpr -> getContainingArgExpr appExpr

    let getContainingTypeExpression (typeName: ITypeReferenceName) =
        let namedTypeUsage = NamedTypeUsageNavigator.GetByReferenceName(typeName)
        let tupleTypeUsage = TupleTypeUsageNavigator.GetByItem(namedTypeUsage)

        let typeUsage: ITypeUsage =
            if isNotNull tupleTypeUsage && tupleTypeUsage.Items.Last() == namedTypeUsage then
                tupleTypeUsage :> _
            else
                namedTypeUsage :> _

        let functionTypeUsage = FunctionTypeUsageNavigator.GetByReturnTypeUsage(typeUsage)
        let typeUsage: ITypeUsage = if isNotNull functionTypeUsage then functionTypeUsage :> _ else typeUsage

        let typedExpr = TypedLikeExprNavigator.GetByTypeUsage(typeUsage)
        if isNotNull typedExpr then
            getContainingArgExpr typedExpr else

        null

    let rec getParentExpression (token: IFSharpTreeNode): IFSharpExpression =
        match token with
        | :? IReferenceExpr as refExpr ->
            let qualifier = refExpr.Qualifier.NotNull()
            ModificationUtil.ReplaceChild(refExpr, qualifier.Copy())

        | :? ITypeReferenceName as referenceName ->
            let qualifier = referenceName.Qualifier.As<ITypeReferenceName>().NotNull()
            let newReferenceName = ModificationUtil.ReplaceChild(referenceName, qualifier.Copy())
            getContainingTypeExpression newReferenceName

        | _ -> null

    member this.GetExpression(context: PostfixExpressionContext) =
        let node = context.Expression :?> IFSharpTreeNode
        let parent = getParentExpression node
        getContainingArgExpr parent


[<AbstractClass>]
type FSharpPostfixTemplateBase() =
    member this.Language = FSharpLanguage.Instance

    abstract CreateBehavior: PostfixTemplateInfo -> PostfixTemplateBehavior
    abstract TryCreateInfo: PostfixTemplateContext -> PostfixTemplateInfo

    abstract IsEnabled: ISolution -> bool
    default this.IsEnabled(solution) =
        solution.IsFSharpExperimentalFeatureEnabled(ExperimentalFeature.PostfixTemplates)

    interface IPostfixTemplate with
        member this.Language = this.Language :> _
        member this.CreateBehavior(info) = this.CreateBehavior(info)

        member this.TryCreateInfo(context) =
            if this.IsEnabled(context.PsiModule.GetSolution()) then
                this.TryCreateInfo(context)
            else
                null
