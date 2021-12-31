﻿namespace BanjoBotAssets.Exporters
{
    internal sealed class WorldItemExporter : UObjectExporter
    {
        public WorldItemExporter(DefaultFileProvider provider) : base(provider) { }

        protected override string Type => "WorldItem";

        protected override bool InterestedInAsset(string name) =>
             name.Contains("Items/ResourcePickups/") && !name.Contains("/Athena");
    }
}