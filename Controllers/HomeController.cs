using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemcSys.Areas.Identity.Data;
using RemcSys.Data;
using RemcSys.Models;
using System.Diagnostics;

namespace RemcSys.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly UserManager<SystemUser> _userManager;
        private readonly RemcDBContext _context;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment hostingEnvironment, UserManager<SystemUser> userManager, RemcDBContext context)
        {
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
            _userManager = userManager;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public IActionResult UnderMaintenance()
        {
            return View();
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> Faculty()
        {
            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                return  NotFound();
            }

            var fra = await _context.FundedResearchApplication.FirstOrDefaultAsync(f => f.UserId == user.Id);

            var pendingApp = await _context.FundedResearchApplication
                .AnyAsync(f => f.application_Status == "Pending" && f.UserId == user.Id);

            ViewBag.PendingApp = pendingApp;

            var submittedUFR = await _context.FundedResearchApplication.AnyAsync(f => f.application_Status == "Submitted" && 
                f.UserId == user.Id && f.fra_Type == "University Funded Research" && f.isArchive == false);
            var evalUFR = await _context.FundedResearchApplication.AnyAsync(f => f.application_Status == "UnderEvaluation" && 
                f.UserId == user.Id && f.fra_Type == "University Funded Research");
            var approvedUFR = await _context.FundedResearchApplication.AnyAsync(f => f.application_Status == "Approved" &&
                f.UserId == user.Id && f.fra_Type == "University Funded Research");
            var rejectedUFR = await _context.FundedResearchApplication.AnyAsync(f => f.application_Status == "Rejected" &&
                f.UserId == user.Id && f.fra_Type == "University Funded Research" && f.isArchive == false);
            var proceedUFR = await _context.FundedResearchApplication.AnyAsync(f => f.application_Status == "Proceed" && 
                f.UserId == user.Id && f.fra_Type == "University Funded Research");

            ViewBag.SubmittedUFR = submittedUFR;
            ViewBag.EvalsUFR = evalUFR;
            ViewBag.ApprovedUFR = approvedUFR;
            ViewBag.RejectedUFR = rejectedUFR;
            ViewBag.ProceedUFR = proceedUFR;

            var submitted = await _context.FundedResearchApplication.AnyAsync(f => f.application_Status == "Submitted" &&
                f.UserId == user.Id && f.fra_Type != "University Funded Research" && f.isArchive == false);
            var approved = await _context.FundedResearchApplication.AnyAsync(f => f.application_Status == "Approved" &&
                f.UserId == user.Id && f.fra_Type != "University Funded Research");
            var proceed = await _context.FundedResearchApplication.AnyAsync(f => f.application_Status == "Proceed" &&
                f.UserId == user.Id && f.fra_Type != "University Funded Research");

            ViewBag.Submitted = submitted;
            ViewBag.Approved = approved;
            ViewBag.Proceed = proceed;
            return View();
        }

        [Authorize(Roles = "Faculty")]
        public IActionResult Forms()
        {
            string folderPath = Path.Combine(_hostingEnvironment.WebRootPath, "forms");
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
            return View();
        }

        [Authorize(Roles = "Faculty")]
        public IActionResult FRType()
        {
           return View();
        }

        [Authorize(Roles = "Faculty")]
        public IActionResult Eligibility(string type)
        {
            ViewBag.Type = type;
            return View();
        }
        
        [Authorize(Roles = "Evaluator")]
        public async Task<IActionResult> Evaluator()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }
            var haveEvaluator = await _context.Evaluator.AnyAsync(e => e.UserId == user.Id);
            if (!haveEvaluator)
            {
                var evaluator = new Evaluator
                {
                    evaluator_Name = user.Name,
                    evaluator_Email = user.Email,
                    field_of_Interest = ["Science","Engineering, Architecture, Design, and Built Environment", 
                        "Computer Science and Information System Technology"],
                    UserId = user.Id,
                    UserType = null,
                    center = ["REMC"]
                };
                _context.Evaluator.Add(evaluator);
                _context.SaveChanges();
                return View();
            }
            return View();
        }

        [Authorize(Roles = "Chief")]
        public IActionResult Chief()
        {
            return View();
        }
    }
}
