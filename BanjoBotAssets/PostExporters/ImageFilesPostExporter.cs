﻿using BanjoBotAssets.Artifacts.Models;
using BanjoBotAssets.Config;
using BanjoBotAssets.PostExporters.Helpers;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures;
using Microsoft.Extensions.Options;

namespace BanjoBotAssets.PostExporters
{
    internal sealed class ImageFilesPostExporter : IPostExporter
    {
        private readonly AbstractVfsFileProvider provider;
        private readonly IOptions<ImageExportOptions> options;
        private readonly ILogger<ImageFilesPostExporter> logger;

        private int assetsLoaded;

        public int AssetsLoaded => assetsLoaded;

        public ImageFilesPostExporter(AbstractVfsFileProvider provider, IOptions<ImageExportOptions> options, ILogger<ImageFilesPostExporter> logger)
        {
            this.provider = provider;
            this.options = options;
            this.logger = logger;
        }

        public void CountAssetLoaded()
        {
            Interlocked.Increment(ref assetsLoaded);
        }

        public async Task ProcessExportsAsync(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes, CancellationToken cancellationToken = default)
        {
            var outputFilenameTransformer = new ConcurrentUniqueTransformer<string, string>(
                Path.GetFileName,
                IncrementFilenameSuffix,
                StringComparer.OrdinalIgnoreCase,
                StringComparer.OrdinalIgnoreCase);
            int filesWritten = 0, pathsUpdated = 0;

            Directory.CreateDirectory(options.Value.OutputDirectory);

            foreach (var (imageType, wantExport) in options.Value.Type)
            {
                if (wantExport == WantImageExport.Yes)
                {
                    logger.LogInformation(Resources.Status_ExportingImagesType, imageType);

                    foreach (var i in exportedAssets.NamedItems.Values)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (i.ImagePaths?.TryGetValue(imageType, out var imagePath) == true)
                        {
                            // map the texture asset path to an output file path
                            bool novel = outputFilenameTransformer.TryTransformIfNovel(imagePath, out var transformedFilename);

                            // update the path in the NamedItem to point to the output file
                            var exportedPath = Path.Combine(options.Value.OutputDirectory, Path.ChangeExtension(transformedFilename, ".png"));
                            i.ImagePaths[imageType] = exportedPath;
                            pathsUpdated++;

                            if (!novel)
                            {
                                // the texture was already saved the first time
                                continue;
                            }

                            var asset = await provider.LoadObjectAsync<UTexture2D>(imagePath);
                            using var bitmap = asset.Decode();
                            if (bitmap == null)
                            {
                                logger.LogError(Resources.Error_CannotDecodeTexture, imagePath);
                                continue;
                            }

                            using var stream = new FileStream(exportedPath, FileMode.Create, FileAccess.Write);
                            if (!bitmap.Encode(stream, SkiaSharp.SKEncodedImageFormat.Png, 100))
                            {
                                logger.LogError(Resources.Error_CannotEncodeTexture, imagePath);
                            }
                            filesWritten++;
                        }
                    }
                }
            }

            logger.LogInformation(Resources.Status_WroteImageFilesUpdatedPaths, filesWritten, pathsUpdated);
        }

        private static readonly Regex NumberSuffixedFilenameRegex = new(@"^(.*)_(\d+)(\..+)?$");

        private static string IncrementFilenameSuffix(string filename)
        {
            if (NumberSuffixedFilenameRegex.Match(filename) is { Success: true, Groups: var g })
            {
                // Foo_1.png -> Foo_2.png
                var num = int.Parse(g[2].Value, CultureInfo.InvariantCulture) + 1;
                return $"{g[1].Value}_{num}{g[3].Value}";
            }
            else
            {
                // Foo.png -> Foo_1.png
                return $"{Path.GetFileNameWithoutExtension(filename)}_1{Path.GetExtension(filename)}";
            }
        }
    }
}