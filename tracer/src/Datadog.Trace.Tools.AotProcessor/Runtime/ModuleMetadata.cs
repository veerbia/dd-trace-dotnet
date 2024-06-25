using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Xml.Linq;
using Datadog.Trace.Tools.AotProcessor.Interfaces;
using Mono.Cecil;

namespace Datadog.Trace.Tools.AotProcessor.Runtime
{
    internal partial class ModuleMetadata : IMetaDataImport2, IMetaDataAssemblyImport, IMetaDataEmit2, IMetaDataAssemblyEmit, IDisposable
    {
        private NativeObjects.IMetaDataImport2 metadataImport;
        private NativeObjects.IMetaDataEmit2 metadataEmit;
        private NativeObjects.IMetaDataAssemblyImport metadataAssemblyImport;
        private NativeObjects.IMetaDataAssemblyEmit metadataAssemblyEmit;

        private nint enumId = 1;
        private Dictionary<nint, IEnumerator> enumerators = new Dictionary<nint, IEnumerator>();

        public ModuleMetadata(ModuleInfo module)
        {
            Module = module;
            metadataImport = NativeObjects.IMetaDataImport2.Wrap(this);
            metadataEmit = NativeObjects.IMetaDataEmit2.Wrap(this);
            metadataAssemblyImport = NativeObjects.IMetaDataAssemblyImport.Wrap(this);
            metadataAssemblyEmit = NativeObjects.IMetaDataAssemblyEmit.Wrap(this);
        }

        public void Dispose()
        {
            metadataImport.Dispose();
            metadataEmit.Dispose();
            metadataAssemblyImport.Dispose();
            metadataAssemblyEmit.Dispose();
        }

        public ModuleInfo Module { get; }

        public HResult QueryInterface(in Guid guid, out IntPtr ptr)
        {
            if (
                guid == IUnknown.Guid ||
                guid == IMetaDataImport.Guid ||
                guid == IMetaDataImport2.Guid)
            {
                ptr = metadataImport;
                return HResult.S_OK;
            }
            else if (
                guid == IUnknown.Guid ||
                guid == IMetaDataEmit.Guid ||
                guid == IMetaDataEmit2.Guid)
            {
                ptr = metadataEmit;
                return HResult.S_OK;
            }
            else if (
                guid == IUnknown.Guid ||
                guid == IMetaDataAssemblyImport.Guid)
            {
                ptr = metadataAssemblyImport;
                return HResult.S_OK;
            }
            else if (
                guid == IUnknown.Guid ||
                guid == IMetaDataAssemblyEmit.Guid)
            {
                ptr = metadataAssemblyEmit;
                return HResult.S_OK;
            }

            ptr = IntPtr.Zero;
            return HResult.E_NOINTERFACE;
        }

        public int AddRef()
        {
            return 1;
        }

        public int Release()
        {
            return 1;
        }

        internal IMetadataTokenProvider? LookupToken(int tokenId)
        {
            var res = Module.Definition.LookupToken(tokenId);
            if (res is not null) { return res; }

            var token = new MetadataToken((uint)tokenId);
            if (token.TokenType == TokenType.AssemblyRef)
            {
                return Module.Definition.AssemblyReferences.FirstOrDefault(r => r.MetadataToken.ToInt32() == tokenId);
            }
            else if (token.TokenType == TokenType.ModuleRef)
            {
                return Module.Definition.ModuleReferences.FirstOrDefault(r => r.MetadataToken.ToInt32() == tokenId);
            }

            return null;
        }

        internal TypeReference SystemVoidRef()
        {
            var res = Module.Definition.GetTypeReferences().FirstOrDefault(r => r.Name == "Void");
            if (res is not null) { return res; }
            return new TypeReference("System", "Void", Module.Definition, null);
        }

        #region IMetadataImport2

        public unsafe HResult FindTypeDefByName(char* szTypeDef, MdToken tkEnclosingClass, MdTypeDef* ptd)
        {
            var name = System.Runtime.InteropServices.Marshal.PtrToStringAuto((IntPtr)szTypeDef);
            var type = Module.Definition.GetType(name);
            if (type is not null && ptd is not null)
            {
                *ptd = new MdTypeDef(type.MetadataToken.ToInt32());
                return HResult.S_OK;
            }

            return HResult.E_INVALIDARG;
        }

