using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpNestedTypeUnionCase : FSharpClass, IFSharpGeneratedFromUnionCase
  {
    public FSharpNestedTypeUnionCase([NotNull] IClassPart part) : base(part)
    {
    }

    public IClrDeclaredElement OriginElement =>
    EnumerateParts()
      .Select(part => (part as UnionCasePart)?.UnionCase)
      .WhereNotNull()
      .FirstOrDefault().NotNull(); // todo

    public IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer() => 
      new FSharpNestedTypeUnionCasePointer(this);
  }

  public class FSharpNestedTypeUnionCasePointer : FSharpGeneratedElementPointerBase<FSharpNestedTypeUnionCase, IUnionCaseWithFields>
  {
    public FSharpNestedTypeUnionCasePointer(FSharpNestedTypeUnionCase nestedType) : base(nestedType)
    {
    }

    public override FSharpNestedTypeUnionCase CreateGenerated(IUnionCaseWithFields unionCase) =>
      unionCase.NestedType;
  }
}
