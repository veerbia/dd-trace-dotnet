//------------------------------------------------------------------------------
// <auto-generated />
// This file was automatically generated by the UpdateVendors tool.
//------------------------------------------------------------------------------
#pragma warning disable CS0618, CS0649, CS1574, CS1580, CS1581, CS1584, CS1591, CS1573, CS8018, SYSLIB0011, SYSLIB0032
#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8604, CS8618, CS8620, CS8714, CS8762, CS8765, CS8766, CS8767, CS8768, CS8769, CS8612, CS8629, CS8774
// Decompiled with JetBrains decompiler
// Type: System.Reflection.Metadata.HandleComparer
// Assembly: System.Reflection.Metadata, Version=7.0.0.2, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// MVID: 2EB35F4B-CF50-496F-AFB8-CC6F6F79CB72

using System.Collections.Generic;


#nullable enable
namespace Datadog.Trace.VendoredMicrosoftCode.System.Reflection.Metadata
{
    internal sealed class HandleComparer : 
    IEqualityComparer<Handle>,
    IComparer<Handle>,
    IEqualityComparer<EntityHandle>,
    IComparer<EntityHandle>
  {

    #nullable disable
    private static readonly HandleComparer s_default = new HandleComparer();

    private HandleComparer()
    {
    }


    #nullable enable
    public static HandleComparer Default => HandleComparer.s_default;

    public bool Equals(Handle x, Handle y) => x.Equals(y);

    public bool Equals(EntityHandle x, EntityHandle y) => x.Equals(y);

    public int GetHashCode(Handle obj) => obj.GetHashCode();

    public int GetHashCode(EntityHandle obj) => obj.GetHashCode();

    /// <summary>Compares two handles.</summary>
    /// <remarks>
    /// The order of handles that differ in kind and are not <see cref="T:System.Reflection.Metadata.EntityHandle" /> is undefined.
    /// Returns 0 if and only if <see cref="M:System.Reflection.Metadata.HandleComparer.Equals(System.Reflection.Metadata.Handle,System.Reflection.Metadata.Handle)" /> returns true.
    /// </remarks>
    public int Compare(Handle x, Handle y) => Handle.Compare(x, y);

    /// <summary>Compares two entity handles.</summary>
    /// <remarks>
    /// Returns 0 if and only if <see cref="M:System.Reflection.Metadata.HandleComparer.Equals(System.Reflection.Metadata.EntityHandle,System.Reflection.Metadata.EntityHandle)" /> returns true.
    /// </remarks>
    public int Compare(EntityHandle x, EntityHandle y) => EntityHandle.Compare(x, y);
  }
}
