﻿// <auto-generated />
using System;
using BookSwap.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BookSwap.Migrations
{
    [DbContext(typeof(BookSwapDbContext))]
    [Migration("20250416150539_UBP12")]
    partial class UBP12
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.15")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("BookSwap.Models.Admin", b =>
                {
                    b.Property<string>("AdminName")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("AdminName");

                    b.ToTable("Admins");
                });

            modelBuilder.Entity("BookSwap.Models.BookOwner", b =>
                {
                    b.Property<int>("BookOwnerID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("BookOwnerID"));

                    b.Property<string>("BookOwnerName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PhoneNumber")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("RequestStatus")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("ssn")
                        .HasColumnType("int");

                    b.HasKey("BookOwnerID");

                    b.ToTable("BookOwners");
                });

            modelBuilder.Entity("BookSwap.Models.BookPost", b =>
                {
                    b.Property<int>("BookPostID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("BookPostID"));

                    b.Property<int>("BookOwnerID")
                        .HasColumnType("int");

                    b.Property<byte[]>("CoverPhoto")
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("EndDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Genre")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ISBN")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsAvailable")
                        .HasColumnType("bit");

                    b.Property<string>("Language")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PostStatus")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Price")
                        .HasColumnType("int");

                    b.Property<DateTime>("PublicationDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("BookPostID");

                    b.HasIndex("BookOwnerID");

                    b.ToTable("BookPosts");
                });

            modelBuilder.Entity("BookSwap.Models.BookRequest", b =>
                {
                    b.Property<int>("RequsetID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("RequsetID"));

                    b.Property<int>("BookPostID")
                        .HasColumnType("int");

                    b.Property<int>("ReaderID")
                        .HasColumnType("int");

                    b.Property<string>("RequsetStatus")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("RequsetID");

                    b.HasIndex("BookPostID");

                    b.HasIndex("ReaderID");

                    b.ToTable("BookRequests");
                });

            modelBuilder.Entity("BookSwap.Models.Comment", b =>
                {
                    b.Property<int>("CommentID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("CommentID"));

                    b.Property<int>("BookPostID")
                        .HasColumnType("int");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("ReaderID")
                        .HasColumnType("int");

                    b.HasKey("CommentID");

                    b.HasIndex("BookPostID");

                    b.HasIndex("ReaderID");

                    b.ToTable("Comments");
                });

            modelBuilder.Entity("BookSwap.Models.Like", b =>
                {
                    b.Property<int>("LikeID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("LikeID"));

                    b.Property<int>("BookPostID")
                        .HasColumnType("int");

                    b.Property<bool>("IsLike")
                        .HasColumnType("bit");

                    b.Property<int>("ReaderID")
                        .HasColumnType("int");

                    b.HasKey("LikeID");

                    b.HasIndex("BookPostID");

                    b.HasIndex("ReaderID");

                    b.ToTable("Likes");
                });

            modelBuilder.Entity("BookSwap.Models.Reader", b =>
                {
                    b.Property<int>("ReaderID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ReaderID"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PhoneNumber")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ReaderName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ReaderID");

                    b.ToTable("Readers");
                });

            modelBuilder.Entity("BookSwap.Models.Reply", b =>
                {
                    b.Property<int>("ReplyID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ReplyID"));

                    b.Property<int>("CommentID")
                        .HasColumnType("int");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("ReaderID")
                        .HasColumnType("int");

                    b.HasKey("ReplyID");

                    b.HasIndex("CommentID");

                    b.HasIndex("ReaderID");

                    b.ToTable("Replies");
                });

            modelBuilder.Entity("BookSwap.Models.BookPost", b =>
                {
                    b.HasOne("BookSwap.Models.BookOwner", "BookOwner")
                        .WithMany("BookPosts")
                        .HasForeignKey("BookOwnerID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("BookOwner");
                });

            modelBuilder.Entity("BookSwap.Models.BookRequest", b =>
                {
                    b.HasOne("BookSwap.Models.BookPost", "BookPost")
                        .WithMany("BookRequests")
                        .HasForeignKey("BookPostID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BookSwap.Models.Reader", "Reader")
                        .WithMany("BookRequests")
                        .HasForeignKey("ReaderID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("BookPost");

                    b.Navigation("Reader");
                });

            modelBuilder.Entity("BookSwap.Models.Comment", b =>
                {
                    b.HasOne("BookSwap.Models.BookPost", "BookPost")
                        .WithMany("Comments")
                        .HasForeignKey("BookPostID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BookSwap.Models.Reader", "Reader")
                        .WithMany("Comments")
                        .HasForeignKey("ReaderID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("BookPost");

                    b.Navigation("Reader");
                });

            modelBuilder.Entity("BookSwap.Models.Like", b =>
                {
                    b.HasOne("BookSwap.Models.BookPost", "BookPost")
                        .WithMany("Likes")
                        .HasForeignKey("BookPostID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BookSwap.Models.Reader", "Reader")
                        .WithMany("Likes")
                        .HasForeignKey("ReaderID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("BookPost");

                    b.Navigation("Reader");
                });

            modelBuilder.Entity("BookSwap.Models.Reply", b =>
                {
                    b.HasOne("BookSwap.Models.Comment", "Comment")
                        .WithMany()
                        .HasForeignKey("CommentID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BookSwap.Models.Reader", "Reader")
                        .WithMany()
                        .HasForeignKey("ReaderID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Comment");

                    b.Navigation("Reader");
                });

            modelBuilder.Entity("BookSwap.Models.BookOwner", b =>
                {
                    b.Navigation("BookPosts");
                });

            modelBuilder.Entity("BookSwap.Models.BookPost", b =>
                {
                    b.Navigation("BookRequests");

                    b.Navigation("Comments");

                    b.Navigation("Likes");
                });

            modelBuilder.Entity("BookSwap.Models.Reader", b =>
                {
                    b.Navigation("BookRequests");

                    b.Navigation("Comments");

                    b.Navigation("Likes");
                });
#pragma warning restore 612, 618
        }
    }
}
