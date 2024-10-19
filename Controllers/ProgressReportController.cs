using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemcSys.Areas.Identity.Data;
using RemcSys.Data;
using RemcSys.Models;

namespace RemcSys.Controllers
{
    public class ProgressReportController : Controller
    {
        private readonly RemcDBContext _context;
        private readonly UserManager<SystemUser> _userManager;
        private readonly ActionLoggerService _actionLogger;

        public ProgressReportController(RemcDBContext context, UserManager<SystemUser> userManager, ActionLoggerService actionLogger)
        {
            _context = context;
            _userManager = userManager;
            _actionLogger = actionLogger;
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> ProgressTracker()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var fr = await _context.FundedResearches.Where(f => f.UserId == user.Id).FirstOrDefaultAsync();
            if (fr == null)
            {
                return NotFound();
            }
            ViewBag.Id = fr.fr_Id;
            ViewBag.Research = fr.research_Title;
            ViewBag.Lead = fr.team_Leader;
            ViewBag.Members = fr.team_Members;
            ViewBag.Field = fr.field_of_Study;
            ViewBag.Status = fr.status;
            ViewBag.Extend1 = fr.isExtension1;
            ViewBag.Extend2 = fr.isExtension2;

            int numReports = 4;
            int interval = fr.project_Duration / numReports;
            List<DateTime> deadlines = new List<DateTime>();
            for (int i = 1; i <= numReports; i++)
            {
                var deadline = fr.start_Date.AddMonths(i * interval);
                deadlines.Add(deadline);
            }

            if (deadlines.Count >= 1) ViewBag.Report1 = deadlines[0];
            if (deadlines.Count >= 2) ViewBag.Report2 = deadlines[1];
            if (deadlines.Count >= 3) ViewBag.Report3 = deadlines[2];
            if (deadlines.Count >= 4) ViewBag.Report4 = deadlines[3];

            ViewBag.Report5 = fr.end_Date.AddMonths(interval);
            ViewBag.Report6 = fr.end_Date.AddMonths(interval * 2);

            var existingRep = await _context.ProgressReports
                .Where(pr => pr.fr_Id == fr.fr_Id)
                .ToListAsync();

            int reportNum = existingRep.Count;
            ViewBag.Count = reportNum;


            var logs = await _context.ActionLogs
                    .Where(f => f.Name == fr.team_Leader && f.isTeamLeader == true)
                    .OrderByDescending(log => log.Timestamp)
                    .ToListAsync();
            
            return View(logs);
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> UploadProgReport(string id)
        {
            if(id == null)
            {
                return NotFound();
            }

            var fr = await _context.FundedResearches.FindAsync(id);
            if(fr == null)
            {
                return NotFound();
            }

            ViewBag.Id = fr.fr_Id;
            ViewBag.Research = fr.research_Title;
            ViewBag.Lead = fr.team_Leader;
            ViewBag.Members = fr.team_Members;

            var docu = await _context.GeneratedForms
                .Where(d => d.fra_Id == fr.fra_Id && d.FileName.Contains("Progress-Report"))
                .ToListAsync();
            return View(docu);
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
        public async Task<IActionResult> SubmitProgressReport(IFormFile file, string id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var fr = await _context.FundedResearches.FindAsync(id);
            if(fr == null)
            {
                return NotFound();
            }

            if (file == null || file.Length == 0)
            {
                return NotFound("There is no uploaded file. Please upload the file and try to submit again.");
            }

            var existingReports = await _context.ProgressReports
                .Where(pr => pr.fr_Id == id)
                .ToListAsync();

            int reportNum = existingReports.Count + 1;
            string docuType = $"Report No.{reportNum}";

            byte[] pdfData;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                pdfData = ms.ToArray();
                var progReport = new ProgressReport
                {
                    pr_Id = Guid.NewGuid().ToString(),
                    file_Name = file.FileName,
                    file_Type = Path.GetExtension(file.FileName),
                    data = pdfData,
                    file_Status = "Pending",
                    document_Type = docuType,
                    file_Feedback = null,
                    file_Uploaded = DateTime.Now,
                    fr_Id = fr.fr_Id
                };
                _context.ProgressReports.Add(progReport);
                fr.status = $"Submitted {docuType}";
            }
            await _actionLogger.LogActionAsync(fr.team_Leader, fr.fr_Type, 
                fr.research_Title + $" already uploaded the {docuType}.", true, true, false, fr.fra_Id);
            await _context.SaveChangesAsync();
            return RedirectToAction("ProgressReportStatus", new { id = id });
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> ProgressReportStatus(string id)
        {
            if (id == null)
            {
                return NotFound();
            }
            
            var fr = await _context.FundedResearches.FindAsync(id);
            if(fr == null)
            {
                return NotFound();
            }

            ViewBag.Id = fr.fr_Id;
            ViewBag.Research = fr.research_Title;
            ViewBag.Lead = fr.team_Leader;
            ViewBag.Members = fr.team_Members;
            ViewBag.Field = fr.field_of_Study;

            var progReport = await _context.ProgressReports
                    .Where(pr => pr.fr_Id == id && pr.file_Type == ".pdf")
                    .OrderBy(pr => pr.file_Name)
                    .ToListAsync();

            return View(progReport);
        }

        public async Task<IActionResult> PreviewFile(string id)
        {
            var progReport = await _context.ProgressReports.FindAsync(id);
            if (progReport == null)
            {
                return NotFound();
            }

            if (progReport.file_Type == ".pdf")
            {
                return File(progReport.data, "application/pdf");
            }

            return BadRequest("Only PDF files can be previewed.");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateFile(string id, IFormFile newFile)
        {
            var progReport = await _context.ProgressReports.FindAsync(id);
            if (progReport == null)
            {
                return NotFound();
            }

            if (newFile != null && newFile.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await newFile.CopyToAsync(memoryStream);
                    progReport.data = memoryStream.ToArray();
                    progReport.file_Name = newFile.FileName;
                    progReport.file_Type = Path.GetExtension(newFile.FileName);
                    progReport.file_Uploaded = DateTime.Now;
                    progReport.file_Status = "Pending";
                    progReport.file_Feedback = null;
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ProgressReportStatus", new { id = progReport.fr_Id });
        }
    }
}
