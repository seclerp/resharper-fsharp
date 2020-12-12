using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.Compiled
{
  public class FSharpCompiledTypeElementBase
  {
    public bool IsSynthetic() => false;

    public MemberPresenceFlag GetMemberPresenceFlag() => MemberPresenceFlag.NONE;
    public bool HasMemberWithName(string shortName, bool ignoreCase) => false;

    public IList<IDeclaredType> GetSuperTypes() => EmptyList<IDeclaredType>.Instance;
    public IList<ITypeElement> GetSuperTypeElements() => EmptyList<ITypeElement>.Instance;
    public IEnumerable<ITypeMember> GetMembers() => EmptyList<ITypeMember>.Instance;

    public bool HasDeclarationsIn(IPsiSourceFile sourceFile) => false;
    public IList<IDeclaration> GetDeclarations() => EmptyList<IDeclaration>.Instance;
    public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile) => EmptyList<IDeclaration>.Instance;
    public HybridCollection<IPsiSourceFile> GetSourceFiles() => HybridCollection<IPsiSourceFile>.Empty;
    public IPsiSourceFile GetSingleOrDefaultSourceFile() => null;

    public IList<IAttributeInstance> GetAttributeInstances(AttributesSource attributesSource) =>
      EmptyList<IAttributeInstance>.Instance;

    public IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, AttributesSource attributesSource) =>
      EmptyList<IAttributeInstance>.Instance;

    public bool HasAttributeInstance(IClrTypeName clrName, AttributesSource attributesSource) => false;

    public IList<ITypeElement> NestedTypes => EmptyList<ITypeElement>.Instance;
    public IEnumerable<IField> Constants => EmptyList<IField>.Instance;
    public IEnumerable<IField> Fields => EmptyList<IField>.Instance;
    public IEnumerable<IConstructor> Constructors => EmptyList<IConstructor>.Instance;
    public IEnumerable<IOperator> Operators => EmptyList<IOperator>.Instance;
    public IEnumerable<IMethod> Methods => EmptyList<IMethod>.Instance;
    public IEnumerable<IProperty> Properties => EmptyList<IProperty>.Instance;
    public IEnumerable<IEvent> Events => EmptyList<IEvent>.Instance;
    public IEnumerable<string> MemberNames => EmptyList<string>.Instance;
  }
}
