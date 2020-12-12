using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using JetBrains.Util.Caches;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
{
  public class FSharpAssemblyPsiFile : AssemblyPsiFile
  {
    public FSharpAssemblyPsiFile([NotNull] Func<FileSystemPath, IPsiModule, MetadataLoader> metadataLoaderFactory,
      [NotNull] IPsiConfiguration psiConfiguration,
      [NotNull] IExternalProviderCache<ICompiledEntity, IType> decodedTypeCache,
      [NotNull] IWeakRefRetainerCache<object> compiledMembersBucketCache)
      : base(metadataLoaderFactory, psiConfiguration, decodedTypeCache, compiledMembersBucketCache)
    {
    }

    public override void LoadAssembly(IMetadataAssembly assembly, IAssemblyPsiModule containingModule)
    {
      Metadata = FSharpMetadataReader.ReadMetadata(containingModule).Single();
      base.LoadAssembly(assembly, containingModule);
      Metadata = null;
    }

    // protected override void LoadAdditionalTypes(IMetadataAssembly assembly, List<ICompiledTypeElement> types)
    // {
    //   if (Metadata.Abbreviations.IsEmpty())
    //     return;
    //
    //   using var builder = GetReflectionBuilder(assembly);
    //   foreach (var abbreviation in Metadata.Abbreviations)
    //     if (!abbreviation.IsNested)
    //       types.Add(new FSharpCompiledTypeAbbreviation(abbreviation, this));
    // }

    public Metadata Metadata { get; set; }

    protected override ReflectionElementPropertiesProvider CreateReflectionElementPropertiesProvider() =>
      new FSharpCompiledTypeElementPropertiesProvider(this);
  }
}
