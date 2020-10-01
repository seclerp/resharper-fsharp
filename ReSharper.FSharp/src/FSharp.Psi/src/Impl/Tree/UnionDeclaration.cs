using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class UnionDeclaration
  {
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(AllAttributes);
    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;

    public override IReadOnlyList<ITypeMemberDeclaration> MemberDeclarations
    {
      get
      {
        var result = new List<ITypeMemberDeclaration>();
        result.AddRange(base.MemberDeclarations);

        foreach (var unionCase in UnionCases)
        {
          result.Add(unionCase);
          if (unionCase is INestedTypeUnionCaseDeclaration nestedTypeUnionCaseDeclaration &&
              nestedTypeUnionCaseDeclaration.NestedType == null)
          {
            result.AddRange(nestedTypeUnionCaseDeclaration.FieldsEnumerable);
          }
        }

        return result;
      }
    }

    public override PartKind TypePartKind =>
      FSharpImplUtil.GetTypeKind(AllAttributes, out var typeKind)
        ? typeKind
        : PartKind.Class;
  }
}
