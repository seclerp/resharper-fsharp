using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  /// <summary>
  /// A union case compiled to a static property.
  /// </summary>
  internal class FSharpUnionCaseProperty : FSharpCompiledPropertyBase<IUnionCaseLikeDeclaration>, IUnionCase
  {
    internal FSharpUnionCaseProperty([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override AccessRights GetAccessRights() => GetContainingType().GetRepresentationAccessRights();
    public AccessRights RepresentationAccessRights => GetContainingType().GetFSharpRepresentationAccessRights();

    public override bool IsStatic => true;

    public override IType ReturnType =>
      GetContainingType() is var containingType && containingType != null
        ? TypeFactory.CreateType(containingType)
        : TypeFactory.CreateUnknownType(Module);
  }

  internal class FSharpHiddenUnionCaseProperty : FSharpUnionCaseProperty, IUnionCaseWithFields
  {
    internal FSharpHiddenUnionCaseProperty([NotNull] NestedTypeUnionCaseDeclaration declaration) : base(declaration)
    {
    }

    public override AccessRights GetAccessRights() => AccessRights.PRIVATE;

    public IList<IUnionCaseField> CaseFields =>
      ((INestedTypeUnionCaseDeclaration) GetDeclaration())?.Fields.Select(d => (IUnionCaseField) d.DeclaredElement).ToIList();

    // todo?
    public FSharpNestedTypeUnionCase NestedType => 
      ((NestedTypeUnionCaseDeclaration)GetDeclaration()).NestedType;

    public IParametersOwner GetConstructor() =>
      new NewUnionCaseMethod(this);
  }
}
