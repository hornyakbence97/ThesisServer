﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ThesisServer.Data.Repository.Db;

namespace ThesisServer.Migrations
{
    [DbContext(typeof(VirtualNetworkDbContext))]
    [Migration("20191002125235_filePeaceEntityAddOrderNumber")]
    partial class filePeaceEntityAddOrderNumber
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.4-rtm-31024")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("ThesisServer.Data.Repository.Db.NetworkEntity", b =>
                {
                    b.Property<Guid>("NetworkId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("NetworkName");

                    b.Property<byte[]>("NetworkPasswordHash");

                    b.HasKey("NetworkId");

                    b.ToTable("Network");
                });

            modelBuilder.Entity("ThesisServer.Data.Repository.Db.UserEntity", b =>
                {
                    b.Property<Guid>("Token1")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("FriendlyName");

                    b.Property<int>("MaxSpace")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValue(300);

                    b.Property<Guid?>("NetworkId");

                    b.Property<Guid>("Token2");

                    b.HasKey("Token1");

                    b.HasIndex("NetworkId");

                    b.HasIndex("Token2");

                    b.ToTable("User");
                });

            modelBuilder.Entity("ThesisServer.Data.Repository.Db.VirtualFileEntity", b =>
                {
                    b.Property<Guid>("FileId")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Created");

                    b.Property<string>("FileName");

                    b.Property<long>("FileSize");

                    b.Property<DateTime>("LastModified");

                    b.Property<string>("MimeType");

                    b.Property<Guid>("ModifiedBy");

                    b.Property<Guid>("NetworkId");

                    b.Property<Guid>("UploadedBy");

                    b.HasKey("FileId");

                    b.HasIndex("NetworkId");

                    b.ToTable("VirtualFile");
                });

            modelBuilder.Entity("ThesisServer.Data.Repository.Db.VirtualFilePieceEntity", b =>
                {
                    b.Property<Guid>("FilePieceId")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("FileId");

                    b.Property<long>("FilePieceSize");

                    b.Property<int>("OrderNumber");

                    b.HasKey("FilePieceId");

                    b.HasIndex("FileId");

                    b.ToTable("VirtualFilePiece");
                });

            modelBuilder.Entity("ThesisServer.Data.Repository.Db.UserEntity", b =>
                {
                    b.HasOne("ThesisServer.Data.Repository.Db.NetworkEntity", "Network")
                        .WithMany("Users")
                        .HasForeignKey("NetworkId")
                        .HasConstraintName("ForeignKey_UserEntity_NetworkEntity");
                });

            modelBuilder.Entity("ThesisServer.Data.Repository.Db.VirtualFileEntity", b =>
                {
                    b.HasOne("ThesisServer.Data.Repository.Db.NetworkEntity", "Network")
                        .WithMany("Files")
                        .HasForeignKey("NetworkId")
                        .HasConstraintName("ForeignKey_VirtualFileEntity_NetworkEntity")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("ThesisServer.Data.Repository.Db.VirtualFilePieceEntity", b =>
                {
                    b.HasOne("ThesisServer.Data.Repository.Db.VirtualFileEntity", "File")
                        .WithMany("FilePieces")
                        .HasForeignKey("FileId")
                        .HasConstraintName("ForeignKey_VirtualFilePieceEntity_VirtualFileEntity")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
