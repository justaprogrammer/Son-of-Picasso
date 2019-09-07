﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SonOfPicasso.Data;

namespace SonOfPicasso.Data.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20190907024343_Initialize")]
    partial class Initialize
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity("SonOfPicasso.Data.Model.Album", b =>
                {
                    b.Property<int>("AlbumId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.HasKey("AlbumId");

                    b.ToTable("Albums");
                });

            modelBuilder.Entity("SonOfPicasso.Data.Model.AlbumImage", b =>
                {
                    b.Property<int>("AlbumImageId")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("AlbumId");

                    b.Property<int>("ImageId");

                    b.HasKey("AlbumImageId");

                    b.HasIndex("AlbumId");

                    b.HasIndex("ImageId");

                    b.ToTable("AlbumImages");
                });

            modelBuilder.Entity("SonOfPicasso.Data.Model.Directory", b =>
                {
                    b.Property<int>("DirectoryId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Path");

                    b.HasKey("DirectoryId");

                    b.ToTable("Directories");
                });

            modelBuilder.Entity("SonOfPicasso.Data.Model.Image", b =>
                {
                    b.Property<int>("ImageId")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("DirectoryId");

                    b.Property<string>("Path");

                    b.HasKey("ImageId");

                    b.HasIndex("DirectoryId");

                    b.ToTable("Images");
                });

            modelBuilder.Entity("SonOfPicasso.Data.Model.AlbumImage", b =>
                {
                    b.HasOne("SonOfPicasso.Data.Model.Album", "Album")
                        .WithMany()
                        .HasForeignKey("AlbumId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("SonOfPicasso.Data.Model.Image", "Image")
                        .WithMany()
                        .HasForeignKey("ImageId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SonOfPicasso.Data.Model.Image", b =>
                {
                    b.HasOne("SonOfPicasso.Data.Model.Directory", "Directory")
                        .WithMany("Images")
                        .HasForeignKey("DirectoryId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
