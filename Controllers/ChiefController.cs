﻿using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

            var researchAppList = await appQuery.ToListAsync();
            var evaluatorList = _context.Evaluations
                .Where(e => researchAppList.Select(r => r.fra_Id).Contains(e.fra_Id))
                .ToList();
            var model = new Tuple<List<FundedResearchApplication>, List<Evaluation>>(researchAppList, evaluatorList);

            return View(model);
        }

        [Authorize(Roles = "Chief")]
        public async Task<IActionResult> EFResearchApp(string searchString)
        {
            ViewData["currentFilter"] = searchString;
            var fileReq = _context.FileRequirement.FirstOrDefault();
            var application = from app in _context.FundedResearchApplication
                              .Where(f => f.fra_Type == "Externally Funded Research" && f.fra_Id == fileReq.fra_Id)
                              select app;

            if (!String.IsNullOrEmpty(searchString))
            {
                application = application.Where(s => s.research_Title.Contains(searchString));
            }

            return View(await application.ToListAsync());
        }

        [Authorize(Roles = "Chief")]
        public async Task<IActionResult> UFRLApp(string searchString)
        {
            ViewData["currentFilter"] = searchString;
            var fileReq = _context.FileRequirement.FirstOrDefault();
            var application = from app in _context.FundedResearchApplication
                              .Where(f => f.fra_Type == "University Funded Research Load" && f.fra_Id == fileReq.fra_Id)
                              select app;

            if (!String.IsNullOrEmpty(searchString))
            {
                application = application.Where(s => s.research_Title.Contains(searchString));
            }

            return View(await application.ToListAsync());
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
                    await CheckAndAssignEvaluators(fra.fra_Id);
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

        public async Task CheckAndAssignEvaluators(string fraId)
        {
            var fra = await _context.FundedResearchApplication.Where(f => f.fra_Id == fraId).FirstOrDefaultAsync();
            var user = await _userManager.FindByEmailAsync(fra.applicant_Email);

            var eligibleEvaluators = await GetEligibleEvaluators(fraId);
            if (eligibleEvaluators.Count == 0)
            {
                throw new Exception("No eligible evaluators found for this research application.");
            }

            var assignedEvaluators = new List<Evaluation>();
            int evaluatorIndex = 0;

            for (int i = 0; i < 5; i++)
            {
                var evaluator = eligibleEvaluators[evaluatorIndex];
                var newEvaluation = new Evaluation
                {
                    evaluation_Id = Guid.NewGuid().ToString(),
                    evaluation_Status = "Pending",
                    evaluator_Id = evaluator.Id,
                    evaluator_Name = evaluator.Name,
                    fra_Id = fraId,
                    evaluation_Grade = null,
                    assigned_Date = DateTime.Now,
                    evaluation_Date = null
                };

                assignedEvaluators.Add(newEvaluation);
                evaluatorIndex = (evaluatorIndex + 1) % eligibleEvaluators.Count;
            }
            _context.Evaluations.AddRange(assignedEvaluators);
            await _context.SaveChangesAsync();
            await _actionLogger.LogActionAsync(user.Id, fraId, null, null, fra.research_Title + " already under evaluation.", null);
        }

        public async Task<List<SystemUser>> GetEligibleEvaluators(string fraId)
        {
            var researchApp = await _context.FundedResearchApplication
                    .FirstOrDefaultAsync(fra => fra.fra_Id == fraId);

            if (researchApp == null)
            {
                throw new Exception("Research application not found.");
            }

            var evaluators = await GetEvaluatorsAsync();
            var eligibleEvaluators = evaluators.Where(e => e.College == researchApp.college).ToList();
            return eligibleEvaluators;
        }

        private async Task<List<SystemUser>> GetEvaluatorsAsync()
        {
            var evaluatorRole = await _roleManager.FindByNameAsync("Evaluator");
            if (evaluatorRole == null)
            {
                throw new Exception("Role Evaluator does not exist.");
            }

            var evaluators = await _userManager.GetUsersInRoleAsync("Evaluator");
            return evaluators.ToList();
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