﻿// <auto-generated />
using System;
using Dex.Cap.Ef.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dex.Cap.Ef.Tests.Migrations
{
    [DbContext(typeof(TestDbContext))]
    [Migration("20230818120155_ChangeOutboxEnvelopeIndex")]
    partial class ChangeOutboxEnvelopeIndex
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.12")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Dex.Cap.Ef.Tests.Model.TestUser", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<int>("Years")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Dex.Cap.OnceExecutor.Models.LastTransaction", b =>
                {
                    b.Property<string>("IdempotentKey")
                        .HasColumnType("text");

                    b.Property<DateTime>("Created")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("IdempotentKey");

                    b.HasIndex("Created");

                    b.ToTable("last_transaction", "cap");
                });

            modelBuilder.Entity("Dex.Cap.Outbox.Models.OutboxEnvelope", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("ActivityId")
                        .HasColumnType("text");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("CorrelationId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Error")
                        .HasColumnType("text");

                    b.Property<string>("ErrorMessage")
                        .HasColumnType("text");

                    b.Property<DateTime?>("LockExpirationTimeUtc")
                        .HasColumnType("timestamp without time zone")
                        .HasComment("Preventive timeout (maximum lifetime of actuality 'LockId')");

                    b.Property<Guid?>("LockId")
                        .IsConcurrencyToken()
                        .HasColumnType("uuid")
                        .HasComment("Idempotency key (unique key of the thread that captured the lock)");

                    b.Property<TimeSpan>("LockTimeout")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("interval")
                        .HasDefaultValue(new TimeSpan(0, 0, 0, 30, 0))
                        .HasComment("Maximum allowable blocking time");

                    b.Property<string>("MessageType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Retries")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("ScheduledStartIndexing")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime?>("StartAtUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("Updated")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.HasIndex("CorrelationId");

                    b.HasIndex("CreatedUtc");

                    b.HasIndex("ScheduledStartIndexing", "Status", "Retries")
                        .HasFilter("\"Status\" in (0,1)");

                    b.ToTable("outbox", "cap");
                });
#pragma warning restore 612, 618
        }
    }
}