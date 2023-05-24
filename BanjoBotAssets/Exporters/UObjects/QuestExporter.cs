﻿/* Copyright 2023 Tara "Dino" Cassatt
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
    internal sealed partial class QuestExporter : UObjectExporter<UFortQuestItemDefinition, QuestItemData>
    {
        private string? questRewardsPath, objectiveStatTablePath, homebaseRatingDifficultyMappingPath;
        private UDataTable? questRewardsTable, objectiveStatTable, homebaseRatingDifficultyMappingTable;

        public QuestExporter(IExporterContext services) : base(services) { }

        protected override string Type => "Quest";

        protected override bool IgnoreLoadFailures => true;

        protected override bool InterestedInAsset(string name)
        {
            if (name.EndsWith("/QuestRewards.uasset", StringComparison.OrdinalIgnoreCase))
            {
                questRewardsPath = name;
            }

            if (name.EndsWith("/ObjectiveStatTable.uasset", StringComparison.OrdinalIgnoreCase))
            {
                objectiveStatTablePath = name;
            }

            if (name.EndsWith("/HomebaseRatingDifficultyMapping.uasset", StringComparison.OrdinalIgnoreCase))
            {
                homebaseRatingDifficultyMappingPath = name;
            }

            return name.Contains("/Content/Quests/", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            questRewardsTable = await TryLoadTableAsync(questRewardsPath);
            objectiveStatTable = await TryLoadTableAsync(objectiveStatTablePath);
            homebaseRatingDifficultyMappingTable = await TryLoadTableAsync(homebaseRatingDifficultyMappingPath);

            await base.ExportAssetsAsync(progress, output, cancellationToken);
        }

        protected override Task<bool> ExportAssetAsync(UFortQuestItemDefinition asset, QuestItemData namedItemData, Dictionary<ImageType, string> imagePaths)
        {
            var objectives = new List<QuestObjective>();

            if (asset.Objectives != null)
            {
                foreach (var o in asset.Objectives)
                {
                    var qo = new QuestObjective
                    {
                        BackendName = o.BackendName.Text,
                        Count = o.Count,
                        Description = o.Description.Text,
                        HudShortDescription = o.HudShortDescription.Text,
                    };

                    if (objectiveStatTable != null)
                    {
                        var row = o.ObjectiveStatHandle.RowName;
                        qo.ZonePowerLevel = TryGetZonePowerLevelCondition(row.Text);

                        if (qo.ZonePowerLevel != null)
                        {
                            qo.Description = qo.Description.Replace("[UIRating]", qo.ZonePowerLevel.ToString(), StringComparison.OrdinalIgnoreCase);
                            qo.HudShortDescription = qo.HudShortDescription.Replace("[UIRating]", qo.ZonePowerLevel.ToString(), StringComparison.OrdinalIgnoreCase);
                        }
                    }

                    objectives.Add(qo);
                }
            }

            namedItemData.Objectives = objectives.ToArray();
            namedItemData.Category = asset.Category?.RowName.Text ?? "";

            var rewards = new List<QuestReward>();
            if (questRewardsTable != null)
            {
                // for Quest:daily_destroyarcademachines, we use the rows "Daily_DestroyArcadeMachines_001", "Daily_DestroyArcadeMachines_002", etc.
                var regex = new Regex(@$"^{Regex.Escape(asset.Name)}_\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

                foreach (var (key, reward) in questRewardsTable.RowMap)
                {
                    if (!key.IsNone && regex.IsMatch(key.Text))
                    {
                        rewards.Add(new QuestReward
                        {
                            Item = reward.Get<FName>("TemplateId").Text,
                            Quantity = reward.Get<int>("Quantity"),
                            Hidden = reward.Get<bool>("Hidden"),
                            Selectable = reward.Get<bool>("Selectable"),
                        });
                    }
                }
            }
            namedItemData.Rewards = rewards.ToArray();

            return Task.FromResult(true);
        }

        private static readonly Regex zoneDifficultyRegex = ZoneDifficultyRegex();

        private int? TryGetZonePowerLevelCondition(string rowKey)
        {
            if (objectiveStatTable == null || homebaseRatingDifficultyMappingTable == null)
                return null;

            if (!objectiveStatTable.TryGetDataTableRow(rowKey, StringComparison.OrdinalIgnoreCase, out var rowValue))
                return null;

            if (rowValue.Properties != null)
            {
                // parse the Condition property if it's one we recognize
                var condition = rowValue.GetOrDefault<string>("Condition");
                if (condition != null && zoneDifficultyRegex.Match(condition) is { Success: true } match)
                {
                    if (int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minDifficulty))
                    {
                        // find the highest number row whose difficulty value is <= the one mentioned in the Condition property
                        // NOTE: this assumes the rows are in decreasing order, which they are as of 19.10
                        foreach (var row in homebaseRatingDifficultyMappingTable.RowMap)
                        {
                            if (row.Value.Get<int>("Difficulty") <= minDifficulty)
                            {
                                return int.Parse(row.Key.Text, CultureInfo.InvariantCulture);
                            }
                        }
                    }

                    // difficulty is literally off the charts
                    logger.LogWarning(Resources.Warning_CannotParseDifficultyCondition, condition);
                }
            }

            return null;
        }

        [GeneratedRegex(@"Zone\.Difficulty\s*>=\s*(\d+)", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex ZoneDifficultyRegex();
    }
}