        public void CloseEnum(HCORENUM hEnum)
        {
            if (enumerators.TryGetValue(hEnum.Value, out var enumerator))
            {
                enumerator.Dispose();
                enumerators.Remove(hEnum.Value);
            }
        }

        public unsafe HResult CountEnum(HCORENUM hEnum, uint* pulCount)
        {
            if (enumerators.TryGetValue(hEnum.Value, out var enumerator))
            {
                *pulCount = (uint)enumerator.Delivered;
                return HResult.S_OK;
            }

            *pulCount = 0;
            return HResult.E_INVALIDARG;
        }

        public HResult ResetEnum(HCORENUM hEnum, uint ulPos)
        {
            if (enumerators.TryGetValue(hEnum.Value, out var enumerator) && enumerator.Reset(ulPos))
            {
                return HResult.S_OK;
            }

            return HResult.E_INVALIDARG;
        }

        public unsafe HResult EnumTypeRefs(HCORENUM* phEnum, MdTypeRef* rTypeRefs, uint cMax, uint* pcTypeRefs)
        {
            Enumerator<TypeReference, MdTypeRef> enumerator;
            if (phEnum is null || phEnum->Value == 0)
            {
                *phEnum = new HCORENUM(enumId++);
                enumerator = new Enumerator<TypeReference, MdTypeRef>(Module.Definition.GetTypeReferences().ToArray(), (i) => new MdTypeRef(i.MetadataToken.ToInt32()));
                enumerators[phEnum->Value] = enumerator;
            }
            else
            {
                enumerator = (Enumerator<TypeReference, MdTypeRef>)enumerators[phEnum->Value];
            }

            *pcTypeRefs = enumerator.Fetch(rTypeRefs, cMax);

            return HResult.S_OK;
        }

        public unsafe HResult GetTypeRefProps(MdTypeRef tr, MdToken* ptkResolutionScope, char* szName, uint cchName, uint* pchName)
        {
            var typeRef = Module.Definition.GetTypeReferences().FirstOrDefault(t => t.MetadataToken.ToInt32() == tr.Value);
            if (typeRef is not null)
            {
                *ptkResolutionScope = new MdToken((int)Module.Id.Value);
                typeRef.Name.CopyTo(cchName, szName, pchName);

                return HResult.S_OK;
            }

            return HResult.E_INVALIDARG;
        }

        public unsafe HResult GetMemberProps(MdToken mb, MdTypeDef* pClass, char* szMember, uint cchMember, uint* pchMember, int* pdwAttr, IntPtr* ppvSigBlob, uint* pcbSigBlob, uint* pulCodeRVA, int* pdwImplFlags, int* pdwCPlusTypeFlag, char* ppValue, uint* pcchValue)
        {
            var member = Module.GetMember(mb.Value);
            if (member is null) { return HResult.E_INVALIDARG; }

            *pClass = member.DeclaringType;
            member.Name.CopyTo(cchMember, szMember, pchMember);

            if (pdwAttr is not null)
            {
                *pdwAttr = member.Attributes;
            }

            if (ppvSigBlob is not null && pcbSigBlob is not null)
            {
                *ppvSigBlob = member.GetSignature();
                *pcbSigBlob = member.SignatureLength;
            }

            if (pdwImplFlags is not null)
            {
                *pdwImplFlags = 0;
            }

            if (pcchValue is not null)
            {
                *pcchValue = 0;
            }

            return HResult.S_OK;
        }

        public unsafe HResult GetTypeDefProps(MdTypeDef td, char* szTypeDef, uint cchTypeDef, uint* pchTypeDef, int* pdwTypeDefFlags, MdToken* ptkExtends)
        {
            var type = LookupToken(td.Value) as TypeDefinition;
            if (type is null) { return HResult.E_INVALIDARG; }

            type.Name.CopyTo(cchTypeDef, szTypeDef, pchTypeDef);

            if (pdwTypeDefFlags is not null)
            {
                *pdwTypeDefFlags = (int)type.Attributes;
            }

            if (ptkExtends is not null)
            {
                *ptkExtends = new MdToken(type.BaseType?.MetadataToken.ToInt32() ?? 0);
            }

            return HResult.S_OK;
        }

