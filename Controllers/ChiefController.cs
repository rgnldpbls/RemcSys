using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Humanizer;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using MimeKit;
using MimeKit.Utils;
using RemcSys.Areas.Identity.Data;
using RemcSys.Data;
using RemcSys.Models;
using System.Runtime.Intrinsics.X86;

namespace RemcSys.Controllers
{
    public class ChiefController : Controller
    {
        private readonly RemcDBContext _context;
        private readonly ActionLoggerService _actionLogger;
        private readonly UserManager<SystemUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ChiefController(RemcDBContext context, ActionLoggerService actionLogger, UserManager<SystemUser> userManager,
            RoleManager<IdentityRole> roleManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _actionLogger = actionLogger;
            _userManager = userManager;
            _roleManager = roleManager;
            _webHostEnvironment = webHostEnvironment;
        }

        [Authorize(Roles = "Chief")]
        public IActionResult ChiefDashboard()
        {
            return View();
        }

        [Authorize(Roles = "Chief")]
        public async Task<IActionResult> ChiefNotif()
        {
            var logs = await _context.ActionLogs
                .Where(f => f.isChief == true)
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();
            return View(logs);
        }

        [Authorize(Roles = "Chief")]
        public async Task<IActionResult> UFResearchApp(string searchString)
        {
            await CheckMissedEvaluations();
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
        public async Task<IActionResult> EFResearchApp(string searchString)
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
        public async Task<IActionResult> UFRLApp(string searchString)
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
        public async Task<IActionResult> DocuList(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fra = await _context.FundedResearchApplication.FindAsync(id);
            if(fra == null)
            {
                return NotFound();
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
            if (fileRequirement == null)
            {
                return NotFound();
            }
            return View(fileRequirement);
        }

        [HttpPost]
        public async Task<IActionResult> SaveFeedback(string fileId, string fileStatus, string fileFeedback)
        {
            var fileReq = _context.FileRequirement.Find(fileId);
            var fra = await _context.FundedResearchApplication.Where(f => f.fra_Id == fileReq.fra_Id).FirstOrDefaultAsync();
            if (fileReq != null)
            {
                fileReq.file_Status = fileStatus;
                fileReq.file_Feedback = fileFeedback;

                _context.SaveChanges();
                await _actionLogger.LogActionAsync(fra.applicant_Name, fra.fra_Type, "You need to resubmit this " + fileReq.file_Name + ". " +
                    "Kindly check the feedback for the changes.", true, false, false, fra.fra_Id);
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string fr_Id, string newStatus)
        {
            var file = _context.FileRequirement.FirstOrDefault(f => f.fr_Id == fr_Id);
            if(file == null)
            {
                return Json(new { success = false, message = "File requirement not found" });
            }

            var fra = await _context.FundedResearchApplication
                .Where(f => f.fra_Id == file.fra_Id)
                .FirstOrDefaultAsync();
            if (fra == null)
            {
                return Json(new { success = false, message = "Funded Research Application not found" });
            }

            var user = await _userManager.FindByEmailAsync(fra.applicant_Email);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
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
            else if(allFilesChecked && fra.fra_Type != "University Funded Research")
            {
                fra.application_Status = "Approved";
                await _context.SaveChangesAsync();
                await _actionLogger.LogActionAsync(fra.applicant_Name, fra.fra_Type, fra.research_Title + " all file requirements approved by the Chief.", 
                    true, false, false, fra.fra_Id);
                return Json(new { approved = true });
            }
            return Json(new { success = true });
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

            return RedirectToAction("DocuList", "Chief", new {id = fraId});
        }

        public async Task<IActionResult> PreviewFile(string id)
        {
            var fileRequirement = await _context.FileRequirement.FindAsync(id);
            if (fileRequirement != null)
            {
                if (fileRequirement.file_Type == ".pdf")
                {
                    return File(fileRequirement.data, "application/pdf");
                }
            }

            var progReport = await _context.ProgressReports.FindAsync(id);
            if(progReport != null)
            {
                if(progReport.file_Type == ".pdf")
                {
                    return File(progReport.data, "application/pdf");
                }
            }

            return BadRequest("Only PDF files can be previewed.");
        }

        public async Task AssignEvaluators(string fraId)
        {
            var fra = await _context.FundedResearchApplication.Where(f => f.fra_Id == fraId).FirstOrDefaultAsync();
            var user = await _userManager.FindByEmailAsync(fra.applicant_Email);

            var eligibleEvaluators = await GetEligibleEvaluators(fraId);
            if (eligibleEvaluators.Count == 0)
            {
                throw new Exception("No eligible evaluators found for this research application.");
            }

            var alreadyAssignedEvaluatorIds = _context.Evaluations.Where(e => e.fra_Id == fraId)
                .Select(e => e.evaluator_Id).ToList();

            eligibleEvaluators = eligibleEvaluators.Where(e => !alreadyAssignedEvaluatorIds.Contains(e.evaluator_Id)).ToList();

            if(eligibleEvaluators.Count == 0)
            {
                throw new Exception("All eligible evaluators are already assigned.");
            }

            var assignedEvaluators = new List<Evaluation>();
            int totalEvaluatorsToAssign = Math.Min(5, eligibleEvaluators.Count);
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
                    evaluation_Deadline = DateTime.Now.AddDays(7),
                    evaluation_Date = null
                };

                assignedEvaluators.Add(newEvaluation);
                await _actionLogger.LogActionAsync(evaluator.evaluator_Name, fra.fra_Type, "The chief assign you to evaluate the " + fra.research_Title + ".", 
                    false, false, true, fra.fra_Id);
                /*await SendEvaluatorEmail(evaluator.evaluator_Email, fra.research_Title, evaluator.evaluator_Name, DateTime.Now.AddDays(7).ToString("MMMM d, yyyy"));*/
                evaluatorIndex = (evaluatorIndex + 1) % eligibleEvaluators.Count;
            }
            _context.Evaluations.AddRange(assignedEvaluators);
            fra.application_Status = "UnderEvaluation";
            await _context.SaveChangesAsync();
            await _actionLogger.LogActionAsync(fra.applicant_Name, fra.fra_Type, fra.research_Title + " already under techical evaluation.", 
                true, false, false, fra.fra_Id);
        }

        public async Task<List<Evaluator>> GetEligibleEvaluators(string fraId)
        {
            var researchApp = await _context.FundedResearchApplication
                    .FirstOrDefaultAsync(fra => fra.fra_Id == fraId);

            if (researchApp == null)
            {
                throw new Exception("Research application not found.");
            }

            var evaluators = await _context.Evaluator.ToListAsync();
            var eligibleEvaluators = evaluators.Where(e => e.field_of_Interest.Contains(researchApp.field_of_Study) && 
                e.evaluator_Name != researchApp.applicant_Name && !researchApp.team_Members.Contains(e.evaluator_Name)).ToList();
            return eligibleEvaluators;
        }

        public async Task SendEvaluatorEmail(string email, string researchTitle, string name, string deadline) //Add Footer
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Research Evaluation and Monitoring Center", "remc.rmo2@gmail.com"));

                string recipientName = email.Split('@')[0];
                message.To.Add(new MailboxAddress(recipientName, email));

                message.Subject = "Assigned as Evaluator in " + researchTitle;

                var bodyBuilder = new BodyBuilder();

                string footerImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "Footer.png");
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
            }catch (Exception ex)
            {
                throw new Exception($"Error occurred while sending email: {ex.Message}");
            }
        }

        [Authorize(Roles = "Chief")]
        public async Task<IActionResult> UEResearchApp(string searchString)
        {
            await CheckMissedEvaluations();
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
                .Where(e => e.evaluation_Status == "Pending")
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
                             assignedCount.Count >= 5
            }).OrderBy(f => f.evaluator_Name);

            return Json(evaluatorData);
        }

        [HttpPost]
        public async Task<IActionResult> DeclineEvaluator(string evaluationId)
        {
            var evals = _context.Evaluations.Where(e => e.evaluation_Id == evaluationId).FirstOrDefault();
            var evaluator = _context.Evaluator.Where(e => e.evaluator_Id == evals.evaluator_Id).FirstOrDefault();
            var fra = _context.FundedResearchApplication.Where(f => f.fra_Id == evals.fra_Id).FirstOrDefault();
            await _actionLogger.LogActionAsync(evals.evaluator_Name, fra.fra_Type, "The chief remove you to evaluate the " + fra.research_Title + ".", 
                false, false, true, fra.fra_Id);
            /*await SendRemoveEmail(evaluator.evaluator_Email, fra.research_Title, evals.evaluator_Name);*/
            evals.evaluation_Status = "Decline";
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        public async Task SendRemoveEmail(string email, string researchTitle, string name)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Research Evaluation and Monitoring Center", "remc.rmo2@gmail.com")); //Name & Email

                string recipientName = email.Split('@')[0];
                message.To.Add(new MailboxAddress(recipientName, email));

                message.Subject = "Approval of Your Request for Evaluator Role Removal in " + researchTitle;

                var bodyBuilder = new BodyBuilder();

                string footerImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "Footer.png");
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

        [HttpPost]
        public async Task<IActionResult> AssignEvaluator(int evaluatorId, string fraId)
        {
            var fra = await _context.FundedResearchApplication.Where(f => f.fra_Id == fraId).FirstOrDefaultAsync();
            var user = await _userManager.FindByEmailAsync(fra.applicant_Email);

            var existingEvaluation = await _context.Evaluations
            .FirstOrDefaultAsync(e => e.evaluator_Id == evaluatorId && e.fra_Id == fraId);

            if (existingEvaluation != null)
            {
                return Json(new { success = false, message = "Evaluator is already assigned to this application." });
            }

            int existingEvals = await _context.Evaluations.CountAsync(e => e.fra_Id == fraId && e.evaluation_Status != "Decline");
            if(existingEvals > 5)
            {
                return Json(new { success = false, message = "Maximum 5 evaluators can be assigned." });
            }

            var evaluators = _context.Evaluator.Find(evaluatorId);

            var evaluation = new Evaluation
            {
                evaluation_Id = Guid.NewGuid().ToString(),
                evaluation_Status = "Pending",
                evaluator_Name = _context.Evaluator.Find(evaluatorId).evaluator_Name,
                evaluation_Grade = null,
                assigned_Date = DateTime.Now,
                evaluation_Deadline = DateTime.Now.AddDays(7),
                evaluation_Date = null,
                evaluator_Id = evaluatorId,
                fra_Id = fraId
            };

            await _actionLogger.LogActionAsync(evaluators.evaluator_Name, fra.fra_Type, "The chief assign you to evaluate the " + fra.research_Title + ".", 
                false, false, true, fra.fra_Id);
            /*await SendEvaluatorEmail(evaluators.evaluator_Email, fra.research_Title, evaluators.evaluator_Name, DateTime.Now.AddDays(7).ToString("MMMM d, yyyy"));*/
            _context.Evaluations.Add(evaluation);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
        
        public async Task CheckMissedEvaluations()
        {
            var today = DateTime.Today;

            var evaluations = await _context.Evaluations
                .Where(e => e.evaluation_Status == "Pending")
                .ToListAsync();

            foreach (var evaluation in evaluations)
            {
                var evaluator = await _context.Evaluator
                    .Where(e => e.evaluator_Id == evaluation.evaluator_Id)
                    .FirstOrDefaultAsync();

                var fra = await _context.FundedResearchApplication
                    .Where(f => f.fra_Id == evaluation.fra_Id)
                    .FirstOrDefaultAsync();

                var daysLeft = (evaluation.evaluation_Deadline - today).Days;
                if(daysLeft < 0)
                {
                    evaluation.evaluation_Status = "Missed";
                    await _context.SaveChangesAsync();
                }
                else if(daysLeft == 0)
                {
                    var content = "This is an urgent reminder that the evaluation for the research titled " + fra.research_Title + " is due today.";
                    await SendRemindEvaluatorEmail(evaluator.evaluator_Email, fra.research_Title, evaluator.evaluator_Name, content);
                    await _actionLogger.LogActionAsync(evaluator.evaluator_Name, fra.fra_Type, "This is an urgent reminder that the evaluation for the research titled "
                        + fra.research_Title + " is due today.", false, false, true, fra.fra_Id);
                }
                else if(daysLeft == 1)
                {
                    var content = "This is a reminder that your evaluation for the research titled " + fra.research_Title + 
                        " is due tomorrow. We kindly request you to submit  the completed Grading and Comments forms before the deadline of " 
                        + evaluation.evaluation_Deadline.ToString("MMMM d, yyyy") + ".";
                    await SendRemindEvaluatorEmail(evaluator.evaluator_Email, fra.research_Title, evaluator.evaluator_Name, content);
                    await _actionLogger.LogActionAsync(evaluator.evaluator_Name, fra.fra_Type, "This is a reminder that your evaluation for the research titled "
                        + fra.research_Title + " is due tomorrow.", false, false, true, fra.fra_Id);
                }
                else if(daysLeft == 3)
                {
                    var content = "This is a gentle reminder that the deadline for submitting your evaluation of the research titled " + 
                        fra.research_Title + " is approaching, with 3 days remaining until " + 
                        evaluation.evaluation_Deadline.ToString("MMMM d, yyyy") + ".";
                    await SendRemindEvaluatorEmail(evaluator.evaluator_Email, fra.research_Title, evaluator.evaluator_Name, content);
                    await _actionLogger.LogActionAsync(evaluator.evaluator_Name, fra.fra_Type, "This is a gentle reminder that the deadline for submitting your evaluation of the research titled "
                        + fra.research_Title + " is approaching, with 3 days remaining until deadline.", false, false, true, fra.fra_Id);
                }
            }

        }

        public async Task SendRemindEvaluatorEmail(string email, string researchTitle, string name, string content)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Research Evaluation and Monitoring Center", "remc.rmo2@gmail.com")); //Name & Email

                string recipientName = email.Split('@')[0];
                message.To.Add(new MailboxAddress(recipientName, email));

                message.Subject = "Research Evaluation Deadline - " + researchTitle;

                var bodyBuilder = new BodyBuilder();

                string footerImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "Footer.png");
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
                    
                            Greetings! <br><br>
                            <strong>{content}</strong>

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


        [Authorize(Roles = "Chief")]
        public async Task<IActionResult> ChiefResearchEvaluation(string id)
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

            var ethics = await _context.FundedResearchEthics
                .FirstOrDefaultAsync(e => e.fra_Id == id);

            if(ethics == null)
            {
                return NotFound("Funded Research Application didn't apply for Ethics Clearance");
            }
            ViewBag.hasUrec = ethics.urec_No == null;
            ViewBag.hasEthicClearance = ethics.ethicClearance_Id == null;
            ViewBag.hasCertificate = ethics.completionCertificate_Id == null;

            var model = new Tuple<List<ViewChiefEvaluationVM>, List<FileRequirement>>
                (evaluationsList, evalFormList);

            return View(model);
        }

        public async Task<IActionResult> Download(string id)
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
                /*await SendApproveEmail(fra.applicant_Email, fra.research_Title, fra.applicant_Name, addComment);*/
                await _actionLogger.LogActionAsync(fra.applicant_Name, fra.fra_Type, fra.research_Title + " is approved in technical evaluation.", 
                    true, false, false, fra.fra_Id);
            } 
            else if (appStatus == "Rejected")
            {
                fra.application_Status = appStatus;
                await _context.SaveChangesAsync();
                /*await SendRejectEmail(fra.applicant_Email, fra.research_Title, fra.applicant_Name, addComment);*/
                await _actionLogger.LogActionAsync(fra.applicant_Name, fra.fra_Type, fra.research_Title + " is rejected in technical evaluation.", 
                    true, false, false, fra.fra_Id);
            }
            return RedirectToAction("UEResearchApp", "Chief");
        }

        public async Task SendApproveEmail(string email, string researchTitle, string name, string comment)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Research Evaluation and Monitoring Center", "remc.rmo2@gmail.com")); //Name & Email

                string recipientName = email.Split('@')[0];
                message.To.Add(new MailboxAddress(recipientName, email));

                message.Subject = "Research Technical Evaluation Results - " + researchTitle;

                var bodyBuilder = new BodyBuilder();

                string footerImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "Footer.png");
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

        public async Task SendRejectEmail(string email, string researchTitle, string name, string comment)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Research Evaluation and Monitoring Center", "remc.rmo2@gmail.com")); //Name & Email

                string recipientName = email.Split('@')[0];
                message.To.Add(new MailboxAddress(recipientName, email));

                message.Subject = "Research Technical Evaluation Results - " + researchTitle;

                var bodyBuilder = new BodyBuilder();

                string footerImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "Footer.png");
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
                .OrderBy(f => f.submission_Date)
                .Join(_context.FundedResearchEthics,
                    fra => fra.fra_Id,
                    fre => fre.fra_Id,
                    (fra, fre) => new ViewNTP
                    {
                        dts_No = fra.dts_No,
                        research_Title = fra.research_Title,
                        field_of_Study = fra.field_of_Study,
                        fra_Type = fra.fra_Type,
                        fra_Id = fra.fra_Id,
                        urec_No = fre.urec_No,
                        ethicClearance_Id = fre.ethicClearance_Id,
                        completionCertificate_Id = fre.completionCertificate_Id
                    })
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
            var user = await _userManager.FindByEmailAsync(fra.applicant_Email);
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
            /*await SendProceedEmail(fra.applicant_Email, fra.research_Title, fra.applicant_Name);*/

            return RedirectToAction("UploadNTP", "Chief");
        }

        public async Task SendProceedEmail(string email, string researchTitle, string name)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Research Evaluation and Monitoring Center", "remc.rmo2@gmail.com")); //Name & Email

                string recipientName = email.Split('@')[0];
                message.To.Add(new MailboxAddress(recipientName, email));

                message.Subject = "Notice to Proceed - " + researchTitle;

                var bodyBuilder = new BodyBuilder();

                string footerImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "Footer.png");
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
        public async Task<IActionResult> ArchivedApplication(string searchString)
        {
            ViewData["currentFilter"] = searchString;
            var appQuery = _context.FundedResearchApplication
                .Where(f => new[] { "University Funded Research", "Externally Funded Research", "University Funded Research Load" }.Contains(f.fra_Type));

            if (!String.IsNullOrEmpty(searchString))
            {
                appQuery = appQuery.Where(s => s.research_Title.Contains(searchString));
            }

            var researchAppList = await appQuery
                .Where(f => f.isArchive == true)
                .OrderByDescending(f => f.submission_Date)
                .ToListAsync();

            return View(researchAppList);
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
                var fundedResearch = await appQuery.OrderByDescending(f => f.start_Date).ToListAsync();

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
                var fundedResearch = await appQuery.OrderByDescending(f => f.start_Date).ToListAsync();

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
                var fundedResearch = await appQuery.OrderByDescending(f => f.start_Date).ToListAsync();

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
                return Json(new { success = false, message = "Progress Report not found" });
            }

            var fr = await _context.FundedResearches.Where(fr => fr.fr_Id == file.fr_Id).FirstOrDefaultAsync();
            if(fr == null)
            {
                return Json(new { success = false, message = "Funded Research not found" });
            }

            file.file_Status = newStatus;

            var existingReports = await _context.ProgressReports
                .Where(pr => pr.fr_Id == file.fr_Id)
                .ToListAsync();

            int reportNum = existingReports.Count + 0;
            string docuType = $"Progress Report No.{reportNum}";

            fr.status = $"Checked {docuType}";
            _context.SaveChanges();
            await _actionLogger.LogActionAsync(fr.team_Leader, fr.fr_Type, file.file_Name + " already checked by the Chief.",
                true, false, false, fr.fra_Id);

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
        public IActionResult GawadTuklas()
        {
            return View();
        }

        [Authorize(Roles ="Chief")]
        public IActionResult GawadLathala()
        {
            return View();
        }

        [Authorize(Roles ="Chief")]
        public IActionResult GawadWinners()
        {
            return View();
        }
    }
}
