﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Snap.Hutao.Context.Database;

#nullable disable

namespace Snap.Hutao.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20221108081525_DailyNoteEntry")]
    partial class DailyNoteEntry
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.10");

            modelBuilder.Entity("Snap.Hutao.Model.Entity.Achievement", b =>
                {
                    b.Property<Guid>("InnerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ArchiveId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Current")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset>("Time")
                        .HasColumnType("TEXT");

                    b.HasKey("InnerId");

                    b.HasIndex("ArchiveId");

                    b.ToTable("achievements");
                });

            modelBuilder.Entity("Snap.Hutao.Model.Entity.AchievementArchive", b =>
                {
                    b.Property<Guid>("InnerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsSelected")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("InnerId");

                    b.ToTable("achievement_archives");
                });

            modelBuilder.Entity("Snap.Hutao.Model.Entity.AvatarInfo", b =>
                {
                    b.Property<Guid>("InnerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Info")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Uid")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("InnerId");

                    b.ToTable("avatar_infos");
                });

            modelBuilder.Entity("Snap.Hutao.Model.Entity.DailyNoteEntry", b =>
                {
                    b.Property<Guid>("InnerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("DailyNote")
                        .HasColumnType("TEXT");

                    b.Property<bool>("DailyTaskNotify")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("DailyTaskNotifySuppressed")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ExpeditionNotify")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ExpeditionNotifySuppressed")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("HomeCoinNotifySuppressed")
                        .HasColumnType("INTEGER");

                    b.Property<int>("HomeCoinNotifyThreshold")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ResinNotifySuppressed")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ResinNotifyThreshold")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ShowInHomeWidget")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("TransformerNotify")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("TransformerNotifySuppressed")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Uid")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT");

                    b.HasKey("InnerId");

                    b.HasIndex("UserId");

                    b.ToTable("daily_notes");
                });

            modelBuilder.Entity("Snap.Hutao.Model.Entity.GachaArchive", b =>
                {
                    b.Property<Guid>("InnerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsSelected")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Uid")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("InnerId");

                    b.ToTable("gacha_archives");
                });

            modelBuilder.Entity("Snap.Hutao.Model.Entity.GachaItem", b =>
                {
                    b.Property<Guid>("InnerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ArchiveId")
                        .HasColumnType("TEXT");

                    b.Property<int>("GachaType")
                        .HasColumnType("INTEGER");

                    b.Property<long>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ItemId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("QueryType")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset>("Time")
                        .HasColumnType("TEXT");

                    b.HasKey("InnerId");

                    b.HasIndex("ArchiveId");

                    b.ToTable("gacha_items");
                });

            modelBuilder.Entity("Snap.Hutao.Model.Entity.GameAccount", b =>
                {
                    b.Property<Guid>("InnerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("AttachUid")
                        .HasColumnType("TEXT");

                    b.Property<string>("MihoyoSDK")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("InnerId");

                    b.ToTable("game_accounts");
                });

            modelBuilder.Entity("Snap.Hutao.Model.Entity.SettingEntry", b =>
                {
                    b.Property<string>("Key")
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT");

                    b.HasKey("Key");

                    b.ToTable("settings");
                });

            modelBuilder.Entity("Snap.Hutao.Model.Entity.User", b =>
                {
                    b.Property<Guid>("InnerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Cookie")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsSelected")
                        .HasColumnType("INTEGER");

                    b.HasKey("InnerId");

                    b.ToTable("users");
                });

            modelBuilder.Entity("Snap.Hutao.Model.Entity.Achievement", b =>
                {
                    b.HasOne("Snap.Hutao.Model.Entity.AchievementArchive", "Archive")
                        .WithMany()
                        .HasForeignKey("ArchiveId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Archive");
                });

            modelBuilder.Entity("Snap.Hutao.Model.Entity.DailyNoteEntry", b =>
                {
                    b.HasOne("Snap.Hutao.Model.Entity.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Snap.Hutao.Model.Entity.GachaItem", b =>
                {
                    b.HasOne("Snap.Hutao.Model.Entity.GachaArchive", "Archive")
                        .WithMany()
                        .HasForeignKey("ArchiveId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Archive");
                });
#pragma warning restore 612, 618
        }
    }
}
