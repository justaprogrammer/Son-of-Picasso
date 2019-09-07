using System;
using System.Drawing;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Bogus;
using ExifLibrary;
using Serilog;
using Skybrud.Colors;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Tools.Extensions;

namespace SonOfPicasso.Tools.Services
{
    public class ToolsService
    {
        protected internal static Faker Faker = new Faker();

        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IDataCache _dataCache;

        public ToolsService(ILogger logger, IFileSystem fileSystem, IDataCache dataCache)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _dataCache = dataCache;
        }

        public IObservable<string> GenerateImages(int count, string fileRoot)
        {
            _logger.Debug("GenerateImages {count} {fileRoot}", count, fileRoot);

            return Observable.Generate(
                initialState: 0,
                condition: value => value < count,
                iterate: value => value + 1,
                resultSelector: value =>
                {
                    var time = Faker.Date.Between(DateTime.Now, DateTime.Now.AddDays(-30));
                    var directory = _fileSystem.Path.Combine(fileRoot, time.ToString("yyyy-MM-dd"));
                    var directoryInfoBase = _fileSystem.Directory.CreateDirectory(directory);

                    var fileName = $"{time.ToString("s").Replace("-", "_").Replace(":", "_")}.jpg";
                    var filePath = _fileSystem.Path.Combine(directoryInfoBase.ToString(), fileName);

                    GenerateImage(filePath, 1024, 768, time);

                    return filePath;
                });
        }

        private void GenerateImage(string path, int width, int height, DateTime time)
        {
            _logger.Debug("GenerateImage {path} {width} {height}", path, width, height);

            var cellHeight = height / 3;
            var cellWidth = width / 3;

            var red = Faker.Random.Int(0, 255);
            var green = Faker.Random.Int(0, 255);
            var blue = Faker.Random.Int(0, 255);

            var colors = SimilarColors(red, green, blue);

            using (var bitmap = new Bitmap(width, height))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    for (var x = 0; x < 3; x++)
                    {
                        for (var y = 0; y < 3; y++)
                        {
                            var xPos = x * cellWidth;
                            var yPos = y * cellHeight;

                            using (var brush = new SolidBrush(GetColor(colors, x, y)))
                            {
                                var rectangle = new Rectangle(xPos, yPos, cellWidth, cellHeight);
                                graphics.FillRectangle(brush, rectangle);
                            }
                        }
                    }
                }

                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
            }

            var imageFile = ImageFile.FromFile(path);
            imageFile.Properties.Add(ExifTag.DateTime, time);

            var longitude = Faker.Address.Longitude();
            imageFile.Properties.Add(ExifTag.GPSLongitude, Math.Abs(longitude));
            imageFile.Properties.Add(ExifTag.GPSLongitudeRef, longitude > 0 ? "E" : "W");

            var latitude = Faker.Address.Latitude();
            imageFile.Properties.Add(ExifTag.GPSLatitude, Math.Abs(latitude));
            imageFile.Properties.Add(ExifTag.GPSLatitudeRef, latitude > 0 ? "N" : "S");

            imageFile.Properties.Add(ExifTag.GPSAltitude, Faker.Random.Double());

            imageFile.Save(path);
        }

        private static Color GetColor(Color[] colors, int x, int y)
        {
            switch (x)
            {
                case 0:
                    switch (y)
                    {
                        case 0:
                            return colors[0];
                        case 1:
                            return colors[1];
                        case 2:
                            return colors[0];
                    }
                    break;
                case 1:
                    switch (y)
                    {
                        case 0:
                            return colors[1];
                        case 1:
                            return colors[2];
                        case 2:
                            return colors[1];
                    }
                    break;
                case 2:
                    switch (y)
                    {
                        case 0:
                            return colors[0];
                        case 1:
                            return colors[1];
                        case 2:
                            return colors[0];
                    }
                    break;
            }

            throw new NotSupportedException();
        }

        private static Color[] SimilarColors(int red, int green, int blue)
        {
            var color0 = new RgbColor(red, green, blue).ToHsl();

            var delta = Faker.Random.Int(25, 30) / 360.0;
            var color1 = new HslColor((color0.H + delta) % 1.0, color0.S, color0.L);
            var color2 = new HslColor((color0.H - delta) % 1.0, color0.S, color0.L);

            return new[] { color0.ToColor(), color1.ToColor(), color2.ToColor() };
        }

        public IObservable<Unit> ClearCache()
        {
            return _dataCache.Clear();
        }
    }
}