using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemcSys.Models;
using System.Diagnostics;

namespace RemcSys.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment hostingEnvironment)
        {
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
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

        [Authorize(Roles = "TeamLeader")]
        public IActionResult TeamLeader()
        {
            return View();
        }

        [Authorize(Roles = "TeamLeader")]
        public IActionResult FRType()
        {
           return View();
        }

        [Authorize(Roles = "TeamLeader")]
        public IActionResult Eligibility(string type)
        {
            ViewBag.Type = type;
            return View();
        }

        public IActionResult Forms()
        {
            string folderPath = Path.Combine(_hostingEnvironment.WebRootPath);
            string[] fileExtensions = { "*.doc","*.docx", "*.pdf" };
            List<Document> files = new List<Document>();
            foreach(var extension in fileExtensions)
            {
                foreach(var file in Directory.GetFiles(folderPath, extension, SearchOption.AllDirectories))
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
            var allFiles = files;
            var docu = allFiles.Where(f => f.FileType != ".pdf").ToList();
            var exec = allFiles.Where(f => f.FileType == ".pdf").ToList();
            ViewBag.docu = docu;
            ViewBag.exec = exec;
            return View();
        }

        public IActionResult PdfViewer(string pdf)
        {
            ViewBag.PdfFilePath = pdf;
            return View();
        }

        [Authorize(Roles = "Evaluator")]
        public IActionResult Evaluator()
        {
            return View();
        }

        [Authorize(Roles = "Chief")]
        public IActionResult Chief()
        {
            return View();
        }
    }
}
