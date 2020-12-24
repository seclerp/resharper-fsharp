using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches.SymbolCache;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Impl.reflection2.elements.Compiled;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.Compiled
{
  public class FSharpCompiledModule : Class, IFSharpCompiledTypeElement // todo: use IFSharpModule
  {
    [NotNull] public FSharpDeclaredName Name { get; }

    public readonly FSharpCompiledTypeAbbreviation[] NestedTypeAbbreviations;

    public FSharpCompiledModule(Module module, [NotNull] ICompiledEntity parent,
      [NotNull] IReflectionBuilder builder, [NotNull] IMetadataTypeInfo info) : base(parent, builder, info)
    {
      Name = module.Name;
      NestedTypeAbbreviations = EmptyArray<FSharpCompiledTypeAbbreviation>.Instance;

      // NestedTypeAbbreviations =
      //   module.Abbreviations.Select(abbr => new FSharpCompiledTypeAbbreviation(abbr, this)).ToArray();
    }

    public override IList<ITypeElement> NestedTypes =>
      NestedTypeAbbreviations.IsEmpty()
        ? base.NestedTypes
        : NestedTypeAbbreviations.Prepend(base.NestedTypes).AsIList();

    public CacheTrieNode AlternativeNameTrieNode { get; set; }

    public string SourceName => Name.SourceName;
    public string AlternativeName => Name.AlternativeName;

    public override string ToString() => this.ToStringWithAlternativeName(base.ToString());
  }
}
