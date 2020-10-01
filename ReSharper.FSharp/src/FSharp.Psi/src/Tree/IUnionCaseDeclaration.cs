using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IUnionCaseLikeDeclaration : ITypeMemberDeclaration, IFSharpDeclaration, IModifiersOwnerDeclaration
  {
  }

  public partial interface INestedTypeUnionCaseDeclaration
  {
    FSharpNestedTypeUnionCase NestedType { get; }
  }
}