        public unsafe HResult GetNestedClassProps(MdTypeDef tdNestedClass, MdTypeDef* ptdEnclosingClass)
        {
            var type = LookupToken(tdNestedClass.Value) as TypeDefinition;
            if (type is null) { return HResult.E_INVALIDARG; }

            *ptdEnclosingClass = type.IsNested ? new MdTypeDef(type.DeclaringType.MetadataToken.ToInt32()) : default;
            return HResult.S_OK;
        }

        public unsafe HResult GetMethodProps(MdMethodDef mb, MdTypeDef* pClass, char* szMethod, uint cchMethod, uint* pchMethod, int* pdwAttr, IntPtr* ppvSigBlob, uint* pcbSigBlob, uint* pulCodeRVA, int* pdwImplFlags)
        {
            return GetMemberProps(new MdToken(mb.Value), pClass, szMethod, cchMethod, pchMethod, pdwAttr, ppvSigBlob, pcbSigBlob, pulCodeRVA, pdwImplFlags, null, null, null);
        }

        #endregion

        #region IMetadataEmit

        public unsafe HResult DefineTypeRefByName(MdToken tkResolutionScope, char* szName, MdTypeRef* ptr)
        {
            var scope = LookupToken(tkResolutionScope.Value) as IMetadataScope;
            var name = System.Runtime.InteropServices.Marshal.PtrToStringAuto((IntPtr)szName);
            if (string.IsNullOrEmpty(name)) { return HResult.E_INVALIDARG; }

            // Look for existing
            var existing = Module.Definition.GetTypeReferences().FirstOrDefault(r => r.FullName == name && r.Scope.MetadataToken.ToInt32() == tkResolutionScope.Value);
            if (existing is not null)
            {
                *ptr = new MdTypeRef(existing.MetadataToken.ToInt32());
                return HResult.S_OK;
            }

            // Split name
            SplitTypeName(name, out var @namespace, out var typeName);

            var typeRef = Module.Definition.AddRaw(new TypeReference(@namespace, typeName, Module.Definition, scope));
            *ptr = new MdTypeRef(typeRef.MetadataToken.ToInt32());

            return HResult.S_OK;
        }

        public unsafe HResult DefineMemberRef(MdToken tkImport, char* szName, IntPtr pvSigBlob, int cbSigBlob, MdMemberRef* pmr)
        {
            // We are going to suppose it's a method reference by now
            var typeRef = LookupToken(tkImport.Value) as TypeReference;
            var name = System.Runtime.InteropServices.Marshal.PtrToStringAuto((IntPtr)szName);

            // Look for existing
            var existing = Module.Definition.GetMemberReferences().FirstOrDefault(r => r.Name == name && r.DeclaringType.MetadataToken.ToInt32() == tkImport.Value);
            if (existing is not null)
            {
                *pmr = new MdMemberRef(existing.MetadataToken.ToInt32());
                return HResult.S_OK;
            }

            var sig = new byte[cbSigBlob];
            System.Runtime.InteropServices.Marshal.Copy(pvSigBlob, sig, 0, cbSigBlob);
            var methodRef = Module.Definition.AddRaw(new MethodReference(name, sig, typeRef));
            *pmr = new MdMemberRef(methodRef.MetadataToken.ToInt32());
            return HResult.S_OK;
        }

        public unsafe HResult DefineUserString(char* szString, int cchString, MdString* pstk)
        {
            var userString = System.Runtime.InteropServices.Marshal.PtrToStringAuto((IntPtr)szString);
            *pstk = new MdString(Module.Definition.AddRaw(userString).ToInt32());
            return HResult.S_OK;
        }

        public unsafe HResult DefineTypeDef(char* szTypeDef, int dwTypeDefFlags, MdToken tkExtends, MdToken* rtkImplements, MdTypeDef* ptd)
        {
            var name = System.Runtime.InteropServices.Marshal.PtrToStringAuto((IntPtr)szTypeDef);
            if (string.IsNullOrEmpty(name)) { return HResult.E_INVALIDARG; }

            var baseType = LookupToken(tkExtends.Value) as TypeReference;
            SplitTypeName(name, out var @namespace, out var typeName);

            var type = Module.Definition.AddRaw(new TypeDefinition(@namespace, typeName, (TypeAttributes)dwTypeDefFlags, baseType));
            *ptd = new MdTypeDef(type.MetadataToken.ToInt32());

            return HResult.S_OK;
        }

