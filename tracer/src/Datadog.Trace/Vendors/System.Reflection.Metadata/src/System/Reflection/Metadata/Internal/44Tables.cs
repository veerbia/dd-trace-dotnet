//------------------------------------------------------------------------------
// <auto-generated />
// This file was automatically generated by the UpdateVendors tool.
//------------------------------------------------------------------------------
#pragma warning disable CS0618, CS0649, CS1574, CS1580, CS1581, CS1584, CS1591, CS1573, CS8018, SYSLIB0011, SYSLIB0032
#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8604, CS8618, CS8620, CS8714, CS8762, CS8765, CS8766, CS8767, CS8768, CS8769, CS8612, CS8629, CS8774
#nullable enable
// Decompiled with JetBrains decompiler
// Type: System.Reflection.Metadata.Ecma335.GenericParamConstraintTableReader
// Assembly: System.Reflection.Metadata, Version=7.0.0.2, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// MVID: 2EB35F4B-CF50-496F-AFB8-CC6F6F79CB72

using Datadog.Trace.VendoredMicrosoftCode.System.Reflection.Internal;

namespace Datadog.Trace.VendoredMicrosoftCode.System.Reflection.Metadata.Ecma335
{
  internal readonly struct GenericParamConstraintTableReader
  {
    internal readonly int NumberOfRows;
    private readonly bool _IsGenericParamTableRowRefSizeSmall;
    private readonly bool _IsTypeDefOrRefRefSizeSmall;
    private readonly int _OwnerOffset;
    private readonly int _ConstraintOffset;
    internal readonly int RowSize;
    internal readonly MemoryBlock Block;

    internal GenericParamConstraintTableReader(
      int numberOfRows,
      bool declaredSorted,
      int genericParamTableRowRefSize,
      int typeDefOrRefRefSize,
      MemoryBlock containingBlock,
      int containingBlockOffset)
    {
      this.NumberOfRows = numberOfRows;
      this._IsGenericParamTableRowRefSizeSmall = genericParamTableRowRefSize == 2;
      this._IsTypeDefOrRefRefSizeSmall = typeDefOrRefRefSize == 2;
      this._OwnerOffset = 0;
      this._ConstraintOffset = this._OwnerOffset + genericParamTableRowRefSize;
      this.RowSize = this._ConstraintOffset + typeDefOrRefRefSize;
      this.Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, this.RowSize * numberOfRows);
      if (declaredSorted || this.CheckSorted())
        return;
      Throw.TableNotSorted(TableIndex.GenericParamConstraint);
    }

    internal GenericParameterConstraintHandleCollection FindConstraintsForGenericParam(
      GenericParameterHandle genericParameter)
    {
      int startRowNumber;
      int endRowNumber;
      this.Block.BinarySearchReferenceRange(this.NumberOfRows, this.RowSize, this._OwnerOffset, (uint) genericParameter.RowId, this._IsGenericParamTableRowRefSizeSmall, out startRowNumber, out endRowNumber);
      return startRowNumber == -1 ? new GenericParameterConstraintHandleCollection() : new GenericParameterConstraintHandleCollection(startRowNumber + 1, (ushort) (endRowNumber - startRowNumber + 1));
    }

    private bool CheckSorted() => this.Block.IsOrderedByReferenceAscending(this.RowSize, this._OwnerOffset, this._IsGenericParamTableRowRefSizeSmall);

    internal EntityHandle GetConstraint(GenericParameterConstraintHandle handle) => TypeDefOrRefTag.ConvertToHandle(this.Block.PeekTaggedReference((handle.RowId - 1) * this.RowSize + this._ConstraintOffset, this._IsTypeDefOrRefRefSizeSmall));

    internal GenericParameterHandle GetOwner(GenericParameterConstraintHandle handle) => GenericParameterHandle.FromRowId(this.Block.PeekReference((handle.RowId - 1) * this.RowSize + this._OwnerOffset, this._IsGenericParamTableRowRefSizeSmall));
  }
}
