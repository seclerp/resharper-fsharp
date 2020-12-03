using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.Compiled;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Impl.reflection2.elements.Compiled;
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

    public Metadata Metadata { get; set; }

    protected override ReflectionElementPropertiesProvider CreateReflectionElementPropertiesProvider() =>
      new FSharpReflectionElementPropertiesProvider(this);
  }

  public class FSharpReflectionElementPropertiesProvider : ReflectionElementPropertiesProvider
  {
    public readonly FSharpAssemblyPsiFile AssemblyPsiFile;
    public readonly FSharpCompiledClassFactory ClassFactory;

    public FSharpReflectionElementPropertiesProvider(FSharpAssemblyPsiFile assemblyPsiFile)
    {
      AssemblyPsiFile = assemblyPsiFile;
      ClassFactory = new FSharpCompiledClassFactory(assemblyPsiFile);
    }

    public override CompiledTypeElementFactory ClassProperties => ClassFactory;

    public class FSharpCompiledClassFactory : ClassFactory
    {
      public FSharpAssemblyPsiFile AssemblyPsiFile { get; }

      public FSharpCompiledClassFactory(FSharpAssemblyPsiFile assemblyPsiFile) =>
        AssemblyPsiFile = assemblyPsiFile;

      public override CompiledTypeElement Create(ICompiledEntity parent, IReflectionBuilder builder,
        IMetadataTypeInfo info)
      {
        if (AssemblyPsiFile.Metadata.Modules.TryGetValue(info.FullyQualifiedName, out var sourceName))
          return new FSharpCompiledModule(sourceName, parent, builder, info);

        return base.Create(parent, builder, info);
      }
    }
  }
}
