﻿/* Copyright 2024 Tara "Dino" Cassatt
 * 
 * This file is part of BanjoBotAssets.
 * 
 * BanjoBotAssets is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * BanjoBotAssets is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with BanjoBotAssets.  If not, see <http://www.gnu.org/licenses/>.
 */
namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class SurvivorPortraitExporter(IExporterContext services) : UObjectExporter(services)
    {
        protected override string Type => "WorkerPortrait";

        protected override bool InterestedInAsset(string name) => name.Contains("/Icon-Worker/IconDefinitions", StringComparison.OrdinalIgnoreCase);

        protected override Task<bool> ExportAssetAsync(UObject asset, NamedItemData itemData, Dictionary<ImageType, string> imagePaths)
        {
            if (asset.GetSoftAssetPath("SmallImage") is string smallImagePath)
                imagePaths.Add(ImageType.SmallPreview, smallImagePath);

            if (asset.GetSoftAssetPath("LargeImage") is string largeImagePath)
                imagePaths.Add(ImageType.LargePreview, largeImagePath);

            return base.ExportAssetAsync(asset, itemData, imagePaths);
        }
    }
}
