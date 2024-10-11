using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemcSys.Areas.Identity.Data;
using RemcSys.Data;
using RemcSys.Models;
using Xceed.Words.NET;

namespace RemcSys.Controllers
{
    public class EvaluatorController : Controller
    {
        private readonly RemcDBContext _context;
        private readonly UserManager<SystemUser> _userManager;
        private readonly ActionLoggerService _actionLogger;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public EvaluatorController(RemcDBContext context, UserManager<SystemUser> userManager, 
            ActionLoggerService actionLogger, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _actionLogger = actionLogger;
            _hostingEnvironment = hostingEnvironment;
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
        public IActionResult EvaluationForm(string id)
        {

            //IR Form Preview
            string folderPath = Path.Combine(_hostingEnvironment.WebRootPath, "evals");
            string extension = "*.pdf";
            List<Document> files = new List<Document>();

            foreach (var file in Directory.GetFiles(folderPath, extension, SearchOption.AllDirectories))
            {
                string fileExtension = Path.GetExtension(file).ToLower();
                files.Add(new Document
                {
                    FileName = Path.GetFileName(file),
                    FilePath = Path.GetRelativePath(_hostingEnvironment.WebRootPath, file).Replace('\\', '/'),
                    FileType = fileExtension
                });
            }
            var exec = files.OrderBy(f => f.FileName).ToList();
            ViewBag.exec = exec;

            // File Requirement - Manuscript
            var fileRequirement = _context.FileRequirement.Where(f => f.fra_Id == id && f.document_Type == "Manuscript")
                .ToList();
            ViewBag.Id = id;
            return View(fileRequirement);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitEvaluation(double aqScore, string aqComment, double reScore, string reComment, double riScore, string riComment,
            double lcScore, string lcComment, double rdScore, string rdComment, double ffScore, string ffComment, string genComment, string fraId)
        {
            var user = await _userManager.GetUserAsync(User);
            var fra = await _context.FundedResearchApplication.Where(f => f.fra_Id == fraId).FirstOrDefaultAsync();
            if(fra != null)
            {
                double tot1 = aqScore + reScore;
                double d1 = tot1 / 20;
                double p1 = d1 * 10;

                double tot2 = riScore + lcScore + rdScore;
                double d2 = tot2 / 30;
                double p2 = d2 * 60;

                double d3 = ffScore / 10;
                double p3 =  d3 * 30;

                double g1 = p1 + p2 + p3;

                string[] templates = { "IR-Eval-Form-1.docx", "IR-Eval-Form-2.docx" };
                string filledFolder = Path.Combine(_hostingEnvironment.WebRootPath, "content", "filled");
                Directory.CreateDirectory(filledFolder);

                foreach (var template in templates)
                {
                    string evalsPath = Path.Combine(_hostingEnvironment.WebRootPath, "content", "evals", template);
                    string filledDocumentPath = Path.Combine(filledFolder, $"Generated_{template}");

                    using (DocX doc = DocX.Load(evalsPath))
                    {
                        doc.ReplaceText("{{Title}}", fra.research_Title);
                        doc.ReplaceText("{{Lead}}", fra.applicant_Name);
                        doc.ReplaceText("{{Staff}}", string.Join(Environment.NewLine, fra.team_Members));
                        doc.ReplaceText("{{College}}", fra.college);
                        doc.ReplaceText("{{Branch}}", fra.branch);
                        doc.ReplaceText("{{AQComment}}", aqComment);
                        doc.ReplaceText("{{REComment}}", reComment);
                        doc.ReplaceText("{{RIComment}}", riComment);
                        doc.ReplaceText("{{LCComment}}", lcComment);
                        doc.ReplaceText("{{RDComment}}", rdComment);
                        doc.ReplaceText("{{FFComment}}", ffComment);
                        doc.ReplaceText("{{GenComment}}", genComment);
                        doc.ReplaceText("{{EvaluatorName}}", user.Name.ToUpper());
                        doc.ReplaceText("{{Date}}", DateTime.Now.ToString("MMMM d, yyyy"));
                        doc.ReplaceText("{{S1}}", aqScore.ToString());
                        doc.ReplaceText("{{S2}}", reScore.ToString());
                        doc.ReplaceText("{{T1}}", tot1.ToString());
                        doc.ReplaceText("{{P1}}", p1.ToString());
                        doc.ReplaceText("{{S3}}", riScore.ToString());
                        doc.ReplaceText("{{S4}}", lcScore.ToString());
                        doc.ReplaceText("{{S5}}", rdScore.ToString());
                        doc.ReplaceText("{{T2}}", tot2.ToString());
                        doc.ReplaceText("{{P2}}", p2.ToString());
                        doc.ReplaceText("{{S6}}", ffScore.ToString());
                        doc.ReplaceText("{{P3}}", p3.ToString());
                        doc.ReplaceText("{{G1}}", g1.ToString());

                        doc.SaveAs(filledDocumentPath);
                    }
                }
                return RedirectToAction("EvaluatorEvaluated");
            }
            return Json(new { success = false, message = "Validation failed." });
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
