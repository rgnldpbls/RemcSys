﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using MimeKit.Utils;
using MimeKit;
using Newtonsoft.Json;
using RemcSys.Areas.Identity.Data;
using RemcSys.Data;
using RemcSys.Models;
using Xceed.Words.NET;
using MailKit.Net.Smtp;
using Org.BouncyCastle.Asn1.Ocsp;
using Microsoft.AspNetCore.Routing.Template;

namespace RemcSys.Controllers
{
    public class FundedResearchApplicationController : Controller
    {
        private readonly RemcDBContext _context;
        private readonly UserManager<SystemUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ActionLoggerService _actionLogger;

        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _smtpUser;
        private readonly string _smtpPass;

        public FundedResearchApplicationController(RemcDBContext context, UserManager<SystemUser> userManager,
            IWebHostEnvironment environment, ActionLoggerService actionLogger)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _actionLogger = actionLogger;
            _smtpUser = Environment.GetEnvironmentVariable("SMTP_USER") ?? "remc.rmo2@gmail.com";
            _smtpPass = Environment.GetEnvironmentVariable("SMTP_PASS") ?? "rhmh oyge mwky ozzx";
        }

        [Authorize(Roles = "Admin")]
        // GET: FundedResearchApplication
        public async Task<IActionResult> Index()
        {
            return View(await _context.FundedResearchApplication.ToListAsync());
        }

        [Authorize(Roles = "Admin")]
        // GET: FundedResearchApplication/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fundedResearchApplication = await _context.FundedResearchApplication
                .FirstOrDefaultAsync(m => m.fra_Id == id);
            if (fundedResearchApplication == null)
            {
                return NotFound();
            }

