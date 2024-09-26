﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RemcSys.Data;

#nullable disable

namespace RemcSys.Migrations.RemcDB
{
    [DbContext(typeof(RemcDBContext))]
    partial class RemcDBContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("RemcSys.Models.FileRequirement", b =>
                {
                    b.Property<int>("fr_Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("fr_Id"));

                    b.Property<string>("FundedResearchApplicationfra_Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<byte[]>("data")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("file_Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("file_Status")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("file_Type")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("file_Uploaded")
                        .HasColumnType("datetime2");

                    b.Property<string>("fra_Id")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("fr_Id");

                    b.HasIndex("FundedResearchApplicationfra_Id");

                    b.ToTable("FileRequirement");
                });

            modelBuilder.Entity("RemcSys.Models.FundedResearchApplication", b =>
                {
                    b.Property<string>("fra_Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("applicant_Email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("applicant_Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("application_Status")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("branch")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("college")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("dts_No")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("field_of_Study")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("fra_Type")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("research_Title")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("submission_Date")
                        .HasColumnType("datetime2");

                    b.HasKey("fra_Id");

                    b.ToTable("FundedResearchApplication");
                });

            modelBuilder.Entity("RemcSys.Models.ResearchStaff", b =>
                {
                    b.Property<int>("rs_Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("rs_Id"));

                    b.Property<string>("Email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FundedResearchApplicationfra_Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("fra_Id")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("rs_Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("rs_Role")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("rs_Id");

                    b.HasIndex("FundedResearchApplicationfra_Id");

                    b.ToTable("ResearchStaff");
                });

            modelBuilder.Entity("RemcSys.Models.FileRequirement", b =>
                {
                    b.HasOne("RemcSys.Models.FundedResearchApplication", "FundedResearchApplication")
                        .WithMany("FileRequirements")
                        .HasForeignKey("FundedResearchApplicationfra_Id");

                    b.Navigation("FundedResearchApplication");
                });

            modelBuilder.Entity("RemcSys.Models.ResearchStaff", b =>
                {
                    b.HasOne("RemcSys.Models.FundedResearchApplication", "FundedResearchApplication")
                        .WithMany("ResearchStaffs")
                        .HasForeignKey("FundedResearchApplicationfra_Id");

                    b.Navigation("FundedResearchApplication");
                });

            modelBuilder.Entity("RemcSys.Models.FundedResearchApplication", b =>
                {
                    b.Navigation("FileRequirements");

                    b.Navigation("ResearchStaffs");
                });
#pragma warning restore 612, 618
        }
    }
}
