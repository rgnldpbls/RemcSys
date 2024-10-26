﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RemcSys.Data;

#nullable disable

namespace RemcSys.Migrations.RemcDB
{
    [DbContext(typeof(RemcDBContext))]
    [Migration("20241026094355_add other attri in settings model")]
    partial class addotherattriinsettingsmodel
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("RemcSys.Models.ActionLog", b =>
                {
                    b.Property<string>("LogId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Action")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FraId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ResearchType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime2");

                    b.Property<bool>("isChief")
                        .HasColumnType("bit");

                    b.Property<bool>("isEvaluator")
                        .HasColumnType("bit");

                    b.Property<bool>("isTeamLeader")
                        .HasColumnType("bit");

                    b.HasKey("LogId");

                    b.HasIndex("FraId");

                    b.ToTable("ActionLogs");
                });

            modelBuilder.Entity("RemcSys.Models.CalendarEvent", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime?>("End")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("Start")
                        .HasColumnType("datetime2");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Visibility")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("CalendarEvents");
                });

            modelBuilder.Entity("RemcSys.Models.Evaluation", b =>
                {
                    b.Property<string>("evaluation_Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime>("assigned_Date")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("evaluation_Date")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("evaluation_Deadline")
                        .HasColumnType("datetime2");

                    b.Property<double?>("evaluation_Grade")
                        .HasColumnType("float");

                    b.Property<string>("evaluation_Status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("evaluator_Id")
                        .HasColumnType("int");

                    b.Property<string>("evaluator_Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("fra_Id")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("evaluation_Id");

                    b.HasIndex("evaluator_Id");

                    b.HasIndex("fra_Id");

                    b.ToTable("Evaluations");
                });

            modelBuilder.Entity("RemcSys.Models.Evaluator", b =>
                {
                    b.Property<int>("evaluator_Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("evaluator_Id"));

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("center")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("evaluator_Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("evaluator_Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("field_of_Interest")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("evaluator_Id");

                    b.ToTable("Evaluator");
                });

            modelBuilder.Entity("RemcSys.Models.FileRequirement", b =>
                {
                    b.Property<string>("fr_Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<byte[]>("data")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("document_Type")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("file_Feedback")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("file_Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("file_Status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("file_Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("file_Uploaded")
                        .HasColumnType("datetime2");

                    b.Property<string>("fra_Id")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("fr_Id");

                    b.HasIndex("fra_Id");

                    b.ToTable("FileRequirement");
                });

            modelBuilder.Entity("RemcSys.Models.FundedResearch", b =>
                {
                    b.Property<string>("fr_Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("branch")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("college")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("dts_No")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("end_Date")
                        .HasColumnType("datetime2");

                    b.Property<string>("field_of_Study")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("fr_Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("fra_Id")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<bool>("isArchive")
                        .HasColumnType("bit");

                    b.Property<bool>("isExtension1")
                        .HasColumnType("bit");

                    b.Property<bool>("isExtension2")
                        .HasColumnType("bit");

                    b.Property<int>("project_Duration")
                        .HasColumnType("int");

                    b.Property<string>("research_Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("start_Date")
                        .HasColumnType("datetime2");

                    b.Property<string>("status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("teamLead_Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("team_Leader")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("team_Members")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double?>("total_project_Cost")
                        .HasColumnType("float");

                    b.HasKey("fr_Id");

                    b.HasIndex("fra_Id")
                        .IsUnique();

                    b.ToTable("FundedResearches");
                });

            modelBuilder.Entity("RemcSys.Models.FundedResearchApplication", b =>
                {
                    b.Property<string>("fra_Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("applicant_Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("applicant_Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("application_Status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("branch")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("college")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("dts_No")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("field_of_Study")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("fra_Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("isArchive")
                        .HasColumnType("bit");

                    b.Property<int>("project_Duration")
                        .HasColumnType("int");

                    b.Property<string>("research_Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("submission_Date")
                        .HasColumnType("datetime2");

                    b.Property<string>("team_Members")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double?>("total_project_Cost")
                        .HasColumnType("float");

                    b.HasKey("fra_Id");

                    b.ToTable("FundedResearchApplication");
                });

            modelBuilder.Entity("RemcSys.Models.FundedResearchEthics", b =>
                {
                    b.Property<string>("fre_Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int?>("completionCertificate_Id")
                        .HasColumnType("int");

                    b.Property<int?>("ethicClearance_Id")
                        .HasColumnType("int");

                    b.Property<string>("fra_Id")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("urec_No")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("fre_Id");

                    b.HasIndex("fra_Id")
                        .IsUnique();

                    b.ToTable("FundedResearchEthics");
                });

            modelBuilder.Entity("RemcSys.Models.GAWADWinners", b =>
                {
                    b.Property<string>("gw_Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("file_Uploaded")
                        .HasColumnType("datetime2");

                    b.Property<byte[]>("gw_Data")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("gw_fileName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("gw_fileType")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("gw_Id");

                    b.ToTable("GAWADWinners");
                });

            modelBuilder.Entity("RemcSys.Models.GenerateGAWADNominees", b =>
                {
                    b.Property<string>("gn_Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("generateDate")
                        .HasColumnType("datetime2");

                    b.Property<byte[]>("gn_Data")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("gn_fileName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("gn_fileType")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("gn_type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("isArchived")
                        .HasColumnType("bit");

                    b.HasKey("gn_Id");

                    b.ToTable("GenerateGAWADNominees");
                });

            modelBuilder.Entity("RemcSys.Models.GenerateReport", b =>
                {
                    b.Property<string>("gr_Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("generateDate")
                        .HasColumnType("datetime2");

                    b.Property<byte[]>("gr_Data")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<DateTime>("gr_endDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("gr_fileName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("gr_fileType")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("gr_startDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("gr_typeofReport")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("isArchived")
                        .HasColumnType("bit");

                    b.HasKey("gr_Id");

                    b.ToTable("GenerateReports");
                });

            modelBuilder.Entity("RemcSys.Models.GeneratedForm", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<byte[]>("FileContent")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("GeneratedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("fra_Id")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("fra_Id");

                    b.ToTable("GeneratedForms");
                });

            modelBuilder.Entity("RemcSys.Models.ProgressReport", b =>
                {
                    b.Property<string>("pr_Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<byte[]>("data")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("document_Type")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("file_Feedback")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("file_Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("file_Status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("file_Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("file_Uploaded")
                        .HasColumnType("datetime2");

                    b.Property<string>("fr_Id")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("pr_Id");

                    b.HasIndex("fr_Id");

                    b.ToTable("ProgressReports");
                });

            modelBuilder.Entity("RemcSys.Models.Settings", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<bool>("isEFRApplication")
                        .HasColumnType("bit");

                    b.Property<bool>("isMaintenance")
                        .HasColumnType("bit");

                    b.Property<bool>("isUFRApplication")
                        .HasColumnType("bit");

                    b.Property<bool>("isUFRLApplication")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("Settings");
                });

            modelBuilder.Entity("RemcSys.Models.ActionLog", b =>
                {
                    b.HasOne("RemcSys.Models.FundedResearchApplication", "fundedResearchApplication")
                        .WithMany("ActionLogs")
                        .HasForeignKey("FraId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("fundedResearchApplication");
                });

            modelBuilder.Entity("RemcSys.Models.Evaluation", b =>
                {
                    b.HasOne("RemcSys.Models.Evaluator", "evaluator")
                        .WithMany("Evaluations")
                        .HasForeignKey("evaluator_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("RemcSys.Models.FundedResearchApplication", "fundedResearchApplication")
                        .WithMany("Evaluations")
                        .HasForeignKey("fra_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("evaluator");

                    b.Navigation("fundedResearchApplication");
                });

            modelBuilder.Entity("RemcSys.Models.FileRequirement", b =>
                {
                    b.HasOne("RemcSys.Models.FundedResearchApplication", "fundedResearchApplication")
                        .WithMany("FileRequirements")
                        .HasForeignKey("fra_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("fundedResearchApplication");
                });

            modelBuilder.Entity("RemcSys.Models.FundedResearch", b =>
                {
                    b.HasOne("RemcSys.Models.FundedResearchApplication", "FundedResearchApplication")
                        .WithOne("FundedResearch")
                        .HasForeignKey("RemcSys.Models.FundedResearch", "fra_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("FundedResearchApplication");
                });

            modelBuilder.Entity("RemcSys.Models.FundedResearchEthics", b =>
                {
                    b.HasOne("RemcSys.Models.FundedResearchApplication", "FundedResearchApplication")
                        .WithOne("FundedResearchEthics")
                        .HasForeignKey("RemcSys.Models.FundedResearchEthics", "fra_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("FundedResearchApplication");
                });

            modelBuilder.Entity("RemcSys.Models.GeneratedForm", b =>
                {
                    b.HasOne("RemcSys.Models.FundedResearchApplication", "FundedResearchApplication")
                        .WithMany("GeneratedForms")
                        .HasForeignKey("fra_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("FundedResearchApplication");
                });

            modelBuilder.Entity("RemcSys.Models.ProgressReport", b =>
                {
                    b.HasOne("RemcSys.Models.FundedResearch", "FundedResearch")
                        .WithMany("ProgressReports")
                        .HasForeignKey("fr_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("FundedResearch");
                });

            modelBuilder.Entity("RemcSys.Models.Evaluator", b =>
                {
                    b.Navigation("Evaluations");
                });

            modelBuilder.Entity("RemcSys.Models.FundedResearch", b =>
                {
                    b.Navigation("ProgressReports");
                });

            modelBuilder.Entity("RemcSys.Models.FundedResearchApplication", b =>
                {
                    b.Navigation("ActionLogs");

                    b.Navigation("Evaluations");

                    b.Navigation("FileRequirements");

                    b.Navigation("FundedResearch")
                        .IsRequired();

                    b.Navigation("FundedResearchEthics")
                        .IsRequired();

                    b.Navigation("GeneratedForms");
                });
#pragma warning restore 612, 618
        }
    }
}