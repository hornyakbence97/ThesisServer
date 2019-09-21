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
    [Migration("20190921113600_NetworkEntityChangeHashToByteArrayRemoveString")]
    partial class NetworkEntityChangeHashToByteArrayRemoveString
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

                    b.HasKey("NetworkId");

                    b.ToTable("Network");
                });

            modelBuilder.Entity("ThesisServer.Data.Repository.Db.UserEntity", b =>
                {
                    b.Property<Guid>("Token1")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("FriendlyName");

                    b.Property<Guid>("Token2");

                    b.HasKey("Token1");

                    b.HasIndex("Token2");

                    b.ToTable("User");
                });
#pragma warning restore 612, 618
        }
    }
}
