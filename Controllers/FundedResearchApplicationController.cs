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
using Newtonsoft.Json;
using RemcSys.Areas.Identity.Data;
using RemcSys.Data;
using RemcSys.Models;

namespace RemcSys.Controllers
{
    public class FundedResearchApplicationController : Controller
    {
        private readonly RemcDBContext _context;
        private readonly UserManager<SystemUser> _userManager;

        public FundedResearchApplicationController(RemcDBContext context, UserManager<SystemUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

        [HttpPost]
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
        }

        [Authorize(Roles = "Faculty")]
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
        }

        [HttpPost]
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
        }

        [Authorize(Roles = "Faculty")]
        public IActionResult ApplicationSuccess()
        {
            return View();
        }
    }
}
