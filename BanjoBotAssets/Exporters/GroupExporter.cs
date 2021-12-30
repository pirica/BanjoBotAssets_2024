﻿using CUE4Parse.FileProvider;
using CUE4Parse.FN.Exports.FortniteGame;
using CUE4Parse_Fortnite.Enums;

namespace BanjoBotAssets.Exporters
{
    internal record BaseParsedItemName(string BaseName, string Rarity, int Tier);

    internal record BaseItemGroupFields(string DisplayName, string? Description, string? SubType)
    {
        public BaseItemGroupFields() : this("", null, null) { }
    }

    internal abstract class GroupExporter<TAsset, TParsedName, TFields, TItemData> : BaseExporter
        where TAsset : UFortItemDefinition
        where TParsedName : BaseParsedItemName
        where TFields : BaseItemGroupFields, new()
        where TItemData : NamedItemData, new()
    {
        private int numToProcess, processedSoFar;

        public GroupExporter(DefaultFileProvider provider) : base(provider) { }

        protected abstract string Type { get; }
        protected abstract TParsedName? ParseAssetName(string name);

        private void Report(IProgress<ExportProgress> progress, string current)
        {
            progress.Report(new ExportProgress
            {
                TotalSteps = numToProcess,
                CompletedSteps = processedSoFar,
                AssetsLoaded = assetsLoaded,
                CurrentItem = current
            });
        }

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, ExportedAssets output)
        {
            var uniqueAssets = assetPaths.ToLookup(path => ParseAssetName(path)?.BaseName);
            numToProcess = uniqueAssets.Count;

            Report(progress, "Exporting " + Type);

            await Parallel.ForEachAsync<IGrouping<string, string>>(uniqueAssets, async (grouping, _cancellationToken) =>
            {
                var baseName = grouping.Key;

                if (baseName == null)
                    return;

                var primaryAssetPath = SelectPrimaryAsset(grouping);
                var file = provider[primaryAssetPath];

                var num = Interlocked.Increment(ref numToProcess);
                Console.WriteLine("Processing {0} group {1} of {2}", Type, num, numToProcess);

                Console.WriteLine("Loading {0}", file.PathWithoutExtension);
                Interlocked.Increment(ref assetsLoaded);

                Report(progress, file.PathWithoutExtension);

                var asset = await provider.LoadObjectAsync<TAsset>(file.PathWithoutExtension);

                if (asset == null)
                {
                    Console.WriteLine("Failed to load {0}", file.PathWithoutExtension);
                    return;
                }

                if (WantThisAsset(asset) == false)
                    return;

                var fields = await ExtractCommonFieldsAsync(asset, grouping);

                LogAssetName(baseName, fields);

                foreach (var path in grouping)
                {
                    var templateId = $"{Type}:{Path.GetFileNameWithoutExtension(path)}";
                    var parsed = ParseAssetName(path);

                    if (parsed == null)
                        continue;

                    var itemData = new TItemData();
                    ExportAsset(parsed, asset, fields, path, itemData);
                    output.NamedItems.TryAdd(templateId, itemData);
                }
            });
        }

        private void ExportAsset(TParsedName parsed, TAsset asset, TFields fields, string path, TItemData itemData)
        {
            itemData.AssetPath = provider.FixPath(Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path)));
            itemData.Description = GetDescription(parsed, asset, fields);
            itemData.DisplayName = GetDisplayName(parsed, asset, fields);
            itemData.Name = Path.GetFileNameWithoutExtension(path);
            itemData.SubType = fields.SubType;
            itemData.Type = Type;
            itemData.Rarity = GetRarity(parsed, asset, fields).GetNameText().Text;
            itemData.Tier = parsed.Tier;
        }

        protected virtual string SelectPrimaryAsset(IGrouping<string?, string> assetGroup)
        {
            return assetGroup.First();
        }

        protected virtual bool WantThisAsset(TAsset asset)
        {
            return true;
        }

        protected virtual Task<TFields> ExtractCommonFieldsAsync(TAsset asset, IGrouping<string?, string> grouping)
        {
            return Task.FromResult(new TFields() with
            {
                Description = asset.Description?.Text,
                DisplayName = asset.DisplayName?.Text ?? $"<{grouping.Key}>",
                SubType = null,
            });
        }

        protected virtual void LogAssetName(string baseName, TFields fields)
        {
            Console.WriteLine("{0} is {1}", baseName, fields.DisplayName);
        }

        protected virtual EFortRarity GetRarity(TParsedName parsedName, TAsset primaryAsset, TFields fields) =>
            parsedName.Rarity switch
            {
                "C" => EFortRarity.Common,
                "R" => EFortRarity.Rare,
                "VR" => EFortRarity.Epic,
                "SR" => EFortRarity.Legendary,
                "UR" => EFortRarity.Mythic,
                _ => EFortRarity.Uncommon,
            };

        protected virtual string GetDisplayName(TParsedName parsedName, TAsset primaryAsset, TFields fields) => fields.DisplayName;

        protected virtual string? GetDescription(TParsedName parsedName, TAsset primaryAsset, TFields fields) => fields.Description;
    }

    internal abstract class GroupExporter<TAsset> : GroupExporter<TAsset, BaseParsedItemName, BaseItemGroupFields, NamedItemData>
        where TAsset : UFortItemDefinition
    {
        public GroupExporter(DefaultFileProvider provider) : base(provider)
        {
        }
    }

    internal record HeroItemGroupFields(string DisplayName, string? Description, string? SubType,
        string HeroPerk, string HeroPerkDescription, string CommanderPerk, string CommanderPerkDescription)
        : BaseItemGroupFields(DisplayName, Description, SubType)
    {
        public HeroItemGroupFields() : this("", null, null, "", "", "", "") { }
    }
}
