﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Oceyra.Dbml.Generator.Samples;

#nullable disable

namespace Oceyra.Dbml.Generator.Samples.Migrations
{
    [DbContext(typeof(DbDiagramDbContext))]
    partial class DbDiagramDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.7");

            modelBuilder.Entity("Oceyra.Dbml.Generator.Samples.Follow", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("id");

                    b.Property<DateTime?>("CreatedAt")
                        .HasColumnType("TEXT")
                        .HasColumnName("created_at");

                    b.Property<int?>("FollowedUserId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("followed_user_id");

                    b.Property<int?>("FollowingUserId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("following_user_id");

                    b.HasKey("Id");

                    b.HasIndex("FollowedUserId");

                    b.HasIndex("FollowingUserId");

                    b.ToTable("follows", (string)null);
                });

            modelBuilder.Entity("Oceyra.Dbml.Generator.Samples.Post", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("id");

                    b.Property<string>("Body")
                        .HasColumnType("TEXT")
                        .HasColumnName("body");

                    b.Property<DateTime?>("CreatedAt")
                        .HasColumnType("TEXT")
                        .HasColumnName("created_at");

                    b.Property<string>("Status")
                        .HasColumnType("TEXT")
                        .HasColumnName("status");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT")
                        .HasColumnName("title");

                    b.Property<int>("UserId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("posts", (string)null);
                });

            modelBuilder.Entity("Oceyra.Dbml.Generator.Samples.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("id");

                    b.Property<DateTime?>("CreatedAt")
                        .HasColumnType("TEXT")
                        .HasColumnName("created_at");

                    b.Property<string>("Role")
                        .HasColumnType("TEXT")
                        .HasColumnName("role");

                    b.Property<string>("Username")
                        .HasColumnType("TEXT")
                        .HasColumnName("username");

                    b.HasKey("Id");

                    b.ToTable("users", (string)null);
                });

            modelBuilder.Entity("Oceyra.Dbml.Generator.Samples.Follow", b =>
                {
                    b.HasOne("Oceyra.Dbml.Generator.Samples.User", "FollowedUserNavigation")
                        .WithMany("FollowedUsers")
                        .HasForeignKey("FollowedUserId");

                    b.HasOne("Oceyra.Dbml.Generator.Samples.User", "FollowingUserNavigation")
                        .WithMany("FollowingUsers")
                        .HasForeignKey("FollowingUserId");

                    b.Navigation("FollowedUserNavigation");

                    b.Navigation("FollowingUserNavigation");
                });

            modelBuilder.Entity("Oceyra.Dbml.Generator.Samples.Post", b =>
                {
                    b.HasOne("Oceyra.Dbml.Generator.Samples.User", "UserNavigation")
                        .WithMany("Users")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("UserNavigation");
                });

            modelBuilder.Entity("Oceyra.Dbml.Generator.Samples.User", b =>
                {
                    b.Navigation("FollowedUsers");

                    b.Navigation("FollowingUsers");

                    b.Navigation("Users");
                });
#pragma warning restore 612, 618
        }
    }
}
