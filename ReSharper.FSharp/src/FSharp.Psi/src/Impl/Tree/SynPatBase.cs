using System.Collections.Generic;
using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TopReferencePat
  {
    public bool IsDeclaration => this.IsDeclaration();

    public override IEnumerable<IFSharpPattern> NestedPatterns => new[] {this};

    public bool IsMutable => Binding?.IsMutable ?? false;

    public void SetIsMutable(bool value)
    {
      var binding = Binding;
      Assertion.Assert(binding != null, "GetBinding() != null");
      binding.SetIsMutable(true);
    }

    public override IBindingLikeDeclaration Binding => this.GetBindingFromHeadPattern();
    public FSharpSymbolReference Reference => ReferenceName?.Reference;
  }

  internal partial class LocalReferencePat
  {
    public override IFSharpIdentifierLikeNode NameIdentifier => ReferenceName?.Identifier;

    public bool IsDeclaration => this.IsDeclaration();

    public override IEnumerable<IFSharpPattern> NestedPatterns => new[] {this};

    public override TreeTextRange GetNameIdentifierRange() =>
      NameIdentifier.GetNameIdentifierRange();

    public bool IsMutable => Binding?.IsMutable ?? false;

    public void SetIsMutable(bool value)
    {
      var binding = Binding;
      Assertion.Assert(binding != null, "GetBinding() != null");
      binding.SetIsMutable(true);
    }

    public bool CanBeMutable => Binding != null;

    public IBindingLikeDeclaration Binding => this.GetBindingFromHeadPattern();
    public FSharpSymbolReference Reference => ReferenceName?.Reference;
  }

  internal partial class ParametersOwnerPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      Parameters.SelectMany(param => param.NestedPatterns);

    public FSharpSymbolReference Reference => ReferenceName?.Reference;
  }

  internal partial class AsPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns
    {
      get
      {
        var pattern1Decls = LeftPattern?.NestedPatterns ?? EmptyList<IFSharpPattern>.Instance;
        var pattern2Decls = RightPattern?.NestedPatterns ?? EmptyList<IFSharpPattern>.Instance;
        return pattern2Decls.Prepend(pattern1Decls);
      }
    }
  }

  internal partial class OrPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns
    {
      get
      {
        var pattern1Decls = Pattern1?.NestedPatterns ?? EmptyList<IFSharpPattern>.Instance;
        var pattern2Decls = Pattern2?.NestedPatterns ?? EmptyList<IFSharpPattern>.Instance;
        return pattern2Decls.Prepend(pattern1Decls);
      }
    }
  }

  internal partial class AndsPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      Patterns.SelectMany(pat => pat.NestedPatterns);
  }

  internal partial class ArrayPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      Patterns.SelectMany(pat => pat.NestedPatterns);
  }

  internal partial class ListPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      Patterns.SelectMany(pat => pat.NestedPatterns);
  }

  internal partial class TuplePat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      Patterns.SelectMany(pat => pat.NestedPatterns);

    public bool IsStruct => StructKeyword != null;
  }

  internal partial class ParenPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      Pattern.NestedPatterns;
  }

  internal partial class AttribPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      Pattern.NestedPatterns;
  }

  internal partial class RecordPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      FieldPatterns.SelectMany(pat => pat.Pattern?.NestedPatterns).WhereNotNull();
  }

  internal partial class OptionalValPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      Pattern.NestedPatterns;
  }

  internal partial class TypedPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      Pattern.NestedPatterns;
  }

  internal partial class ListConsPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns
    {
      get
      {
        var pattern1Decls = HeadPattern?.NestedPatterns ?? EmptyList<IFSharpPattern>.Instance;
        var pattern2Decls = TailPattern?.NestedPatterns ?? EmptyList<IFSharpPattern>.Instance;
        return pattern2Decls.Prepend(pattern1Decls);
      }
    }
  }

  internal partial class FieldPat
  {
    public FSharpSymbolReference Reference => ReferenceName?.Reference;
  }

  internal static class ReferencePatternUtil
  {
    internal static bool IsDeclaration(this IReferencePat refPat)
    {
      if (refPat.Parent is IBindingLikeDeclaration)
        return true;

      var referenceName = refPat.ReferenceName;
      if (!(referenceName is {Qualifier: null}))
        return false;

      var name = referenceName.ShortName;
      if (!name.IsEmpty() && name[0].IsLowerFast())
        return true;

      var idOffset = refPat.GetNameIdentifierRange().StartOffset.Offset;
      return refPat.FSharpFile.GetSymbolUse(idOffset) == null;
    }
  }
}
