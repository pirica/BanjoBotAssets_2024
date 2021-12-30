﻿using CUE4Parse.FN.Structs.Engine;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using System.Text.RegularExpressions;

namespace BanjoBotAssets
{
    internal static class AbilityDescription
    {
        private sealed class NullAssetCounter : IAssetCounter
        {
            public void CountAssetLoaded()
            {
                // nada
            }

            public static readonly NullAssetCounter Instance = new NullAssetCounter();
        }

        [Obsolete("Pass a real IAssetCounter")]
        public static Task<string?> GetAsync(UObject? grantedAbilityKit) => GetAsync(grantedAbilityKit, NullAssetCounter.Instance);

        public static async Task<string?> GetAsync(UObject? grantedAbilityKit, IAssetCounter assetCounter)
        {
            var (markup, cdo) = await GetMarkupAsync(grantedAbilityKit, assetCounter);

            if (markup == null)
                return null;

            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (cdo != null)
                await GetTokensAsync(cdo, tokens, assetCounter);

            return FormatMarkup(markup, tokens);
        }

        static async Task<(string? markup, UObject? tooltip)> GetMarkupAsync(UObject? grantedAbilityKit, IAssetCounter assetCounter)
        {
            var tooltipDescription = grantedAbilityKit?.GetOrDefault<FText>("TooltipDescription");
            if (tooltipDescription != null)
            {
                // hi, Chaos Agent
                return (tooltipDescription.Text, null);
            }
            var tooltip = grantedAbilityKit?.GetOrDefault<UBlueprintGeneratedClass>("Tooltip");
            if (tooltip == null)
            {
                return (null, null);
            }
            var cdo = await tooltip.ClassDefaultObject.LoadAsync();
            assetCounter.CountAssetLoaded();
            return (cdo == null ? null : (await cdo.GetInheritedOrDefaultAsync<FText>("Description", assetCounter))?.Text, cdo);
        }