        public unsafe HResult DefineField(MdTypeDef td, char* szName, int dwFieldFlags, IntPtr pvSigBlob, int cbSigBlob, int dwCPlusTypeFlag, IntPtr pValue, int cchValue, MdFieldDef* pmd)
        {
            var type = LookupToken(td.Value) as TypeDefinition;
            var name = System.Runtime.InteropServices.Marshal.PtrToStringAuto((IntPtr)szName);
            var sig = new byte[cbSigBlob];
            System.Runtime.InteropServices.Marshal.Copy(pvSigBlob, sig, 0, cbSigBlob);

            var field = new FieldDefinition(name, (FieldAttributes)dwFieldFlags, sig, type);
            Module.Definition.AddRaw(field);
            *pmd = new MdFieldDef(field.MetadataToken.ToInt32());

            return HResult.S_OK;
        }

        public unsafe HResult DefineMethod(MdTypeDef td, char* szName, int dwMethodFlags, IntPtr pvSigBlob, int cbSigBlob, int ulCodeRVA, int dwImplFlags, MdMethodDef* pmd)
        {
            var type = LookupToken(td.Value) as TypeDefinition;
            var name = System.Runtime.InteropServices.Marshal.PtrToStringAuto((IntPtr)szName);
            var sig = new byte[cbSigBlob];
            System.Runtime.InteropServices.Marshal.Copy(pvSigBlob, sig, 0, cbSigBlob);

            var method = Module.Definition.AddRaw(new MethodDefinition(name, (MethodAttributes)dwMethodFlags, sig, type));
            *pmd = new MdMethodDef(method.MetadataToken.ToInt32());

            return HResult.S_OK;
        }

        public HResult SetMethodImplFlags(MdMethodDef md, int dwImplFlags)
        {
            var method = LookupToken(md.Value) as MethodDefinition;
            if (method is null) { return HResult.E_INVALIDARG; }

            method.ImplAttributes = (MethodImplAttributes)dwImplFlags;
            return HResult.S_OK;
        }

        public unsafe HResult DefineModuleRef(char* szName, MdModuleRef* pmur)
        {
            var name = System.Runtime.InteropServices.Marshal.PtrToStringAuto((IntPtr)szName);
            if (string.IsNullOrEmpty(name)) { return HResult.E_INVALIDARG; }
            var fileName = Path.GetFileName(name);

            var module = Module.Definition.AddRaw(new ModuleReference(fileName));
            *pmur = new MdModuleRef(module.MetadataToken.ToInt32());
            return HResult.S_OK;
        }

        public unsafe HResult DefinePinvokeMap(MdToken tk, int dwMappingFlags, char* szImportName, MdModuleRef mrImportDLL)
        {
            var token = LookupToken(tk.Value) as MethodDefinition;
            var moduleRef = LookupToken(mrImportDLL.Value) as ModuleReference;
            if (token is null || moduleRef is null) { return HResult.E_INVALIDARG; }

            token.PInvokeInfo = new PInvokeInfo((PInvokeAttributes)dwMappingFlags, System.Runtime.InteropServices.Marshal.PtrToStringAuto((IntPtr)szImportName), moduleRef);

            return HResult.S_OK;
        }

        public unsafe HResult GetTokenFromSig(IntPtr pvSig, int cbSig, MdSignature* pmsig)
        {
            var sig = new byte[cbSig];
            System.Runtime.InteropServices.Marshal.Copy(pvSig, sig, 0, cbSig);

            *pmsig = new MdSignature(Module.Definition.AddRaw(sig).ToInt32());

            return HResult.S_OK;
        }

        #endregion

        #region IMetadataAssemblyImport

        public unsafe HResult GetAssemblyFromScope(out MdAssembly ptkAssembly)
        {
            ptkAssembly = new MdAssembly((int)Module.Assembly.Id.Value);
            return HResult.S_OK;
        }

        public unsafe HResult GetAssemblyProps(MdAssembly mda, IntPtr* ppbPublicKey, int* pcbPublicKey, int* pulHashAlgId, char* szName, uint cchName, uint* pchName, ASSEMBLYMETADATA* pMetaData, int* pdwAssemblyFlags)
        {
            var assembly = Module.Assembly.Runtime.GetAssemblyInfo(mda.Value);
            if (assembly is null)
            {
                return HResult.E_INVALIDARG;
            }

            if (assembly.Definition.Name.HasPublicKey)
            {
                // Retrieve the public key (not needed ATM)
            }

            assembly.Name.CopyTo(cchName, szName, pchName);

            *pMetaData = assembly.AssemblyMetaData;

            return HResult.S_OK;
        }

