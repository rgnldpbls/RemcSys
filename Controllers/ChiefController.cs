using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemcSys.Data;
using RemcSys.Models;

namespace RemcSys.Controllers
{
    public class ChiefController : Controller
    {
        private readonly RemcDBContext _context;

        public ChiefController(RemcDBContext context)
        {
            _context = context;
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
            var fileReq = _context.FileRequirement.FirstOrDefault();
            var application = from app in _context.FundedResearchApplication
                              .Where(f => f.fra_Type == "University Funded Research" && f.fra_Id == fileReq.fra_Id)
                              select app;

            if (!String.IsNullOrEmpty(searchString))
            {
                application = application.Where(s => s.research_Title.Contains(searchString));
            }

            return View(await application.ToListAsync());
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
        public IActionResult SaveFeedback(string fileId, string fileStatus, string fileFeedback)
        {
            var fileReq = _context.FileRequirement.Find(fileId);
            if (fileReq != null)
            {
                fileReq.file_Status = fileStatus;
                fileReq.file_Feedback = fileFeedback;

                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult UpdateStatus(string fr_Id, string newStatus)
        {
            var file = _context.FileRequirement.FirstOrDefault(f => f.fr_Id == fr_Id);
            if(file != null)
            {
                file.file_Status = newStatus;
                _context.SaveChanges();
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
