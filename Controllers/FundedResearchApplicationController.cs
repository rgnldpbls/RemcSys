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

        public FundedResearchApplicationController(RemcDBContext context, UserManager<SystemUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
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

            return View("ListDocuments", filledFiles);
        }

        [Authorize(Roles = "Faculty")]
        public IActionResult ListDocuments()
        {
            return View();
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> GeneratedDocuments()
        {
            var documents = await _context.GeneratedForms.OrderBy(f => f.FileName).ToListAsync();
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

        public ActionResult Download2(string fileName)
        {
            string filePath = Path.Combine(_environment.WebRootPath, "content", "outputs", fileName);
            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
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
        public async Task<IActionResult> AddFundedResearch(string type)
        {
            var user = await _userManager.GetUserAsync(User);

            ViewBag.Name = user.Name;
            ViewBag.Email = user.Email;
            ViewBag.College = user.College;
            ViewBag.Branch = user.Branch;
            ViewBag.Type = type;
            ViewBag.Status = "Pending";
            ViewBag.Date = DateTime.Now;
            ViewBag.DTSNo = null;
            ViewBag.UserId = user.Id;
            return View();
        }

        /*[HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddFundedResearch(FundedResearchApplication fra, List<ResearchStaff> researchStaffList)
        {
            if(ModelState.IsValid)
            {
                TempData["FundedResearchApplication"] = JsonConvert.SerializeObject(fra);
                TempData["ResearchStaffList"] = JsonConvert.SerializeObject(researchStaffList);
                return RedirectToAction("UploadFile");
            }
            return View(fra);
        }*/

        /*[Authorize(Roles = "Faculty")]
        public IActionResult UploadFile()
        {
            var researchAppJson = TempData["FundedResearchApplication"] as string;
            var researchStaffListJson = TempData["ResearchStaffList"] as string;

            var researchApp = JsonConvert.DeserializeObject<FundedResearchApplication>(researchAppJson);
            ViewBag.ResearchTitle = researchApp.research_Title;
            ViewBag.Proponent = researchApp.applicant_Name;
            ViewBag.ResearchStaff = JsonConvert.DeserializeObject<List<ResearchStaff>>(researchStaffListJson);

            TempData.Keep("FundedResearchApplication");
            TempData.Keep("ResearchStaffList");
            return View();
        }*/

        /*[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(List<IFormFile> files)
        {
            var researchAppJson = TempData["FundedResearchApplication"] as string;
            var researchStaffListJson = TempData["ResearchStaffList"] as string;

            var researchApp = JsonConvert.DeserializeObject<FundedResearchApplication>(researchAppJson);
            var researchStaffList = JsonConvert.DeserializeObject<List<ResearchStaff>>(researchStaffListJson);

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
            researchApp.fra_Id = $"FRA-{currentDate}-{nextNumber:D3}";
            var existingEntry = _context.Entry(researchApp);
            if (existingEntry != null)
            {
                _context.Entry(researchApp).State = EntityState.Detached;
            }
            _context.FundedResearchApplication.Add(researchApp);
            _context.SaveChanges();

            //Save Research Staff
            foreach (var staff in researchStaffList)
            {
                staff.fra_Id = researchApp.fra_Id;
                _context.ResearchStaff.Add(staff);
            }
            _context.SaveChanges();

            if (files == null || files.Count == 0)
            {
                ModelState.AddModelError("files", "Please upload at least one file.");
                return View();
            }
            List<FileRequirement> fileList = new List<FileRequirement>();

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await file.CopyToAsync(memoryStream);
                        var fileData = new FileRequirement
                        {
                            file_Name = file.Name,
                            file_Type = file.ContentType,
                            data = memoryStream.ToArray(),
                            file_Status = "Pending",
                            file_Uploaded = DateTime.UtcNow,
                            fra_Id = researchApp.fra_Id
                        };
                        fileList.Add(fileData);
                    }
                }
            }
            _context.FileRequirement.AddRange(fileList);
            await _context.SaveChangesAsync();

            return RedirectToAction("ApplicationSuccess");
        }*/

        [Authorize(Roles = "Faculty")]
        public IActionResult ApplicationSuccess()
        {
            return View();
        }
    }
}