            return View(fundedResearchApplication);
        }

        [Authorize(Roles = "Admin")]
        // GET: FundedResearchApplication/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: FundedResearchApplication/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("fra_Id,fra_Type,research_Title,applicant_Name,applicant_Email,college,branch,field_of_Study,application_Status,submission_Date,dts_No,UserId")] FundedResearchApplication fundedResearchApplication)
        {
            if (ModelState.IsValid)
            {
                _context.Add(fundedResearchApplication);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(fundedResearchApplication);
        }

        [Authorize(Roles = "Admin")]
        // GET: FundedResearchApplication/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fundedResearchApplication = await _context.FundedResearchApplication.FindAsync(id);
            if (fundedResearchApplication == null)
            {
                return NotFound();
            }
            return View(fundedResearchApplication);
        }

        // POST: FundedResearchApplication/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("fra_Id,fra_Type,research_Title,applicant_Name,applicant_Email,college,branch,field_of_Study,application_Status,submission_Date,dts_No,UserId")] FundedResearchApplication fundedResearchApplication)
        {
            if (id != fundedResearchApplication.fra_Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(fundedResearchApplication);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FundedResearchApplicationExists(fundedResearchApplication.fra_Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(fundedResearchApplication);
        }

        [Authorize(Roles = "Admin")]
        // GET: FundedResearchApplication/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fundedResearchApplication = await _context.FundedResearchApplication
                .FirstOrDefaultAsync(m => m.fra_Id == id);
            if (fundedResearchApplication == null)
            {
                return NotFound();
            }

            return View(fundedResearchApplication);
        }

        // POST: FundedResearchApplication/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var fundedResearchApplication = await _context.FundedResearchApplication.FindAsync(id);
            if (fundedResearchApplication != null)
            {
                _context.FundedResearchApplication.Remove(fundedResearchApplication);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FundedResearchApplicationExists(string id)
        {
            return _context.FundedResearchApplication.Any(e => e.fra_Id == id);
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> TeamLeaderDashboard() // Dashboard of the Faculty or TeamLeader
        {
            if (_context.Settings.First().isMaintenance)
            {
                return RedirectToAction("UnderMaintenance", "Home");
            }

            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                return NotFound("No user found!");
            }

            var fra = await _context.FundedResearchApplication
                .Include(f => f.FundedResearch)
                .Where(f => f.UserId == user.Id)
                .OrderByDescending(f => f.submission_Date)
                .ToListAsync();

            return View(fra);
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> FormFill(string type) // General Information of Research Application
        {
            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                return NotFound("No user found!");
            }

            ViewBag.Name = user.Name;
            ViewBag.Type = type;
            return View();
        }

        [HttpPost]
        public JsonResult CheckResearchTitle(string projectTitle) // Check if the Proposed Research Title is already exists
        {
            var existingTitles = _context.FundedResearchApplication
                .Select(x => x.research_Title.ToLower())
                .ToList();

            bool isTitleExists = existingTitles.Contains(projectTitle.ToLower());

            return Json(new { exists = isTitleExists });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormFill(FormModel model, FundedResearchApplication fundedResearchApp) // Funded Research Application stored and Generated Documentary Requirements
        {
            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                return NotFound("No user found!");
            }

            string currentDate = DateTime.Now.ToString("yyyyMMdd");
            var latestFra = await _context.FundedResearchApplication
                .AsNoTracking()
                .Where(f => f.fra_Id.Contains(currentDate))
                .OrderByDescending(f => f.fra_Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (latestFra != null)
            {
                string[] parts = latestFra.fra_Id.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            fundedResearchApp.fra_Id = $"FRA-{currentDate}-{nextNumber:D3}";
            var existingEntry = _context.Entry(fundedResearchApp);
            if (existingEntry != null)
            {
                _context.Entry(fundedResearchApp).State = EntityState.Detached;
            }
            fundedResearchApp.fra_Type = model.ResearchType;
            fundedResearchApp.research_Title = model.ProjectTitle;
            fundedResearchApp.applicant_Name = model.ProjectLeader;
            fundedResearchApp.applicant_Email = user.Email;
            fundedResearchApp.team_Members = model.ProjectMembers
                .Split(new[] { Environment.NewLine, "," }, StringSplitOptions.RemoveEmptyEntries)
                .Select(member => member.Trim())
                .ToList();
            fundedResearchApp.college = user.College;
            fundedResearchApp.branch = user.Branch;
            fundedResearchApp.field_of_Study = model.StudyField;
            fundedResearchApp.application_Status = "Pending";
            fundedResearchApp.submission_Date = DateTime.Now;
            fundedResearchApp.dts_No = null;
            fundedResearchApp.project_Duration = Convert.ToInt32(model.ProjectDuration);
            fundedResearchApp.total_project_Cost = Convert.ToDouble(model.TotalProjectCost);
            fundedResearchApp.UserId = user.Id;
            fundedResearchApp.isArchive = false;

            _context.FundedResearchApplication.Add(fundedResearchApp);
            await _context.SaveChangesAsync();

            var templates = await _context.Guidelines
                .Where(g => g.document_Type == "DocumentaryForm" && g.file_Type == ".docx")
                .ToListAsync();

            string filledFolder = Path.Combine(_environment.WebRootPath, "content", "docu_forms");
            Directory.CreateDirectory(filledFolder);

            foreach (var template in templates)
            {
                using (var templateStream = new MemoryStream(template.data))
                {
                    string filledDocumentPath = Path.Combine(filledFolder, $"Generated_{template.file_Name}");

                    var teamMembers = fundedResearchApp.team_Members.Contains("N/A") ?
                        string.Empty : string.Join(Environment.NewLine, fundedResearchApp.team_Members);
                    var externalFundingAgency = string.IsNullOrEmpty(model.NameOfExternalFundingAgency) ? string.Empty : model.NameOfExternalFundingAgency;

                    using (DocX document = DocX.Load(templateStream))
                    {
                        document.ReplaceText("{{ProjectTitle}}", model.ProjectTitle);
                        document.ReplaceText("{{ProjectLead}}", model.ProjectLeader);
                        document.ReplaceText("{{LeadEmail}}", user.Email);
                        document.ReplaceText("{{ProjectStaff}}", teamMembers);
                        document.ReplaceText("{{ImplementInsti}}", model.ImplementingInstitution);
                        document.ReplaceText("{{CollabInsti}}", model.CollaboratingInstitution);
                        document.ReplaceText("{{ExternalFundingAgency}}", externalFundingAgency);
                        document.ReplaceText("{{ProjectDur}}", model.ProjectDuration + " month/s");
                        document.ReplaceText("{{TotalProjectCost}}", "₱ " + model.TotalProjectCost);
                        document.ReplaceText("{{Objectives}}", model.Objectives);
                        document.ReplaceText("{{Scope}}", model.Scope);
                        document.ReplaceText("{{Methodology}}", model.Methodology);
                        document.ReplaceText("{{ProjectLeaderCaps}}", model.ProjectLeader.ToUpper());

                        document.SaveAs(filledDocumentPath);
                    }

                    byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filledDocumentPath);

                    var doc = new GeneratedForm
                    {
                        Id = Guid.NewGuid().ToString(),
                        FileName = $"Generated_{template.file_Name}",
                        FileContent = fileBytes,
                        GeneratedAt = DateTime.Now,
                        fra_Id = fundedResearchApp.fra_Id
                    };

                    _context.GeneratedForms.Add(doc);
                }
            }
            await _context.SaveChangesAsync();
            Directory.Delete(filledFolder, true);

            return RedirectToAction("GenerateInfo");
        }


        [Authorize(Roles = "Faculty")]
        public IActionResult GenerateInfo() // Generated Documents Instructions
        {
            return View();
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> GeneratedDocuments() // List of Generated Documents
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("No user found!");
            }
            var fra = await _context.FundedResearchApplication.FirstOrDefaultAsync(f => f.UserId == user.Id && f.isArchive == false);
            if (fra == null)
            {
                return NotFound("No Funded Research Application found!");
            }
            var documents = await _context.GeneratedForms.Where(e => e.fra_Id == fra.fra_Id).OrderBy(f => f.FileName).ToListAsync();
            return View(documents);
        }

        public async Task<IActionResult> Download(string id) // Download the Generated Documents
        {
            var document = await _context.GeneratedForms.FindAsync(id);

            if (document == null)
            {
                return NotFound("No file found!");
            }
            return File(document.FileContent, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", document.FileName);
        }

        [HttpPost]
        public async Task<IActionResult> Reset(string fraId) // Reset Funded Research Application
        {
            var researchApp = await _context.FundedResearchApplication
                .FirstOrDefaultAsync(f => f.fra_Id == fraId && f.isArchive == false);

            if (researchApp != null)
            {
                _context.FundedResearchApplication.Remove(researchApp);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Faculty", "Home");
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> UploadFile() // Upload Documentary Requirements
        {
            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                return NotFound();
            }
            var fra = await _context.FundedResearchApplication
                .Where(s => s.application_Status == "Pending" && s.UserId == user.Id && s.isArchive == false)
                .ToListAsync();
            return View(fra);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(IFormCollection files) // Documentary Requirements stored 
        {
            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                return NotFound();
            }
            var fra = await _context.FundedResearchApplication
                .Where(s => s.application_Status == "Pending" && s.UserId == user.Id)
                .FirstOrDefaultAsync();

            if (fra == null)
            {
                return NotFound();
            }

            foreach (var file in files.Files)
            {
                if (file.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await file.CopyToAsync(ms);
                        var fileRequirement = new FileRequirement
                        {
                            fr_Id = Guid.NewGuid().ToString(),
                            file_Name = file.FileName,
                            file_Type = Path.GetExtension(file.FileName),
                            data = ms.ToArray(),
                            file_Status = "Pending",
                            document_Type = "Forms",
                            file_Feedback = null,
                            file_Uploaded = DateTime.Now,
                            fra_Id = fra.fra_Id
                        };

                        _context.FileRequirement.Add(fileRequirement);
                    }
                }
            }
            fra.application_Status = "Submitted";
            var genForms = await _context.GeneratedForms.Where(f => f.fra_Id == fra.fra_Id).ToListAsync();
            foreach (var form in genForms)
            {
                _context.GeneratedForms.Remove(form);
            }
            await _actionLogger.LogActionAsync(fra.applicant_Name, fra.fra_Type, fra.research_Title + " is submitted.", true, true, false, fra.fra_Id);
            await _context.SaveChangesAsync();
            if (fra.fra_Type == "University Funded Research")
            {
                await SendUFREmail(fra.applicant_Email, fra.research_Title, fra.applicant_Name);
            }
            else if (fra.fra_Type != "University Funded Research")
            {
                await SendSubmitEmail(fra.applicant_Email, fra.research_Title, fra.applicant_Name);
            }

            return RedirectToAction("ApplicationSuccess", "FundedResearchApplication", new {id = fra.fra_Id});
        }

        private async Task SendEmailAsync(string email, string subject, string htmlBody) // Email Configuration
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Research Evaluation and Monitoring Center", _smtpUser));

                string recipientName = email.Split('@')[0];
                message.To.Add(new MailboxAddress(recipientName, email));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();

                /*string footerImagePath = Path.Combine(_environment.WebRootPath, "images", "Footer.png");
                if (!System.IO.File.Exists(footerImagePath))
                {
                    throw new FileNotFoundException($"Footer image not found at: {footerImagePath}");
                }

                var image = bodyBuilder.LinkedResources.Add(footerImagePath);
                image.ContentId = MimeUtils.GenerateMessageId();

                htmlBody = htmlBody.Replace("{{footerImageContentId}}", image.ContentId);*/

                bodyBuilder.HtmlBody = htmlBody;
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_smtpUser, _smtpPass); //Email & App Password
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }
            catch (SmtpCommandException ex)
            {
                throw new Exception($"SMTP Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occured while sending email: {ex.Message}");
            }
        }

        public async Task SendUFREmail(string email, string researchTitle, string name) // Email for University Funded Research Application
        {
            var subject = $"Application successfully Submitted - {researchTitle}";
            var htmlBody = $@"
                <html>
                    <body style='font-family: Arial, sans-serif;'  font-size: 20px>
                        <br>
                        <div style='margin-bottom: 22px;'>
                            Dear Professor {name},<br><br>

                            We are pleased to inform you that your <strong> application has been successfully received </strong> by the Research Evaluation and Monitoring Center (REMC).
                            Your submission is<strong> currently under review </strong>by the Chief, who will assess its completeness before proceeding to the technical evaluation phase.
                        
                        </div>

                        <div style='margin-bottom: 22px;'>
                            <strong>Next Steps:</strong>
                            <ol>
                                <li>Your application will undergo a preliminary review by the Chief.</li>
                                <li>Upon approval, it will proceed to the technical evaluation stage, where it will be carefully assessed by our panel of experts.</li>
                                <li>For the meantime, you may<strong> start applying for Ethics Clearance </strong> through the system, or upload if you have already obtained one.</li>
                            </ol><br>

                            For real-time updates on the status of your application, we recommend checking the REMC website at {{websiteLink}}. Thank you for your submission, and we look forward to working with you throughout this process.

                        </div>
                        <hr>

                        <footer style='margin-top: 30px; font-size: 1em;'>
                            <strong><em>This is an automated email from the Research Evaluation Management Center (REMC). Please do not reply to this email.
                            For inquiries, contact the chief at <strong>chief@example.com</strong>.</em></strong><br><br>
                            <img src='cid:{{footerImageContentId}}' alt='Footer Image' style='width: 100%; max-width: 800px; height: auto;' />
                        </footer>
                    </body>
                </html>";

            await SendEmailAsync(email, subject, htmlBody);
        }

        public async Task SendSubmitEmail(string email, string researchTitle, string name) // Email for Externally Funded Research or University Funded Research Load Application
        {
            var subject = "Application Successfully Submitted - " + researchTitle;
            var htmlBody = $@"
                <html>
                    <body style='font-family: Arial, sans-serif;'  font-size: 20px>
                        <br>
                        <div style='margin-bottom: 22px;'>
                            Dear Professor {name},<br><br>

                            We are pleased to inform you that your <strong> application has been successfully received </strong> by the Research Evaluation and Monitoring Center (REMC).
                            Your submission is<strong> currently under review </strong>by the Chief, who will assess its completeness.
                        
                        </div>

                        <div style='margin-bottom: 22px;'>
                            <strong>Next Steps:</strong>
                            <ol>
                                <li>Your application will undergo a preliminary review by the Chief.</li>
                                <li> For the meantime, you may<strong> start applying for Ethics Clearance </strong> through the system, or upload if you have already obtained one.</li>
                            </ol><br>

                            For real-time updates on the status of your application, we recommend checking the REMC website at {{websiteLink}}. Thank you for your submission, and we look forward to working with you throughout this process.

                        </div>
                        <hr>

                        <footer style='margin-top: 30px; font-size: 1em;'>
                            <strong><em>This is an automated email from the Research Evaluation Management Center (REMC). Please do not reply to this email.
                            For inquiries, contact the chief at <strong>chief@example.com</strong>.</em></strong><br><br>
                            <img src='cid:{{footerImageContentId}}' alt='Footer Image' style='width: 100%; max-width: 800px; height: auto;' />
                        </footer>
                    
                    </body>
                </html>";
            await SendEmailAsync(email, subject, htmlBody);
        }

        [Authorize(Roles = "Faculty")]
        public IActionResult ApplicationSuccess(string id) // Application succesfully submitted page
        {
            var fra = _context.FundedResearchApplication.Find(id);
            if(fra == null)
            {
                return NotFound("No Funded Research Application found!");
            }
            ViewBag.Type = fra.fra_Type;
            return View();
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> ApplicationTracker() // Application Tracker for University Funded Research Application
        {
            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                return NotFound("No user found!");
            }
            var fraList = await _context.FundedResearchApplication
                .Where(s => s.UserId == user.Id && s.isArchive == false)
                .ToListAsync();

            if (fraList == null || !fraList.Any())
            {
                return NotFound("No Funded Research Application found!");
            }

            var teamLead = fraList.Select(s => s.applicant_Name).FirstOrDefault();
            var fraId = fraList.Select(s => s.fra_Id).FirstOrDefault();

            var logs = await _context.ActionLogs
                .Where(f => f.Name == teamLead && f.isTeamLeader == true && f.FraId == fraId)
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();

            var model = new Tuple<IEnumerable<FundedResearchApplication>, IEnumerable<ActionLog>>(fraList, logs);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SetDTS(string DTSNo, string fraId) // Set DTS No. for University Funded Research Application
        {
            var fra = await _context.FundedResearchApplication
                .FirstOrDefaultAsync(f => f.fra_Id == fraId);

            if (fra == null)
            {
                return NotFound("No Funded Research Application found!");
            }

            fra.dts_No = DTSNo;

            await _context.SaveChangesAsync();

            return RedirectToAction("ApplicationTracker", "FundedResearchApplication");
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> ApplicationStatus(string id) // List of Documentary Requirements
        {
            if(id == null)
            {
                return NotFound("No Funded Research Application ID found!");
            }
            var fra = await _context.FundedResearchApplication.FindAsync(id);
            if(fra == null)
            {
                return NotFound("No Funded Research Application found!");
            }
            ViewBag.FraId = fra.fra_Id;
            ViewBag.ProjTitle = fra.research_Title;
            ViewBag.TeamLead = fra.applicant_Name;
            ViewBag.TeamMembers = fra.team_Members;
            ViewBag.Field = fra.field_of_Study;
            ViewBag.Type = fra.fra_Type;
            ViewBag.Status = fra.application_Status;
            var fileRequirements = await _context.FileRequirement
                .Where(fr => fr.fra_Id == id && fr.file_Type == ".pdf")
                .OrderBy(fr => fr.file_Name)
                .ToListAsync();

            return View(fileRequirements);
        }

        [HttpPost]
        public async Task<IActionResult> Withdrawn(string fraId) // Withdrawn Funded Research Application
        {
            var researchApp = await _context.FundedResearchApplication
                .FirstOrDefaultAsync(f => f.fra_Id == fraId && f.isArchive == false);

            if (researchApp != null)
            {
                researchApp.application_Status = "Withdrawn";
                researchApp.isArchive = true;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Faculty", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> GoToEthics(FundedResearchEthics fundedResearchEthics, string id) // To be Revised
        {
            if(id == null)
            {
                return NotFound();
            }
            var fra = await _context.FundedResearchApplication.FindAsync(id);
            if(fra == null)
            {
                return NotFound("No submitted application found for this user.");
            }

            var existingEthics = await _context.FundedResearchEthics
                .FirstOrDefaultAsync(e => e.fra_Id == id);

            if(existingEthics == null)
            {
                var fre = new FundedResearchEthics
                {
                    fre_Id = Guid.NewGuid().ToString(),
                    fra_Id = fra.fra_Id,
                    urec_No = null,
                    ethicClearance_Id = null,
                    completionCertificate_Id = null
                };
                _context.FundedResearchEthics.Add(fre);
                await _context.SaveChangesAsync();
                return RedirectToAction("UnderMaintenance", "Home");
            }
            return RedirectToAction("UnderMaintenance", "Home");
        }

        public async Task<IActionResult> PreviewFile(string id) // Preview of PDF Files
        {
            var fileRequirement = await _context.FileRequirement.FindAsync(id);
            if(fileRequirement == null)
            {
                return NotFound("No File found!");
            }

            if(fileRequirement.file_Type == ".pdf")
            {
                return File(fileRequirement.data, "application/pdf");
            }

            return BadRequest("Only PDF files can be previewed.");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateFile(string id, IFormFile newFile) // Upload updated file
        {
            var fileRequirement = await _context.FileRequirement.FindAsync(id);
            if( fileRequirement == null)
            {
                return NotFound("No File found!");
            }

            if(newFile != null && newFile.Length > 0)
            {
                using (var memoryStream = new  MemoryStream())
                {
                    await newFile.CopyToAsync(memoryStream);
                    fileRequirement.data = memoryStream.ToArray();
                    fileRequirement.file_Name = newFile.FileName;
                    fileRequirement.file_Type = Path.GetExtension(newFile.FileName);
                    fileRequirement.file_Uploaded = DateTime.Now;
                    fileRequirement.file_Status = "Pending";
                    fileRequirement.file_Feedback = null;
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ApplicationStatus", new {id = fileRequirement.fra_Id});
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> ApplicationTrackerII() // Application Tracker for Externally Funded Research & University Funded Research Load Application
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("No user found!");
            }
            var fraList = await _context.FundedResearchApplication
                .Where(s => s.UserId == user.Id && s.isArchive == false)
                .ToListAsync();

            if (fraList == null || !fraList.Any())
            {
                return NotFound("No Funded Research Application found!");
            }

            var teamLead = fraList.Select(s => s.applicant_Name).FirstOrDefault();
            var fraId = fraList.Select(s => s.fra_Id).FirstOrDefault();

            var logs = await _context.ActionLogs
                .Where(f => f.Name == teamLead && f.isTeamLeader == true && f.FraId == fraId)
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();

            var model = new Tuple<IEnumerable<FundedResearchApplication>, IEnumerable<ActionLog>>(fraList, logs);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SetDTS2(string DTSNo, string fraId) // Set DTS No. for Externally Funded Research & University Funded Research Load Application
        {
            var fra = await _context.FundedResearchApplication
                .FirstOrDefaultAsync(f => f.fra_Id == fraId);

            if (fra == null)
            {
                return NotFound("No Funded Research Application found!");
            }

            fra.dts_No = DTSNo;

            await _context.SaveChangesAsync();

            return RedirectToAction("ApplicationTrackerII", "FundedResearchApplication");
        }

        [Authorize (Roles = "Faculty")]
        public async Task<IActionResult> EvaluationResult(string id) // List of Evaluation Result for University Funded Research Application
        {
            var fra = await _context.FundedResearchApplication.FindAsync(id);
            if (fra == null)
            {
                return NotFound("No Funded Research Application found!");
            }
            ViewBag.Id = fra.fra_Id;
            ViewBag.DTSNo = fra.dts_No;
            ViewBag.Research = fra.research_Title;
            ViewBag.Field = fra.field_of_Study;
            ViewBag.Lead = fra.applicant_Name;
            ViewBag.Member = fra.team_Members;
            ViewBag.Type = fra.fra_Type;

            var evaluationsList = await _context.Evaluations
                .Where(e => e.fra_Id == id && e.evaluation_Status != "Decline")
                .Join(_context.Evaluator,
                    evaluation => evaluation.evaluator_Id,
                    evaluator => evaluator.evaluator_Id,
                    (evaluation, evaluator) => new ViewChiefEvaluationVM
                    {
                        evaluator_Name = evaluation.evaluator_Name,
                        field_of_Interest = evaluator.field_of_Interest,
                        evaluation_Grade = evaluation.evaluation_Grade,
                        remarks = evaluation.evaluation_Status
                    })
                .ToListAsync();

            var evalFormList = await _context.FileRequirement
                .Where(e => e.fra_Id == id)
                .ToListAsync();

            var model = new Tuple<List<ViewChiefEvaluationVM>, List<FileRequirement>>
                (evaluationsList, evalFormList);

            return View(model);
        }

        public async Task<IActionResult> PdfDownload(string id) // Download Notice to Proceed
        {
            var file = await _context.FileRequirement.FirstOrDefaultAsync(f => f.fra_Id == id && f.document_Type == "Notice to Proceed");
            if(file == null)
            {
                return NotFound();
            }
            return File(file.data, "application/pdf", file.file_Name);
        }

        [HttpPost]
        public async Task<IActionResult> ReApply(string fraId) // Re-Apply for another Funded Research Application
        {
            var fra = await _context.FundedResearchApplication.FindAsync(fraId);
            if(fra == null)
            {
                return NotFound("No Funded Research Application found.");
            }

            fra.isArchive = true;
            await _context.SaveChangesAsync();
            return RedirectToAction("Forms", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Progress(string fraId) // Redirect to Progress Report Tracker
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("No user found!");
            }
            var fra = await _context.FundedResearchApplication.FindAsync(fraId);
            if (fra == null)
            {
                return NotFound("No Funded Research Application found.");
            }

            fra.isArchive = true;
            await _context.SaveChangesAsync();

            string currentDate = DateTime.Now.ToString("yyyyMMdd");
            var latestfr = await _context.FundedResearches
                .AsNoTracking()
                .Where(f => f.fr_Id.Contains(currentDate))
                .OrderByDescending(f => f.fr_Id)
                .FirstOrDefaultAsync();

            int nextNum = 1;
            if (latestfr != null)
            {
                string[] parts = latestfr.fr_Id.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastNum))
                {
                    nextNum = lastNum + 1;
                }
            }

            var frId = fra.fra_Type switch
            {
                "University Funded Research" => $"UFRW-{currentDate}-{nextNum:D3}",
                "Externally Funded Research" => $"EFRW-{currentDate}-{nextNum:D3}",
                "University Funded Research Load" => $"UFRL-{currentDate}-{nextNum:D3}",
                _ => throw new InvalidOperationException("Invalid fra_Type")
            };


            var fr = new FundedResearch
            {
                fr_Id = frId,
                fr_Type = fra.fra_Type,
                research_Title = fra.research_Title,
                team_Leader = fra.applicant_Name,
                teamLead_Email = fra.applicant_Email,
                team_Members = fra.team_Members,
                college = fra.college,
                branch = fra.branch,
                field_of_Study = fra.field_of_Study,
                status = "Ongoing",
                start_Date = DateTime.Now,
                end_Date = DateTime.Now.AddMonths(fra.project_Duration),
                dts_No = fra.dts_No,
                project_Duration = fra.project_Duration,
                total_project_Cost = fra.total_project_Cost,
                fra_Id = fraId,
                UserId = user.Id,
                isArchive = false,
                isExtension1 = false,
                isExtension2 = false,
            };

            var existingEntry = _context.Entry(fr);
            if (existingEntry != null)
            {
                _context.Entry(fr).State = EntityState.Detached;
            }

            _context.FundedResearches.Add(fr);
            _context.SaveChanges();

            var templates = _context.Guidelines
                .Where(g => (g.document_Type == "ProgressReport" || g.document_Type == "TerminalReport") && g.file_Type == ".docx")
                .ToList();
            string filledFolder = Path.Combine(_environment.WebRootPath, "content", "reports_forms");
            Directory.CreateDirectory(filledFolder);

            foreach (var template in templates)
            {
                using (var templateStream = new MemoryStream(template.data))
                {
                    string filledDocumentPath = Path.Combine(filledFolder, $"Generated_{template.file_Name}");

                    var teamMembers = fra.team_Members.Contains("N/A")
                        ? string.Empty : string.Join(Environment.NewLine, fra.team_Members);

                    using (DocX document = DocX.Load(templateStream))
                    {
                        document.ReplaceText("{{ResearchWorkNum}}", frId);
                        document.ReplaceText("{{ResearchWorkTitle}}", fra.research_Title);
                        document.ReplaceText("{{TeamLeader}}", fra.applicant_Name);
                        document.ReplaceText("{{TeamMembers}}", teamMembers);
                        document.ReplaceText("{{Duration}}", DateTime.Now.ToString("MMMM d, yyyy") + " - " +
                            DateTime.Now.AddMonths(fra.project_Duration).ToString("MMMM d, yyyy"));
                        document.ReplaceText("{{TeamLeaderCaps}}", fra.applicant_Name.ToUpper());
                        document.ReplaceText("{{TeamMembersCaps}}", string.Join(Environment.NewLine, fra.team_Members).ToUpper());

                        document.SaveAs(filledDocumentPath);
                    }

                    byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filledDocumentPath);
                    var doc = new GeneratedForm
                    {
                        Id = Guid.NewGuid().ToString(),
                        FileName = $"Generated_{template.file_Name}",
                        FileContent = fileBytes,
                        GeneratedAt = DateTime.Now,
                        fra_Id = fra.fra_Id
                    };

                    _context.GeneratedForms.Add(doc);
                }
            }
            Directory.Delete(filledFolder, true);
            await _context.SaveChangesAsync();
            return RedirectToAction("ProgressTracker", "ProgressReport");
        }

        [HttpPost]
        public async Task<IActionResult> AddEvent(string eventTitle, DateTime startDate, DateTime endDate) // Add event in Faculty or TeamLeader calendar
        {
            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                return NotFound("No user found!");
            }
            if (ModelState.IsValid)
            {
                var addEvent = new CalendarEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = eventTitle,
                    Start = startDate,
                    End = endDate.AddDays(1),
                    Visibility = "JustYou",
                    UserId = user.Id
                };

                _context.CalendarEvents.Add(addEvent);
                await _context.SaveChangesAsync();

                return RedirectToAction("TeamLeaderDashboard");
            }
            return RedirectToAction("TeamLeaderDashboard");
        }

        [HttpGet]
        public async Task<IActionResult> GetUserEvents() // Get all calendar events
        {
            var user = await _userManager.GetUserAsync(User);
            if( user == null )
            {
                return NotFound("No user found!");
            }

            var events = _context.CalendarEvents
                .Where(e => e.Visibility == "Broadcasted" || (e.Visibility == "JustYou" && e.UserId == user.Id))
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Start,
                    e.End,
                    e.Visibility,
                })
                .ToList();

            return Json(events);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEvent(string id) // Delete calendar event
        {
            var events = await _context.CalendarEvents.FindAsync(id);
            if (events != null && events.Visibility == "JustYou")
            {
                _context.CalendarEvents.Remove(events);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}
