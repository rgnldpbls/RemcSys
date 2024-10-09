using DocumentFormat.OpenXml.Spreadsheet;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using MimeKit;
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

        public ChiefController(RemcDBContext context, ActionLoggerService actionLogger, UserManager<SystemUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _actionLogger = actionLogger;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [Authorize(Roles = "Chief")]
        public IActionResult ChiefDashboard()
        {
            return View();
        }

        [Authorize(Roles = "Chief")]
        public async Task<IActionResult> ChiefNotif()
        {
            var logs = await _context.ActionLogs.OrderByDescending(log => log.Timestamp).ToListAsync();
            return View(logs);
        }

        [Authorize(Roles = "Chief")]
        public async Task<IActionResult> UFResearchApp(string searchString)
        {
            ViewData["currentFilter"] = searchString;
            var appQuery = _context.FundedResearchApplication
                .Where(f => f.fra_Type == "University Funded Research");

            if (!String.IsNullOrEmpty(searchString))
            {
                appQuery = appQuery.Where(s => s.research_Title.Contains(searchString));
            }

            var researchAppList = await appQuery.OrderByDescending(f => f.submission_Date).ToListAsync();
            var evaluatorList = _context.Evaluations
                .Where(e => researchAppList.Select(r => r.fra_Id).Contains(e.fra_Id))
                .ToList();

            var eligibleEvaluators = _context.Evaluator
                .Where(e => e.field_of_Interest.Any(f => researchAppList.Select(r => r.field_of_Study).Contains(f)))
                .OrderBy(f => f.evaluator_Name).ToList();
            var model = new Tuple<List<FundedResearchApplication>, List<Evaluation>, List<Evaluator>>(researchAppList, evaluatorList, eligibleEvaluators);

            return View(model); 
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
                var researchApp = await application.ToListAsync();
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
                var researchApp = await application.ToListAsync();
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
            ViewBag.FraId = fra.fra_Id;
            ViewBag.DTSNo = fra.dts_No;
            ViewBag.Research = fra.research_Title;
            ViewBag.Field = fra.field_of_Study;
            ViewBag.Lead = fra.applicant_Name;
            ViewBag.Member = fra.team_Members;
            ViewBag.Type = fra.fra_Type;

            var fileRequirement = await _context.FileRequirement.Where(f => f.fra_Id == id)
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
            var user = await _userManager.FindByEmailAsync(fra.applicant_Email);
            if (fileReq != null)
            {
                fileReq.file_Status = fileStatus;
                fileReq.file_Feedback = fileFeedback;

                _context.SaveChanges();
                await _actionLogger.LogActionAsync(user.Id, fra.fra_Id, null, null, "You need to resubmit " + fileReq.file_Name + ".", null);
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string fr_Id, string newStatus)
        {
            var file = _context.FileRequirement.FirstOrDefault(f => f.fr_Id == fr_Id);
            var fra = await _context.FundedResearchApplication.Where(f => f.fra_Id == file.fra_Id).FirstOrDefaultAsync();
            var user = await _userManager.FindByEmailAsync(fra.applicant_Email);
            if (file != null)
            {
                file.file_Status = newStatus;
                _context.SaveChanges();
                await _actionLogger.LogActionAsync(user.Id, fra.fra_Id, null, null, file.file_Name + " already checked.", null);

                var allFilesChecked = _context.FileRequirement
                    .Where(fr => fr.fra_Id == fra.fra_Id)
                    .All(fr => fr.file_Status == "Checked");

                if (allFilesChecked)
                {
                    await AssignEvaluators(fra.fra_Id);
                }
                return Json(new { success = true });
            }
            return Json(new { success = false });
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
            if (fileRequirement == null)
            {
                return NotFound();
            }

            if (fileRequirement.file_Type == ".pdf")
            {
                return File(fileRequirement.data, "application/pdf");
            }

            return BadRequest("Only PDF files can be previewed.");
        }

        [HttpPost]
        public async Task<IActionResult> DeclineEvaluator(string evaluationId)
        {
            var evals = _context.Evaluations.Where(e => e.evaluation_Id == evaluationId).FirstOrDefault();
            var evaluator = _context.Evaluator.Where(e => e.evaluator_Id == evals.evaluator_Id).FirstOrDefault();
            var fra = _context.FundedResearchApplication.Where(f => f.fra_Id == evals.fra_Id).FirstOrDefault();
            var user = await _userManager.FindByEmailAsync(fra.applicant_Email);
            await _actionLogger.LogActionAsync(user.Id, fra.fra_Id, evals.evaluator_Name, null,
                    "The chief remove you to evaluate " + fra.research_Title + ".", null);
            await SendRemoveEmail(evaluator.evaluator_Email, fra.research_Title, evals.evaluator_Name);
            evals.evaluation_Status = "Decline";
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AssignEvaluator(int evaluatorId, string fraId)
        {
            var fra = await _context.FundedResearchApplication.Where(f => f.fra_Id == fraId).FirstOrDefaultAsync();
            var user = await _userManager.FindByEmailAsync(fra.applicant_Email);

            var assignedEvaluators = await _context.Evaluations
                .Where(e => e.fra_Id == fraId)
                .ToListAsync();

            if(assignedEvaluators.Count >= 5)
            {
                return Json(new { success = false, message = "Maximum 5 evaluators can be assigned." });
            }

            bool isAlreadyAssigned = assignedEvaluators.Any(e => e.evaluator_Id == evaluatorId);
            if (isAlreadyAssigned)
            {
                return Json(new { success = false, message = "Evaluator is already assigned." });
            }

            var evaluation = new Evaluation
            {
                evaluation_Id = Guid.NewGuid().ToString(),
                evaluation_Status = "Pending",
                evaluator_Name = _context.Evaluator.Find(evaluatorId).evaluator_Name,
                evaluation_Grade = null,
                assigned_Date = DateTime.Now,
                evaluation_Date = null,
                evaluator_Id = evaluatorId,
                fra_Id = fraId
            };

            await _actionLogger.LogActionAsync(user.Id, fraId, _context.Evaluator.Find(evaluatorId).evaluator_Name, null,
                    "The chief assign you to evaluate " + fra.research_Title + ".", null);
            await SendEvaluatorEmail(_context.Evaluator.Find(evaluatorId).evaluator_Email, 
                _context.FundedResearchApplication.Find(fraId).research_Title, _context.Evaluator.Find(evaluatorId).evaluator_Name);
            _context.Evaluations.Add(evaluation);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
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
                    evaluation_Date = null
                };

                assignedEvaluators.Add(newEvaluation);
                await _actionLogger.LogActionAsync(user.Id, fraId, evaluator.evaluator_Name, null,
                    "The chief assign you to evaluate " + fra.research_Title + ".", null);
                await SendEvaluatorEmail(evaluator.evaluator_Email, fra.research_Title, evaluator.evaluator_Name);
                evaluatorIndex = (evaluatorIndex + 1) % eligibleEvaluators.Count;
            }
            _context.Evaluations.AddRange(assignedEvaluators);
            fra.application_Status = "UnderEvaluation";
            await _context.SaveChangesAsync();
            await _actionLogger.LogActionAsync(user.Id, fraId, null, null, fra.research_Title + " already under evaluation.", null);
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

        public async Task SendEvaluatorEmail(string email, string researchTitle, string name)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Research Evaluation and Monitoring Center", "remc.rmo2@gmail.com")); //Name & Email

                string recipientName = email.Split('@')[0];
                message.To.Add(new MailboxAddress(recipientName, email));

                message.Subject = "Assigned for Evaluation";

                var bodyBuilder = new BodyBuilder();

                var htmlBody = $@"
                <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='margin-bottom: 20px;'>
                            Greetings, {name}! <br> You have been assigned to evaluate the research titled: <strong>{researchTitle}</strong>.
                        </div>
                        <p>Please review the provided materials and submit your evaluation in the system.</p>
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

        public async Task SendRemoveEmail(string email, string researchTitle, string name)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Research Evaluation and Monitoring Center", "remc.rmo2@gmail.com")); //Name & Email

                string recipientName = email.Split('@')[0];
                message.To.Add(new MailboxAddress(recipientName, email));

                message.Subject = "Assigned for Evaluation";

                var bodyBuilder = new BodyBuilder();

                var htmlBody = $@"
                <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='margin-bottom: 20px;'>
                            Greetings, {name}! <br> You have been removed to evaluate the research titled: <strong>{researchTitle}</strong>.
                        </div>
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
