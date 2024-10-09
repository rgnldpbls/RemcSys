using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemcSys.Areas.Identity.Data;
using RemcSys.Data;
using RemcSys.Models;

namespace RemcSys.Controllers
{
    public class EvaluatorController : Controller
    {
        private readonly RemcDBContext _context;
        private readonly UserManager<SystemUser> _userManager;
        private readonly ActionLoggerService _actionLogger;

        public EvaluatorController(RemcDBContext context, UserManager<SystemUser> userManager, ActionLoggerService actionLogger)
        {
            _context = context;
            _userManager = userManager;
            _actionLogger = actionLogger;
        }

        [Authorize(Roles ="Evaluator")]
        public async Task<IActionResult> EvaluatorNotif()
        {
            var user = await _userManager.GetUserAsync(User);
            var logs = await _context.ActionLogs
                .Where(l => l.ProjLead == user.Name)
                .OrderByDescending(log => log.Timestamp).ToListAsync();
            return View(logs);
        }

        [Authorize(Roles ="Evaluator")]
        public async Task<IActionResult> EvaluatorPending()
        {
            var user = await _userManager.GetUserAsync(User);
            var evaluator = _context.Evaluator.Where(e => e.UserId == user.Id).FirstOrDefault();

            if(evaluator != null)
            {
                var pendingEvaluations = await _context.Evaluations
                    .Where(e => e.evaluator_Id == evaluator.evaluator_Id && e.evaluation_Status == "Pending")
                    .Select(e => e.fra_Id)
                    .ToListAsync();

                var fraList = await _context.FundedResearchApplication
                    .Where(f => pendingEvaluations.Contains(f.fra_Id))
                    .ToListAsync();

                return View(fraList);
            }
            return View(new List<FundedResearchApplication>());
        }

        [Authorize(Roles = "Evaluator")]
        public async Task<IActionResult> EvaluatorMissed()
        {
            var user = await _userManager.GetUserAsync(User);
            var evaluator = _context.Evaluator.Where(e => e.UserId == user.Id).FirstOrDefault();

            if (evaluator != null)
            {
                var missedEvaluations = await _context.Evaluations
                    .Where(e => e.evaluator_Id == evaluator.evaluator_Id && e.evaluation_Status == "Missed")
                    .Select(e => e.fra_Id)
                    .ToListAsync();

                var fraList = await _context.FundedResearchApplication
                    .Where(f => missedEvaluations.Contains(f.fra_Id))
                    .ToListAsync();

                return View(fraList);
            }
            return View(new List<FundedResearchApplication>());
        }

        [Authorize(Roles = "Evaluator")]
        public async Task<IActionResult> EvaluatorEvaluated()
        {
            var user = await _userManager.GetUserAsync(User);
            var evaluator = _context.Evaluator.Where(e => e.UserId == user.Id).FirstOrDefault();

            if (evaluator != null)
            {
                var doneEvaluations = await _context.Evaluations
                    .Where(e => e.evaluator_Id == evaluator.evaluator_Id && e.evaluation_Status == "Evaluated")
                    .Select(e => e.fra_Id)
                    .ToListAsync();

                var fraList = await _context.FundedResearchApplication
                    .Where(f => doneEvaluations.Contains(f.fra_Id))
                    .ToListAsync();

                return View(fraList);
            }
            return View(new List<FundedResearchApplication>());
        }

        [Authorize(Roles = "Evaluator")]
        public async Task<IActionResult> EvaluationForm(string id)
        {
            if(id == null)
            {
                return NotFound();
            }

            var fileRequirement = await _context.FileRequirement.Where(f => f.fra_Id == id && f.document_Type == "Manuscript")
                .ToListAsync();
            if (fileRequirement == null)
            {
                return NotFound();
            }
            return View(fileRequirement);
        }

        public IActionResult PreviewFile(string id)
        {
            var file = _context.FileRequirement.FirstOrDefault(f => f.fr_Id == id);
            if(file != null)
            {
                return File(file.data, "application/pdf");
            }
            return NotFound();
        }
    }
}
