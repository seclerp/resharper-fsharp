using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches.SymbolCache;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.DeclaredElements;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Impl.reflection2.elements.Compiled;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.Compiled
{
  public class FSharpCompiledTypeAbbreviation : FSharpCompiledTypeElementBase, ICompiledTypeElement, IClass
  {
    private readonly IClrTypeName myClrTypeName;
    private readonly ICompiledEntity myParent;
    private readonly ITypeParameter[] myTypeParameters;

    public FSharpCompiledTypeAbbreviation(Abbreviation clrTypeName, ICompiledEntity parent)
    {
      myClrTypeName = clrTypeName.ClrTypeName;
      myTypeParameters = clrTypeName.TypeParameters
        .Select((name, index) => (ITypeParameter) new FSharpCompiledTypeParameter(name, this, index)).AsArray();
      myParent = parent;
    }

    internal CacheTrieNode TrieNode { get; set; }

    CacheTrieNode ICacheTrieNodeOwner.TrieNode
    {
      get => TrieNode;
      set => TrieNode = value;
    }

    [NotNull]
    internal AssemblyPsiFile GetContainingFile()
    {
      ICompiledEntity entity = myParent;
      for (; entity.Parent != null; entity = entity.Parent)
      {
        /* do nothing */
      }

      var assemblyPsiFile = entity as AssemblyPsiFile;
      return assemblyPsiFile.NotNull("assemblyPsiFile != null");
    }

    public string ShortName => myClrTypeName.ShortName;
    public IClrTypeName GetClrName() => myClrTypeName;

    public IList<ITypeParameter> TypeParameters => myTypeParameters;

    public AccessRights GetAccessRights() => AccessRights.PUBLIC;

    public string XMLDocId => XMLDocUtil.GetTypeElementXmlDocId(this);
    public XmlNode GetXMLDoc(bool inherit) => null; // todo
    public XmlNode GetXMLDescriptionSummary(bool inherit) => null; // todo

    public bool CaseSensitiveName => true;
    public PsiLanguageType PresentationLanguage => UnknownLanguage.Instance;
    public DeclaredElementType GetElementType() => CLRDeclaredElementType.CLASS; // todo: separate type?

    public ITypeElement GetContainingType() => myParent as ITypeElement;
    public ITypeMember GetContainingTypeMember() => myParent as ITypeMember;

    // todo: fix build
    public INamespace GetContainingNamespace() => myParent.Module.GetSymbolScope().GlobalNamespace;
      // myParent is ICompiledTypeElement typeElement
      //   ? typeElement.GetContainingNamespace()
      //   : TrieNode.Parent.Namespace ?? ((SymbolCache) GetPsiServices().Symbols).GlobalNamespace;

    public bool IsValid() => myParent.IsValid();
    public IPsiModule Module => myParent.Module;
    public IPsiServices GetPsiServices() => GetContainingFile().GetPsiServices();

    public ISubstitution IdSubstitution => TypeElementIdSubstitution.Create(this);

    public IList<TypeMemberInstance> GetHiddenMembers() => EmptyList<TypeMemberInstance>.Instance;

    public bool IsAbstract => false;
    public bool IsSealed => false;
    public bool IsVirtual => false;
    public bool IsOverride => false;
    public bool IsStatic => false;
    public bool IsReadonly => false;
    public bool IsExtern => false;
    public bool IsUnsafe => false;
    public bool IsVolatile => false;

    public Hash? CalcHash() => null;

    public AccessibilityDomain AccessibilityDomain => SharedImplUtil.CalcAccessibilityDomain(this);
    public MemberHidePolicy HidePolicy => MemberHidePolicy.HIDE_BY_NAME;

    public IDeclaredType GetBaseClassType() => Module.GetPredefinedType().Object;
    public IClass GetSuperClass() => GetBaseClassType().GetClassType();

    public IEnumerable<string> GetNamespaceNames() => myParent is ICompiledTypeElement
      ? EmptyList<string>.Instance
      : myClrTypeName.NamespaceNames;

    public void Unbind() => TrieNode = null;

    public void Dump(TextWriter writer, string indent)
    {
    }

    public ISet<string> ExtendsListNames => new HashSet<string>();
    public ICollection<Method> ExtensionMethods => EmptyList<Method>.Instance;
  }
}
