using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Humanizer;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using MimeKit;
using MimeKit.Utils;
using OfficeOpenXml;
using RemcSys.Areas.Identity.Data;
using RemcSys.Data;
using RemcSys.Models;
using System.Runtime.Intrinsics.X86;
using Xceed.Words.NET;

namespace RemcSys.Controllers
{
    public class ChiefController : Controller
    {
        private readonly RemcDBContext _context;
        private readonly ActionLoggerService _actionLogger;
        private readonly UserManager<SystemUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly MLContext _mlContext;

        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _smtpUser;
        private readonly string _smtpPass;

        public ChiefController(RemcDBContext context, ActionLoggerService actionLogger, UserManager<SystemUser> userManager,
            RoleManager<IdentityRole> roleManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _actionLogger = actionLogger;
            _userManager = userManager;
            _roleManager = roleManager;
            _webHostEnvironment = webHostEnvironment;
            _mlContext = new MLContext(seed: 0);
            _smtpUser = Environment.GetEnvironmentVariable("SMTP_USER") ?? "remc.rmo2@gmail.com";
            _smtpPass = Environment.GetEnvironmentVariable("SMTP_PASS") ?? "rhmh oyge mwky ozzx";
        }

        [Authorize(Roles = "Chief")]
        public async Task<IActionResult> ChiefDashboard() // Dashboard of the Chief
        {
            if (_context.Settings.First().isMaintenance)
            {
                return RedirectToAction("UnderMaintenance", "Home");
            }

            await RemindEvaluations();
            await RemindSubmitProgressReport();

            var fundedResearch = await _context.FundedResearches.ToListAsync();

            var rankedBranch = _context.FundedResearches
                .GroupBy(r => r.branch)
                .Select(g => new
                {
                    BranchName = g.Key,
                    TotalResearch = g.Count(),
                })
                .OrderByDescending(g => g.TotalResearch)
                .Take(3)
                .ToList();

            var model = new Tuple<IEnumerable<FundedResearch>, IEnumerable<dynamic>>(fundedResearch, rankedBranch);

            return View(model);
        }
         
        [Authorize(Roles = "Chief")]
        public async Task<IActionResult> ChiefNotif() // Notification of the Chief
        {
            if (_context.Settings.First().isMaintenance)
            {
                return RedirectToAction("UnderMaintenance", "Home");
            }

            var logs = await _context.ActionLogs
                .Where(f => f.isChief == true)
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();
            return View(logs);
        }

        [Authorize(Roles = "Chief")]
        public async Task<IActionResult> UFResearchApp(string searchString) // List of University Funded Research Application
        {
            /*await CheckMissedEvaluations();*/
            ViewData["currentFilter"] = searchString;
            var appQuery = _context.FundedResearchApplication
                .Where(f => f.fra_Type == "University Funded Research");

            if (!String.IsNullOrEmpty(searchString))
            {
                appQuery = appQuery.Where(s => s.research_Title.Contains(searchString));
            }

            if(appQuery != null)
            {
                var researchAppList = await appQuery.Where(f => f.application_Status == "Submitted")
                .OrderBy(f => f.submission_Date).ToListAsync();

                return View(researchAppList);
            }

            return View(new List<FundedResearchApplication>());
        }

        [Authorize(Roles = "Chief")]
        public async Task<IActionResult> EFResearchApp(string searchString) // List of Externally Funded Research Application
        {
            ViewData["currentFilter"] = searchString;
            var application = _context.FundedResearchApplication
                .Where(f => f.fra_Type == "Externally Funded Research");

            if (!String.IsNullOrEmpty(searchString))
            {
                application = application.Where(s => s.research_Title.Contains(searchString));
            }

            if(application != null)
            {
                var researchApp = await application.Where(f => f.application_Status == "Submitted")
                    .OrderBy(f => f.submission_Date).ToListAsync();
                return View(researchApp);
            }

            return View(new List<FundedResearchApplication>());
        }

        [Authorize(Roles = "Chief")]
        public async Task<IActionResult> UFRLApp(string searchString) // List of University Funded Research Load Application
        {
            ViewData["currentFilter"] = searchString;
            var application = _context.FundedResearchApplication
                .Where(f => f.fra_Type == "University Funded Research Load");

            if (!String.IsNullOrEmpty(searchString))
            {
                application = application.Where(s => s.research_Title.Contains(searchString));
            }
            if(application != null)
            {
                var researchApp = await application.Where(f => f.application_Status == "Submitted")
                    .OrderBy(f => f.submission_Date).ToListAsync();
                return View(researchApp);
            }

            return View(new List<FundedResearchApplication>());
        }

