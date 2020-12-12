using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.Compiled;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Impl.reflection2.elements.Compiled;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
{
  public class FSharpCompiledTypeElementPropertiesProvider : ReflectionElementPropertiesProvider
  {
    public FSharpCompiledTypeElementPropertiesProvider(FSharpAssemblyPsiFile assemblyPsiFile)
    {
      var metadata = assemblyPsiFile.Metadata;
      EnumProperties = new FSharpCompiledEnumFactory(metadata);
      ClassProperties = new FSharpCompiledClassFactory(metadata);
      StructProperties = new FSharpCompiledStructFactory(metadata);
      DelegateProperties = new FSharpCompiledDelegateFactory(metadata);
      InterfaceProperties = new FSharpCompiledInterfaceFactory(metadata);
    }

    public override CompiledTypeElementFactory EnumProperties { get; }
    public override CompiledTypeElementFactory ClassProperties { get; }
    public override CompiledTypeElementFactory StructProperties { get; }
    public override CompiledTypeElementFactory DelegateProperties { get; }
    public override CompiledTypeElementFactory InterfaceProperties { get; }

    [CanBeNull]
    private static FSharpCompiledType GetCompiledType(IMetadataTypeInfo info,
      IDictionary<string, FSharpCompiledType> types) =>
      types.TryGetValue(info.FullyQualifiedName, out var type) ? type : null;

    public class FSharpCompiledClassFactory : ClassFactory
    {
      public Metadata Metadata { get; }

      public FSharpCompiledClassFactory(Metadata metadata) => Metadata = metadata;

      public override CompiledTypeElement Create(ICompiledEntity parent, IReflectionBuilder builder,
        IMetadataTypeInfo info)
      {
        if (Metadata.Modules.TryGetValue(info.FullyQualifiedName, out var module))
          return new FSharpCompiledModule(module, parent, builder, info);

        return GetCompiledType(info, Metadata.Classes) is { } type
          ? new FSharpCompiledClass(type, parent, builder, info)
          : base.Create(parent, builder, info);
      }
    }

    public class FSharpCompiledInterfaceFactory : InterfaceFactory
    {
      public Metadata Metadata { get; }

      public FSharpCompiledInterfaceFactory(Metadata metadata) => Metadata = metadata;

      public override CompiledTypeElement Create(ICompiledEntity parent, IReflectionBuilder builder,
        IMetadataTypeInfo info) =>
        GetCompiledType(info, Metadata.Interfaces) is { } type
          ? new FSharpCompiledInterface(type, parent, builder, info)
          : base.Create(parent, builder, info);
    }

    public class FSharpCompiledStructFactory : StructFactory
    {
      public Metadata Metadata { get; }

      public FSharpCompiledStructFactory(Metadata metadata) => Metadata = metadata;

      public override CompiledTypeElement Create(ICompiledEntity parent, IReflectionBuilder builder,
        IMetadataTypeInfo info) =>
        GetCompiledType(info, Metadata.Structs) is { } type
          ? new FSharpCompiledStruct(type, parent, builder, info)
          : base.Create(parent, builder, info);
    }

    public class FSharpCompiledEnumFactory : EnumFactory
    {
      public Metadata Metadata { get; }

      public FSharpCompiledEnumFactory(Metadata metadata) => Metadata = metadata;

      public override CompiledTypeElement Create(ICompiledEntity parent, IReflectionBuilder builder,
        IMetadataTypeInfo info) =>
        GetCompiledType(info, Metadata.Enums) is { } type
          ? new FSharpCompiledEnum(type, parent, builder, info)
          : base.Create(parent, builder, info);
    }

    public class FSharpCompiledDelegateFactory : DelegateFactory
    {
      public Metadata Metadata { get; }

      public FSharpCompiledDelegateFactory(Metadata metadata) => Metadata = metadata;

      public override CompiledTypeElement Create(ICompiledEntity parent, IReflectionBuilder builder,
        IMetadataTypeInfo info) =>
        GetCompiledType(info, Metadata.Delegates) is { } type
          ? new FSharpCompiledDelegate(type, parent, builder, info)
          : base.Create(parent, builder, info);
    }
  }
}