        public unsafe HResult EnumAssemblyRefs(HCORENUM* phEnum, MdAssemblyRef* rAssemblyRefs, uint cMax, out uint pcTokens)
        {
            Enumerator<AssemblyNameReference, MdAssemblyRef> enumerator;
            if (phEnum is null || phEnum->Value == 0)
            {
                *phEnum = new HCORENUM(enumId++);
                enumerator = new Enumerator<AssemblyNameReference, MdAssemblyRef>(Module.Definition.AssemblyReferences.ToArray(), (i) => new MdAssemblyRef(i.MetadataToken.ToInt32()));
                enumerators[phEnum->Value] = enumerator;
            }
            else
            {
                enumerator = (Enumerator<AssemblyNameReference, MdAssemblyRef>)enumerators[phEnum->Value];
            }

            pcTokens = enumerator.Fetch(rAssemblyRefs, cMax);

            return HResult.S_OK;
        }

        public unsafe HResult GetAssemblyRefProps(MdAssemblyRef mdar, byte* ppbPublicKeyOrToken, int* pcbPublicKeyOrToken, char* szName, uint cchName, uint* pchName, ASSEMBLYMETADATA* pMetaData, byte* ppbHashValue, int* pcbHashValue, int* pdwAssemblyRefFlags)
        {
            var assemblyRef = Module.Definition.AssemblyReferences.FirstOrDefault(a => a.MetadataToken.ToInt32() == mdar.Value);
            if (assemblyRef is null) { return HResult.E_INVALIDARG; }

            assemblyRef.Name.CopyTo(cchName, szName, pchName);
            if (pMetaData is not null)
            {
                *pMetaData = new ASSEMBLYMETADATA(assemblyRef.Version.Major, assemblyRef.Version.Minor, assemblyRef.Version.Build, assemblyRef.Version.Revision);
            }

            return HResult.S_OK;
        }

        #endregion

        #region IMetaDataAssemblyEmit

        public unsafe HResult DefineAssemblyRef(IntPtr pbPublicKeyOrToken, int cbPublicKeyOrToken, char* szName, ASSEMBLYMETADATA* pMetaData, IntPtr pbHashValue, int cbHashValue, int dwAssemblyRefFlags, MdAssemblyRef* pmdar)
        {
            var name = System.Runtime.InteropServices.Marshal.PtrToStringAuto((IntPtr)szName);
            if (string.IsNullOrEmpty(name)) { return HResult.E_INVALIDARG; }
            if (name == "mscorlib") { name = "System.Runtime"; }

            var reference = Module.Definition.AssemblyReferences.FirstOrDefault(a => a.Name == "System.Runtime");
            if (reference is not null)
            {
                *pmdar = new MdAssemblyRef(reference.MetadataToken.ToInt32());
                return HResult.S_OK;
            }

            reference = new AssemblyNameReference(
                   name,
                   new Version(pMetaData->usMajorVersion, pMetaData->usMinorVersion, pMetaData->usBuildNumber, pMetaData->usRevisionNumber));

            if (cbPublicKeyOrToken > 0)
            {
                reference.PublicKeyToken = new byte[cbPublicKeyOrToken];
                System.Runtime.InteropServices.Marshal.Copy(pbPublicKeyOrToken, reference.PublicKeyToken, 0, cbPublicKeyOrToken);
            }

            reference.MetadataToken = new MetadataToken(TokenType.AssemblyRef, Module.Definition.AssemblyReferences.Count + 1);

            Module.Definition.AssemblyReferences.Add(reference);
            *pmdar = new MdAssemblyRef(reference.MetadataToken.ToInt32());

            return HResult.S_OK;
        }

        #endregion

        private void SplitTypeName(string name, out string @namespace, out string typeName)
        {
            var index = name.LastIndexOf('.');
            if (index == -1)
            {
                @namespace = string.Empty;
                typeName = name;
            }
            else
            {
                @namespace = name.Substring(0, index);
                typeName = name.Substring(index + 1);
            }
        }
    }
}