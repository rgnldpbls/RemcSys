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

        public IActionResult UFRAppClosed()
        {
            return View();
        }

        public IActionResult EFRAppClosed()
        {
            return View();
        }

        public IActionResult UFRLAppClosed()
        {
            return View();
        }

        [Authorize(Roles = "Faculty")]
        public IActionResult Faculty()
        {
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
            var isUFRApp = _context.Settings.First().isUFRApplication;
            if(!isUFRApp && type == "University Funded Research")
            {
                return RedirectToAction("UFRAppClosed", "Home");
            }

            var isEFRApp = _context.Settings.First().isEFRApplication;
            if(!isEFRApp && type == "Externally Funded Research")
            {
                return RedirectToAction("EFRAppClosed", "Home");
            }

            var isUFRLApp = _context.Settings.First().isUFRLApplication;
            if(!isUFRLApp && type == "University Funded Research Load")
            {
                return RedirectToAction("UFRLAppClosed", "Home");

            }

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
                    field_of_Interest = ["Engineering, Architecture, Design, and Built Environment", "Science"],
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