        static async Task GetTokensAsync(UObject cdo, Dictionary<string, string> tokens, IAssetCounter assetCounter)
        {
            var style = cdo.GetOrDefault("dataRows_conversionStyle", new UScriptMap());

            //const string LEVEL = "F_Level_8_E09B5737400C3B2BE4EB12A65A011266";
            const string ROW = "Row_4_BFED534C47DE4BA0FAD849A5DFCFFEA2";
            const string RETURN_FORMATTING = "ReturnFormating_5_537D683042CBD7E588B26ABF9AC9ABE6";
            const string SHOULD_MODIFY_VALUE = "ShouldModifyValue_25_8A6AE4A84CC50870C881B987E540F003";
            //const string MODIFY_LEVEL = "F_ModifyLevel_19_9F27BA314EA57935BE8DF7AAE57CDC1E";
            const string MODIFY_ROW = "ModfifyRow_20_FB710B884BD580129A762F82C8CE1C03";
            const string MODIFY_OPERATION = "ModifyOperation_21_A19E46F44E5371B3E69778A2D93B9AE9";

            IEnumerable<KeyValuePair<FPropertyTagType?, FPropertyTagType?>>? props = style.Properties;

            // some tokens might only be defined in the parent...
            if (cdo.Template?.Name.Text is string and not "Default__TTT_Perks_C")
            {
                assetCounter.CountAssetLoaded();
                var parent = await cdo.Template.LoadAsync();
                var parentProps = parent?.GetOrDefault("dataRows_conversionStyle", new UScriptMap()).Properties;
                if (parentProps != null)
                    props = parentProps.Concat(props);
            }

            foreach (var p in props)
            {
                if (p.Key == null || p.Value == null)
                    continue;

                const string prefix = "Tooltip.Token.";

                var tagName = (p.Key.GetValue(typeof(FStructFallback)) as FStructFallback)?.GetOrDefault<FName>("TagName");
                if (tagName?.Text.StartsWith(prefix) != true)
                    continue;

                var tokenName = tagName.Value.Text[prefix.Length..];

                if (p.Value.GetValue(typeof(FStructFallback)) is not FStructFallback tokenDef)
                    continue;

                // get the value from the curve table
                float? GetValueFromCurveTable(string property)
                {
                    var row = tokenDef.Get<FStructFallback>(property);

                    var multiplier = row.Get<float>("Value");
                    var curveTableRow = row.Get<FCurveTableRowHandle>("Curve");

                    // find the right FName to use, what a pain
                    var rowNameStr = curveTableRow.RowName.Text;
                    var curveName = curveTableRow.CurveTable.RowMap.Keys.FirstOrDefault(k => k.Text == rowNameStr);

                    if (curveName.IsNone)
                    {
                        Console.WriteLine("WARNING: Curve table has no row {0}", rowNameStr);
                        return null;
                    }

                    var curve = curveTableRow.CurveTable.FindCurve(curveName);
                    return curve?.Eval(1) * multiplier;
                }

                var maybeValue = GetValueFromCurveTable(ROW);
                if (maybeValue == null)
                    continue;
                var value = maybeValue.Value;

                // modify the value?
                var shouldModifyValue = tokenDef.Get<bool>(SHOULD_MODIFY_VALUE);
                if (shouldModifyValue)
                {
                    var maybeModValue = GetValueFromCurveTable(MODIFY_ROW);
                    if (maybeModValue == null)
                        continue;
                    var modValue = maybeModValue.Value;

                    switch (tokenDef.Get<FName>(MODIFY_OPERATION).Text)
                    {
                        case "TTT_ModifierOperation::NewEnumerator0":
                            // Add
                            value += modValue;
                            break;
                        case "TTT_ModifierOperation::NewEnumerator1":
                            // Multiply
                            value *= modValue;
                            break;
                        case "TTT_ModifierOperation::NewEnumerator2":
                            // Divide
                            value /= modValue;
                            break;
                        case "TTT_ModifierOperation::NewEnumerator4":
                            // Override
                            value = modValue;
                            break;
                        case "TTT_ModifierOperation::NewEnumerator6":
                            // Add (Percentage)
                            value += modValue - 1;
                            break;
                        default:
                            Console.WriteLine("WARNING: Ignoring unknown modify operation {0}", tokenDef.Get<FName>(MODIFY_OPERATION).Text);
                            break;
                    }
                }

                // format the value
                var formatting = tokenDef.Get<FName>(RETURN_FORMATTING);
                string formattedValue;

                switch (formatting.Text)
                {
                    case "TTT_List::NewEnumerator0":
                        // To Percentage
                        // NOTE: the input value might be less than 1.0, in which case it's a straight percentage: 0.375 = 37.5%
                        // or it might be 1.0 or greater, in which case it's a multiplier for additive percentage: 1.13 = 13%
                        formattedValue = ((value > 1 ? value - 1 : value) * 100).ToString("0.#");
                        break;
                    case "TTT_List::NewEnumerator1":
                        // Negative to Positive
                        formattedValue = (-value).ToString("0");
                        break;
                    case "TTT_List::NewEnumerator2":
                        // No Formatting
                        formattedValue = value.ToString("0.#");
                        break;
                    case "TTT_List::NewEnumerator4":
                        // Subtract From 1
                        formattedValue = ((1 - value) * 100).ToString("0.#");
                        break;
                    case "TTT_List::NewEnumerator5":
                        // Distance to Tiles
                        formattedValue = (value / 512).ToString("0.###");
                        break;
                    case "TTT_List::NewEnumerator6":
                        // To Percentage (No Subtract)
                        formattedValue = (value * 100).ToString("0.#");
                        break;
                    case "TTT_List::NewEnumerator7":
                        // To Percentage (Divisor)
                        formattedValue = (100 / value).ToString("0.#");
                        break;
                    case "TTT_List::NewEnumerator8":
                        // Override Percentage
                        formattedValue = "???";
                        Console.WriteLine("WARNING: I don't know how to Override Percentage");
                        break;
                    default:
                        formattedValue = "???";
                        Console.WriteLine("WARNING: Unknown formatting style {0}", formatting.Text);
                        break;
                }

                tokens[tokenName] = formattedValue;
            }
        }

        private static string FormatMarkup(string markup, Dictionary<string, string> tokens)
        {
            var tokenRegex = new Regex(@"\[(Ability\.Line\d+)\]", RegexOptions.IgnoreCase);

            markup = tokenRegex.Replace(markup, match => tokens.GetValueOrDefault(match.Groups[1].Value, match.Value));

            var tagRegex = new Regex(@"<(?:\w+)>([^<]*)</>");

            return tagRegex.Replace(markup, match => match.Groups[1].Value);
        }
    }
}
