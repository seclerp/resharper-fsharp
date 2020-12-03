using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using FSharp.Compiler;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using Microsoft.FSharp.Core;

// ReSharper disable NotResolvedInText
// ReSharper disable UnusedVariable

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
{
  public class FSharpMetadataReader
  {
    public static void ReadMetadata(FSharpAssemblyUtil.FSharpSignatureDataResource resource)
    {
      using var resourceReader = resource.MetadataResource.CreateResourceReader();
      using var reader = new Reader(resourceReader, Encoding.UTF8);
      reader.ReadMetadata(); // unpickleObjWithDanglingCcus
    }

    public static void ReadMetadata(IPsiModule psiModule)
    {
      var metadataResources = FSharpAssemblyUtil.GetFSharpMetadataResources(psiModule);
      foreach (var metadataResource in metadataResources)
        ReadMetadata(metadataResource);
    }
  }

  public enum TypeOrMeasureKind
  {
    Type = 0,
    Measure = 1
  }

  public enum TypeKind
  {
    Tuple = 0,
    SimpleType = 1,
    TypeApp = 2,
    Function = 3,
    TypeReference = 4,
    ForAll = 5,
    Measure = 6,
    UnionCase = 7,
    StructTuple = 8,
    AnonRecord = 9
  }

  public enum Option
  {
    None = 0,
    Some = 1
  }

  public enum TypeRepresentationKind
  {
    Record = 0,
    Union = 1,
    MetadataType = 2,
    ObjectModel = 3,
    Measure = 4,
  }

  public enum ExceptionRepresentationKind
  {
    Abbreviation = 0,
    MetadataType = 1,
    Fresh = 2,
    None = 3
  }

  public enum EntityKind
  {
    ModuleWithSuffix = 0,
    ModuleOrType = 1,
    Namespace = 2
  }

  public enum ObjectModelTypeKind
  {
    Class = 0,
    Interface = 1,
    Struct = 2,
    Delegate = 3,
    Enum = 4
  }

  public enum IlScopeRef
  {
    Local = 0,
    Module = 1,
    Assembly = 2
  }

  public enum ReferenceKind
  {
    Local = 0,
    NonLocal = 1
  }

  public enum AttributeKind
  {
    Metadata = 0,
    FSharp = 1
  }

  public enum ValFlags
  {
    IsCompilerGenerated = 0b00000000000000001000
  }

  internal class Reader : BinaryReader
  {
    private class EntityStackItem
    {
      public int Index;
      public string Name;
    }

    public Reader([NotNull] Stream input, [NotNull] Encoding encoding) : base(input, encoding)
    {
    }

    public readonly IDictionary<int, IClrTypeName> TypeDecls = new Dictionary<int, IClrTypeName>();
    private readonly Stack<EntityStackItem> myState = new Stack<EntityStackItem>();

    private static readonly Func<Reader, int> ReadIntFunc = reader => reader.ReadPackedInt();
    private static readonly Func<Reader, int[]> ReadIntArrayFunc = reader => reader.ReadArray(ReadIntFunc);
    private static readonly Func<Reader, FSharpOption<int>> ReadIntOption = reader => reader.ReadOption(ReadIntFunc);

    private static readonly Func<Reader, bool> ReadBoolFunc = reader => reader.ReadBoolean();
    private static readonly Func<Reader, string> ReadStringFunc = reader => reader.ReadString();
    private static readonly Func<Reader, object> ReadTypeFunc = reader => reader.ReadType();
    private static readonly Func<Reader, object> ReadIlTypeFunc = reader => reader.ReadIlType();
    private static readonly Func<Reader, object> ReadExpressionFunc = reader => reader.ReadExpression();
    private static readonly Func<Reader, object> ReadValueRefFunc = reader => reader.ReadValueRef();
    private static readonly Func<Reader, Range.range> ReadRangeFunc = reader => reader.ReadRange();

    private static readonly Func<Reader, string> ReadUniqueStringFunc =
      reader => reader.ReadUniqueString();

    private static readonly Func<bool, object> IgnoreBoolFunc = _ => null;

    private string[] myStrings;
    private int[][] myPublicPaths;
    private Tuple<int, int[]>[] myNonLocalTypeRefs;

    /// Referenced cross compilation unit names.
    private string[] myReferencedCcuNames;

    private int[] mySimpleTypes;

    private void CheckTagValue(int value, int maxValue, [CallerMemberName] string methodName = "") =>
      CheckTagValue(value, maxValue, methodName, "tag");

    [Conditional("JET_MODE_ASSERT")]
    private void CheckTagValue(string tagName, int value, int maxValue, [CallerMemberName] string methodName = "") =>
      CheckTagValue(value, maxValue, methodName, tagName);

    [Conditional("JET_MODE_ASSERT")]
    private void CheckTagValue(int value, int maxValue, string methodName, string tagName)
    {
      if (value > maxValue)
        Assertion.Fail($"{methodName}: {tagName} <= {maxValue}, actual: {value}");
    }

    private Tuple<T1, T2> ReadTuple2<T1, T2>(Func<Reader, T1> reader1,
      Func<Reader, T2> reader2)
    {
      var item1 = reader1(this);
      var item2 = reader2(this);
      return Tuple.Create(item1, item2);
    }

    private Tuple<T1, T2, T3> ReadTuple3<T1, T2, T3>(Func<Reader, T1> reader1,
      Func<Reader, T2> reader2, Func<Reader, T3> reader3)
    {
      var item1 = reader1(this);
      var item2 = reader2(this);
      var item3 = reader3(this);
      return Tuple.Create(item1, item2, item3);
    }

    private T[] ReadArray<T>(Func<Reader, T> reader)
    {
      var arrayLength = ReadPackedInt();
      return arrayLength == 0 ? EmptyArray<T>.Instance : ReadArray(reader, arrayLength);
    }

    private T[] ReadArray<T>(Func<Reader, T> reader, int arrayLength)
    {
      var array = new T[arrayLength];
      for (var i = 0; i < arrayLength; i++)
        array[i] = reader(this);
      return array;
    }

    private FSharpOption<T> ReadOption<T>(Func<Reader, T> reader) =>
      (Option) ReadByte() switch
      {
        Option.None => null,
        Option.Some => reader(this),
        _ => throw new ArgumentOutOfRangeException()
      };

    public override string ReadString()
    {
      var stringLength = ReadPackedInt();
      var bytes = ReadBytes(stringLength);
      return Encoding.UTF8.GetString(bytes);
    }

    public int ReadPackedInt()
    {
      var b0 = ReadByte();
      if (b0 <= 0x7F)
        return b0;

      if (b0 <= 0xBF)
      {
        var b10 = b0 & 0x7F;
        var b11 = (int) ReadByte();
        return (b10 << 8) | b11;
      }

      Assertion.Assert(b0 == 0xFF, "b0 == 0xFF");
      return ReadInt32();
    }

    public override long ReadInt64()
    {
      var i1 = ReadPackedInt();
      var i2 = ReadPackedInt();
      return i1 | ((long) i2 << 32);
    }

    public T ReadByteAsEnum<T>(string tagName = "tag") where T : struct, Enum
    {
      var value = ReadByte();
      Assertion.Assert(value < Enum.GetValues(typeof(T)).Length, $"{tagName}: {value}");
      return Unsafe.As<byte, T>(ref value);
    }

    public void ReadMetadata()
    {
      myReferencedCcuNames = ReadCcuRefNames();

      // Also includes namespaces and a top level module.
      var entitiesCount = ReadEntitiesCount(out var hasAnonRecords);

      var typeParametersCount = ReadPackedInt();
      var valuesCount = ReadPackedInt();
      var anonRecordsCount = hasAnonRecords ? ReadPackedInt() : 0;

      myStrings = ReadArray(ReadStringFunc);
      myPublicPaths = ReadArray(ReadIntArrayFunc); // u_encoded_pubpath

      myNonLocalTypeRefs = ReadArray(reader => reader.ReadTuple2(ReadIntFunc, ReadIntArrayFunc)); // u_encoded_nleref
      mySimpleTypes = ReadArray(ReadIntFunc); // u_encoded_simpletyp
      ReadPackedInt(); // Chunk size of phase1bytes.

      ReadEntitySpec(); // unpickleCcuInfo

      var compileTimeWorkingDir = ReadUniqueString();
      var usesQuotations = ReadBoolean();
      SkipZeros(3);
    }

    private object ReadEntitySpec()
    {
      var index = ReadPackedInt();
      var typeParameters = ReadArray(reader => reader.ReadTypeParameterSpec());
      var logicalName = ReadUniqueString();
      myState.Push(new EntityStackItem {Index = index, Name = logicalName});

      var compiledName = ReadOption(ReadUniqueStringFunc);
      var range = ReadRange();

      var publicPath = ReadOption(reader => reader.ReadPublicPath());
      if (publicPath?.Value is string[] path)
      {
        var joined = path.Join(".");
        var typeName = new ClrTypeName(joined);
        TypeDecls[index] = typeName;
      }

      var accessibility = ReadAccessibility();
      var representationAccessibility = ReadAccessibility();
      var attributes = ReadAttributes();
      var typeRepresentation = ReadTypeRepresentation();
      var typeAbbreviationOption = ReadOption(ReadTypeFunc);

      var typeAugmentation = ReadTypeAugmentation();

      var xmlDocId = ReadUniqueString();
      Assertion.Assert(xmlDocId.IsEmpty(), "xmlDocId.IsEmpty()");

      var typeKind = ReadTypeKind();
      var typeRepresentationFlag = ReadInt64();
      var compilationPath = ReadOption(reader => reader.ReadCompilationPath());
      var moduleType = ReadModuleOrNamespaceEntity(); // u_modul_typ
      var exceptionRepresentation = ReadExceptionRepresentation();
      var xmlDoc = ReadXmlDocOption();

      myState.Pop();

      return null;
    }

    private object ReadUnionCaseSpec()
    {
      var fields = ReadFieldsTable();
      var returnType = ReadType();
      var ignoredCaseCompiledName = ReadUniqueString();
      var name = ReadIdent();
      var attributes = ReadAttributesAndXmlDoc();
      var xmlDocId = ReadUniqueString();
      var accessibility = ReadAccessibility();

      return null;
    }

    private object ReadTypeObjectModelData()
    {
      ReadTypeObjectModelKind();
      ReadArray(ReadValueRefFunc);
      ReadFieldsTable();

      return null;
    }

    private object ReadTypeObjectModelKind() =>
      (ObjectModelTypeKind) ReadByte() switch
      {
        ObjectModelTypeKind.Class => null,
        ObjectModelTypeKind.Interface => null,
        ObjectModelTypeKind.Struct => null,
        ObjectModelTypeKind.Delegate => ReadAbstractSlotSignature(),
        ObjectModelTypeKind.Enum => null,
        _ => throw new ArgumentOutOfRangeException()
      };

    private object ReadAbstractSlotSignature()
    {
      var name = ReadUniqueString();
      var containingType = ReadType();
      var containingTypeTypeParameters = ReadArray(reader => reader.ReadTypeParameterSpec());
      var typeParameters = ReadArray(reader => reader.ReadTypeParameterSpec());
      var parameters = ReadArray(reader => reader.ReadArray(reader1 => reader1.ReadAbstractSlotParameter()));
      var returnType = ReadOption(ReadTypeFunc);

      return name;
    }

    private object ReadAbstractSlotParameter()
    {
      var name = ReadOption(ReadUniqueStringFunc);
      var type = ReadType();
      var isIn = ReadBoolean();
      var isOut = ReadBoolean();
      var isOptional = ReadBoolean();
      var attributes = ReadAttributes();

      return name;
    }

    private object ReadCompilationPath()
    {
      ReadIlScopeRef();
      return ReadArray(reader => reader.ReadTuple2(ReadUniqueStringFunc, reader1 => reader1.ReadEntityKind()));
    }

    private EntityKind ReadEntityKind() =>
      ReadByteAsEnum<EntityKind>();

    private object ReadIlScopeRef() =>
      (IlScopeRef) ReadByte() switch
      {
        IlScopeRef.Local => null,
        IlScopeRef.Module => ReadIlModuleRef(),
        IlScopeRef.Assembly => ReadIlAssemblyRef(),
        _ => throw new ArgumentOutOfRangeException()
      };

    private object ReadIlModuleRef()
    {
      var name = ReadUniqueString();
      var hasMetadata = ReadBoolean();
      var hash = ReadOption(reader => reader.ReadBytes());

      return name;
    }

    private object ReadIlAssemblyRef()
    {
      SkipZeros(1);

      var name = ReadUniqueString();
      var hash = ReadOption(reader => reader.ReadBytes());
      var publicKey = ReadOption(reader => reader.ReadIlPublicKey());
      var retargetable = ReadBoolean();

      var version = ReadOption(reader => reader.ReadIlVersion());
      var locale = ReadOption(ReadUniqueStringFunc);

      return name;
    }

    private object ReadIlPublicKey()
    {
      var tag = ReadByte();
      CheckTagValue(tag, 2);
      ReadBytes();

      return null;
    }

    private object ReadIlVersion()
    {
      var major = ReadPackedInt();
      var minor = ReadPackedInt();
      var build = ReadPackedInt();
      var revision = ReadPackedInt();

      return new Version(major, minor, build, revision);
    }

    private object ReadBytes()
    {
      var bytes = ReadPackedInt();
      ReadBytes(bytes);

      return null;
    }

    private object ReadValue()
    {
      var index = ReadPackedInt();

      var logicalName = ReadUniqueString();
      var compiledName = ReadOption(ReadUniqueStringFunc);
      var ranges = ReadOption(reader => reader.ReadTuple2(ReadRangeFunc, ReadRangeFunc));
      var type = ReadType();
      var flags = ReadInt64();
      var memberInfo = ReadOption(reader => reader.ReadMemberInfo());
      var attributes = ReadAttributes();
      var methodRepresentationInfo = ReadOption(reader => reader.ReadValueRepresentationInfo());
      var xmlDocId = ReadUniqueString();
      var accessibility = ReadAccessibility();

      var declaringEntity = ReadOption(reader => reader.ReadTypeRef());

      var constValue = ReadOption(reader => reader.ReadConst());
      var xmlDoc = ReadXmlDocOption();

      var isCompilerGenerated = (flags & (long) ValFlags.IsCompilerGenerated) != 0;
      var isModuleValue = memberInfo == null && !isCompilerGenerated;

      return logicalName;
    }

    private object ReadValueRepresentationInfo()
    {
      // Values compiled as methods infos

      var typeParameters = ReadArray(reader => reader.ReadTypeParameterRepresentationInfo());
      var parameters = ReadArray(reader => reader.ReadArray(reader1 => reader1.ReadArgumentRepresentationInfo()));
      var result = ReadArgumentRepresentationInfo();

      return null;
    }

    private object ReadTypeParameterRepresentationInfo()
    {
      var name = ReadIdent();
      var kind = ReadTypeKind();
      return name;
    }

    private object ReadArgumentRepresentationInfo()
    {
      ReadAttributes();
      ReadOption(reader => reader.ReadIdent());

      return null;
    }

    private object ReadMemberInfo()
    {
      var apparentEnclosingEntity = ReadTypeRef();
      var memberFlags = ReadMemberFlags();
      var implementedSlotSigs = ReadArray(reader => reader.ReadAbstractSlotSignature());
      var isImplemented = ReadBoolean();

      return null;
    }

    private object ReadMemberFlags()
    {
      var isInstance = ReadBoolean();
      ReadBoolean();
      var isDispatchSlot = ReadBoolean();
      var isOverrideOrExplicitImpl = ReadBoolean();
      var isFinal = ReadBoolean();

      var tag = ReadByte();
      CheckTagValue(tag, 4);

      return null;
    }

    private object ReadModuleOrNamespaceEntity()
    {
      // from u_lazy:
      var chunkLength = ReadInt32();

      var otyconsIdx1 = ReadInt32();
      var otyconsIdx2 = ReadInt32();
      var otyparsIdx1 = ReadInt32();
      var otyparsIdx2 = ReadInt32();
      var ovalsIdx1 = ReadInt32();
      var ovalsIdx2 = ReadInt32();

      var entityKind = ReadEntityKind();
      ReadArray(reader => reader.ReadValue());
      ReadArray(reader => reader.ReadEntitySpec());

      return null;
    }

    private object ReadExceptionRepresentation() =>
      (ExceptionRepresentationKind) ReadByte() switch
      {
        ExceptionRepresentationKind.Abbreviation => ReadTypeRef(),
        ExceptionRepresentationKind.MetadataType => ReadIlTypeRef(),
        ExceptionRepresentationKind.Fresh => ReadFieldsTable(),
        ExceptionRepresentationKind.None => null,
        _ => throw new ArgumentOutOfRangeException()
      };

    private object ReadTypeParameterSpec()
    {
      var index = ReadPackedInt();

      var ident = ReadIdent();
      var attributes = ReadAttributes();
      var flags = ReadInt64();
      var constraints = ReadArray(reader => reader.ReadTypeParameterConstraint());
      var xmlDoc = ReadXmlDoc();

      return null;
    }

    [NotNull]
    private Func<bool, object> ReadTypeRepresentation()
    {
      switch ((Option) ReadByte())
      {
        case Option.None:
          return IgnoreBoolFunc;

        case Option.Some:
          switch ((TypeRepresentationKind) ReadByte())
          {
            case TypeRepresentationKind.Record:
              ReadFieldsTable();
              return IgnoreBoolFunc;

            case TypeRepresentationKind.Union:
              ReadArray(reader => reader.ReadUnionCaseSpec());
              return IgnoreBoolFunc;

            case TypeRepresentationKind.MetadataType:
              ReadIlType();
              return flag => null;

            case TypeRepresentationKind.ObjectModel:
              ReadTypeObjectModelData();
              return IgnoreBoolFunc;

            case TypeRepresentationKind.Measure:
              ReadType();
              return IgnoreBoolFunc;

            default:
              throw new ArgumentOutOfRangeException("tag2");
          }

        default:
          throw new ArgumentOutOfRangeException("tag1");
      }
    }

    private object ReadFieldsTable() =>
      ReadArray(reader => reader.ReadRecordFieldSpec());

    private object ReadRecordFieldSpec()
    {
      var isMutable = ReadBoolean();
      var isVolatile = ReadBoolean();
      var type = ReadType();
      var isStatic = ReadBoolean();
      var isCompilerGenerated = ReadBoolean();
      var constantValue = ReadOption(reader => reader.ReadConst());
      var name = ReadIdent();
      var propertyAttributes = ReadAttributesAndXmlDoc();
      var fieldAttributes = ReadAttributes();
      var xmlDocId = ReadUniqueString();
      var accessibility = ReadAccessibility();

      return name;
    }

    private object ReadAttributesAndXmlDoc()
    {
      var b = ReadPackedInt();
      if ((b & 0x80000000) == 0x80000000)
        ReadXmlDoc();

      return ReadArray(reader => reader.ReadAttribute(), b & 0x7FFFFFFF);
    }

    private string ReadUniqueString() =>
      myStrings[ReadPackedInt()];

    private object ReadIlType()
    {
      var tag = ReadByte();
      CheckTagValue(tag, 8);

      if (tag == 0)
        return null;

      if (tag == 1)
      {
        ReadIlArrayShape();
        ReadIlType();
      }

      if (tag == 2 || tag == 3)
        ReadIlTypeSpec();

      if (tag == 4 || tag == 5)
        ReadIlType();

      if (tag == 6)
      {
        ReadCallingConvention();
        ReadArray(ReadIlTypeFunc);
        ReadIlType();
      }

      if (tag == 7)
        ReadPackedInt();

      if (tag == 8)
      {
        ReadBoolean();
        ReadIlTypeRef();
        ReadIlType();
      }

      return null;
    }

    private object ReadIlArrayShape() =>
      ReadArray(reader => reader.ReadTuple2(ReadIntOption, ReadIntOption));

    private object ReadIlTypes() =>
      ReadArray(ReadIlTypeFunc);

    private object ReadIlTypeSpec()
    {
      var typeRef = ReadIlTypeRef();
      var substitution = ReadIlTypes();
      return null;
    }

    private object ReadType()
    {
      switch ((TypeKind) ReadByte())
      {
        case TypeKind.Tuple:
          ReadArray(ReadTypeFunc);
          break;

        case TypeKind.SimpleType:
          return GetNonLocalTypeRef(mySimpleTypes[ReadPackedInt()]);

        case TypeKind.TypeApp:
          ReadTypeRef();
          ReadArray(ReadTypeFunc);
          break;

        case TypeKind.Function:
          ReadType();
          ReadType();
          break;

        case TypeKind.TypeReference:
          ReadTypeParameterRef();
          break;

        case TypeKind.ForAll:
          ReadArray(reader => reader.ReadTypeParameterSpec());
          ReadType();
          break;

        case TypeKind.Measure:
          ReadMeasureExpression();
          break;

        case TypeKind.UnionCase:
        {
          var unionCase = ReadUnionCaseRef();
          var substitution = ReadArray(ReadTypeFunc);
          break;
        }

        case TypeKind.StructTuple:
          ReadArray(ReadTypeFunc);
          break;

        case TypeKind.AnonRecord:
        {
          var anonRecord = ReadAnonRecord();
          var substitution = ReadTypes();
          break;
        }

        default:
          throw new ArgumentOutOfRangeException();
      }

      return null;
    }

    private object ReadMeasureExpression()
    {
      var tag = ReadByte();
      CheckTagValue(tag, 5);

      if (tag == 0)
        ReadTypeRef();

      if (tag == 1)
        ReadMeasureExpression();

      if (tag == 2)
      {
        ReadMeasureExpression();
        ReadMeasureExpression();
      }

      if (tag == 3)
        ReadTypeParameterRef();

      if (tag == 5)
      {
        ReadMeasureExpression();
        ReadPackedInt();
        ReadPackedInt();
      }

      return null;
    }

    private object ReadTypes()
    {
      return ReadArray(reader => reader.ReadType());
    }

    private object ReadTypeAugmentation()
    {
      ReadOption(reader => reader.ReadTuple2(ReadValueRefFunc, ReadValueRefFunc));
      ReadOption(ReadValueRefFunc);
      ReadOption(reader => reader.ReadTuple3(ReadValueRefFunc, ReadValueRefFunc, ReadValueRefFunc));
      ReadOption(reader => reader.ReadTuple2(ReadValueRefFunc, ReadValueRefFunc));
      ReadArray(reader => reader.ReadTuple2(ReadUniqueStringFunc, ReadValueRefFunc));
      ReadArray(reader => reader.ReadTuple2(ReadTypeFunc, ReadBoolFunc));
      ReadOption(ReadTypeFunc);
      ReadBoolean();
      SkipZeros(1);

      return null;
    }

    private string ReadIdent()
    {
      var name = ReadUniqueString();
      var range = ReadRange();
      return name;
    }

    private Range.range ReadRange()
    {
      var filePath = ReadUniqueString();
      var startLine = ReadPackedInt();
      var startColumn = ReadPackedInt();
      var endLine = ReadPackedInt();
      var endColumn = ReadPackedInt();
      return Range.mkRange(filePath, Range.mkPos(startLine, startColumn), Range.mkPos(endLine, endColumn));
    }

    private object[] ReadAttributes() =>
      ReadArray(reader => reader.ReadAttribute());

    private object ReadAttribute()
    {
      var typeRef = ReadTypeRef();
      var attributeKind = ReadAttributeKind();
      var positionalArgs = ReadArray(reader => reader.ReadAttributeExpression());
      var namedArgs = ReadArray(reader => reader.ReadAttributeNamedArg());
      var appliedToGetterOrSetter = ReadBoolean();
      return null;
    }

    private object ReadAttributeKind() =>
      (AttributeKind) ReadByte() switch
      {
        AttributeKind.Metadata => ReadIlMethodRef(),
        AttributeKind.FSharp => ReadValueRef(),
        _ => throw new ArgumentOutOfRangeException()
      };

    private object ReadAttributeExpression()
    {
      var source = ReadExpression();
      var evaluated = ReadExpression();
      return null;
    }

    private object ReadAttributeNamedArg()
    {
      var name = ReadUniqueString();
      var type = ReadType();
      var isField = ReadBoolean();
      var value = ReadAttributeExpression();
      return null;
    }

    private object ReadIlMethodRef()
    {
      var enclosingTypeRef = ReadIlTypeRef();
      var callingConvention = ReadCallingConvention();
      var typeParametersCount = ReadPackedInt();
      var name = ReadUniqueString();
      var substitution = ReadIlTypes();
      var returnType = ReadIlType();

      return enclosingTypeRef + "." + name;
    }

    private object ReadCallingConvention()
    {
      var tag1 = ReadByte();
      CheckTagValue(nameof(tag1), tag1, 2);
      var tag2 = ReadByte();
      CheckTagValue(nameof(tag2), tag2, 5);
      return null;
    }

    private object ReadValueRef()
    {
      switch ((ReferenceKind) ReadByte())
      {
        case ReferenceKind.Local:
        {
          var index = ReadPackedInt();
          return null;
        }
        case ReferenceKind.NonLocal:
        {
          var enclosingEntity = ReadTypeRef();
          var parentMangledNameOption = ReadOption(ReadUniqueStringFunc);
          var isOverride = ReadBoolean();
          var logicalName = ReadUniqueString();
          var typeParametersCount = ReadPackedInt();
          var typeForLinkage = ReadOption(reader => reader.ReadType());

          return logicalName;
        }
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private object ReadValueRefFlags()
    {
      var tag = ReadByte();
      CheckTagValue(tag, 4);

      if (tag == 3)
        ReadType();

      return null;
    }

    private object ReadTypeRef() =>
      (ReferenceKind) ReadByte() switch
      {
        ReferenceKind.Local => GetLocalTypeRef(),
        ReferenceKind.NonLocal => ReadNonLocalTypeRef(),
        _ => throw new ArgumentOutOfRangeException()
      };

    private object GetLocalTypeRef()
    {
      var index = ReadPackedInt();
      return null;
    }

    private object ReadNonLocalTypeRef() => 
      GetNonLocalTypeRef(ReadPackedInt());

    private object GetNonLocalTypeRef(int index)
    {
      var (ccuRef, nameIds) = myNonLocalTypeRefs[index];
      var ccu = myReferencedCcuNames[ccuRef];
      var names = nameIds.Select(nameIndex => myStrings[nameIndex]).ToArray();

      return Tuple.Create(ccu, names);
    }

    private object ReadTypeParameterRef()
    {
      var index = ReadPackedInt();

      return null;
    }

    private object ReadUnionCaseRef()
    {
      var unionType = ReadTypeRef();
      var caseName = ReadUniqueString();
      return null;
    }

    private object ReadIlTypeRef()
    {
      var scope = ReadIlScopeRef();
      var enclosingTypes = ReadArray(ReadUniqueStringFunc);
      var name = ReadUniqueString();

      return name;
    }

    private object ReadTypeParameterConstraint()
    {
      var tag = ReadByte();
      CheckTagValue(tag, 12);

      if (tag == 0)
        ReadType();

      if (tag == 1)
        ReadMemberConstraint();

      if (tag == 2)
        ReadType();

      if (tag == 7)
        ReadTypes();

      if (tag == 8)
        ReadType();

      if (tag == 9)
      {
        ReadType();
        ReadType();
      }

      return null;
    }

    private object ReadMemberConstraint()
    {
      var types = ReadTypes();
      var name = ReadUniqueString();
      var memberFlags = ReadMemberFlags();
      var substitution = ReadTypes();
      var returnType = ReadOption(ReadTypeFunc);
      var solution = ReadOption(reader => reader.ReadMemberConstraintSolution());

      return name;
    }

    private object ReadMemberConstraintSolution()
    {
      var tag = ReadByte();
      CheckTagValue(tag, 5);

      if (tag == 0)
      {
        ReadType();
        ReadOption(reader => reader.ReadIlTypeRef());
        ReadIlMethodRef();
        ReadTypes();
      }

      if (tag == 1)
      {
        ReadType();
        ReadValueRef();
        ReadTypes();
      }

      if (tag == 3)
        ReadExpression();

      if (tag == 4)
      {
        ReadTypes();
        ReadFieldRef();
        ReadBoolean();
      }

      if (tag == 5)
      {
        var anonRecord = ReadAnonRecord();
        var substitution = ReadTypes();
        var fieldIndex = ReadPackedInt();
      }

      return null;
    }

    private object ReadFieldRef()
    {
      var typeRef = ReadTypeRef();
      var name = ReadUniqueString();

      return name;
    }

    private object ReadAnonRecord()
    {
      var index = ReadPackedInt();

      var ccuRefIndex = ReadPackedInt();
      ReadBoolean();
      var fieldNames = ReadArray(reader => reader.ReadIdent());

      return fieldNames;
    }

    private object ReadExpression()
    {
      var tag = ReadByte();
      CheckTagValue(tag, 13);

      if (tag == 0)
      {
        ReadConst();
        ReadType();

        return null;
      }

      if (tag == 1)
      {
        ReadValueRef();
        ReadValueRefFlags();
      }

      if (tag == 2)
      {
        ReadOperation();
        ReadTypes();
        ReadArray(ReadExpressionFunc);
      }

      if (tag == 3)
      {
        ReadExpression();
        ReadExpression();
        ReadPackedInt();
      }

      if (tag == 4)
      {
        ReadOption(reader => reader.ReadValue());
        ReadOption(reader => reader.ReadValue());
        ReadArray(reader => reader.ReadValue());
        ReadExpression();
        ReadType();
      }

      if (tag == 5)
      {
        ReadArray(reader => reader.ReadTypeParameterSpec());
        ReadExpression();
        ReadType();
      }

      if (tag == 6)
      {
        ReadExpression();
        ReadType();
        ReadTypes();
        ReadArray(ReadExpressionFunc);
      }

      if (tag == 7)
      {
        ReadArray(reader => reader.ReadBinding());
        ReadExpression();
      }

      if (tag == 8)
      {
        ReadBinding();
        ReadExpression();
      }

      if (tag == 9)
      {
        ReadDecisionTree();
        ReadArray(reader => reader.ReadTuple2(reader1 => reader1.ReadValue(), ReadExpressionFunc));
        ReadType();
      }

      if (tag == 10)
      {
        ReadType();
        ReadOption(reader => reader.ReadValue());
        ReadExpression();
        ReadArray(reader => reader.ReadObjectExpressionMethod());
        ReadArray(reader =>
          reader.ReadTuple2(
            ReadTypeFunc,
            reader1 => reader1.ReadArray(reader2 => reader2.ReadObjectExpressionMethod())));
      }

      if (tag == 11)
      {
        ReadArray(reader => reader.ReadStaticOptimizationConstraint());
        ReadExpression();
        ReadExpression();
      }

      if (tag == 12)
      {
        ReadArray(reader => reader.ReadTypeParameterSpec());
        ReadExpression();
      }

      if (tag == 13)
      {
        ReadExpression();
        ReadType();
      }

      return null;
    }

    private object ReadDecisionTree()
    {
      var tag = ReadByte();
      CheckTagValue(tag, 2);

      if (tag == 0)
      {
        ReadExpression();
        ReadArray(reader => reader.DecisionTreeCase());
        ReadOption(reader => reader.ReadDecisionTree());
      }

      if (tag == 1)
      {
        ReadArray(ReadExpressionFunc);
        ReadPackedInt();
      }

      if (tag == 3)
      {
        ReadType();
        ReadType();
      }

      if (tag == 4)
      {
        ReadPackedInt();
        ReadType();
      }

      return null;
    }

    private object DecisionTreeCase()
    {
      ReadDecisionTreeDiscriminator();
      ReadDecisionTree();

      return null;
    }

    private object ReadDecisionTreeDiscriminator()
    {
      var tag = ReadByte();
      CheckTagValue(tag, 4);
      if (tag == 0)
      {
        ReadUnionCaseRef();
        ReadTypes();
      }

      if (tag == 1)
        ReadConst();

      if (tag == 3)
      {
        ReadType();
        ReadType();
      }

      if (tag == 4)
      {
        ReadPackedInt();
        ReadType();
      }

      return null;
    }

    private object ReadOperation()
    {
      var tag = ReadByte();
      CheckTagValue(tag, 32);

      if (tag == 0)
        ReadUnionCaseRef();

      if (tag == 1 || tag == 3)
        ReadTypeRef();

      if (tag == 4 || tag == 5)
        ReadFieldRef();

      if (tag == 6)
        ReadTypeRef();

      if (tag == 7 || tag == 8)
      {
        ReadUnionCaseRef();
        ReadPackedInt();
      }

      if (tag == 9 || tag == 10)
      {
        ReadTypeRef();
        ReadPackedInt();
      }

      if (tag == 11)
        ReadPackedInt();

      if (tag == 12)
      {
        ReadArray(reader => reader.ReadIlInstruction());
        ReadTypes();
      }

      if (tag == 14)
        ReadUnionCaseRef();

      if (tag == 16)
        ReadMemberConstraint();

      if (tag == 17)
      {
        var tag17 = ReadByte();
        CheckTagValue(nameof(tag17), tag17, 3);
        ReadValueRef();
      }

      if (tag == 18)
      {
        ReadBoolean();
        ReadBoolean();
        ReadBoolean();
        ReadBoolean();
        ReadValueRefFlags();
        ReadBoolean();
        ReadBoolean();
        ReadIlMethodRef();
        ReadTypes();
        ReadTypes();
        ReadTypes();
      }

      if (tag == 21)
        ReadPackedInt();

      if (tag == 22)
        ReadBytes();

      if (tag == 25)
        ReadFieldRef();

      if (tag == 26)
        ReadArray(ReadIntFunc);

      if (tag == 28)
      {
        ReadUnionCaseRef();
        ReadPackedInt();
      }

      if (tag == 30)
        ReadPackedInt();

      if (tag == 31)
        ReadAnonRecord();

      if (tag == 32)
      {
        ReadAnonRecord();
        ReadPackedInt();
      }

      return null;
    }

    private object ReadIlInstruction()
    {
      var tag = ReadByte();
      CheckTagValue(tag, 66);

      if (tag == 1)
        ReadPackedInt();

      if (tag == 4 || tag == 24 || tag == 55)
      {
        ReadIlMethodRef();
        ReadIlType();
        ReadIlTypes();
      }

      if (tag == 20 || tag == 22 || tag == 23)
      {
        var basicTypeTag = ReadPackedInt();
        CheckTagValue(nameof(basicTypeTag), basicTypeTag, 13);
      }

      if (tag == 31 || tag == 33 | tag == 34 || tag == 36)
      {
        var volatilityTag = ReadPackedInt();
        CheckTagValue(nameof(volatilityTag), volatilityTag, 1);
        ReadIlFieldSpec();
      }

      if (tag == 32 || tag == 35)
        ReadIlFieldSpec();

      if (tag == 43 || tag == 38 || tag == 29 || tag == 61 || tag == 27 || tag == 28 || tag == 25 || tag == 37 ||
          tag == 58 || tag == 3 || tag == 63 || tag == 65)
        ReadIlType();

      if (tag == 26)
        ReadUniqueString();

      if (tag == 39 || tag == 60 || tag == 59)
      {
        ReadIlArrayShape();
        ReadIlType();
      }

      if (tag == 41)
      {
        var readonlyTag = ReadPackedInt();
        CheckTagValue(nameof(readonlyTag), readonlyTag, 1);
        ReadIlArrayShape();
        ReadIlType();
      }

      if (tag == 62)
      {
        ReadPackedInt();
        ReadPackedInt();
      }

      return null;
    }

    private object ReadIlFieldSpec()
    {
      var fieldRef = ReadIlFieldRef();
      var containingType = ReadIlType();

      return null;
    }

    private object ReadIlFieldRef()
    {
      ReadIlTypeRef();
      var name = ReadUniqueString();
      ReadIlType();

      return name;
    }

    private object ReadObjectExpressionMethod()
    {
      var signature = ReadAbstractSlotSignature();
      ReadAttributes();
      ReadArray(reader => reader.ReadTypeParameterSpec());
      ReadArray(reader => reader.ReadValue());
      ReadExpression();

      return signature;
    }

    private object ReadStaticOptimizationConstraint()
    {
      var tag = ReadByte();
      CheckTagValue(tag, 1);

      if (tag == 0)
      {
        ReadType();
        ReadType();
      }

      if (tag == 1)
        ReadType();

      return null;
    }

    private object ReadBinding()
    {
      ReadValue();
      ReadExpression();

      return null;
    }

    private object ReadConst()
    {
      var tag = ReadByte();
      CheckTagValue(tag, 17);
      return tag switch
      {
        0 => ReadBoolean(),
        1 => ReadPackedInt(),
        2 => ReadByte(),
        var x when x >= 3 && x <= 6 => ReadPackedInt(),
        var x when x >= 7 && x <= 10 => ReadPackedInt(),
        11 => ReadPackedInt(),
        12 => ReadInt64(),
        13 => ReadPackedInt(),
        14 => ReadUniqueString(),
        17 => ReadArray(ReadIntFunc),
        _ => null
      };
    }

    private string[] ReadXmlDoc(bool skipZeroAfter = false)
    {
      var xmlDoc = ReadArray(ReadUniqueStringFunc);
      if (skipZeroAfter)
        SkipZeros(1);
      return xmlDoc;
    }

    private string[] ReadXmlDocOption() =>
      (Option) ReadByte() switch
      {
        Option.None => null,
        Option.Some => ReadXmlDoc(true), // Converted from u_used_space1 u_xmldoc
        _ => throw new ArgumentOutOfRangeException()
      };

    private object ReadPublicPath()
    {
      var index = ReadPackedInt();

      var pathPartIndexes = myPublicPaths[index];
      var path = new string[pathPartIndexes.Length];
      for (var i = 0; i < path.Length; i++)
        path[i] = myStrings[pathPartIndexes[i]];

      return path;
    }

    private object ReadAccessibility() =>
      ReadArray(reader => reader.ReadCompilationPath());

    private TypeOrMeasureKind ReadTypeKind() =>
      (TypeOrMeasureKind) ReadByte();

    private string[] ReadCcuRefNames()
    {
      var ccuRefNamesCount = ReadPackedInt();
      var names = new string[ccuRefNamesCount];
      for (var i = 0; i < ccuRefNamesCount; i++)
      {
        var separator = ReadPackedInt();
        Assertion.Assert(separator == 0, "separator == 0");
        names[i] = ReadString();
      }

      return names;
    }

    private int ReadEntitiesCount(out bool hasAnonRecords)
    {
      var encodedTypeDeclsNumber = ReadPackedInt();
      hasAnonRecords = encodedTypeDeclsNumber < 0;
      return hasAnonRecords
        ? -encodedTypeDeclsNumber - 1
        : encodedTypeDeclsNumber;
    }

    private void SkipZeros(int bytes)
    {
      for (var i = 0; i < bytes; i++)
      {
        var b = ReadByte();
        CheckTagValue(nameof(b), b, 0);
      }
    }
  }
}
