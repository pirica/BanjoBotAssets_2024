﻿using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Models
{
    internal class HeroItemData : NamedItemData
    {
        [DisallowNull]
        public string? HeroPerk { get; set; }
        [DisallowNull]
        public string? HeroPerkDescription { get; set; }
        [DisallowNull]
        public string? CommanderPerk { get; set; }
        [DisallowNull]
        public string? CommanderPerkDescription { get; set; }
    }
}