        [Authorize(Roles = "Chief")]
        public async Task<IActionResult> DocuList(string id) // Documentary Requirements per Application
        {
            if (id == null)
            {
                return NotFound("No Funded Research Application ID found!");
            }

            var fra = await _context.FundedResearchApplication.FindAsync(id);
            if(fra == null)
            {
                return NotFound("No Funded Research Application Found!");
            }
            ViewBag.FraId = fra.fra_Id;
            ViewBag.DTSNo = fra.dts_No;
            ViewBag.Research = fra.research_Title;
            ViewBag.Field = fra.field_of_Study;
            ViewBag.Lead = fra.applicant_Name;
            ViewBag.Member = fra.team_Members;
            ViewBag.Type = fra.fra_Type;
            ViewBag.Status = fra.application_Status;

            var fileRequirement = await _context.FileRequirement.Where(f => f.fra_Id == id && f.file_Type == ".pdf")
                .OrderBy(fr => fr.file_Name)
                .ToListAsync();

            var researchEthics = await _context.FundedResearchEthics
                 .Where(e => e.fra_Id == id)
                 .ToListAsync();

            var model = new Tuple<IEnumerable<FileRequirement>, IEnumerable<FundedResearchEthics>>(fileRequirement, researchEthics);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveFeedback(string fileId, string fileStatus, string fileFeedback) // Saving Feedback of the Chief on each Documentary Requirements
        {
            var fileReq = _context.FileRequirement.Find(fileId);
            if (fileReq != null)
            {
                var fra = await _context.FundedResearchApplication.Where(f => f.fra_Id == fileReq.fra_Id).FirstOrDefaultAsync();
                if (fra == null)
                {
                    return NotFound("No Funded Research Application found!");
                }
                fileReq.file_Status = fileStatus;
                fileReq.file_Feedback = fileFeedback;

                _context.SaveChanges();
                await _actionLogger.LogActionAsync(fra.applicant_Name, fra.fra_Type, "You need to resubmit this " + fileReq.file_Name + ". " +
                    "Kindly check the feedback for the changes.", true, false, false, fra.fra_Id);
                return Json(new { success = true });
            }

            var researchEthics = _context.FundedResearchEthics.Find(fileId);
            if (researchEthics != null)
            {
                var fra = await _context.FundedResearchApplication.Where(f => f.fra_Id == researchEthics.fra_Id).FirstOrDefaultAsync();
                if (fra == null)
                {
                    return NotFound("No Funded Research Application found!");
                }
                researchEthics.file_Status = fileStatus;
                researchEthics.file_Feedback = fileFeedback;

                _context.SaveChanges();
                await _actionLogger.LogActionAsync(fra.applicant_Name, fra.fra_Type, "You need to resubmit this " + researchEthics.file_Name + ". " +
                    "Kindly check the feedback for the changes.", true, false, false, fra.fra_Id);
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string fr_Id, string newStatus) //Updating the File Status into Checked on each Documentary Requirements
        {
            var file = _context.FileRequirement.FirstOrDefault(f => f.fr_Id == fr_Id);
            if (file != null)
            {
                var fra = await _context.FundedResearchApplication
                .Where(f => f.fra_Id == file.fra_Id)
                .FirstOrDefaultAsync();
                if (fra == null)
                {
                    return Json(new { success = false, message = "Funded Research Application not found" });
                }
                file.file_Status = newStatus;
                _context.SaveChanges();
                await _actionLogger.LogActionAsync(fra.applicant_Name, fra.fra_Type, file.file_Name + " already checked by the Chief.",
                    true, false, false, fra.fra_Id);

                var allFilesChecked = _context.FileRequirement
                       .Where(fr => fr.fra_Id == fra.fra_Id)
                       .All(fr => fr.file_Status == "Checked");

                if (allFilesChecked && fra.fra_Type == "University Funded Research")
                {
                    await AssignEvaluators(fra.fra_Id);
                    return Json(new { assigned = true });
                }
                else if (allFilesChecked && fra.fra_Type != "University Funded Research")
                {
                    fra.application_Status = "Approved";
                    await _context.SaveChangesAsync();
                    await _actionLogger.LogActionAsync(fra.applicant_Name, fra.fra_Type, fra.research_Title + " all file requirements approved by the Chief.",
                        true, false, false, fra.fra_Id);
                    await _actionLogger.LogActionAsync(fra.applicant_Name, fra.fra_Type, $"{fra.research_Title} needs to apply/upload ethics clearance.", true, false, false, fra.fra_Id);
                    return Json(new { approved = true });
                }
                return Json(new { success = true });
            }

            var ethics = _context.FundedResearchEthics.FirstOrDefault(f => f.fre_Id == fr_Id);
            if (ethics != null)
            {
                var fra = await _context.FundedResearchApplication
                .Where(f => f.fra_Id == ethics.fra_Id)
                .FirstOrDefaultAsync();
                if (fra == null)
                {
                    return Json(new { success = false, message = "Funded Research Application not found" });
                }
                ethics.file_Status = newStatus;
                _context.SaveChanges();

                await _actionLogger.LogActionAsync(fra.applicant_Name, fra.fra_Type, ethics.file_Name + " already verified by the Chief.",
                   true, false, false, fra.fra_Id);

                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Bad Request!" });
        }

        [HttpPost]
        public async Task<IActionResult> SetDTS(string DTSNo, string fraId) // Set DTS No of each Application
        {
            var fra = await _context.FundedResearchApplication
                .FirstOrDefaultAsync(f => f.fra_Id == fraId);

            if (fra == null)
            {
                return NotFound();
            }

            fra.dts_No = DTSNo;

            await _context.SaveChangesAsync();

            return RedirectToAction("DocuList", "Chief", new {id = fraId});
        }

        public async Task<IActionResult> PreviewFile(string id) // Preview of each PDF File while Download if not PDF file
        {
            var fileRequirement = await _context.FileRequirement.FindAsync(id);
            if (fileRequirement != null)
            {
                if (fileRequirement.file_Type == ".pdf")
                {
                    return File(fileRequirement.data, "application/pdf");
                }
            }

            var researchEthics = await _context.FundedResearchEthics.FindAsync(id);
            if(researchEthics != null)
            {
                if(researchEthics.file_Type == ".pdf")
                {
                    return File(researchEthics.clearanceFile, "application/pdf");
                }
            }

            var progReport = await _context.ProgressReports.FindAsync(id);
            if(progReport != null)
            {
                if(progReport.file_Type == ".pdf")
                {
                    return File(progReport.data, "application/pdf");
                }
                else
                {
                    var contentType = GetContentType(progReport.file_Type);
                    return File(progReport.data, contentType, progReport.file_Name);
                }
            }

            var genReport = await _context.GenerateReports.FindAsync(id);
            if(genReport != null)
            {
                var contentType = GetContentType(genReport.gr_fileType);
                return File(genReport.gr_Data, contentType, genReport.gr_fileName);
            }

            var genNominees = await _context.GenerateGAWADNominees.FindAsync(id);
            if(genNominees != null)
            {
                var contentType = GetContentType(genNominees.gn_fileType);
                return File(genNominees.gn_Data, contentType, genNominees.gn_fileName);
            }

            var gawadFiles = await _context.GAWADWinners.FindAsync(id);
            if(gawadFiles != null)
            {
                var contentType = GetContentType(gawadFiles.gw_fileType);
                return File(gawadFiles.gw_Data, contentType, gawadFiles.gw_fileName);
            }

            var guidelines = await _context.Guidelines.FindAsync(id);
            if(guidelines != null)
            {
                if(guidelines.file_Type == ".pdf")
                {
                    return File(guidelines.data, "application/pdf");
                }
                else
                {
                    var contentType = GetContentType(guidelines.file_Type);
                    return File(guidelines.data, contentType, guidelines.file_Name);
                }
            }

            return BadRequest("Only PDF files can be previewed.");
        }

        private string GetContentType(string fileType) // Get the specific file type for download
        {
            switch (fileType)
            {
                case ".xls":
                    return "application/vnd.ms-excel";
                case ".xlsx":
                    return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".doc":
                    return "application/msword";
                case ".docx":
                    return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                default:
                    return "application/octet-stream";
            }
        }

        public async Task AssignEvaluators(string fraId) // Automated Assign of Evaluators for University Funded Research
        {
            int evaluatorCount = _context.Settings.First().evaluatorNum;
            int daysEvaluation = _context.Settings.First().daysEvaluation;
            var fra = await _context.FundedResearchApplication.Where(f => f.fra_Id == fraId).FirstOrDefaultAsync();
            if (fra == null)
            {
                throw new Exception("No Funded Research Application found!");
            }

            var eligibleEvaluators = await GetEligibleEvaluators(fraId);

            var alreadyAssignedEvaluatorIds = _context.Evaluations.Where(e => e.fra_Id == fraId)
                .Select(e => e.evaluator_Id).ToList();

            eligibleEvaluators = eligibleEvaluators.Where(e => !alreadyAssignedEvaluatorIds.Contains(e.evaluator_Id)).ToList();

            var assignedEvaluators = new List<Evaluation>();
            int totalEvaluatorsToAssign = Math.Min(evaluatorCount, eligibleEvaluators.Count);
            int evaluatorIndex = 0;

            for (int i = 0; i < totalEvaluatorsToAssign; i++)
            {
                var evaluator = eligibleEvaluators[evaluatorIndex];
                var newEvaluation = new Evaluation
                {
                    evaluation_Id = Guid.NewGuid().ToString(),
                    evaluation_Status = "Pending",
                    evaluator_Id = evaluator.evaluator_Id,
                    evaluator_Name = evaluator.evaluator_Name,
                    fra_Id = fraId,
                    evaluation_Grade = null,
                    assigned_Date = DateTime.Now,
                    evaluation_Deadline = DateTime.Now.AddDays(daysEvaluation),
                    evaluation_Date = null
                };

                assignedEvaluators.Add(newEvaluation);
                await _actionLogger.LogActionAsync(evaluator.evaluator_Name, fra.fra_Type, "The chief assign you to evaluate the " + fra.research_Title + ".",
                    false, false, true, fra.fra_Id);
                await SendEvaluatorEmail(evaluator.evaluator_Email, fra.research_Title, evaluator.evaluator_Name, DateTime.Now.AddDays(daysEvaluation).ToString("MMMM d, yyyy"));
                evaluatorIndex = (evaluatorIndex + 1) % eligibleEvaluators.Count;
            }
            _context.Evaluations.AddRange(assignedEvaluators);
            fra.application_Status = "UnderEvaluation";
            await _context.SaveChangesAsync();
            await _actionLogger.LogActionAsync(fra.applicant_Name, fra.fra_Type, fra.research_Title + " already under techical evaluation.",
                true, false, false, fra.fra_Id);
        }

        public async Task<List<Evaluator>> GetEligibleEvaluators(string fraId) // Assess if the Evaluators is eligible based on their field of interests
        {
            var researchApp = await _context.FundedResearchApplication
                    .FirstOrDefaultAsync(fra => fra.fra_Id == fraId);

            if (researchApp == null)
            {
                throw new Exception("Research application not found.");
            }

            var evaluators = await _context.Evaluator
                .Where(e => e.field_of_Interest.Contains(researchApp.field_of_Study) &&
                            e.evaluator_Name != researchApp.applicant_Name && 
                            !researchApp.team_Members.Contains(e.evaluator_Name))
                .ToListAsync();
            var eligibleEvaluators = new List<Evaluator>();
            foreach(var evaluator in evaluators)
            {
                var pendingAndMissedCount = await _context.Evaluations
                    .Where(ev => ev.evaluator_Id == evaluator.evaluator_Id && 
                        (ev.evaluation_Status == "Pending" || ev.evaluation_Status == "Missed"))
                    .CountAsync();

                if(pendingAndMissedCount <= 10)
                {
                    eligibleEvaluators.Add(evaluator);
                }
            }
            return eligibleEvaluators;
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

                /*string footerImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "Footer.png");
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

        public async Task SendEvaluatorEmail(string email, string researchTitle, string name, string deadline) // Email for Assigned Evaluators
        {
            var subject = "Assigned as Evaluator in " + researchTitle;
            var htmlBody = $@"
                <html>
                    <body style='font-family: Arial, sans-serif;'  font-size: 20px>
                        <br>
                        <div style='margin-bottom: 22px;'>
                            Dear Professor {name},<br><br>
                            Greetings!<br><br>
                            You have been assigned to evaluate the research titled <strong>{researchTitle}</strong>.
                            The evaluation will play a key role in determining the research's progress, and we kindly request your prompt review.
                        </div>

                        <div style='margin-bottom: 22px;'>
                            <strong>Evaluation Details:</strong><br>
                            In the system, you will find two IR Evaluation forms that need to be completed:
                            <ul>
                                <li><strong>Grading Form</strong> – Includes scoring per criterion and a section for comments and suggestions.</li>
                                <li><strong>Comments Form</strong> – A form for providing detailed comments and suggestions only.</li>
                            </ul>
                        </div>

                        <div style='margin-bottom: 22px;'>
                            <strong>How to Submit:</strong>
                            <ol>
                                <li>Log in to the <strong>Research Evaluation Management Center (REMC)</strong> system: <a href='{{systemLink}}'>{{systemLink}}</a>.</li>
                                <li>Navigate to the ""Evaluator"" section.</li>
                                <li>Complete both the <strong>Grading Form</strong> and <strong>Comments Form</strong>.</li>
                                <li>Submit your evaluations on or before <strong>{deadline}</strong>.</li>
                            </ol>
                        </div>

                        <div style='margin-bottom: 22px;'>
                            Your timely feedback is crucial, and we appreciate your effort in completing both evaluations.
                            Should you have any questions or concerns, feel free to contact the Research Management Office (RMO) at [RMO Contact Information].
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

        [Authorize(Roles = "Chief")]
        public async Task<IActionResult> UEResearchApp(string searchString)
        {
            ViewData["currentFilter"] = searchString;
            var appQuery = _context.FundedResearchApplication
                .Where(f => f.fra_Type == "University Funded Research");

            if (!String.IsNullOrEmpty(searchString))
            {
                appQuery = appQuery.Where(s => s.research_Title.Contains(searchString));
            }

            var researchAppList = await appQuery
                .Where(f => f.application_Status == "UnderEvaluation")
                .OrderBy(f => f.submission_Date)
                .ToListAsync();

            var evaluatorList = _context.Evaluations
                .Where(e => researchAppList.Select(r => r.fra_Id).Contains(e.fra_Id))
                .ToList();

            var allEvaluators = await _context.Evaluator
                .OrderBy(f => f.evaluator_Name)
                .ToListAsync();

            var model = new Tuple<List<FundedResearchApplication>, List<Evaluation>, List<Evaluator>>(researchAppList, evaluatorList, allEvaluators);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetEvaluatorsForResearch(string fraId, string fieldOfStudy)
        {
            var research = await _context.FundedResearchApplication
                .FirstOrDefaultAsync(f => f.fra_Id == fraId);

            var evaluatorCount = _context.Settings.First().evaluatorNum;

            if (research == null)
            {
                return NotFound();
            }

            var evaluators = await _context.Evaluator
                .Where(e => e.field_of_Interest.Contains(fieldOfStudy))
                .ToListAsync();

            var assignedEvaluators = await _context.Evaluations
                .Where(e => e.fra_Id == fraId)
                .Select(e => e.evaluator_Id)
                .ToListAsync();

            var assignedCount = await _context.Evaluations
                .Where(e => e.fra_Id == fraId && e.evaluation_Status != "Decline")
                .ToListAsync();

            var pendingCount = await _context.Evaluations
                .Where(e => e.evaluation_Status == "Pending" || e.evaluation_Status == "Missed")
                .ToListAsync();

            var evaluatorData = evaluators.Select(e => new
            {
                e.evaluator_Id,
                e.evaluator_Name,
                e.field_of_Interest,
                pendingCount = pendingCount.Count(a => a.evaluator_Id == e.evaluator_Id),
                IsDisabled = e.evaluator_Name == research.applicant_Name ||
                             research.team_Members.Contains(e.evaluator_Name) ||
                             assignedEvaluators.Contains(e.evaluator_Id) ||
                             assignedCount.Count >= evaluatorCount
            }).OrderBy(f => f.evaluator_Name);

            return Json(evaluatorData);
        }

        [HttpPost]
        public async Task<IActionResult> DeclineEvaluator(string evaluationId)
        {
            var evals = _context.Evaluations.Find(evaluationId);
            if(evals == null)
            {
                return NotFound("Evaluation not found!");
            }

            var evaluator = _context.Evaluator.FirstOrDefault(e => e.evaluator_Id == evals.evaluator_Id);
            if(evaluator == null)
            {
                return NotFound("Evaluator not found!");
            }

            var fra = _context.FundedResearchApplication.FirstOrDefault(f => f.fra_Id == evals.fra_Id);
            if(fra == null)
            {
                return NotFound("Funded Research Application not found!");
            }

            await _actionLogger.LogActionAsync(evals.evaluator_Name, fra.fra_Type, "The chief remove you to evaluate the " + fra.research_Title + ".",
                false, false, true, fra.fra_Id);
            await SendRemoveEmail(evaluator.evaluator_Email, fra.research_Title, evals.evaluator_Name);
            evals.evaluation_Status = "Decline";
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        public async Task SendRemoveEmail(string email, string researchTitle, string name)
        {
            var subject = "Approval of Your Request for Evaluator Role Removal in " + researchTitle;
            var htmlBody = $@"
                <html>
                    <body style='font-family: Arial, sans-serif;'  font-size: 20px>
                        <br>
                        <div style='margin-bottom: 22px;'>
                            Dear Professor {name},<br><br>
                            We hope this message finds you well.<br><br>
                            In response to your recent request, we are pleased to inform you that the Chief of the Research Evaluation Management Center (REMC)
                            has <strong>approved your removal from the evaluator role </strong> for the research titled <strong>{researchTitle}</strong>.                    
                        </div>

                        <div style='margin-bottom: 22px;'>
                            We understand your need for this request and are grateful for the time and effort you’ve already contributed to the evaluation process. 
                            Should you wish to engage in future evaluations or have any other concerns, please don't hesitate to reach out to the 
                            Research Management Office (RMO). <br><br>   

                            Thank you once again for your valuable contributions.
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

        [HttpPost]
        public async Task<IActionResult> AssignEvaluator(int evaluatorId, string fraId)
        {
            var fra = await _context.FundedResearchApplication.FirstOrDefaultAsync(f => f.fra_Id == fraId);
            if(fra == null)
            {
                return NotFound("Funded Research Application not found!");
            }
            var evaluatorCount = _context.Settings.First().evaluatorNum;
            var daysEvaluation = _context.Settings.First().daysEvaluation;

            var existingEvaluation = await _context.Evaluations
            .FirstOrDefaultAsync(e => e.evaluator_Id == evaluatorId && e.fra_Id == fraId);

            if (existingEvaluation != null)
            {
                return Json(new { success = false, message = "Evaluator is already assigned to this application." });
            }

            int existingEvals = await _context.Evaluations.CountAsync(e => e.fra_Id == fraId && e.evaluation_Status != "Decline");
            if(existingEvals > evaluatorCount)
            {
                return Json(new { success = false, message = $"Maximum {evaluatorCount} evaluators can be assigned." });
            }

            var evaluators = _context.Evaluator.Find(evaluatorId);
            if (evaluators == null)
            {
                return NotFound("Evaluator not found!");
            }

            var evaluation = new Evaluation
            {
                evaluation_Id = Guid.NewGuid().ToString(),
                evaluation_Status = "Pending",
                evaluator_Name = evaluators.evaluator_Name,
                evaluation_Grade = null,
                assigned_Date = DateTime.Now,
                evaluation_Deadline = DateTime.Now.AddDays(daysEvaluation),
                evaluation_Date = null,
                evaluator_Id = evaluatorId,
                fra_Id = fraId
            };

            await _actionLogger.LogActionAsync(evaluators.evaluator_Name, fra.fra_Type, "The chief assign you to evaluate the " + fra.research_Title + ".", 
                false, false, true, fra.fra_Id);
            await SendEvaluatorEmail(evaluators.evaluator_Email, fra.research_Title, evaluators.evaluator_Name, DateTime.Now.AddDays(daysEvaluation).ToString("MMMM d, yyyy"));
            _context.Evaluations.Add(evaluation);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
        
        public async Task RemindEvaluations()
        {
            var today = DateTime.Today;

            var evaluations = await _context.Evaluations
                .Where(e => e.evaluation_Status == "Pending")
                .Include(e => e.evaluator)
                .Include(e => e.fundedResearchApplication)
                .ToListAsync();

            foreach (var evaluation in evaluations)
            {
                var daysLeft = (evaluation.evaluation_Deadline - today).Days;
                if(daysLeft < 0)
                {
                    evaluation.evaluation_Status = "Missed";
                }
                else if(daysLeft == 0 && !evaluation.reminded_Today)
                {
                    var content = $"This is an urgent reminder that the evaluation for the research titled {evaluation.fundedResearchApplication.research_Title} is due today.";
                    
                    await SendRemindEvaluatorEmail(
                        evaluation.evaluator.evaluator_Email, 
                        evaluation.fundedResearchApplication.research_Title, 
                        evaluation.evaluator.evaluator_Name, 
                        content);

                    await _actionLogger.LogActionAsync(
                        evaluation.evaluator.evaluator_Name, 
                        evaluation.fundedResearchApplication.fra_Type, content, 
                        false, false, true, 
                        evaluation.fundedResearchApplication.fra_Id);

                    evaluation.reminded_Today = true;
                    _context.SaveChanges();
                }
                else if(daysLeft == 1 && !evaluation.reminded_OneDayBefore)
                {
                    var content = $"This is a reminder that your evaluation for the research titled {evaluation.fundedResearchApplication.research_Title} is due tomorrow. We kindly request you to submit  the completed Grading and Comments forms before the deadline of {evaluation.evaluation_Deadline.ToString("MMMM d, yyyy")}.";
                    
                    await SendRemindEvaluatorEmail(
                        evaluation.evaluator.evaluator_Email, 
                        evaluation.fundedResearchApplication.research_Title, 
                        evaluation.evaluator.evaluator_Name, 
                        content);

                    await _actionLogger.LogActionAsync(
                        evaluation.evaluator.evaluator_Name, 
                        evaluation.fundedResearchApplication.fra_Type, content, 
                        false, false, true, 
                        evaluation.fundedResearchApplication.fra_Id);

                    evaluation.reminded_OneDayBefore = true;
                    _context.SaveChanges();
                }
                else if(daysLeft == 3 && !evaluation.reminded_ThreeDaysBefore)
                {
                    var content = $"This is a gentle reminder that the deadline for submitting your evaluation of the research titled {evaluation.fundedResearchApplication.research_Title} is approaching, with 3 days remaining until {evaluation.evaluation_Deadline.ToString("MMMM d, yyyy")}.";
                    
                    await SendRemindEvaluatorEmail(
                        evaluation.evaluator.evaluator_Email, 
                        evaluation.fundedResearchApplication.research_Title, 
                        evaluation.evaluator.evaluator_Name, 
                        content);

                    await _actionLogger.LogActionAsync(
                        evaluation.evaluator.evaluator_Name, 
                        evaluation.fundedResearchApplication.fra_Type, content, 
                        false, false, true, 
                        evaluation.fundedResearchApplication.fra_Id);

                    evaluation.reminded_ThreeDaysBefore = true;
                    _context.SaveChanges();
                }
            }

            var overDueEvaluations = await _context.Evaluations.Where(e => e.evaluation_Status == "Missed" && e.evaluation_Deadline < today)
                .Include(e => e.evaluator)
                .Include(e => e.fundedResearchApplication)
                .ToListAsync();
            foreach (var evaluation in overDueEvaluations)
            {
                var daysPastDeadline = (today - evaluation.evaluation_Deadline).Days;
                if(daysPastDeadline == 1 && !evaluation.reminded_OneDayOverdue)
                {
                    var content = $"This is a follow-up regarding the overdue evaluation for the research titled {evaluation.fundedResearchApplication.research_Title}. The deadline was on {evaluation.evaluation_Deadline.ToString("MMMM d, yyyy")}, and the submission is now 1 day overdue";

                    await SendOverDueEvaluatorEmail(
                        evaluation.evaluator.evaluator_Email,
                        evaluation.fundedResearchApplication.research_Title,
                        evaluation.evaluator.evaluator_Name,
                        content);

                    await _actionLogger.LogActionAsync(
                        evaluation.evaluator.evaluator_Name,
                        evaluation.fundedResearchApplication.fra_Type, content,
                        false, false, true,
                        evaluation.fundedResearchApplication.fra_Id);

                    evaluation.reminded_OneDayOverdue = true;
                    _context.SaveChanges();
                }
                else if(daysPastDeadline == 3 && !evaluation.reminded_ThreeDaysOverdue)
                {
                    var content = $"This is a follow-up regarding the overdue evaluation for the research titled {evaluation.fundedResearchApplication.research_Title}. The deadline was on {evaluation.evaluation_Deadline.ToString("MMMM d, yyyy")}, and the submission is now 3 day overdue";

                    await SendOverDueEvaluatorEmail(
                        evaluation.evaluator.evaluator_Email,
                        evaluation.fundedResearchApplication.research_Title,
                        evaluation.evaluator.evaluator_Name,
                        content);

                    await _actionLogger.LogActionAsync(
                        evaluation.evaluator.evaluator_Name,
                        evaluation.fundedResearchApplication.fra_Type, content,
                        false, false, true,
                        evaluation.fundedResearchApplication.fra_Id);

                    evaluation.reminded_ThreeDaysOverdue = true;
                    _context.SaveChanges();
                }
                else if(daysPastDeadline == 7 && !evaluation.reminded_SevenDaysOverdue)
                {
                    var content = $"We are reaching out again as the evaluation for the research titled {evaluation.fundedResearchApplication.research_Title} is now 7 days overdue. Your feedback is essential to the research's progress, and we kindly ask that you complete the Grading and Comments forms as soon as possible.";

                    await SendOverDueEvaluatorEmail(
                        evaluation.evaluator.evaluator_Email,
                        evaluation.fundedResearchApplication.research_Title,
                        evaluation.evaluator.evaluator_Name,
                        content);

                    await _actionLogger.LogActionAsync(
                        evaluation.evaluator.evaluator_Name,
                        evaluation.fundedResearchApplication.fra_Type, content,
                        false, false, true,
                        evaluation.fundedResearchApplication.fra_Id);

                    evaluation.reminded_SevenDaysOverdue = true;
                    _context.SaveChanges();
                }
            }
        }

        public async Task SendRemindEvaluatorEmail(string email, string researchTitle, string name, string content)
        {
            var subject = "Research Evaluation Deadline - " + researchTitle;
            var htmlBody = $@"
                <html>
                    <body style='font-family: Arial, sans-serif;'  font-size: 20px>
                        <br>
                        <div style='margin-bottom: 22px;'>
                            Dear Professor {name},<br><br>
                    
                            Greetings! <br><br>
                            {content}

                        </div>

                        <div style='margin-bottom: 22px;'>
                            Please log in to the system and complete both evaluation forms:
                            <ol>
                                <li><strong>Grading Form</strong>: Includes scoring per criterion and a section for comments and suggestions.</li>
                                <li><strong>Comments Form</strong>: A form for providing detailed comments and suggestions only.</li>
                            </ol>

                            Your timely completion is crucial for the continued progress of this research, and we appreciate your attention to this task.
                            Should you have any concerns or need assistance, please don’t hesitate to reach out to the REMC Chief.

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

        public async Task SendOverDueEvaluatorEmail(string email, string researchTitle, string name, string content)
        {
            var subject = "Follow Up: Research Evaluation - " + researchTitle;
            var htmlBody = $@"
                <html>
                    <body style='font-family: Arial, sans-serif;'  font-size: 20px>
                        <br>
                        <div style='margin-bottom: 22px;'>
                            Dear Professor {name},<br><br>
                    
                            Greetings! <br><br>
                            {content}

                        </div
                        <div style='margin-bottom: 22px;'>
                            <strong>Forms to be completed:</strong>
                            <ol>
                                <li><strong>Grading Form</strong>: Includes scoring per criterion and a section for comments and suggestions.</li>
                                <li><strong>Comments Form</strong>: A form for providing detailed comments and suggestions only.</li>
                            </ol>
                        </div>

                        <div style='margin-bottom: 22px;'>

                            Please submit the Grading and Comments forms at your earliest convenience to ensure the research's timely progress. 
                            If you need assistance, feel free to contact the REMC Chief.

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

        public async Task RemindSubmitProgressReport()
        {
            var today = DateTime.Today;
            var fundedResearches = await _context.FundedResearches.ToListAsync();
            foreach(var fundedResearch in fundedResearches)
            {
                int numReports = 4;
                int interval = fundedResearch.project_Duration / numReports;
                var deadlines = Enumerable.Range(1, numReports)
                    .Select(i => fundedResearch.start_Date.AddMonths(i * interval)).ToList();

                List<DateTime> extensionDeadlines = new List<DateTime>();
                if(fundedResearch.isExtension1 || fundedResearch.isExtension2)
                {
                    int extraReports = fundedResearch.isExtension1 ? 1 : 2;
                    int extensionInterval = fundedResearch.project_Duration / numReports;
                    extensionDeadlines = Enumerable.Range(1, extraReports)
                        .Select(i => fundedResearch.end_Date.AddMonths(i * extensionInterval)).ToList();
                }

                var allDeadlines = deadlines.Concat(extensionDeadlines).ToList();
                var statuses = new[]
                {
                    "Ongoing",
                    "Checked Progress Report No.1",
                    "Checked Progress Report No.2",
                    "Checked Progress Report No.3",
                    "Checked Progress Report No.4",
                    "Checked Progress Report No.5"
                };

                for (int i = 0; i < allDeadlines.Count; i++)
                {
                    int daysLeft = (allDeadlines[i] - today).Days;
                    bool remind3DaysBefore = daysLeft == 3 && fundedResearch.status == statuses[i] && !fundedResearch.reminded_ThreeDaysBefore;
                    bool remindDueTomorrow = daysLeft == 1 && fundedResearch.status == statuses[i] && !fundedResearch.reminded_OneDayBefore;
                    bool remindToday = daysLeft == 0 && fundedResearch.status == statuses[i] && !fundedResearch.reminded_Today;
                    bool delaySubmission = daysLeft < 0 && fundedResearch.status == statuses[i];
                    if(remind3DaysBefore)
                    {
                        var content = $"This is a gentle reminder that your progress report for the research titled {fundedResearch.research_Title} is due in 3 days. Please ensure that the report is submitted on or before {allDeadlines[i]:MMMM d, yyyy} to avoid any delays in the research process.";
                        var subContent = $"If you have any questions or require assistance, feel free to contact the REMC Chief";

                        await SendRemindPREmail(
                            fundedResearch.teamLead_Email,
                            fundedResearch.research_Title,
                            fundedResearch.team_Leader,
                            content, subContent);

                        await _actionLogger.LogActionAsync(
                            fundedResearch.team_Leader,
                            fundedResearch.fr_Type,
                            content,
                            true, false, false,
                            fundedResearch.fra_Id);

                        fundedResearch.reminded_ThreeDaysBefore = true;
                        await _context.SaveChangesAsync();
                        break;
                    }
                    else if (remindDueTomorrow)
                    {
                        var content = $"This is a gentle reminder that your progress report for the research titled {fundedResearch.research_Title} is due tomorrow. Please ensure that the report is submitted on or before {allDeadlines[i]:MMMM d, yyyy} to avoid any delays in the research process.";
                        var subContent = $"If you have any questions or require assistance, feel free to contact the REMC Chief";

                        await SendRemindPREmail(
                            fundedResearch.teamLead_Email,
                            fundedResearch.research_Title,
                            fundedResearch.team_Leader,
                            content, subContent);

                        await _actionLogger.LogActionAsync(
                            fundedResearch.team_Leader,
                            fundedResearch.fr_Type,
                            content,
                            true, false, false,
                            fundedResearch.fra_Id);

                        fundedResearch.reminded_OneDayBefore = true;
                        await _context.SaveChangesAsync();
                        break;
                    }
                    else if (remindToday)
                    {
                        var content = $"This is an urgent reminder that your progress report for your research titled {fundedResearch.research_Title} is due today. Please make sure to submit it by the end of the day to avoid any delays.";
                        var subContent = $"We appreciate your immediate attention to this matter. Should you require any assitance, don't hesitate to contact us.";

                        await SendRemindPREmail(
                            fundedResearch.teamLead_Email,
                            fundedResearch.research_Title,
                            fundedResearch.team_Leader,
                            content, subContent);

                        await _actionLogger.LogActionAsync(
                            fundedResearch.team_Leader,
                            fundedResearch.fr_Type,
                            content,
                            true, false, false,
                            fundedResearch.fra_Id);

                        fundedResearch.reminded_Today = true;
                        await _context.SaveChangesAsync();
                        break;
                    }
                    else if (delaySubmission)
                    {
                        if (statuses[i] == "Ongoing")
                        {
                            fundedResearch.status = "Missing Progress Report No.1";
                        }
                        else if (statuses[i] == "Checked Progress Report No.1")
                        {
                            fundedResearch.status = "Missing Progress Report No.2";
                        }
                        else if(statuses[i] == "Checked Progress Report Report No.2")
                        {
                            fundedResearch.status = "Missing Progress Report No.3";
                        }
                        else if (statuses[i] == "Checked Progress Report No.3")
                        {
                            fundedResearch.status = "Missing Progress Report No.4";
                        }
                        else if (statuses[i] == "Checked Progress Report No.4")
                        {
                            fundedResearch.status = "Missing Progress Report No.5";
                        }
                        else if (statuses[i] == "Checked Progress No.5")
                        {
                            fundedResearch.status = "Missing Progress Report No.6";
                        }
                    }

                    int daysPastDeadline = (today - allDeadlines[i]).Days;
                    bool remind1DayAfter = daysPastDeadline == 1 && fundedResearch.status == statuses[i] && !fundedResearch.reminded_OneDayOverdue;
                    bool remind3DaysAfter = daysPastDeadline == 3 && fundedResearch.status == statuses[i] && !fundedResearch.reminded_ThreeDaysOverdue;
                    bool remind7DaysAfter = daysPastDeadline == 7 && fundedResearch.status == statuses[i] && !fundedResearch.reminded_SevenDaysOverdue;
                    if (remind1DayAfter)
                    {
                        var content = $"We noticed that your progress report for the research titled {fundedResearch.research_Title} has not been submitted, and the deadline has been passed. Please submit the report as soon as possible to avoid any further delays in the research process.";
                        var subContent = $"Kindly note that continued delay may result in sanctions from the Research Evaluation and Monitoring Center (REMC), and the University.";

                        await SendOverDuePREmail(
                            fundedResearch.teamLead_Email,
                            fundedResearch.research_Title,
                            fundedResearch.team_Leader,
                            content, subContent);

                        await _actionLogger.LogActionAsync(
                            fundedResearch.team_Leader,
                            fundedResearch.fr_Type,
                            content,
                            true, false, false,
                            fundedResearch.fra_Id);

                        fundedResearch.reminded_OneDayOverdue = true;
                        await _context.SaveChangesAsync();
                        break;
                    }
                    else if (remind3DaysAfter)
                    {
                        var content = $"This is a follow-up regarding the overdue progress report for your research titled {fundedResearch.research_Title}, which is now 3 days late. Please be advised that failure to submit the report promptly may lead to institutional sanctions, which could affect the status of your research project.";
                        var subContent = $"We kindly urge you to submit the report as soon as possible to avoid these penalties.";

                        await SendOverDuePREmail(
                            fundedResearch.teamLead_Email,
                            fundedResearch.research_Title,
                            fundedResearch.team_Leader,
                            content, subContent);

                        await _actionLogger.LogActionAsync(
                            fundedResearch.team_Leader,
                            fundedResearch.fr_Type,
                            content,
                            true, false, false,
                            fundedResearch.fra_Id);

                        fundedResearch.reminded_ThreeDaysOverdue = true;
                        await _context.SaveChangesAsync();
                        break;
                    }
                    else if (remind7DaysAfter)
                    {
                        var content = $"This is the final notice regarding the overdue progress report for your research titled {fundedResearch.research_Title}, which is now 7 days late. In line with University policies, your failure to submit the report on time will lead to sanctions imposed by Research Evaluation and Monitoring Center (REMC).";
                        var subContent = $"Please be advised that further delay may lead to disqualification of your research project and other penalties as the university may deem appropriate, including suspension of project activities, funding withdrawal, or exclusion from future research opportunities. We strongly urge you to submit the report immediately to avoid these actions.";

                        await SendOverDuePREmail(
                            fundedResearch.teamLead_Email,
                            fundedResearch.research_Title,
                            fundedResearch.team_Leader,
                            content, subContent);

                        await _actionLogger.LogActionAsync(
                            fundedResearch.team_Leader,
                            fundedResearch.fr_Type,
                            content,
                            true, false, false,
                            fundedResearch.fra_Id);

                        fundedResearch.reminded_SevenDaysOverdue = true;
                        await _context.SaveChangesAsync();
                        break;
                    }
                }
            }
        }

        public async Task SendRemindPREmail(string email, string researchTitle, string name, string content, string subContent)
        {
            var subject = "Reminder: Progress Report Deadline - " + researchTitle;
            var htmlBody = $@"
                <html>
                    <body style='font-family: Arial, sans-serif;'  font-size: 20px>
                    <br>
                        <div style='margin-bottom: 22px;'>
                            Dear Professor {name},<br><br>
            
                            Greetings! <br><br>
                            {content}

                        </div

                        <div style='margin-bottom: 22px;'>
                            {subContent}
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

        public async Task SendOverDuePREmail(string email, string researchTitle, string name, string content, string subContent)
        {
            var subject = "Progress Report Submission Overdue - " + researchTitle;
            var htmlBody = $@"
                <html>
                    <body style='font-family: Arial, sans-serif;'  font-size: 20px>
                        <br>
                        <div style='margin-bottom: 22px;'>
                            Dear Professor {name},<br><br>
                    
                            Greetings! <br><br>
                            {content}
                        </div

                        <div style='margin-bottom: 22px;'>
                            {subContent}
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

        [Authorize(Roles ="Chief")]
        public IActionResult EthicsClearanceStatus()
        {
            return View();
        }

        [Authorize(Roles = "Chief")]
        public async Task<IActionResult> ChiefResearchEvaluation(string id)
        {
            var fra = await _context.FundedResearchApplication.FindAsync(id);
            if (fra == null)
            {
                return NotFound("Funded Research Application not found!");
            }
            ViewBag.Id = fra.fra_Id;
            ViewBag.DTSNo = fra.dts_No;
            ViewBag.Research = fra.research_Title;
            ViewBag.Field = fra.field_of_Study;
            ViewBag.Lead = fra.applicant_Name;
            ViewBag.Member = fra.team_Members;

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

            var ethics = await _context.FundedResearchEthics
                .FirstOrDefaultAsync(e => e.fra_Id == id);

            var logAction = await _context.ActionLogs.Where(l => l.FraId == fra.fra_Id && l.Action.Contains($"{fra.research_Title} needs to apply/upload ethics clearance.") && l.isTeamLeader == true).FirstOrDefaultAsync();
            if(logAction == null)
            {
                await _actionLogger.LogActionAsync(fra.applicant_Name, fra.fra_Type, $"{fra.research_Title} needs to apply/upload ethics clearance.", true, false, false, fra.fra_Id);
            }

            if(ethics == null || ethics.file_Status == "Pending")
            {
                if(ethics == null)
                {
                    return RedirectToAction("EthicsClearanceStatus");
                }
                else if (ethics.file_Status == "Pending")
                {
                    return RedirectToAction("DocuList", "Chief", new {id = fra.fra_Id});
                }
            }

            var model = new Tuple<List<ViewChiefEvaluationVM>, List<FileRequirement>>
                (evaluationsList, evalFormList);

            return View(model);
        }

        public async Task<IActionResult> Download(string id) // Download Files
        {
            var document = await _context.FileRequirement.FindAsync(id);

            if (document == null)
            {
                return NotFound();
            }
            return File(document.data, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", document.file_Name);
        }

        [HttpPost]
        public async Task<IActionResult> SendResult(string appStatus, string addComment, string fraId)
        {
            var fra = await _context.FundedResearchApplication.FirstOrDefaultAsync(f => f.fra_Id == fraId);
            var user = await _userManager.FindByEmailAsync(fra.applicant_Email);
            if (appStatus == "Approved")
            {
                fra.application_Status = appStatus;
                await _context.SaveChangesAsync();
                await SendApproveEmail(fra.applicant_Email, fra.research_Title, fra.applicant_Name, addComment);
                await _actionLogger.LogActionAsync(fra.applicant_Name, fra.fra_Type, fra.research_Title + " is approved in technical evaluation.", 
                    true, false, false, fra.fra_Id);
            } 
            else if (appStatus == "Rejected")
            {
                fra.application_Status = appStatus;
                await _context.SaveChangesAsync();
                await SendRejectEmail(fra.applicant_Email, fra.research_Title, fra.applicant_Name, addComment);
                await _actionLogger.LogActionAsync(fra.applicant_Name, fra.fra_Type, fra.research_Title + " is rejected in technical evaluation.", 
                    true, false, false, fra.fra_Id);
            }
            return RedirectToAction("UEResearchApp", "Chief");
        }

        public async Task SendApproveEmail(string email, string researchTitle, string name, string comment)
        {
            var subject = "Research Technical Evaluation Results - " + researchTitle;
            var htmlBody = $@"
                <html>
                    <body style='font-family: Arial, sans-serif;'  font-size: 20px>
                        <br>
                        <div style='margin-bottom: 22px;'>
                            Dear Professor {name},<br><br>
                            We are pleased to inform you that after a strict evaluation process, your research titled <strong>{researchTitle}</strong> 
                            has <strong>successfully passed the technical evaluations </strong>of the Research Evaluation Management Center (REMC).                    
                        </div>

                        <div style='margin-bottom: 22px;'>
                            <strong>Next Steps:</strong>
                            <ol>
                                <li><strong>Await Further Approval</strong> – Your research will now proceed to the final phase of approval, 
                                    which includes the approval of both the University Research Ethics Committee (UREC) and the Executive Committee (EXECOMM).</li>
                                <li><strong>Await Notice to Proceed</strong> – Once all final approvals are completed, you will receive a 
                                    formal notice to proceed with the next steps of your research or implementation, if applicable.</li>
                            </ol>
                        </div>

                        <div style='margin - bottom: 22px;'>
                            Please note that <strong> this process may take some time</strong>, and we encourage you to monitor updates via the REMC system or contact
                            the Research Management Office (RMO) if necessary. You may also check the university’s website for any general announcements.<br><br>
                            We congratulate you once again on this significant milestone and wish you continued success in your research journey.
                            <br><br>
                            Additional Chief Comments and Suggestions: <br>{comment}
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

        public async Task SendRejectEmail(string email, string researchTitle, string name, string comment)
        {
            var subject = "Research Technical Evaluation Results - " + researchTitle;
            var htmlBody = $@"
                <html>
                    <body style='font-family: Arial, sans-serif;'  font-size: 20px>
                        <br>
                        <div style='margin-bottom: 22px;'>
                            Dear Professor {name},<br><br>
                            We regret to inform you that after a strict evaluation process, your research titled <strong>{researchTitle}</strong> 
                            has not met the required standards of the Research Evaluation and Monitoring Center (REMC), and <strong> has been rejected for funding </strong> at this time. <br><br>
                            We understand that this news may be disappointing, and we appreciate the time and effort you put into your submission. Please know that this decision was not made lightly.
                            We encourage you to review the feedback provided on our website for more specific guidance on how to strengthen your future proposals.
                        </div>

                        <div style='margin-bottom: 22px;'>
                            <strong>What You Can Do:</strong>
                            <ol>
                                <li><strong>Revisions and Resubmission</strong> –  If you wish to continue pursuing this research, we encourage you to carefully review the evaluators' 
                                    feedback and consider revising your work. You may reapply in the next evaluation cycle after making the necessary improvements.</li>
                                <li><strong>Contact the Chief</strong> – Should you require clarification regarding the evaluation results, 
                                    or if you believe there were errors in the process, you may contact the Research Management Office (RMO) for assistance.</li>
                            </ol><br>

                             We encourage you to persevere and continue refining your research, as each step in this process brings valuable learning opportunities.
                            <br><br>
                            Additional Chief Comments and Suggestions: <br> {comment}
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

        [Authorize (Roles = "Chief")]
        public async Task<IActionResult> UploadNTP(string searchString)
        {
            ViewData["currentFilter"] = searchString;
            var appQuery = _context.FundedResearchApplication
                .Where(f => new[] {"University Funded Research", "Externally Funded Research", "University Funded Research Load"}.Contains(f.fra_Type));

            if (!String.IsNullOrEmpty(searchString))
            {
                appQuery = appQuery.Where(s => s.research_Title.Contains(searchString));
            }

            var researchAppList = await appQuery
                .Where(f => f.application_Status == "Approved")
                .Include(f => f.FundedResearchEthics)
                .OrderBy(f => f.submission_Date)
                .ToListAsync();

            return View(researchAppList);
        }

        [HttpPost]
        public async Task<IActionResult> UploadNotice(IFormFile file, string fraId)
        {
            var fra = await _context.FundedResearchApplication.FindAsync(fraId);
            if(fra == null)
            {
                return NotFound();
            }
            if(file == null || file.Length == 0)
            {
                return NotFound("There is no uploaded file. Please upload the file and try to submit again.");
            }

            byte[] pdfData;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                pdfData = ms.ToArray();
                var fileRequirement = new FileRequirement
                {
                    fr_Id = Guid.NewGuid().ToString(),
                    file_Name = file.FileName,
                    file_Type = Path.GetExtension(file.FileName),
                    data = pdfData,
                    file_Status = "Proceed",
                    document_Type = "Notice to Proceed",
                    file_Feedback = null,
                    file_Uploaded = DateTime.Now,
                    fra_Id = fra.fra_Id
                };
                _context.FileRequirement.Add(fileRequirement);
            }
            fra.application_Status = "Proceed";
            await _context.SaveChangesAsync();
            await _actionLogger.LogActionAsync(fra.applicant_Name, fra.fra_Type, fra.research_Title + "'s Notice to proceed already uploaded.", 
                true, false, false, fra.fra_Id);
            await SendProceedEmail(fra.applicant_Email, fra.research_Title, fra.applicant_Name);

            return RedirectToAction("UploadNTP", "Chief");
        }

        [HttpPost]
        public async Task<IActionResult> GotoDocuList(string id)
        {
            if(id == null)
            {
                return NotFound("Funded Research Application ID not found!");
            }

            var ethics = await _context.FundedResearchEthics
                .FirstOrDefaultAsync(e => e.fra_Id == id);

            if (ethics == null)
            {
                return RedirectToAction("EthicsClearanceStatus");
            }

            return RedirectToAction("DocuList", "Chief", new { id = id });
        }

        public async Task SendProceedEmail(string email, string researchTitle, string name)
        {
            var subject = "Notice to Proceed - " + researchTitle;
            var htmlBody = $@"
                <html>
                    <body style='font-family: Arial, sans-serif;'  font-size: 20px>
                        <br>
                        <div style='margin-bottom: 22px;'>
                            Dear Professor {name},<br><br>

                            We are pleased to inform you that the <strong> Notice to Proceed (NTP) </strong> for your research titled <strong>{researchTitle}</strong> has been successfully
                            uploaded to the system. You may now <strong>download the NTP</strong> for your records at {{websiteLink}} and proceed with the next steps in the process.

                        </div>

                        <div style='margin-bottom: 22px;'>
                            <strong>Next Steps:</strong>
                            <ol>
                                <li><strong>Project Commencement</strong>: With the issuance of the NTP, your project is now officially authorized to commence.</li>
                                <li><strong>Progress Monitoring</strong>: Please proceed with the project implementation and report your project's progress
                                    quarterly through the system's Project Progress Monitoring.</li>
                            </ol><br>

                            Should there be any need for modification of the approved research project, kindly note that it must be submitted for approval by the UREC, especially if the NTP has already been released.

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

        [Authorize(Roles ="Chief")]
        public async Task<IActionResult> ArchivedResearch(string searchString)
        {
            ViewData["currentFilter"] = searchString;
            var appQuery = _context.FundedResearchApplication
                .Where(f => new[] { "University Funded Research", "Externally Funded Research", "University Funded Research Load" }.Contains(f.fra_Type));

            if (!String.IsNullOrEmpty(searchString))
            {
                appQuery = appQuery.Where(s => s.research_Title.Contains(searchString));
            }

            var researchAppList = await appQuery
                .Include(f => f.FundedResearch)
                .Where(f => f.isArchive == true)
                .OrderByDescending(f => f.submission_Date)
                .ToListAsync();

            return View(researchAppList);
        }

        [Authorize(Roles = "Chief")]
        public async Task<IActionResult> RequirementList(string id) // Documentary Requirements per Application in Archived Research
        {
            if (id == null)
            {
                return NotFound("No Application ID found!");
            }

            var fra = await _context.FundedResearchApplication.FindAsync(id);
            if (fra == null)
            {
                return NotFound("No Application found!");
            }
            ViewBag.FraId = fra.fra_Id;
            ViewBag.DTSNo = fra.dts_No;
            ViewBag.Research = fra.research_Title;
            ViewBag.Field = fra.field_of_Study;
            ViewBag.Lead = fra.applicant_Name;
            ViewBag.Member = fra.team_Members;
            ViewBag.Type = fra.fra_Type;
            ViewBag.Status = fra.application_Status;

            var fileRequirement = await _context.FileRequirement.Where(f => f.fra_Id == id && f.file_Type == ".pdf")
                .OrderBy(fr => fr.file_Name)
                .ToListAsync();
            return View(fileRequirement);
        }

        [Authorize(Roles = "Chief")]
        public async Task<IActionResult> ChiefEvaluationResult(string id)
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

        [Authorize(Roles ="Chief")]
        public async Task<IActionResult> UniversityFundedResearch(string searchString)
        {
            ViewData["currentFilter"] = searchString;
            var appQuery = _context.FundedResearches
                .Where(f => f.fr_Type == "University Funded Research");

            if (!String.IsNullOrEmpty(searchString))
            {
                appQuery = appQuery.Where(s => s.research_Title.Contains(searchString));
            }

            if(appQuery != null)
            {
                var fundedResearch = await appQuery
                    .Where(f => f.isArchive == false)
                    .OrderByDescending(f => f.start_Date).ToListAsync();

                return View(fundedResearch);
            }

            return View(new List<FundedResearch>());
        }

        [Authorize(Roles ="Chief")]
        public async Task<IActionResult> ExternallyFundedResearch(string searchString)
        {
            ViewData["currentFilter"] = searchString;
            var appQuery = _context.FundedResearches
                .Where(f => f.fr_Type == "Externally Funded Research");

            if(!String.IsNullOrEmpty(searchString))
            {
                appQuery = appQuery.Where(s => s.research_Title.Contains(searchString));
            }

            if(appQuery != null)
            {
                var fundedResearch = await appQuery
                    .Where(f => f.isArchive == false)
                    .OrderByDescending(f => f.start_Date).ToListAsync();

                return View(fundedResearch);
            }
            return View(new List<FundedResearch>());
        }

        [Authorize(Roles ="Chief")]
        public async Task<IActionResult> UniversityFundedResearchLoad(string searchString)
        {
            ViewData["currentFilter"] = searchString;
            var appQuery = _context.FundedResearches
                .Where(f => f.fr_Type == "University Funded Research Load");

            if(!String.IsNullOrEmpty(searchString))
            {
                appQuery = appQuery.Where(s => s.research_Title.Contains(searchString));
            }

            if(appQuery != null)
            {
                var fundedResearch = await appQuery
                    .Where(f => f.isArchive == false)
                    .OrderByDescending(f => f.start_Date).ToListAsync();

                return View(fundedResearch);
            }
            return View(new List<FundedResearch>());
        }

        [Authorize(Roles ="Chief")]
        public async Task<IActionResult> ProgressReportList(string id)
        {
            if(id == null)
            {
                return NotFound();
            }

            var fr = await _context.FundedResearches.FindAsync(id);
            if(fr == null)
            {
                return NotFound("No currently Progress Report/s uploaded.");
            }

            ViewBag.FrId = fr.fr_Id;
            ViewBag.Research = fr.research_Title;
            ViewBag.Field = fr.field_of_Study;
            ViewBag.Lead = fr.team_Leader;
            ViewBag.Member = fr.team_Members;
            ViewBag.Type = fr.fr_Type;
            ViewBag.Extend1 = fr.isExtension1;
            ViewBag.Extend2 = fr.isExtension2;
            ViewBag.Status = fr.status;

            var progReport = await _context.ProgressReports
                .Where(pr => pr.fr_Id == id)
                .OrderBy(pr => pr.file_Uploaded)
                .ToListAsync();
            return View(progReport);
        }

        [HttpPost]
        public async Task<IActionResult> SaveProgressFeedback(string fileId, string fileStatus, string fileFeedback)
        {
            var progReport = _context.ProgressReports.Find(fileId);
            var fr = await _context.FundedResearches.Where(f => f.fr_Id == progReport.fr_Id).FirstOrDefaultAsync();
            if (progReport != null)
            {
                progReport.file_Status = fileStatus;
                progReport.file_Feedback = fileFeedback;

                _context.SaveChanges();
                await _actionLogger.LogActionAsync(fr.team_Leader, fr.fr_Type, "You need to resubmit this " + progReport.file_Name + ". " +
                    "Kindly check the feedback for the changes.", true, false, false, fr.fra_Id);
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePRStatus(string pr_Id, string newStatus)
        {
            var file = _context.ProgressReports.FirstOrDefault(f => f.pr_Id == pr_Id);
            if(file == null)
            {
                return Json(new { success = false, message = "Report not found" });
            }

            var fr = await _context.FundedResearches.Where(fr => fr.fr_Id == file.fr_Id).FirstOrDefaultAsync();
            if(fr == null)
            {
                return Json(new { success = false, message = "Funded Research not found" });
            }

            file.file_Status = newStatus;

            if(file.document_Type == "Terminal Report")
            {
                fr.status = "Checked Terminal Report";
            }
            else if(file.document_Type == "Liquidation Report")
            {
                fr.status = "Checked Liquidation Report";
            }
            else if(file.document_Type == "Progress Report")
            {
                var existingReports = await _context.ProgressReports
                .Where(pr => pr.fr_Id == file.fr_Id)
                .ToListAsync();

                int reportNum = existingReports.Count + 0;
                string docuType = $"Progress Report No.{reportNum}";

                fr.status = $"Checked {docuType}";
            }
            _context.SaveChanges();
            await _actionLogger.LogActionAsync(fr.team_Leader, fr.fr_Type, file.file_Name + " already checked by the Chief.",
                true, false, false, fr.fra_Id);

            var allProgressReportChecked = _context.ProgressReports
                .Where(fr => fr.fr_Id == file.fr_Id && fr.document_Type == "Progress Report")
                .All(fr => fr.file_Status == "Checked");

            var terminalReportExist = _context.ProgressReports.Any(fr => fr.fr_Id == file.fr_Id
                && fr.document_Type == "Terminal Report" && fr.file_Status == "Checked");

            var liquidationReportExist = _context.ProgressReports.Any(fr => fr.fr_Id == file.fr_Id
                && fr.document_Type == "Liquidation Report" && fr.file_Status == "Checked");

            if (allProgressReportChecked && terminalReportExist && liquidationReportExist)
            {
                var template = _context.Guidelines
                    .First(g => g.document_Type == "CertificateCompletion" && g.file_Type == ".docx");
                string filledFolder = Path.Combine(_webHostEnvironment.WebRootPath, "content", "ceOutput");
                Directory.CreateDirectory(filledFolder);

                using (var templateStream = new MemoryStream(template.data))
                {
                    string filledDocumentPath = Path.Combine(filledFolder, $"Generated_{template.file_Name}");
                    using (DocX document = DocX.Load(templateStream))
                    {
                        document.ReplaceText("{{ResearchTitle}}", fr.research_Title);
                        document.ReplaceText("{{FundedType}}", fr.fr_Type);
                        document.ReplaceText("{{DateToday}}", DateTime.Now.ToString("MMMM d, yyyy"));

                        document.SaveAs(filledDocumentPath);
                    }

                    byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filledDocumentPath);
                    var doc = new ProgressReport
                    {
                        pr_Id = Guid.NewGuid().ToString(),
                        file_Name = "Certificate-of-Completion.docx",
                        file_Type = ".docx",
                        data = fileBytes,
                        file_Status = "Checked",
                        document_Type = "Certificate of Completion",
                        file_Feedback = null,
                        file_Uploaded = DateTime.Now,
                        fr_Id = fr.fr_Id
                    };
                    _context.ProgressReports.Add(doc);
                }

                var ufrProject = new UFRForecasting
                {
                    ProjectCosts = Convert.ToSingle(fr.total_project_Cost),
                    Year = DateTime.Now.Year
                };
                _context.UFRForecastings.Add(ufrProject);

                fr.status = "Completed";
                fr.end_Date = DateTime.Now;
                Directory.Delete(filledFolder, true);
                await _context.SaveChangesAsync();
                await _actionLogger.LogActionAsync(fr.team_Leader, fr.fr_Type, fr.research_Title + " already uploaded the Certificate of Completion. Congratulations!",
                    true, false, false, fr.fra_Id);
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ExtendForm1(string frId)
        {
            var fr = await _context.FundedResearches.FindAsync(frId);
            if(fr == null)
            {
                return NotFound();
            }

            fr.isExtension1 = true;
            await _context.SaveChangesAsync();
            await _actionLogger.LogActionAsync(fr.team_Leader, fr.fr_Type, "Your request for extension has been granted.", true, false, false, fr.fra_Id);
            return RedirectToAction("ProgressReportList", "Chief", new { id = frId });
        }

        [HttpPost]
        public async Task<IActionResult> ExtendForm2(string frId)
        {
            var fr = await _context.FundedResearches.FindAsync(frId);
            if(fr == null)
            {
                return NotFound();
            }

            fr.isExtension2 = true;
            await _context.SaveChangesAsync();
            await _actionLogger.LogActionAsync(fr.team_Leader, fr.fr_Type, "Your request for extension has been granted.", true, false, false, fr.fra_Id);
            return RedirectToAction("ProgressReportList", "Chief", new {id = frId});
        }

        [Authorize(Roles ="Chief")]
        public async Task<IActionResult> GenerateReport()
        {
            var recentReports = await _context.GenerateReports
                .OrderByDescending(r => r.generateDate)
                .Take(10)
                .ToListAsync();

            return View(recentReports);
        }

        [Authorize(Roles ="Chief")]
        public async Task<IActionResult> ArchivedReport(string searchString)
        {
            ViewData["currentFilter"] = searchString;
            var genReport = _context.GenerateReports
                .Where(f => f.isArchived == true);

            if (!String.IsNullOrEmpty(searchString))
            {
                genReport = genReport.Where(s => s.gr_typeofReport.Contains(searchString));
            }

            var genReports = await genReport
                .OrderByDescending(r => r.generateDate)
                .ToListAsync();

            return View(genReports);
        }

        public async Task<IActionResult> GenerateReports(string reportType, DateTime startDate, DateTime endDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if(reportType == "Application")
            {
                var application = await _context.FundedResearchApplication
                    .Where(f => new[] {"Submitted", "UnderEvaluation","Approved"}.Contains(f.application_Status)
                        && DateOnly.FromDateTime(f.submission_Date) >= DateOnly.FromDateTime(startDate)
                        && DateOnly.FromDateTime(f.submission_Date) <= DateOnly.FromDateTime(endDate))
                    .OrderBy(f => f.fra_Type)
                    .ToListAsync();

                if (application == null || !application.Any())
                {
                    return NotFound("No data found for the selected report type and date range.");
                }

                // Generate Excel report using EPPlus
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("FundedResearchApplications");

                    // Add headers
                    worksheet.Cells[1, 1].Value = "DTS Number";
                    worksheet.Cells[1, 2].Value = "Colleges/Branches";
                    worksheet.Cells[1, 3].Value = "Research Title";
                    worksheet.Cells[1, 4].Value = "Funded Research Type";
                    worksheet.Cells[1, 5].Value = "Proponent/s";
                    worksheet.Cells[1, 6].Value = "Nature of Involvement";
                    worksheet.Cells[1, 7].Value = "Status";
                    worksheet.Cells[1, 8].Value = "Amount of Funding";
                    worksheet.Cells[1, 9].Value = "Date of Submission";

                    // Add data to cells
                    int row = 2;
                    foreach (var item in application)
                    {
                        var teamMembers = item.team_Members.Contains("N/A") ? string.Empty : "/" + string.Join("/", item.team_Members);
                        var involvement = item.team_Members.Contains("N/A") ? string.Empty : "/" + string.Join("/", item.team_Members.Select(_ => "Co-Lead"));

                        worksheet.Cells[row, 1].Value = item.dts_No;
                        worksheet.Cells[row, 2].Value = item.college + "/" + item.branch;
                        worksheet.Cells[row, 3].Value = item.research_Title;
                        worksheet.Cells[row, 4].Value = item.fra_Type;
                        worksheet.Cells[row, 5].Value = item.applicant_Name + teamMembers;
                        worksheet.Cells[row, 6].Value = "Lead" + involvement;
                        worksheet.Cells[row, 7].Value = item.application_Status;
                        worksheet.Cells[row, 8].Value = "Php" + (item.total_project_Cost.HasValue ? item.total_project_Cost.Value.ToString("N0") : "0");
                        worksheet.Cells[row, 9].Value = item.submission_Date.ToString("MMMM d, yyyy");
                        row++;
                    }

                    worksheet.Cells["A1:I1"].Style.Font.Bold = true;
                    worksheet.Cells.AutoFitColumns();

                    // Convert Excel package to a byte array
                    var excelData = package.GetAsByteArray();
                    var s = startDate.ToString("MMddyyyy");
                    var e = endDate.ToString("MMddyyyy");
                    var genRep = new GenerateReport
                    {
                        gr_Id = Guid.NewGuid().ToString(),
                        gr_fileName = $"FRAReport{s}-{e}.xlsx",
                        gr_fileType = ".xlsx",
                        gr_Data = excelData,
                        gr_startDate = startDate,
                        gr_endDate = endDate,
                        gr_typeofReport = "Funded Research Applications",
                        generateDate = DateTime.Now,
                        UserId = user.Id,
                        isArchived = false
                    };
                    _context.GenerateReports.Add(genRep);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("GenerateReport");
                }
            }
            else if(reportType == "Evaluator")
            {
                var evaluators = await _context.Evaluator
                    .Include(e => e.Evaluations)
                    .ToListAsync();

                if (evaluators == null || !evaluators.Any())
                {
                    return NotFound("No data found for the selected report type and date range.");
                }

                // Generate Excel report using EPPlus
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("EvaluatorsReport");

                    // Add headers
                    worksheet.Cells[1, 1].Value = "Evaluator Name";
                    worksheet.Cells[1, 2].Value = "Evaluator Email";
                    worksheet.Cells[1, 3].Value = "Field of Interest";
                    worksheet.Cells[1, 4].Value = "Pending";
                    worksheet.Cells[1, 5].Value = "Completed";
                    worksheet.Cells[1, 6].Value = "Declined";

                    // Add data to cells
                    int row = 2;
                    foreach (var item in evaluators)
                    {
                        var interestFields = item.field_of_Interest.Contains("N/A") ? string.Empty : string.Join(" / ", item.field_of_Interest);
                        int pending = item.Evaluations.Where(e => e.evaluation_Status == "Pending").Count();
                        int completed = item.Evaluations.Where(e => new[] {"Approved", "Rejected"}.Contains(e.evaluation_Status)).Count();
                        int declined = item.Evaluations.Where(e => e.evaluation_Status == "Decline").Count();
                        

                        worksheet.Cells[row, 1].Value = item.evaluator_Name;
                        worksheet.Cells[row, 2].Value = item.evaluator_Email;
                        worksheet.Cells[row, 3].Value = interestFields;
                        worksheet.Cells[row, 4].Value = pending;
                        worksheet.Cells[row, 5].Value = completed;
                        worksheet.Cells[row, 6].Value = declined;
                        row++;
                    }

                    worksheet.Cells["A1:F1"].Style.Font.Bold = true;
                    worksheet.Cells.AutoFitColumns();

                    // Convert Excel package to a byte array
                    var excelData = package.GetAsByteArray();
                    var date = DateTime.Now.ToString("MMddyyyy:HHmmss");
                    var genRep = new GenerateReport
                    {
                        gr_Id = Guid.NewGuid().ToString(),
                        gr_fileName = $"UFREvaluatorsReport{date}.xlsx",
                        gr_fileType = ".xlsx",
                        gr_Data = excelData,
                        gr_startDate = DateTime.Now,
                        gr_endDate = DateTime.Now,
                        gr_typeofReport = "UFR - Evaluators",
                        generateDate = DateTime.Now,
                        UserId = user.Id,
                        isArchived = false
                    };
                    _context.GenerateReports.Add(genRep);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("GenerateReport");
                }

            }
            else if(reportType == "OngoingUFR")
            {
                var ongoingUFR = await _context.FundedResearches
                    .Where(f => f.fr_Type == "University Funded Research" && f.status != "Completed"
                       && DateOnly.FromDateTime(f.start_Date) >= DateOnly.FromDateTime(startDate)
                       && DateOnly.FromDateTime(f.start_Date) <= DateOnly.FromDateTime(endDate))
                    .OrderBy(f => f.start_Date)
                    .ToListAsync();

                if(ongoingUFR == null || !ongoingUFR.Any())
                {
                    return NotFound("No data found for the selected report type and date range.");
                }

                // Generate Excel report using EPPlus
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("OngoingUFR");

                    // Add headers
                    worksheet.Cells[1, 1].Value = "Research Work Number";
                    worksheet.Cells[1, 2].Value = "Colleges/Branches";
                    worksheet.Cells[1, 3].Value = "Research Title";
                    worksheet.Cells[1, 4].Value = "Proponent/s";
                    worksheet.Cells[1, 5].Value = "Nature of Involvement";
                    worksheet.Cells[1, 6].Value = "Status";
                    worksheet.Cells[1, 7].Value = "Amount of Funding";
                    worksheet.Cells[1, 8].Value = "Started Date";
                    worksheet.Cells[1, 9].Value = "Projected End Date";

                    // Add data to cells
                    int row = 2;
                    foreach (var item in ongoingUFR)
                    {
                        var teamMembers = item.team_Members.Contains("N/A") ? string.Empty : "/" + string.Join("/", item.team_Members);
                        var involvement = item.team_Members.Contains("N/A") ? string.Empty : "/" + string.Join("/", item.team_Members.Select(_ => "Co-Lead"));

                        worksheet.Cells[row, 1].Value = item.fr_Id;
                        worksheet.Cells[row, 2].Value = item.college + "/" + item.branch;
                        worksheet.Cells[row, 3].Value = item.research_Title;
                        worksheet.Cells[row, 4].Value = item.team_Leader + teamMembers;
                        worksheet.Cells[row, 5].Value = "Lead" + involvement;
                        worksheet.Cells[row, 6].Value = item.status;
                        worksheet.Cells[row, 7].Value = "Php" + (item.total_project_Cost.HasValue ? item.total_project_Cost.Value.ToString("N0") : "0");
                        worksheet.Cells[row, 8].Value = item.start_Date.ToString("MMMM d, yyyy");
                        worksheet.Cells[row, 9].Value = item.end_Date.ToString("MMMM d, yyyy");
                        row++;
                    }

                    worksheet.Cells["A1:I1"].Style.Font.Bold = true;
                    worksheet.Cells.AutoFitColumns();

                    // Convert Excel package to a byte array
                    var excelData = package.GetAsByteArray();
                    var s = startDate.ToString("MMddyyyy");
                    var e = endDate.ToString("MMddyyyy");
                    var genRep = new GenerateReport
                    {
                        gr_Id = Guid.NewGuid().ToString(),
                        gr_fileName = $"OngoingUFRReport{s}-{e}.xlsx",
                        gr_fileType = ".xlsx",
                        gr_Data = excelData,
                        gr_startDate = startDate,
                        gr_endDate = endDate,
                        gr_typeofReport = "Ongoing University Funded Research",
                        generateDate = DateTime.Now,
                        UserId = user.Id,
                        isArchived = false
                    };
                    _context.GenerateReports.Add(genRep);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("GenerateReport");
                }
            }
            else if(reportType == "OngoingEFR")
            {
                var ongoingEFR = await _context.FundedResearches
                    .Where(f => f.fr_Type == "Externally Funded Research" && f.status != "Completed"
                       && DateOnly.FromDateTime(f.start_Date) >= DateOnly.FromDateTime(startDate)
                       && DateOnly.FromDateTime(f.start_Date) <= DateOnly.FromDateTime(endDate))
                    .OrderBy(f => f.start_Date)
                    .ToListAsync();

                if (ongoingEFR == null || !ongoingEFR.Any())
                {
                    return NotFound("No data found for the selected report type and date range.");
                }

                // Generate Excel report using EPPlus
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("OngoingEFR");

                    // Add headers
                    worksheet.Cells[1, 1].Value = "Research Work Number";
                    worksheet.Cells[1, 2].Value = "Colleges/Branches";
                    worksheet.Cells[1, 3].Value = "Research Title";
                    worksheet.Cells[1, 4].Value = "Proponent/s";
                    worksheet.Cells[1, 5].Value = "Nature of Involvement";
                    worksheet.Cells[1, 6].Value = "Status";
                    worksheet.Cells[1, 7].Value = "Amount of Funding";
                    worksheet.Cells[1, 8].Value = "Started Date";
                    worksheet.Cells[1, 9].Value = "Projected End Date";

                    // Add data to cells
                    int row = 2;
                    foreach (var item in ongoingEFR)
                    {
                        var teamMembers = item.team_Members.Contains("N/A") ? string.Empty : "/" + string.Join("/", item.team_Members);
                        var involvement = item.team_Members.Contains("N/A") ? string.Empty : "/" + string.Join("/", item.team_Members.Select(_ => "Co-Lead"));

                        worksheet.Cells[row, 1].Value = item.fr_Id;
                        worksheet.Cells[row, 2].Value = item.college + "/" + item.branch;
                        worksheet.Cells[row, 3].Value = item.research_Title;
                        worksheet.Cells[row, 4].Value = item.team_Leader + teamMembers;
                        worksheet.Cells[row, 5].Value = "Lead" + involvement;
                        worksheet.Cells[row, 6].Value = item.status;
                        worksheet.Cells[row, 7].Value = "Php" + (item.total_project_Cost.HasValue ? item.total_project_Cost.Value.ToString("N0") : "0");
                        worksheet.Cells[row, 8].Value = item.start_Date.ToString("MMMM d, yyyy");
                        worksheet.Cells[row, 9].Value = item.end_Date.ToString("MMMM d, yyyy");
                        row++;
                    }

                    worksheet.Cells["A1:I1"].Style.Font.Bold = true;
                    worksheet.Cells.AutoFitColumns();

                    // Convert Excel package to a byte array
                    var excelData = package.GetAsByteArray();
                    var s = startDate.ToString("MMddyyyy");
                    var e = endDate.ToString("MMddyyyy");
                    var genRep = new GenerateReport
                    {
                        gr_Id = Guid.NewGuid().ToString(),
                        gr_fileName = $"OngoingEFRReport{s}-{e}.xlsx",
                        gr_fileType = ".xlsx",
                        gr_Data = excelData,
                        gr_startDate = startDate,
                        gr_endDate = endDate,
                        gr_typeofReport = "Ongoing Externally Funded Research",
                        generateDate = DateTime.Now,
                        UserId = user.Id,
                        isArchived = false
                    };
                    _context.GenerateReports.Add(genRep);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("GenerateReport");
                }
            }
            else if(reportType == "OngoingUFRL")
            {
                var ongoingUFRL = await _context.FundedResearches
                    .Where(f => f.fr_Type == "University Funded Research Load" && f.status != "Completed"
                       && DateOnly.FromDateTime(f.start_Date) >= DateOnly.FromDateTime(startDate)
                       && DateOnly.FromDateTime(f.start_Date) <= DateOnly.FromDateTime(endDate))
                    .OrderBy(f => f.start_Date)
                    .ToListAsync();

                if (ongoingUFRL == null || !ongoingUFRL.Any())
                {
                    return NotFound("No data found for the selected report type and date range.");
                }

                // Generate Excel report using EPPlus
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("OngoingUFRL");

                    // Add headers
                    worksheet.Cells[1, 1].Value = "Research Work Number";
                    worksheet.Cells[1, 2].Value = "Colleges/Branches";
                    worksheet.Cells[1, 3].Value = "Research Title";
                    worksheet.Cells[1, 4].Value = "Proponent/s";
                    worksheet.Cells[1, 5].Value = "Nature of Involvement";
                    worksheet.Cells[1, 6].Value = "Status";
                    worksheet.Cells[1, 7].Value = "Amount of Funding";
                    worksheet.Cells[1, 8].Value = "Started Date";
                    worksheet.Cells[1, 9].Value = "Projected End Date";

                    // Add data to cells
                    int row = 2;
                    foreach (var item in ongoingUFRL)
                    {
                        var teamMembers = item.team_Members.Contains("N/A") ? string.Empty : "/" + string.Join("/", item.team_Members);
                        var involvement = item.team_Members.Contains("N/A") ? string.Empty : "/" + string.Join("/", item.team_Members.Select(_ => "Co-Lead"));

                        worksheet.Cells[row, 1].Value = item.fr_Id;
                        worksheet.Cells[row, 2].Value = item.college + "/" + item.branch;
                        worksheet.Cells[row, 3].Value = item.research_Title;
                        worksheet.Cells[row, 4].Value = item.team_Leader + teamMembers;
                        worksheet.Cells[row, 5].Value = "Lead" + involvement;
                        worksheet.Cells[row, 6].Value = item.status;
                        worksheet.Cells[row, 7].Value = "Php" + (item.total_project_Cost.HasValue ? item.total_project_Cost.Value.ToString("N0") : "0");
                        worksheet.Cells[row, 8].Value = item.start_Date.ToString("MMMM d, yyyy");
                        worksheet.Cells[row, 9].Value = item.end_Date.ToString("MMMM d, yyyy");
                        row++;
                    }

                    worksheet.Cells["A1:I1"].Style.Font.Bold = true;
                    worksheet.Cells.AutoFitColumns();

                    // Convert Excel package to a byte array
                    var excelData = package.GetAsByteArray();

                    var s = startDate.ToString("MMddyyyy");
                    var e = endDate.ToString("MMddyyyy");
                    var genRep = new GenerateReport
                    {
                        gr_Id = Guid.NewGuid().ToString(),
                        gr_fileName = $"OngoingUFRLReport{s}-{e}.xlsx",
                        gr_fileType = ".xlsx",
                        gr_Data = excelData,
                        gr_startDate = startDate,
                        gr_endDate = endDate,
                        gr_typeofReport = "Ongoing University Funded Research Load",
                        generateDate = DateTime.Now,
                        UserId = user.Id,
                        isArchived = false
                    };
                    _context.GenerateReports.Add(genRep);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("GenerateReport");
                }
            }
            else if(reportType == "ResearchProduction")
            {
                // Retrieve data from the database based on reportType, startDate, and endDate
                var reportData = await _context.FundedResearches
                    .Where(f => f.status == "Completed" && DateOnly.FromDateTime(f.end_Date) >= DateOnly.FromDateTime(startDate)
                        && DateOnly.FromDateTime(f.end_Date) <= DateOnly.FromDateTime(endDate))
                    .OrderBy(f => f.fr_Type)
                    .ToListAsync();

                if (reportData == null || !reportData.Any())
                {
                    return NotFound("No data found for the selected report type and date range.");
                }

                // Generate Excel report using EPPlus
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("ResearchProduction");

                    // Add headers
                    worksheet.Cells[1, 1].Value = "Research Work Number";
                    worksheet.Cells[1, 2].Value = "Colleges/Branches";
                    worksheet.Cells[1, 3].Value = "Research Title";
                    worksheet.Cells[1, 4].Value = "Proponent/s";
                    worksheet.Cells[1, 5].Value = "Nature of Involvement";
                    worksheet.Cells[1, 6].Value = "Funded Type";
                    worksheet.Cells[1, 7].Value = "Amount of Funding";
                    worksheet.Cells[1, 8].Value = "Started Date";
                    worksheet.Cells[1, 9].Value = "Completed Date";

                    // Add data to cells
                    int row = 2;
                    foreach (var item in reportData)
                    {
                        var teamMembers = item.team_Members.Contains("N/A") ? string.Empty : "/" + string.Join("/", item.team_Members);
                        var involvement = item.team_Members.Contains("N/A") ? string.Empty : "/" + string.Join("/", item.team_Members.Select(_ => "Co-Lead"));

                        worksheet.Cells[row, 1].Value = item.fr_Id;
                        worksheet.Cells[row, 2].Value = item.college + "/" + item.branch;
                        worksheet.Cells[row, 3].Value = item.research_Title;
                        worksheet.Cells[row, 4].Value = item.team_Leader + teamMembers;
                        worksheet.Cells[row, 5].Value = "Lead" + involvement;
                        worksheet.Cells[row, 6].Value = item.fr_Type;
                        worksheet.Cells[row, 7].Value = "Php" + (item.total_project_Cost.HasValue ? item.total_project_Cost.Value.ToString("N0") : "0");
                        worksheet.Cells[row, 8].Value = item.start_Date.ToString("MMMM d, yyyy");
                        worksheet.Cells[row, 9].Value = item.end_Date.ToString("MMMM d, yyyy");
                        row++;
                    }

                    worksheet.Cells["A1:I1"].Style.Font.Bold = true;
                    worksheet.Cells.AutoFitColumns();

                    // Convert Excel package to a byte array
                    var excelData = package.GetAsByteArray();

                    var s = startDate.ToString("MMddyyyy");
                    var e = endDate.ToString("MMddyyyy");
                    var genRep = new GenerateReport
                    {
                        gr_Id = Guid.NewGuid().ToString(),
                        gr_fileName = $"ResearchProductionReport{s}-{e}.xlsx",
                        gr_fileType = ".xlsx",
                        gr_Data = excelData,
                        gr_startDate = startDate,
                        gr_endDate = endDate,
                        gr_typeofReport = "Research Production",
                        generateDate = DateTime.Now,
                        UserId = user.Id,
                        isArchived = false
                    };
                    _context.GenerateReports.Add(genRep);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("GenerateReport");
                }
            }
            else if (reportType == "ForecastedUFRFunds")
            {
                var yearlyCosts = _context.UFRForecastings
                    .GroupBy(d => d.Year)
                    .Select(g => new UFRForecasting { Year = g.Key, ProjectCosts = g.Sum(x => x.ProjectCosts) })
                    .OrderBy(x => x.Year)
                    .ToList();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Forecast");

                worksheet.Cells[1, 1].Value = "Year";
                worksheet.Cells[1, 2].Value = "Total University Funded Research Cost";
                worksheet.Cells[1, 3].Value = "Forecasted Year 1";
                worksheet.Cells[1, 4].Value = "Forecasted Fund of Year 1";
                worksheet.Cells[1, 5].Value = "Forecasted Year 2";
                worksheet.Cells[1, 6].Value = "Forecasted Fund of Year 2";

                int row = 2;
                foreach (var item in yearlyCosts)
                {
                    worksheet.Cells[row, 1].Value = item.Year;
                    worksheet.Cells[row, 2].Value = item.ProjectCosts;

                    if (yearlyCosts.Where(y => y.Year <= item.Year).Count() >= 5)
                    {
                        var currentData = yearlyCosts.Where(y => y.Year <= item.Year).ToList();
                        var dataView = _mlContext.Data.LoadFromEnumerable(currentData);

                        var forecastingPipeline = _mlContext.Forecasting.ForecastBySsa(
                            outputColumnName: nameof(ForecastOutput.ForecastedCosts),
                            inputColumnName: nameof(UFRForecasting.ProjectCosts),
                            windowSize: 2,
                            seriesLength: currentData.Count,
                            trainSize: currentData.Count,
                            horizon: 2,
                            confidenceLevel: 0.95f,
                            confidenceLowerBoundColumn: nameof(ForecastOutput.LowerBoundCosts),
                            confidenceUpperBoundColumn: nameof(ForecastOutput.UpperBoundCosts)
                        );

                        var model = forecastingPipeline.Fit(dataView);
                        var forecastEngine = model.CreateTimeSeriesEngine<UFRForecasting, ForecastOutput>(_mlContext);
                        var forecast = forecastEngine.Predict();

                        int forecastYear1 = item.Year + 1;
                        float forecastedFundYear1 = forecast.ForecastedCosts[0];
                        worksheet.Cells[row, 3].Value = forecastYear1;
                        worksheet.Cells[row, 4].Value = Math.Round(forecast.ForecastedCosts[0], 2);

                        currentData.Add(new UFRForecasting { Year = forecastYear1, ProjectCosts = forecastedFundYear1 });

                        var updatedDataView = _mlContext.Data.LoadFromEnumerable(currentData);
                        var updatedForecastingPipeline = _mlContext.Forecasting.ForecastBySsa(
                            outputColumnName: nameof(ForecastOutput.ForecastedCosts),
                            inputColumnName: nameof(UFRForecasting.ProjectCosts),
                            windowSize: 2,
                            seriesLength: currentData.Count,
                            trainSize: currentData.Count,
                            horizon: 2,
                            confidenceLevel: 0.95f,
                            confidenceLowerBoundColumn: nameof(ForecastOutput.LowerBoundCosts),
                            confidenceUpperBoundColumn: nameof(ForecastOutput.UpperBoundCosts)
                        );

                        var updatedModel = updatedForecastingPipeline.Fit(updatedDataView);
                        var updatedForecastingEngine = updatedModel.CreateTimeSeriesEngine<UFRForecasting, ForecastOutput>(_mlContext);
                        var updatedForecast = updatedForecastingEngine.Predict();

                        int forecastYear2 = item.Year + 2;
                        worksheet.Cells[row, 5].Value = forecastYear2;
                        worksheet.Cells[row, 6].Value = Math.Round(updatedForecast.ForecastedCosts[1], 2);
                    }

                    row++;
                }

                worksheet.Cells[1, 1, 1, 6].Style.Font.Bold = true;
                worksheet.Cells[2, 2, row - 1, 2].Style.Numberformat.Format = "#,##0.00";
                worksheet.Cells[2, 4, row - 1, 4].Style.Numberformat.Format = "#,##0.00";
                worksheet.Cells[2, 6, row - 1, 6].Style.Numberformat.Format = "#,##0.00";
                worksheet.Cells[1, 1, row - 1, 6].AutoFitColumns();

                var excelData = package.GetAsByteArray();
                var date = DateTime.Now.ToString("MMddyyyy:HHmmss");
                var genRep = new GenerateReport
                {
                    gr_Id = Guid.NewGuid().ToString(),
                    gr_fileName = $"UFRFundsReport{date}.xlsx",
                    gr_fileType = ".xlsx",
                    gr_Data = excelData,
                    gr_startDate = startDate,
                    gr_endDate = endDate,
                    gr_typeofReport = "Research Production",
                    generateDate = DateTime.Now,
                    UserId = user.Id,
                    isArchived = false
                };
                _context.GenerateReports.Add(genRep);
                await _context.SaveChangesAsync();

                return RedirectToAction("GenerateReport");

            }
            return NotFound("Invalid selected report type");
        }

        [Authorize(Roles ="Chief")]
        public IActionResult Forecasting()
        {
            var yearlyCosts = _context.UFRForecastings
                .GroupBy(d => d.Year)
                .Select(g => new UFRForecasting { Year = g.Key, ProjectCosts = g.Sum(x => x.ProjectCosts) })
                .OrderBy(x => x.Year)
                .ToList();

            var forecastData = new List<ForecastViewModel>();

            foreach(var item in yearlyCosts)
            {
                var forecastRow = new ForecastViewModel
                {
                    Year = item.Year,
                    ProjectCosts = item.ProjectCosts
                };

                if(yearlyCosts.Where(y => y.Year <= item.Year).Count() >= 5)
                {
                    var currentData = yearlyCosts.Where(y => y.Year <= item.Year).ToList();
                    var dataView = _mlContext.Data.LoadFromEnumerable(currentData);

                    var forecastingPipeline = _mlContext.Forecasting.ForecastBySsa(
                        outputColumnName: nameof(ForecastOutput.ForecastedCosts),
                        inputColumnName: nameof(UFRForecasting.ProjectCosts),
                        windowSize: 2,
                        seriesLength: currentData.Count,
                        trainSize: currentData.Count,
                        horizon: 2,
                        confidenceLevel: 0.95f,
                        confidenceLowerBoundColumn: nameof(ForecastOutput.LowerBoundCosts),
                        confidenceUpperBoundColumn: nameof(ForecastOutput.UpperBoundCosts)
                    );

                    var model = forecastingPipeline.Fit(dataView);
                    var forecastEngine = model.CreateTimeSeriesEngine<UFRForecasting, ForecastOutput>(_mlContext);
                    var forecast = forecastEngine.Predict();

                    int forecastYear1 = item.Year + 1;
                    float forecastedFundYear1 = forecast.ForecastedCosts[0];

                    forecastRow.ForecastYear1 = forecastYear1;
                    forecastRow.ForecastedFundYear1 = forecastedFundYear1;

                    currentData.Add(new UFRForecasting { Year = forecastYear1, ProjectCosts = forecastedFundYear1 });
                    var updatedDataView = _mlContext.Data.LoadFromEnumerable(currentData);

                    var updatedForecastingPipeline = _mlContext.Forecasting.ForecastBySsa(
                        outputColumnName: nameof(ForecastOutput.ForecastedCosts),
                        inputColumnName: nameof(UFRForecasting.ProjectCosts),
                        windowSize: 2,
                        seriesLength: currentData.Count,
                        trainSize: currentData.Count,
                        horizon: 2,
                        confidenceLevel: 0.95f,
                        confidenceLowerBoundColumn: nameof(ForecastOutput.LowerBoundCosts),
                        confidenceUpperBoundColumn: nameof(ForecastOutput.UpperBoundCosts)
                    );

                    var updatedModel = updatedForecastingPipeline.Fit(updatedDataView);
                    var updatedForecastingEngine = updatedModel.CreateTimeSeriesEngine<UFRForecasting, ForecastOutput>(_mlContext);
                    var updatedForecast = updatedForecastingEngine.Predict();

                    int forecastYear2 = item.Year + 2;
                    float forecastedFundYear2 = updatedForecast.ForecastedCosts[1];

                    forecastRow.ForecastYear2 = forecastYear2;
                    forecastRow.ForecastedFundYear2 = forecastedFundYear2;
                }

                forecastData.Add(forecastRow);
            }
            return View(forecastData);
        }

        public async Task<IActionResult> ArchiveReport(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var genReport = await _context.GenerateReports.FindAsync(id);
            if (genReport == null)
            {
                return NotFound();
            }

            genReport.isArchived = true;
            await _context.SaveChangesAsync();

            return RedirectToAction("ArchivedReport");
        }

        public async Task<IActionResult> UnarchiveReport(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var genReport = await _context.GenerateReports.FindAsync(id);
            if (genReport == null)
            {
                return NotFound();
            }

            genReport.isArchived = false;
            await _context.SaveChangesAsync();

            return RedirectToAction("ArchivedReport");
        }

        [Authorize(Roles ="Chief")]
        public async Task<IActionResult> GenerateGAWADNominees()
        {
            var recentFile = await _context.GenerateGAWADNominees
                .OrderByDescending(r => r.generateDate)
                .Take(10)
                .ToListAsync();

            return View(recentFile);
        }

        public async Task<IActionResult> GenerateNominees(string gawadType)
        {
            var user = await _userManager.GetUserAsync(User);
            if (gawadType == "Tuklas")
            {
                var tuklas = await _context.FundedResearches
                    .Where(f => f.status == "Completed")
                    .OrderBy(f => f.team_Leader)
                    .ToListAsync();

                if (tuklas == null || !tuklas.Any())
                {
                    return NotFound("No data found for the selected report type and date range.");
                }

                // Generate Excel report using EPPlus
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("GAWADTuklasNominees");

                    // Add headers
                    worksheet.Cells["A1:D1"].Merge = true;
                    worksheet.Cells["A1"].Value = $"Gawad Tuklas Nominees - {DateTime.Now.Year}";
                    worksheet.Cells["A1"].Style.Font.Bold = true;
                    worksheet.Cells["A1"].Style.Font.Size = 20;

                    worksheet.Cells["A2"].Value = "Name";
                    worksheet.Cells["B2"].Value = "Nature of Involvement";
                    worksheet.Cells["C2"].Value = "Research Title";
                    worksheet.Cells["D2"].Value = "Funded Research Type";
                    worksheet.Cells["E2"].Value = "College/Branch";
                    worksheet.Cells["F2"].Value = "Field of Study";
                    worksheet.Cells["G2"].Value = "Email";
                    worksheet.Cells["H2"].Value = "Research Co-Proponent/s";
                    worksheet.Cells["I2"].Value = "Completion Date";

                    worksheet.Cells["A2:I2"].Style.Font.Bold = true;
                    worksheet.Cells["A2:I2"].Style.Font.Size = 14;
                    worksheet.Cells["A2:I2"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells["A2:I2"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    // Add data to cells
                    int row = 3;
                    foreach (var item in tuklas)
                    {
                        var teamMembers = item.team_Members.Contains("N/A") ? string.Empty : string.Join("/", item.team_Members);

                        worksheet.Cells[row, 1].Value = item.team_Leader;
                        worksheet.Cells[row, 2].Value = "Project Leader";
                        worksheet.Cells[row, 3].Value = item.research_Title;
                        worksheet.Cells[row, 4].Value = item.fr_Type;
                        worksheet.Cells[row, 5].Value = $"{item.college}/{item.branch}";
                        worksheet.Cells[row, 6].Value = item.field_of_Study;
                        worksheet.Cells[row, 7].Value = item.teamLead_Email;
                        worksheet.Cells[row, 8].Value = teamMembers;
                        worksheet.Cells[row, 9].Value = item.end_Date.ToString("MMMM d, yyyy");
                        row++;
                    }

                    worksheet.Cells["A1:G1"].Style.Font.Bold = true;
                    worksheet.Cells.AutoFitColumns();

                    // Convert Excel package to a byte array
                    var excelData = package.GetAsByteArray();
                    var genTuklas = new GenerateGAWADNominees
                    {
                        gn_Id = Guid.NewGuid().ToString(),
                        gn_fileName = $"Gawad-Tuklas-Nominees_{DateTime.Now.Year}.xlsx",
                        gn_fileType = ".xlsx",
                        gn_Data = excelData,
                        gn_type = "GAWAD Tuklas",
                        generateDate = DateTime.Now,
                        UserId = user.Id,
                        isArchived = false
                    };
                    _context.GenerateGAWADNominees.Add(genTuklas);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("GenerateGAWADNominees");
                }
            }
            else if(gawadType == "Lathala")
            {
                var lathala = await _context.FundedResearches
                    .Where(f => f.status == "Completed")
                    .OrderBy(f => f.team_Leader)
                    .ToListAsync();

                if (lathala == null || !lathala.Any())
                {
                    return NotFound("No data found for the selected report type and date range.");
                }

                // Generate Excel report using EPPlus
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("GAWADLathalaNominees");

                    // header
                    worksheet.Cells["A1:D1"].Merge = true;
                    worksheet.Cells["A1"].Value = $"Gawad Lathala Nominees - {DateTime.Now.Year}";
                    worksheet.Cells["A1"].Style.Font.Bold = true;
                    worksheet.Cells["A1"].Style.Font.Size = 20;

                    worksheet.Cells["A2"].Value = "Name";
                    worksheet.Cells["B2"].Value = "Research Title";
                    worksheet.Cells["C2"].Value = "Funded Research Type";
                    worksheet.Cells["D2"].Value = "College/Branch";
                    worksheet.Cells["E2"].Value = "Field of Study";
                    worksheet.Cells["F2"].Value = "Email";
                    worksheet.Cells["G2"].Value = "Completion Date";

                    worksheet.Cells["A2:G2"].Style.Font.Bold = true;
                    worksheet.Cells["A2:G2"].Style.Font.Size = 14;
                    worksheet.Cells["A2:G2"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells["A2:G2"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    // Add data to cells
                    int row = 3;
                    foreach (var item in lathala)
                    {

                        worksheet.Cells[row, 1].Value = item.team_Leader;
                        worksheet.Cells[row, 2].Value = item.research_Title;
                        worksheet.Cells[row, 3].Value = item.fr_Type;
                        worksheet.Cells[row, 4].Value = $"{item.college}/{item.branch}";
                        worksheet.Cells[row, 5].Value = item.field_of_Study;
                        worksheet.Cells[row, 6].Value = item.teamLead_Email;
                        worksheet.Cells[row, 7].Value = item.end_Date.ToString("MMMM d, yyyy");
                        row++;
                    }

                    worksheet.Cells["A1:E1"].Style.Font.Bold = true;
                    worksheet.Cells.AutoFitColumns();

                    // Convert Excel package to a byte array
                    var excelData = package.GetAsByteArray();
                    var genLathala = new GenerateGAWADNominees
                    {
                        gn_Id = Guid.NewGuid().ToString(),
                        gn_fileName = $"Gawad-Lathala-Nominees_{DateTime.Now.Year}.xlsx",
                        gn_fileType = ".xlsx",
                        gn_Data = excelData,
                        gn_type = "GAWAD Lathala",
                        generateDate = DateTime.Now,
                        UserId = user.Id,
                        isArchived = false
                    };
                    _context.GenerateGAWADNominees.Add(genLathala);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("GenerateGAWADNominees");
                }
            }
            return NotFound("Invalid selected GAWAD type");
        }

        [HttpPost]
        public async Task<IActionResult> AddEvent(string eventTitle, DateTime startDate, DateTime endDate, string eventVisibility)
        {
            var user = await _userManager.GetUserAsync(User);
            if (ModelState.IsValid)
            {
                var addEvent = new CalendarEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = eventTitle,
                    Start = startDate,
                    End = endDate.AddDays(1),
                    Visibility = eventVisibility,
                    UserId = user.Id
                };

                _context.CalendarEvents.Add(addEvent);
                await _context.SaveChangesAsync();

                return RedirectToAction("ChiefDashboard");
            }
            return RedirectToAction("ChiefDashboard");
        }

        [HttpGet]
        public async Task<IActionResult> GetUserEvents()
        {
            var user = await _userManager.GetUserAsync(User);

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
        public async Task<IActionResult> DeleteEvent(string id)
        {
            var events = await _context.CalendarEvents.FindAsync(id);
            if(events != null)
            {
                _context.CalendarEvents.Remove(events);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [Authorize(Roles ="Chief")]
        [HttpGet]
        public IActionResult Settings()
        {
            var settings = _context.Settings.First();
            var guidelines = _context.Guidelines.OrderBy(f => f.file_Uploaded).ToList();
            var criteria = _context.Criterias.OrderBy(f => f.Id).ToList();
            var subCategory = _context.SubCategories.OrderBy(f => f.Id).ToList();

            var model = new Tuple<Settings, List<Guidelines>, List<Criteria>, List<SubCategory>>(settings, guidelines, criteria, subCategory);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMaintenanceMode(bool isMaintenanceMode)
        {
            var maintenance = await _context.Settings.FirstAsync();
            if (maintenance != null)
            {
                maintenance.isMaintenance = isMaintenanceMode;
                await _context.SaveChangesAsync();

                return Json(new {success  = true });
            }

            return Json(new { success = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUFRMode(bool isUFRMode)
        {
            var ufrApp = await _context.Settings.FirstAsync();
            if (ufrApp != null)
            {
                ufrApp.isUFRApplication = isUFRMode;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEFRMode(bool isEFRMode)
        {
            var efrApp = await _context.Settings.FirstAsync();
            if (efrApp != null)
            {
                efrApp.isEFRApplication = isEFRMode;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUFRLMode(bool isUFRLMode)
        {
            var ufrlApp = await _context.Settings.FirstAsync();
            if (ufrlApp != null)
            {
                ufrlApp.isUFRLApplication = isUFRLMode;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEvaluatorNum(int count)
        {
            if (count < 1 || count > 10)
            {
                return BadRequest("Invalid number of evaluators");
            }

            var settings = await _context.Settings.FirstAsync();
            if (settings != null)
            {
                settings.evaluatorNum = count;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEvaluationDays(int days)
        {
            if(days < 1 || days > 30)
            {
                return BadRequest("Invalid number of days");
            }

            var settings = await _context.Settings.FirstAsync();
            if(settings != null)
            {
                settings.daysEvaluation = days;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadGuidelines(IFormFile file, string documentType)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Invalid file." });
            }

            byte[] pdfData;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                pdfData = ms.ToArray();
                var guidelines = new Guidelines
                {
                    Id = Guid.NewGuid().ToString(),
                    file_Name = file.FileName,
                    file_Type = Path.GetExtension(file.FileName),
                    data = pdfData,
                    document_Type = documentType,
                    file_Uploaded = DateTime.Now
                };
                _context.Guidelines.Add(guidelines);

                await _context.SaveChangesAsync();
                return Json(new { success = true, fileName = file.FileName, fileId = guidelines.Id });
            }
        }

        public IActionResult RemoveFile(string id)
        {
            var guideline = _context.Guidelines.Find(id);
            if(guideline == null)
            {
                return NotFound("No file found!");
            }

            _context.Guidelines.Remove(guideline);
            _context.SaveChanges();

            return RedirectToAction("Settings");
        }

        public IActionResult AddCriteria(string criteriaName, double criteriaWeight)
        {
            var criteria = new Criteria
            {
                Name = criteriaName,
                Weight = criteriaWeight
            };

            _context.Criterias.Add(criteria);
            _context.SaveChanges();

            return RedirectToAction("Settings");
        }

        public IActionResult RemoveCriteria(int id)
        {
            var criteria = _context.Criterias.Find(id);
            if(criteria == null)
            {
                return NotFound("Criteria not found!");
            }

            _context.Criterias.Remove(criteria);
            _context.SaveChanges();

            return RedirectToAction("Settings");
        }

        public IActionResult AddSubCategory(int criteriaId, string subcategoryName, int subcategoryMaxScore)
        {
            var subCategory = new SubCategory
            {
                CriteriaId = criteriaId,
                Name = subcategoryName,
                MaxScore = subcategoryMaxScore
            };

            _context.SubCategories.Add(subCategory);
            _context.SaveChanges();

            return RedirectToAction("Settings");
        }

        public IActionResult RemoveSubCategory(int id)
        {
            var subCategory = _context.SubCategories.Find(id);
            if(subCategory == null)
            {
                return NotFound("Sub-Category not found!");
            }

            _context.SubCategories.Remove(subCategory);
            _context.SaveChanges();

            return RedirectToAction("Settings");
        }
    }
}
