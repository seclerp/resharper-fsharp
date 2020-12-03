using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Impl.reflection2.elements.Compiled;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.Compiled
{
  public class FSharpCompiledModule : Class, IFSharpTypeElement // todo: use IFSharpModule
  {
    public string SourceName { get; }

    public FSharpCompiledModule(string sourceName, [NotNull] ICompiledEntity parent,
      [NotNull] IReflectionBuilder builder, [NotNull] IMetadataTypeInfo info) : base(parent, builder, info)
    {
      SourceName = sourceName;
    }
  }
}
