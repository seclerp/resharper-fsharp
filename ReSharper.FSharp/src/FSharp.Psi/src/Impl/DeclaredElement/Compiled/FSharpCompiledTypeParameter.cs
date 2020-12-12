using System.Collections.Generic;
using System.Xml;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.Compiled
{
  public class FSharpCompiledTypeParameter : FSharpCompiledTypeElementBase, ITypeParameter
  {
    public int Index { get; }
    public string ShortName { get; }
    public ITypeElement TypeElement { get; }

    public FSharpCompiledTypeParameter(string shortName, ITypeElement typeElement, int index)
    {
      Index = index;
      ShortName = shortName;
      TypeElement = typeElement;
    }

    public DeclaredElementType GetElementType() => CLRDeclaredElementType.TYPE_PARAMETER;

    public bool IsValid() => TypeElement.IsValid();

    public IPsiServices GetPsiServices() => TypeElement.GetPsiServices();

    public XmlNode GetXMLDoc(bool inherit) => null;
    public XmlNode GetXMLDescriptionSummary(bool inherit) => null;

    public bool CaseSensitiveName => true;
    public PsiLanguageType PresentationLanguage => UnknownLanguage.Instance;

    public ITypeElement GetContainingType() => TypeElement;
    public ITypeMember GetContainingTypeMember() => TypeElement as ITypeMember;

    public IPsiModule Module => TypeElement.Module;
    public ISubstitution IdSubstitution => EmptySubstitution.INSTANCE;

    public IClrTypeName GetClrName() => EmptyClrTypeName.Instance;
    public IList<ITypeParameter> TypeParameters => EmptyList<ITypeParameter>.Instance;

    public INamespace GetContainingNamespace() => TypeElement.GetContainingNamespace();

    public TypeParameterNullability Nullability => TypeParameterNullability.Unknown;

    public TypeParameterNullability GetNullability(ISubstitution explicitInheritorSubstitution) =>
      TypeParameterNullability.Unknown;

    public TypeParameterVariance Variance => TypeParameterVariance.INVARIANT;
    public bool IsValueType => false;
    public bool IsReferenceType => false;
    public bool IsUnmanagedType => false;
    public bool HasDefaultConstructor => false;
    public bool IsNotNullableValueOrReferenceType => false;
    public bool HasTypeConstraints => false;
    public IList<IType> TypeConstraints => EmptyList<IType>.Instance;
    public TypeParameterConstraintFlags Constraints => TypeParameterConstraintFlags.None;

    public ITypeElement OwnerType => TypeElement;
    public ITypeParametersOwner Owner => TypeElement;

    public IMethod OwnerMethod => null;
    public IParametersOwner OwnerFunction => null;
  }
}
