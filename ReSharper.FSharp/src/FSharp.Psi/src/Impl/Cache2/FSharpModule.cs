using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.Compiled;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches.SymbolCache;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  internal class FSharpModule : FSharpClass, IFSharpModule, IAlternativeNameCacheTrieNodeOwner
  {
    public new string SourceName { get; }

    public FSharpModule([NotNull] IModulePart part, string sourceName) : base(part)
    {
      SourceName = sourceName;
    }

    protected override LocalList<IDeclaredType> CalcSuperTypes() =>
      new LocalList<IDeclaredType>(new[] {Module.GetPredefinedType().Object});

    private IModulePart ModulePart =>
      this.GetPart<IModulePart>().NotNull();

    public bool IsAnonymous => ModulePart.IsAnonymous;

    public ModuleMembersAccessKind AccessKind => ModulePart.AccessKind;

    public bool IsAutoOpen => AccessKind == ModuleMembersAccessKind.AutoOpen;
    public bool RequiresQualifiedAccess => AccessKind == ModuleMembersAccessKind.RequiresQualifiedAccess;

    protected override bool AcceptsPart(TypePart part) =>
      part is IModulePart;

    public ITypeElement AssociatedTypeElement =>
      EnumerateParts()
        .Select(part => (part as IModulePart)?.AssociatedTypeElement)
        .WhereNotNull()
        .FirstOrDefault();

    public CacheTrieNode AlternativeNameTrieNode { get; set; }
    public string AlternativeName => SourceName != ShortName ? SourceName : null;

    public override string ToString() => this.ToStringWithAlternativeName(base.ToString());
  }
}
