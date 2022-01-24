﻿using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Models
{
    internal class SchematicItemData : NamedItemData
    {
        public string? EvoType { get; set; }
        [DisallowNull]
        public AlterationSlot[]? AlterationSlots { get; set; }
    }
}
