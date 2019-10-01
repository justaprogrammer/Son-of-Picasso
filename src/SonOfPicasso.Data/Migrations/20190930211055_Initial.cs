using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SonOfPicasso.Data.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Albums",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true),
                    Date = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Albums", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExifData",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Make = table.Column<string>(nullable: true),
                    Model = table.Column<string>(nullable: true),
                    Software = table.Column<string>(nullable: true),
                    UserComment = table.Column<string>(nullable: true),
                    FileSource = table.Column<string>(nullable: true),
                    ImageDescription = table.Column<string>(nullable: true),
                    DocumentName = table.Column<string>(nullable: true),
                    Orientation = table.Column<string>(nullable: true),
                    XResolution = table.Column<string>(nullable: true),
                    YResolution = table.Column<string>(nullable: true),
                    ThumbnailXResolution = table.Column<string>(nullable: true),
                    ThumbnailYResolution = table.Column<string>(nullable: true),
                    ExposureTime = table.Column<string>(nullable: true),
                    CompressedBitsPerPixel = table.Column<string>(nullable: true),
                    FocalLength = table.Column<string>(nullable: true),
                    ThumbnailImageDescription = table.Column<string>(nullable: true),
                    ThumbnailMake = table.Column<string>(nullable: true),
                    ThumbnailModel = table.Column<string>(nullable: true),
                    ThumbnailSoftware = table.Column<string>(nullable: true),
                    InteroperabilityIndex = table.Column<string>(nullable: true),
                    PixelXDimension = table.Column<uint>(nullable: false),
                    PixelYDimension = table.Column<uint>(nullable: false),
                    InteroperabilityIFDPointer = table.Column<uint>(nullable: false),
                    ThumbnailJPEGInterchangeFormat = table.Column<uint>(nullable: false),
                    ThumbnailJPEGInterchangeFormatLength = table.Column<uint>(nullable: false),
                    DateTime = table.Column<DateTime>(nullable: false),
                    EXIFIFDPointer = table.Column<uint>(nullable: false),
                    DateTimeDigitized = table.Column<DateTime>(nullable: false),
                    DateTimeOriginal = table.Column<DateTime>(nullable: false),
                    FNumber = table.Column<string>(nullable: true),
                    MaxApertureValue = table.Column<string>(nullable: true),
                    DigitalZoomRatio = table.Column<string>(nullable: true),
                    ThumbnailDateTime = table.Column<DateTime>(nullable: false),
                    ISOSpeedRatings = table.Column<ushort>(nullable: false),
                    FocalLengthIn35mmFilm = table.Column<ushort>(nullable: false),
                    ColorSpace = table.Column<string>(nullable: true),
                    ExposureMode = table.Column<string>(nullable: true),
                    MeteringMode = table.Column<string>(nullable: true),
                    LightSource = table.Column<string>(nullable: true),
                    SceneCaptureType = table.Column<string>(nullable: true),
                    ResolutionUnit = table.Column<string>(nullable: true),
                    YCbCrPositioning = table.Column<string>(nullable: true),
                    ExposureProgram = table.Column<string>(nullable: true),
                    Flash = table.Column<string>(nullable: true),
                    SceneType = table.Column<string>(nullable: true),
                    CustomRendered = table.Column<string>(nullable: true),
                    WhiteBalance = table.Column<string>(nullable: true),
                    Contrast = table.Column<string>(nullable: true),
                    Saturation = table.Column<string>(nullable: true),
                    Sharpness = table.Column<string>(nullable: true),
                    ThumbnailCompression = table.Column<string>(nullable: true),
                    ThumbnailOrientation = table.Column<string>(nullable: true),
                    ThumbnailResolutionUnit = table.Column<string>(nullable: true),
                    ThumbnailYCbCrPositioning = table.Column<string>(nullable: true),
                    ExifVersion = table.Column<string>(nullable: true),
                    FlashpixVersion = table.Column<string>(nullable: true),
                    InteroperabilityVersion = table.Column<string>(nullable: true),
                    BrightnessValue = table.Column<string>(nullable: true),
                    ExposureBiasValue = table.Column<string>(nullable: true),
                    LensSpecification = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExifData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Folders",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(nullable: true),
                    Date = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Folders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FolderId = table.Column<int>(nullable: false),
                    Path = table.Column<string>(nullable: true),
                    ExifDataId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Images_ExifData_ExifDataId",
                        column: x => x.ExifDataId,
                        principalTable: "ExifData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Images_Folders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "Folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlbumImages",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AlbumId = table.Column<int>(nullable: false),
                    ImageId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlbumImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlbumImages_Albums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlbumImages_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlbumImages_AlbumId",
                table: "AlbumImages",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_AlbumImages_ImageId",
                table: "AlbumImages",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_ExifDataId",
                table: "Images",
                column: "ExifDataId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_FolderId",
                table: "Images",
                column: "FolderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlbumImages");

            migrationBuilder.DropTable(
                name: "Albums");

            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.DropTable(
                name: "ExifData");

            migrationBuilder.DropTable(
                name: "Folders");
        }
    }
}
