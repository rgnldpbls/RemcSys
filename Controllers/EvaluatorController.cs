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
        public async Task<IActionResult> EvaluatorDashboard()
        {
            if (_context.Settings.First().isMaintenance)
            {
                return RedirectToAction("UnderMaintenance", "Home");
            }

            var user = await _userManager.GetUserAsync(User);
            var evaluator = await _context.Evaluator.FirstOrDefaultAsync(f => f.UserId == user.Id);
            ViewBag.Pending = await _context.Evaluations.Where(e => e.evaluation_Status == "Pending" && e.evaluator_Id == evaluator.evaluator_Id).CountAsync();
            ViewBag.Missed = await _context.Evaluations.Where(e => e.evaluation_Status == "Missed" && e.evaluator_Id == evaluator.evaluator_Id).CountAsync();
            ViewBag.Evaluated = await _context.Evaluations.Where(e => (e.evaluation_Status == "Approved" || e.evaluation_Status == "Rejected")
                && e.evaluator_Id == evaluator.evaluator_Id).CountAsync();

            var pendingEvals = await _context.Evaluations
                .Include(e => e.fundedResearchApplication)
                .Where(e => e.evaluation_Status == "Pending" && e.evaluator_Id == evaluator.evaluator_Id)
                .OrderBy(e => e.evaluation_Deadline)
                .ToListAsync();
            
            return View(pendingEvals);
        }

        [Authorize(Roles ="Evaluator")]
        public async Task<IActionResult> EvaluatorNotif()
        {
            if (_context.Settings.First().isMaintenance)
            {
                return RedirectToAction("UnderMaintenance", "Home");
            }

            var user = await _userManager.GetUserAsync(User);
            var logs = await _context.ActionLogs
                .Where(f => f.Name == user.Name && f.isEvaluator == true)
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();
            return View(logs);
        }

        public async Task CheckMissedEvaluations()
        {
            var today = DateTime.Today;

            var missedEvaluations = await _context.Evaluations
                .Where(e => e.evaluation_Status == "Pending" && e.evaluation_Deadline <= today)
                .ToListAsync();

            foreach(var evaluation in missedEvaluations)
            {
                evaluation.evaluation_Status = "Missed";
            }

            await _context.SaveChangesAsync();
        }

        [Authorize(Roles ="Evaluator")]
        public async Task<IActionResult> EvaluatorPending()
        {
            await CheckMissedEvaluations();

            var user = await _userManager.GetUserAsync(User);
            var evaluator = _context.Evaluator.Where(e => e.UserId == user.Id).FirstOrDefault();

            if(evaluator != null)
            {
                var pendingEvaluations = await _context.Evaluations
                    .Where(e => e.evaluator_Id == evaluator.evaluator_Id && e.evaluation_Status == "Pending")
                    .Join(_context.FundedResearchApplication,
                        evaluation => evaluation.fra_Id,
                        researchApp => researchApp.fra_Id,
                        (evaluation, researchApp) => new ViewEvaluationVM
                        {
                            dts_No = researchApp.dts_No,
                            research_Title = researchApp.research_Title,
                            field_of_Study = researchApp.field_of_Study,
                            application_Status =  researchApp.application_Status,
                            evaluation_deadline =  evaluation.evaluation_Deadline,
                            fra_Id =  researchApp.fra_Id
                        })
                    .ToListAsync();

                return View(pendingEvaluations);
            }
            return View(new List<ViewEvaluationVM>());
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
                    .Join(_context.FundedResearchApplication,
                        evaluation => evaluation.fra_Id,
                        researchApp => researchApp.fra_Id,
                        (evaluation, researchApp) => new ViewEvaluationVM
                        {
                            dts_No = researchApp.dts_No,
                            research_Title = researchApp.research_Title,
                            field_of_Study = researchApp.field_of_Study,
                            application_Status = researchApp.application_Status,
                            evaluation_deadline = evaluation.evaluation_Deadline,
                            fra_Id = researchApp.fra_Id
                        })
                    .ToListAsync();

                return View(missedEvaluations);
            }
            return View(new List<ViewEvaluationVM>());
        }

        [Authorize(Roles = "Evaluator")]
        public async Task<IActionResult> EvaluatorEvaluated()
        {
            var user = await _userManager.GetUserAsync(User);
            var evaluator = _context.Evaluator.Where(e => e.UserId == user.Id).FirstOrDefault();

            if (evaluator != null)
            {
                var doneEvaluations = await _context.Evaluations
                    .Where(e => e.evaluator_Id == evaluator.evaluator_Id && (e.evaluation_Status == "Approved" || e.evaluation_Status == "Rejected"))
                    .Join(_context.FundedResearchApplication,
                        evaluation => evaluation.fra_Id,
                        researchApp => researchApp.fra_Id,
                        (evaluation, researchApp) => new ViewEvaluationVM
                        {
                            dts_No = researchApp.dts_No,
                            research_Title = researchApp.research_Title,
                            field_of_Study = researchApp.field_of_Study,
                            application_Status = evaluation.evaluation_Status,
                            evaluation_deadline = evaluation.evaluation_Date,
                            fra_Id = researchApp.fra_Id
                        })
                    .ToListAsync();

                return View(doneEvaluations);
            }
            return View(new List<ViewEvaluationVM>());
        }

        [Authorize(Roles = "Evaluator")]
        public IActionResult EvaluationForm(string id)
        {

            //IR Form Preview
            string folderPath = Path.Combine(_hostingEnvironment.WebRootPath, "evals");
            string extension = "*.pdf";
            List<InternalDocument> files = new List<InternalDocument>();

            foreach (var file in Directory.GetFiles(folderPath, extension, SearchOption.AllDirectories))
            {
                string fileExtension = Path.GetExtension(file).ToLower();
                files.Add(new InternalDocument
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
            var evaluator = await _context.Evaluator.Where(e => e.UserId == user.Id).FirstOrDefaultAsync();
            var fra = await _context.FundedResearchApplication.Where(f => f.fra_Id == fraId).FirstOrDefaultAsync();

            double tot1 = aqScore + reScore;
            double d1 = tot1 / 20;
            double p1 = d1 * 10;

            double tot2 = riScore + lcScore + rdScore;
            double d2 = tot2 / 30;
            double p2 = d2 * 60;

            double d3 = ffScore / 10;
            double p3 = d3 * 30;

            double g1 = p1 + p2 + p3;

            string[] templates = { "IR-Eval-Form-1.docx", "IR-Eval-Form-2.docx" };
            string filledFolder = Path.Combine(_hostingEnvironment.WebRootPath, "content", "filled");
            Directory.CreateDirectory(filledFolder);

            foreach (var template in templates)
            {
                string evalsPath = Path.Combine(_hostingEnvironment.WebRootPath, "content", "evals", template);
                string filledDocumentPath = Path.Combine(filledFolder, $"{user.Name}_{template}");
                string pdfPath = Path.ChangeExtension(filledDocumentPath, ".pdf");

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
                    doc.ReplaceText("{{EvaluatorName}}", evaluator.evaluator_Name.ToUpper());
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

                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filledDocumentPath);

                var fileReq = new FileRequirement
                {
                    fr_Id = Guid.NewGuid().ToString(),
                    file_Name = $"{user.Name}_{template}",
                    file_Type = ".docx",
                    data = fileBytes,
                    file_Status = "Evaluated",
                    document_Type = "EvaluationForms",
                    file_Feedback = null,
                    file_Uploaded = DateTime.Now,
                    fra_Id = fraId
                };

                _context.FileRequirement.Add(fileReq);
            }
            var evals = _context.Evaluations.Where(e => e.fra_Id == fraId && e.evaluator_Id == evaluator.evaluator_Id).FirstOrDefault();
            evals.evaluation_Grade = g1;
            if (evals.evaluation_Grade >= 70)
            {
                evals.evaluation_Status = "Approved";
            } else
            {
                evals.evaluation_Status = "Rejected";
            }
            evals.evaluation_Date = DateTime.Now;
            await _context.SaveChangesAsync();
            Directory.Delete(filledFolder, true);
            await _actionLogger.LogActionAsync(fra.applicant_Name, fra.fra_Type, evaluator.evaluator_Name + " already evaluated the " + fra.research_Title + ".", 
                false, true, false, fra.fra_Id);

            return RedirectToAction("EvaluatorEvaluated", "Evaluator");
        }

        [Authorize(Roles = "Evaluator")]
        public async Task<IActionResult> GenerateEvalsForm(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            var evaluator = await _context.Evaluator.Where(e => e.UserId == user.Id).FirstOrDefaultAsync();
            var evaluations = await _context.Evaluations.Where(e => e.fra_Id == id && e.evaluator_Id == evaluator.evaluator_Id)
                .FirstOrDefaultAsync();
            var fr = await _context.FileRequirement.Where(f => f.fra_Id == evaluations.fra_Id && f.document_Type == "EvaluationForms"
                && f.file_Name.Contains(evaluator.evaluator_Name))
                .OrderBy(f => f.file_Name)
                .ToListAsync();
            ViewBag.Grade = evaluations.evaluation_Grade;
            ViewBag.Remarks = evaluations.evaluation_Status;
            return View(fr);
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

        public IActionResult PreviewFile(string id)
        {
            var file = _context.FileRequirement.FirstOrDefault(f => f.fr_Id == id);
            if(file != null)
            {
                return File(file.data, "application/pdf");
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> AddEvent(string eventTitle, DateTime startDate, DateTime endDate)
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
                    Visibility = "JustYou",
                    UserId = user.Id
                };

                _context.CalendarEvents.Add(addEvent);
                await _context.SaveChangesAsync();

                return RedirectToAction("EvaluatorDashboard");
            }
            return RedirectToAction("EvaluatorDashboard");
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
