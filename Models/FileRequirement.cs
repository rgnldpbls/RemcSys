﻿using System.ComponentModel.DataAnnotations;

namespace RemcSys.Models
{
    public class FileRequirement
    {
        [Key]
        public int fr_Id { get; set; }
        public string? file_Name { get; set; }
        public string? file_Type { get; set; }
        public byte[] data { get; set; }
        public string? file_Status { get; set; }
        public DateTime file_Uploaded { get; set; }
        public string? fra_Id { get; set; }
        public FundedResearchApplication? FundedResearchApplication { get; set; }
    }
}