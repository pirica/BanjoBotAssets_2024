﻿using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Artifacts
{
    internal class ItemRatingTier
    {
        public int FirstLevel { get; set; }
        [DisallowNull]
        public float[]? Ratings { get; set; }
    }
}