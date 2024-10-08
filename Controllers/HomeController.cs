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
            var pendingResearchApp = await _context.FundedResearchApplication.AnyAsync(f => f.application_Status == "Pending" && f.UserId == user.Id);
            var submittedResearchApp = await _context.FundedResearchApplication.AnyAsync(f => f.application_Status == "Submitted" && f.UserId == user.Id);
            var evalResearchApp = await _context.FundedResearchApplication.AnyAsync(f => f.application_Status == "UnderEvaluation" && f.UserId == user.Id);

            ViewBag.Pending = pendingResearchApp;
            ViewBag.Submitted = submittedResearchApp;
            ViewBag.Evals = evalResearchApp;
            return View();
        }

        [Authorize(Roles = "Faculty")]
        public IActionResult Forms()
        {
            string folderPath = Path.Combine(_hostingEnvironment.WebRootPath);
            string[] fileExtensions = { "*.docx", "*.pdf" };
            List<Document> files = new List<Document>();
            foreach (var extension in fileExtensions)
            {
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
            }
            var exec = files.Where(f => f.FileType == ".pdf").ToList();
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
            var haveEvaluator = await _context.Evaluator.AnyAsync(e => e.UserId == user.Id);
            if (!haveEvaluator)
            {
                var evaluator = new Evaluator
                {
                    evaluator_Name = user.Name,
                    evaluator_Email = user.Email,
                    field_of_Interest = ["Science","Engineering, Architecture, Design, and Built Environment"],
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
