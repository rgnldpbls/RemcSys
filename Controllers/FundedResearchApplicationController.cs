using System;
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

namespace RemcSys.Controllers
{
    public class FundedResearchApplicationController : Controller
    {
        private readonly RemcDBContext _context;
        private readonly UserManager<SystemUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ActionLoggerService _actionLogger;

        public FundedResearchApplicationController(RemcDBContext context, UserManager<SystemUser> userManager,
            IWebHostEnvironment environment, ActionLoggerService actionLogger)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _actionLogger = actionLogger;
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
        public async Task<IActionResult> FormFill(string type)
        {
            var user = await _userManager.GetUserAsync(User);

            ViewBag.Name = user.Name;
            ViewBag.Type = type;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormFill(FormModel model, FundedResearchApplication fundedResearchApp)
        {
            var user = await _userManager.GetUserAsync(User);

            // Generate fra_Id with template "FRA" + current date + incremented number
            string currentDate = DateTime.Now.ToString("yyyyMMdd");
            var latestFra = await _context.FundedResearchApplication
                .AsNoTracking()
                .Where(f => f.fra_Id.Contains(currentDate))
                .OrderByDescending(f => f.fra_Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (latestFra != null)
            {
                // Extract the last number in the current date's fra_Id and increment it
                string[] parts = latestFra.fra_Id.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            // Generate the unique fra_Id
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
            _context.SaveChanges();

            string[] templates = {"Capsule-Research-Proposal.docx","Form-1-Term-of-Reference.docx","Form-2-Line-Item-Budget.docx",
                "Form-3-Schedule-of-Outputs.docx", "Form-4-Workplan.docx"};
            string filledFolder = Path.Combine(_environment.WebRootPath, "content", "outputs");
            Directory.CreateDirectory(filledFolder);

            foreach (var template in templates)
            {
                string templatePath = Path.Combine(_environment.WebRootPath, "content", "templates", template);
                string filledDocumentPath = Path.Combine(filledFolder, $"Generated_{template}");

                using (DocX document = DocX.Load(templatePath))
                {
                    document.ReplaceText("{{ProjectTitle}}", model.ProjectTitle);
                    document.ReplaceText("{{ProjectLead}}", model.ProjectLeader);
                    document.ReplaceText("{{LeadEmail}}", user.Email);
                    document.ReplaceText("{{ProjectStaff}}", string.Join(Environment.NewLine, fundedResearchApp.team_Members));
                    document.ReplaceText("{{ImplementInsti}}", model.ImplementingInstitution);
                    document.ReplaceText("{{CollabInsti}}", model.CollaboratingInstitution);
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
                    FileName = $"Generated_{template}",
                    FileContent = fileBytes,
                    GeneratedAt = DateTime.Now,
                    fra_Id = fundedResearchApp.fra_Id
                };

                _context.GeneratedForms.Add(doc);
            }
            await _context.SaveChangesAsync();
            Directory.Delete(filledFolder, true);
            return RedirectToAction("GenerateInfo");
        }

        [Authorize(Roles = "Faculty")]
        public IActionResult GenerateInfo()
        {
            return View();
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> GeneratedDocuments()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }
            var fra = await _context.FundedResearchApplication.Where(f => f.UserId == user.Id).FirstOrDefaultAsync();
            if (fra == null)
            {
                return NotFound();
            }
            var documents = await _context.GeneratedForms.Where(e => e.fra_Id == fra.fra_Id).OrderBy(f => f.FileName).ToListAsync();
            return View(documents);
        }

        public async Task<IActionResult> Download(string id)
        {
            var document = await _context.GeneratedForms.FindAsync(id);

            if (document == null)
            {
                return NotFound();
            }
            return File(document.FileContent, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", document.FileName);
        }

        [HttpPost]
        public async Task<IActionResult> Reset(string fraId)
        {
            var researchApp = await _context.FundedResearchApplication
                .FirstOrDefaultAsync(f => f.fra_Id == fraId);

            if (researchApp != null)
            {
                _context.FundedResearchApplication.Remove(researchApp);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Faculty", "Home");
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> UploadFile()
        {
            var user = await _userManager.GetUserAsync(User);
            var fra = await _context.FundedResearchApplication.Where(s => s.application_Status == "Pending" && s.UserId == user.Id)
                .ToListAsync();
            return View(fra);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(IFormCollection files)
        {
            var user = await _userManager.GetUserAsync(User);
            var fra = await _context.FundedResearchApplication.Where(s => s.application_Status == "Pending" && s.UserId == user.Id)
                .FirstOrDefaultAsync();

            if (fra == null)
            {
                return NotFound();
            }

            var manuscriptFile = files.Files["manuscript"];
            if (manuscriptFile != null && manuscriptFile.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await manuscriptFile.CopyToAsync(ms);
                    var manuscriptReq = new FileRequirement
                    {
                        fr_Id = Guid.NewGuid().ToString(),
                        file_Name = manuscriptFile.FileName,
                        file_Type = Path.GetExtension(manuscriptFile.FileName),
                        data = ms.ToArray(),
                        file_Status = "Pending",
                        document_Type = "Manuscript",
                        file_Feedback = null,
                        file_Uploaded = DateTime.Now,
                        fra_Id = fra.fra_Id
                    };

                    _context.FileRequirement.Add(manuscriptReq);
                }
            }

            foreach (var file in files.Files)
            {
                if (file.Name != "manuscript" && file.Length > 0)
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
            /*if (fra.fra_Type == "University Funded Research")
            {
                await SendUFREmail(fra.applicant_Email, fra.research_Title, fra.applicant_Name);
            }
            else if (fra.fra_Type != "University Funded Research")
            {
                await SendSubmitEmail(fra.applicant_Email, fra.research_Title, fra.applicant_Name);
            }*/

            return RedirectToAction("ApplicationSuccess", "FundedResearchApplication");
        }

        public async Task SendUFREmail(string email, string researchTitle, string name)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Research Evaluation and Monitoring Center", "remc.rmo2@gmail.com")); //Name & Email

                string recipientName = email.Split('@')[0];
                message.To.Add(new MailboxAddress(recipientName, email));

                message.Subject = "Application Successfully Submitted - " + researchTitle;
                var bodyBuilder = new BodyBuilder();

                string footerImagePath = Path.Combine(_environment.WebRootPath, "images", "Footer.png");
                if (!System.IO.File.Exists(footerImagePath))
                {
                    throw new FileNotFoundException($"Footer image not found at: {footerImagePath}");
                }

                var image = bodyBuilder.LinkedResources.Add(footerImagePath);
                image.ContentId = MimeUtils.GenerateMessageId();

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
                            <img src='cid:{image.ContentId}' alt='Footer Image' style='width: 100%; max-width: 800px; height: auto;' />
                        </footer>
                    </body>
                </html>";

                bodyBuilder.HtmlBody = htmlBody;
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync("remc.rmo2@gmail.com", "rhmh oyge mwky ozzx"); //Email & App Password
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while sending email: {ex.Message}");
            }
        }

        public async Task SendSubmitEmail(string email, string researchTitle, string name)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Research Evaluation and Monitoring Center", "remc.rmo2@gmail.com")); //Name & Email

                string recipientName = email.Split('@')[0];
                message.To.Add(new MailboxAddress(recipientName, email));

                message.Subject = "Application Successfully Submitted - " + researchTitle;
                var bodyBuilder = new BodyBuilder();

                string footerImagePath = Path.Combine(_environment.WebRootPath, "images", "Footer.png");
                if (!System.IO.File.Exists(footerImagePath))
                {
                    throw new FileNotFoundException($"Footer image not found at: {footerImagePath}");
                }

                var image = bodyBuilder.LinkedResources.Add(footerImagePath);
                image.ContentId = MimeUtils.GenerateMessageId();

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
                            <img src='cid:{image.ContentId}' alt='Footer Image' style='width: 100%; max-width: 800px; height: auto;' />
                        </footer>
                    
                    </body>
                </html>";

                bodyBuilder.HtmlBody = htmlBody;
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync("remc.rmo2@gmail.com", "rhmh oyge mwky ozzx"); //Email & App Password
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while sending email: {ex.Message}");
            }
        }

        [Authorize(Roles = "Faculty")]
        public IActionResult ApplicationSuccess()
        {
            return View();
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> ApplicationTracker()
        {
            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                return NotFound();
            }
            var fraList = await _context.FundedResearchApplication
                .Where(s => s.UserId == user.Id && s.isArchive == false)
                .ToListAsync();

            if (fraList == null || !fraList.Any())
            {
                return NotFound();
            }

            var teamLead = fraList.Select(s => s.applicant_Name).FirstOrDefault();

            var logs = await _context.ActionLogs
                .Where(f => f.Name == teamLead && f.isTeamLeader == true)
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();

            var model = new Tuple<IEnumerable<FundedResearchApplication>, IEnumerable<ActionLog>>(fraList, logs);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SetDTS(string DTSNo, string fraId)
        {
            var fra = await _context.FundedResearchApplication
                .FirstOrDefaultAsync(f => f.fra_Id == fraId);

            if (fra == null)
            {
                return NotFound();
            }

            fra.dts_No = DTSNo;

            await _context.SaveChangesAsync();

            return RedirectToAction("ApplicationTracker");
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> ApplicationStatus(string id)
        {
            if(id == null)
            {
                return NotFound();
            }
            var fra = await _context.FundedResearchApplication.FindAsync(id);
            if(fra == null)
            {
                return NotFound();
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
        public async Task<IActionResult> GoToEthics(FundedResearchEthics fundedResearchEthics, string id)
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

        public async Task<IActionResult> PreviewFile(string id)
        {
            var fileRequirement = await _context.FileRequirement.FindAsync(id);
            if(fileRequirement == null)
            {
                return NotFound();
            }

            if(fileRequirement.file_Type == ".pdf")
            {
                return File(fileRequirement.data, "application/pdf");
            }

            return BadRequest("Only PDF files can be previewed.");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateFile(string id, IFormFile newFile)
        {
            var fileRequirement = await _context.FileRequirement.FindAsync(id);
            if( fileRequirement == null)
            {
                return NotFound();
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
        public async Task<IActionResult> ApplicationTrackerII()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }
            var fraList = await _context.FundedResearchApplication
                .Where(s => s.UserId == user.Id && s.isArchive == false)
                .ToListAsync();

            if (fraList == null || !fraList.Any())
            {
                return NotFound();
            }

            var teamLead = fraList.Select(s => s.applicant_Name).FirstOrDefault();

            var logs = await _context.ActionLogs
                .Where(f => f.Name == teamLead && f.isTeamLeader == true)
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();

            var model = new Tuple<IEnumerable<FundedResearchApplication>, IEnumerable<ActionLog>>(fraList, logs);
            return View(model);
        }

        [Authorize (Roles = "Faculty")]
        public async Task<IActionResult> EvaluationResult(string id)
        {
            var fra = await _context.FundedResearchApplication.FindAsync(id);
            if (fra == null)
            {
                return NotFound();
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

        public async Task<IActionResult> PdfDownload(string id)
        {
            var file = await _context.FileRequirement.FirstOrDefaultAsync(f => f.fra_Id == id && f.document_Type == "Notice to Proceed");
            if(file == null)
            {
                return NotFound();
            }
            return File(file.data, "application/pdf", file.file_Name);
        }

        [HttpPost]
        public async Task<IActionResult> ReApply(string fraId)
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


    }
}
