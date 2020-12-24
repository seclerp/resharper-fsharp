using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using JetBrains.ReSharper.Psi.Caches.SymbolCache;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Impl.reflection2.elements.Compiled;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.Compiled
{
  public class FSharpCompiledDelegate : Delegate, IFSharpCompiledTypeElement
  {
    public FSharpCompiledType CompiledType { get; }

    public FSharpCompiledDelegate(FSharpCompiledType compiledType, [NotNull] ICompiledEntity parent,
      [NotNull] IReflectionBuilder builder, [NotNull] IMetadataTypeInfo info) : base(parent, builder, info)
    {
      CompiledType = compiledType;
    }

    public string SourceName => CompiledType.Name.SourceName;
    public string AlternativeName => CompiledType.Name.AlternativeName;

    public CacheTrieNode AlternativeNameTrieNode { get; set; }

    public override string ToString() => this.ToStringWithAlternativeName(base.ToString());
  }
}
