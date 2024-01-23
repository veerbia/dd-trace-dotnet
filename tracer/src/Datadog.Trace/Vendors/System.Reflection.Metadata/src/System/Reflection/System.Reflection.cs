//------------------------------------------------------------------------------
// <auto-generated />
// This file was automatically generated by the UpdateVendors tool.
//------------------------------------------------------------------------------
#pragma warning disable CS0618, CS0649, CS1574, CS1580, CS1581, CS1584, CS1591, CS1573, CS8018, SYSLIB0011, SYSLIB0032
#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8604, CS8618, CS8620, CS8714, CS8762, CS8765, CS8766, CS8767, CS8768, CS8769, CS8612, CS8629, CS8774
#nullable enable
// Decompiled with JetBrains decompiler
// Type: System.Reflection.MethodSemanticsAttributes
// Assembly: System.Reflection.Metadata, Version=7.0.0.2, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// MVID: 2EB35F4B-CF50-496F-AFB8-CC6F6F79CB72

using System;

namespace Datadog.Trace.VendoredMicrosoftCode.System.Reflection
{
  [Flags]
  internal enum MethodSemanticsAttributes
  {
    /// <summary>
    /// Used to modify the value of the property.
    /// CLS-compliant setters are named with set_ prefix.
    /// </summary>
    Setter = 1,
    /// <summary>
    /// Used to read the value of the property.
    /// CLS-compliant getters are named with get_ prefix.
    /// </summary>
    Getter = 2,
    /// <summary>
    /// Other method for property (not getter or setter) or event (not adder, remover, or raiser).
    /// </summary>
    Other = 4,
    /// <summary>
    /// Used to add a handler for an event.
    /// Corresponds to the AddOn flag in the Ecma 335 CLI specification.
    /// CLS-compliant adders are named with add_ prefix.
    /// </summary>
    Adder = 8,
    /// <summary>
    /// Used to remove a handler for an event.
    /// Corresponds to the RemoveOn flag in the Ecma 335 CLI specification.
    /// CLS-compliant removers are named with remove_ prefix.
    /// </summary>
    Remover = 16, // 0x00000010
    /// <summary>
    /// Used to indicate that an event has occurred.
    /// Corresponds to the Fire flag in the Ecma 335 CLI specification.
    /// CLS-compliant raisers are named with raise_ prefix.
    /// </summary>
    Raiser = 32, // 0x00000020
  }
}