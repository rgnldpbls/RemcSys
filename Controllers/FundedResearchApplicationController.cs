using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using RemcSys.Areas.Identity.Data;
using RemcSys.Data;
using RemcSys.Models;
using Xceed.Words.NET;

namespace RemcSys.Controllers
{
    public class FundedResearchApplicationController : Controller
    {
        private readonly RemcDBContext _context;
        private readonly UserManager<SystemUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ActionLoggerService _actionLogger;

        public FundedResearchApplicationController(RemcDBContext context, UserManager<SystemUser> userManager, 
            IWebHostEnvironment environment, ActionLoggerService actionLogger)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _actionLogger = actionLogger;
        }

        // GET: FundedResearchApplication
        public async Task<IActionResult> Index()
        {
            return View(await _context.FundedResearchApplication.ToListAsync());
        }

        // GET: FundedResearchApplication/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fundedResearchApplication = await _context.FundedResearchApplication
                .FirstOrDefaultAsync(m => m.fra_Id == id);
            if (fundedResearchApplication == null)
            {
                return NotFound();
            }

            return View(fundedResearchApplication);
        }

        // GET: FundedResearchApplication/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: FundedResearchApplication/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("fra_Id,fra_Type,research_Title,applicant_Name,applicant_Email,college,branch,field_of_Study,application_Status,submission_Date,dts_No,UserId")] FundedResearchApplication fundedResearchApplication)
        {
            if (ModelState.IsValid)
            {
                _context.Add(fundedResearchApplication);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(fundedResearchApplication);
        }

        // GET: FundedResearchApplication/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fundedResearchApplication = await _context.FundedResearchApplication.FindAsync(id);
            if (fundedResearchApplication == null)
            {
                return NotFound();
            }
            return View(fundedResearchApplication);
        }

        // POST: FundedResearchApplication/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("fra_Id,fra_Type,research_Title,applicant_Name,applicant_Email,college,branch,field_of_Study,application_Status,submission_Date,dts_No,UserId")] FundedResearchApplication fundedResearchApplication)
        {
            if (id != fundedResearchApplication.fra_Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(fundedResearchApplication);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FundedResearchApplicationExists(fundedResearchApplication.fra_Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(fundedResearchApplication);
        }

        // GET: FundedResearchApplication/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fundedResearchApplication = await _context.FundedResearchApplication
                .FirstOrDefaultAsync(m => m.fra_Id == id);
            if (fundedResearchApplication == null)
            {
                return NotFound();
            }

            return View(fundedResearchApplication);
        }

        // POST: FundedResearchApplication/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var fundedResearchApplication = await _context.FundedResearchApplication.FindAsync(id);
            if (fundedResearchApplication != null)
            {
                _context.FundedResearchApplication.Remove(fundedResearchApplication);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FundedResearchApplicationExists(string id)
        {
            return _context.FundedResearchApplication.Any(e => e.fra_Id == id);
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> FormFill(string type)
        {
            var user = await _userManager.GetUserAsync(User);

            ViewBag.Name = user.Name;
            ViewBag.Type = type;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormFill(FormModel model, FundedResearchApplication fundedResearchApp)
        {
            var user = await _userManager.GetUserAsync(User);

            // Generate fra_Id with template "FRA" + current date + incremented number
            string currentDate = DateTime.Now.ToString("yyyyMMdd");
            var latestFra = await _context.FundedResearchApplication
                .AsNoTracking()
                .Where(f => f.fra_Id.Contains(currentDate))
                .OrderByDescending(f => f.fra_Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (latestFra != null)
            {
                // Extract the last number in the current date's fra_Id and increment it
                string[] parts = latestFra.fra_Id.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            // Generate the unique fra_Id
            fundedResearchApp.fra_Id = $"FRA-{currentDate}-{nextNumber:D3}";
            var existingEntry = _context.Entry(fundedResearchApp);
            if (existingEntry != null)
            {
                _context.Entry(fundedResearchApp).State = EntityState.Detached;
            }
            fundedResearchApp.fra_Type = model.ResearchType;
            fundedResearchApp.research_Title = model.ProjectTitle;
            fundedResearchApp.applicant_Name = model.ProjectLeader;
            fundedResearchApp.applicant_Email = user.Email;
            fundedResearchApp.team_Members = model.ProjectMembers.Split(new string[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).ToList();
            fundedResearchApp.college = user.College;
            fundedResearchApp.branch = user.Branch;
            fundedResearchApp.field_of_Study = model.StudyField;
            fundedResearchApp.application_Status = "Pending";
            fundedResearchApp.submission_Date = DateTime.Now;
            fundedResearchApp.dts_No = null;
            fundedResearchApp.UserId = user.Id;
            _context.FundedResearchApplication.Add(fundedResearchApp);
            _context.SaveChanges();

            string[] templates = {"Capsulized-Research-Proposal-Template.docx","Form-1-Term-of-Reference.docx","Form-2-Line-Item-Budget.docx",
                "Form-3-Schedule-of-Outputs.docx", "Form-4-Workplan.docx"};
            string filledFolder = Path.Combine(_environment.WebRootPath, "content", "outputs");
            Directory.CreateDirectory(filledFolder);

            List<string> filledFiles = new List<string>();

            foreach (var template in templates)
            {
                string templatePath = Path.Combine(_environment.WebRootPath, "content", "templates", template);
                string filledDocumentPath = Path.Combine(filledFolder, $"Generated_{template}");

                using (DocX document = DocX.Load(templatePath))
                {
                    document.ReplaceText("{{ProjectTitle}}", model.ProjectTitle);
                    document.ReplaceText("{{ProjectLead}}", model.ProjectLeader);
                    document.ReplaceText("{{ProjectMembers}}", model.ProjectMembers);
                    document.ReplaceText("{{ImplementInsti}}", model.ImplementingInstitution);
                    document.ReplaceText("{{CollabInsti}}", model.CollaboratingInstitution);
                    document.ReplaceText("{{ProjectDur}}", model.ProjectDuration);
                    document.ReplaceText("{{TotalProjectCost}}", model.TotalProjectCost);
                    document.ReplaceText("{{Objectives}}", model.Objectives);
                    document.ReplaceText("{{Scope}}", model.Scope);
                    document.ReplaceText("{{Methodology}}", model.Methodology);
                    document.ReplaceText("{{ProjectLeaderCaps}}", model.ProjectLeader.ToUpper());

                    document.SaveAs(filledDocumentPath);

                    filledFiles.Add($"Generated_{template}");
                }

                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filledDocumentPath);

                var doc = new GeneratedForm
                {
                    Id = Guid.NewGuid().ToString(),
                    FileName = $"Generated_{template}",
                    FileContent = fileBytes,
                    GeneratedAt = DateTime.Now,
                    fra_Id = fundedResearchApp.fra_Id
                };

                _context.GeneratedForms.Add(doc);
            }
            await _context.SaveChangesAsync();

            return RedirectToAction("GenerateInfo");
        }

        [Authorize(Roles = "Faculty")]
        public IActionResult GenerateInfo()
        {
            return View();
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> GeneratedDocuments()
        {
            var user = await _userManager.GetUserAsync(User);
            var fra = await _context.FundedResearchApplication.Where(f => f.UserId == user.Id).FirstOrDefaultAsync();
            var documents = await _context.GeneratedForms.Where(e => e.fra_Id == fra.fra_Id ).OrderBy(f => f.FileName).ToListAsync();
            return View(documents);
        }
        
        public async Task<IActionResult> Download(string id)
        {
            var document = await _context.GeneratedForms.FindAsync(id);

            if(document == null)
            {
                return NotFound();
            }
            return File(document.FileContent, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", document.FileName);
        }

        [HttpPost]
        public async Task<IActionResult> Reset(string fraId)
        {
            var researchApp = await _context.FundedResearchApplication
                .FirstOrDefaultAsync(f => f.fra_Id == fraId);

            if(researchApp != null)
            {
                _context.FundedResearchApplication.Remove(researchApp);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Faculty", "Home");
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> UploadFile()
        {
            var user = await _userManager.GetUserAsync(User);
            var fra = await _context.FundedResearchApplication.Where(s => s.application_Status == "Pending" && s.UserId == user.Id)
                .FirstOrDefaultAsync();
            ViewBag.Title = fra.research_Title;
            ViewBag.TeamLead = fra.applicant_Name;
            ViewBag.TeamMembers = fra.team_Members;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(IFormCollection files)
        {
            var user = await _userManager.GetUserAsync(User);
            var fra = await _context.FundedResearchApplication.Where(s => s.application_Status == "Pending" && s.UserId == user.Id)
                .FirstOrDefaultAsync();
            fra.application_Status = "Submitted";
            if (files != null && files.Files.Count > 0)
            {
                foreach(var file in files.Files)
                {
                    if(file.Length > 0)
                    {
                        using(var ms = new MemoryStream())
                        {
                            await file.CopyToAsync(ms);
                            var fileRequirement = new FileRequirement
                            {
                                fr_Id = Guid.NewGuid().ToString(),
                                file_Name = file.FileName,
                                file_Type = Path.GetExtension(file.FileName),
                                data = ms.ToArray(),
                                file_Status = "Pending",
                                file_Feedback = null,
                                file_Uploaded = DateTime.Now,
                                fra_Id = fra.fra_Id
                            };

                            _context.FileRequirement.Add(fileRequirement);
                        }
                    }
                }
            }
            /*await _actionLogger.LogActionAsync(user.Id, fra.fra_Id, "Application Form was uploaded.");*/
            await _actionLogger.LogActionAsync(user.Id, fra.fra_Id, fra.applicant_Name, fra.fra_Type, "Application was submitted.", "submitted application.");
            await _context.SaveChangesAsync();
            return RedirectToAction("ApplicationSuccess", "FundedResearchApplication");
        }

        [Authorize(Roles = "Faculty")]
        public IActionResult ApplicationSuccess()
        {
            return View();
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> ApplicationTracker()
        {
            var user = await _userManager.GetUserAsync(User);
            var fra = await _context.FundedResearchApplication.Where(s => s.application_Status == "Submitted" && s.UserId == user.Id)
                .FirstOrDefaultAsync();
            var logs = await _context.ActionLogs
                .Where(f => f.FraId == fra.fra_Id)
                .OrderByDescending(log => log.Timestamp).ToListAsync();
            ViewBag.FraId = fra.fra_Id;
            ViewBag.ProjTitle = fra.research_Title;
            ViewBag.TeamLead = fra.applicant_Name;
            ViewBag.TeamMembers = fra.team_Members;
            ViewBag.Field = fra.field_of_Study;
            ViewBag.Date = fra.submission_Date.ToString("MM-dd-yyyy");
            ViewBag.DateFormat = fra.submission_Date.ToString("MM-dd-yyyy HH:mm:ss");
            ViewBag.DTS = fra.dts_No;
            return View(logs);
        }

        [HttpPost]
        public async Task<IActionResult> SetDTS(string DTSNo, string fraId)
        {
            var fra = await _context.FundedResearchApplication
                .FirstOrDefaultAsync(f => f.fra_Id == fraId);

            if(fra == null)
            {
                return NotFound();
            }

            fra.dts_No = DTSNo;

            await _context.SaveChangesAsync();

            return RedirectToAction("ApplicationTracker");
        }

        [Authorize(Roles ="Faculty")]
        public async Task<IActionResult> ApplicationStatus()
        {
            var user = await _userManager.GetUserAsync(User);
            var fra = await _context.FundedResearchApplication.Where(s => s.application_Status == "Submitted" && s.UserId == user.Id)
                .FirstOrDefaultAsync();
            ViewBag.FraId = fra.fra_Id;
            ViewBag.ProjTitle = fra.research_Title;
            ViewBag.TeamLead = fra.applicant_Name;
            ViewBag.TeamMembers = fra.team_Members;
            ViewBag.Field = fra.field_of_Study;
            var fileRequirements = await _context.FileRequirement
                .Where(fr => fr.fra_Id == fra.fra_Id)
                .OrderBy(fr => fr.file_Name)
                .ToListAsync();
            return View(fileRequirements);
        }

        [HttpPost]
        public async Task<IActionResult> GoToEthics(FundedResearchEthics fundedResearchEthics)
        {
            var user = await _userManager.GetUserAsync(User);
            var fra = await _context.FundedResearchApplication
                .Where(s => s.application_Status == "Submitted" && s.UserId == user.Id)
                .FirstOrDefaultAsync();

            if(fra == null)
            {
                return NotFound("No submitted application found for this user.");
            }

            var existingEthics = await _context.FundedResearchEthics
                .FirstOrDefaultAsync(e => e.fra_Id == fra.fra_Id);

            if(existingEthics == null)
            {
                var fre = new FundedResearchEthics
                {
                    fre_Id = Guid.NewGuid().ToString(),
                    fra_Id = fra.fra_Id,
                    urec_No = null,
                    ethicClearance_Id = null,
                    completionCertificate_Id = null
                };
                _context.FundedResearchEthics.Add(fre);
                await _context.SaveChangesAsync();
                return RedirectToAction("UnderMaintenance", "Home");
            }
            return RedirectToAction("UnderMaintenance", "Home");
        }

        public async Task<IActionResult> PreviewFile(string id)
        {
            var fileRequirement = await _context.FileRequirement.FindAsync(id);
            if(fileRequirement == null)
            {
                return NotFound();
            }

            if(fileRequirement.file_Type == ".pdf")
            {
                return File(fileRequirement.data, "application/pdf");
            }

            return BadRequest("Only PDF files can be previewed.");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateFile(string id, IFormFile newFile)
        {
            var fileRequirement = await _context.FileRequirement.FindAsync(id);
            if( fileRequirement == null)
            {
                return NotFound();
            }

            if(newFile != null && newFile.Length > 0)
            {
                using (var memoryStream = new  MemoryStream())
                {
                    await newFile.CopyToAsync(memoryStream);
                    fileRequirement.data = memoryStream.ToArray();
                    fileRequirement.file_Name = newFile.FileName;
                    fileRequirement.file_Type = Path.GetExtension(newFile.FileName);
                    fileRequirement.file_Uploaded = DateTime.Now;
                    fileRequirement.file_Status = "Pending";
                    fileRequirement.file_Feedback = null;
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ApplicationStatus");
        }

        public IActionResult EvaluationResult()
        {
            return View();
        }
    }
}
