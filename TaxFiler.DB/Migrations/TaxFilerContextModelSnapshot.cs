﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TaxFiler.DB;

#nullable disable

namespace TaxFiler.DB.Migrations
{
    [DbContext(typeof(TaxFilerContext))]
    partial class TaxFilerContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.4");

            modelBuilder.Entity("TaxFiler.DB.Model.Account", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Accounts");
                });

            modelBuilder.Entity("TaxFiler.DB.Model.Booking", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AccountId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("DocumnentId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("TransactionId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("AccountId");

                    b.HasIndex("DocumnentId");

                    b.HasIndex("TransactionId");

                    b.ToTable("Bookings");
                });

            modelBuilder.Entity("TaxFiler.DB.Model.Document", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ExternalRef")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<DateOnly?>("InvoiceDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("InvoiceNumber")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<bool>("Orphaned")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Parsed")
                        .HasColumnType("INTEGER");

                    b.Property<decimal?>("Skonto")
                        .HasColumnType("TEXT");

                    b.Property<decimal?>("SubTotal")
                        .HasColumnType("TEXT");

                    b.Property<decimal?>("TaxAmount")
                        .HasColumnType("TEXT");

                    b.Property<decimal?>("TaxRate")
                        .HasColumnType("TEXT");

                    b.Property<decimal?>("Total")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Documents");
                });

            modelBuilder.Entity("TaxFiler.DB.Model.Transaction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AccountId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Counterparty")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<int?>("DocumentId")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("GrossAmount")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsIncomeTaxRelevant")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsOutgoing")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsSalesTaxRelevant")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("NetAmount")
                        .HasColumnType("TEXT");

                    b.Property<string>("SenderReceiver")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<decimal>("TaxAmount")
                        .HasColumnType("TEXT");

                    b.Property<int?>("TaxMonth")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("TaxRate")
                        .HasColumnType("TEXT");

                    b.Property<int?>("TaxYear")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("TransactionDateTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("TransactionNote")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<string>("TransactionReference")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("AccountId");

                    b.HasIndex("DocumentId");

                    b.ToTable("Transactions");
                });

            modelBuilder.Entity("TaxFiler.DB.Model.Booking", b =>
                {
                    b.HasOne("TaxFiler.DB.Model.Account", "Account")
                        .WithMany()
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TaxFiler.DB.Model.Document", "Document")
                        .WithMany()
                        .HasForeignKey("DocumnentId");

                    b.HasOne("TaxFiler.DB.Model.Transaction", "Transaction")
                        .WithMany()
                        .HasForeignKey("TransactionId");

                    b.Navigation("Account");

                    b.Navigation("Document");

                    b.Navigation("Transaction");
                });

            modelBuilder.Entity("TaxFiler.DB.Model.Transaction", b =>
                {
                    b.HasOne("TaxFiler.DB.Model.Account", "Account")
                        .WithMany()
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TaxFiler.DB.Model.Document", "Document")
                        .WithMany()
                        .HasForeignKey("DocumentId");

                    b.Navigation("Account");

                    b.Navigation("Document");
                });
#pragma warning restore 612, 618
        }
    }
}
