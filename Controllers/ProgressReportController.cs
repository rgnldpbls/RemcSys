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

        public ProgressReportController(RemcDBContext context, UserManager<SystemUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> ProgressTracker()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var ufrwList = await _context.UniversityFundedResearches.Where(u => u.UserId == user.Id).FirstOrDefaultAsync();
            var efrwList = await _context.ExternallyFundedResearches.Where(e => e.UserId == user.Id).FirstOrDefaultAsync();
            var ufrlList = await _context.UniversityFundedResearchLoads.Where(u => u.UserId == user.Id).FirstOrDefaultAsync();

            if(ufrwList != null)
            {
                ViewBag.Id = ufrwList.ufrw_Id;
                ViewBag.Research = ufrwList.research_Title;
                ViewBag.Lead = ufrwList.team_Leader;
                ViewBag.Members = ufrwList.team_Members;
                ViewBag.Field = ufrwList.field_of_Study;

                int numReports = 4;
                int interval = ufrwList.project_Duration / numReports;
                List<DateTime> deadlines = new List<DateTime>();
                for (int i = 1; i <= numReports; i++)
                {
                    var deadline = ufrwList.start_Date.AddMonths(i * interval);
                    deadlines.Add(deadline);
                }

                if (deadlines.Count >= 1) ViewBag.Report1 = deadlines[0].ToString("MMMM d, yyyy");
                if (deadlines.Count >= 2) ViewBag.Report2 = deadlines[1].ToString("MMMM d, yyyy");
                if (deadlines.Count >= 3) ViewBag.Report3 = deadlines[2].ToString("MMMM d, yyyy");
                if (deadlines.Count >= 4) ViewBag.Report4 = deadlines[3].ToString("MMMM d, yyyy");

                var logs = await _context.ActionLogs
                    .Where(f => f.Name == ufrwList.team_Leader && f.isTeamLeader == true)
                    .OrderByDescending(log => log.Timestamp)
                    .ToListAsync();
                
                return View(logs);
            }
            else if(efrwList != null)
            {
                ViewBag.Id = efrwList.efrw_Id;
                ViewBag.Research = efrwList.research_Title;
                ViewBag.Lead = efrwList.team_Leader;
                ViewBag.Members = efrwList.team_Members;
                ViewBag.Field = efrwList.field_of_Study;

                int numReports = 4;
                int interval = efrwList.project_Duration / numReports;
                List<DateTime> deadlines = new List<DateTime>();
                for (int i = 1; i <= numReports; i++)
                {
                    var deadline = efrwList.start_Date.AddMonths(i * interval);
                    deadlines.Add(deadline);
                }

                if (deadlines.Count >= 1) ViewBag.Report1 = deadlines[0].ToString("MMMM d, yyyy");
                if (deadlines.Count >= 2) ViewBag.Report2 = deadlines[1].ToString("MMMM d, yyyy");
                if (deadlines.Count >= 3) ViewBag.Report3 = deadlines[2].ToString("MMMM d, yyyy");
                if (deadlines.Count >= 4) ViewBag.Report4 = deadlines[3].ToString("MMMM d, yyyy");

                var logs = await _context.ActionLogs
                    .Where(f => f.Name == efrwList.team_Leader && f.isTeamLeader == true)
                    .OrderByDescending(log => log.Timestamp)
                    .ToListAsync();

                return View(logs);
            }
            else if(ufrlList != null)
            {
                ViewBag.Id = ufrlList.ufrl_Id;
                ViewBag.Research = ufrlList.research_Title;
                ViewBag.Lead = ufrlList.team_Leader;
                ViewBag.Members = ufrlList.team_Members;
                ViewBag.Field = ufrlList.field_of_Study;

                int numReports = 4;
                int interval = ufrlList.project_Duration / numReports;
                List<DateTime> deadlines = new List<DateTime>();
                for (int i = 1; i <= numReports; i++)
                {
                    var deadline = ufrlList.start_Date.AddMonths(i * interval);
                    deadlines.Add(deadline);
                }

                if (deadlines.Count >= 1) ViewBag.Report1 = deadlines[0].ToString("MMMM d, yyyy");
                if (deadlines.Count >= 2) ViewBag.Report2 = deadlines[1].ToString("MMMM d, yyyy");
                if (deadlines.Count >= 3) ViewBag.Report3 = deadlines[2].ToString("MMMM d, yyyy");
                if (deadlines.Count >= 4) ViewBag.Report4 = deadlines[3].ToString("MMMM d, yyyy");

                var logs = await _context.ActionLogs
                    .Where(f => f.Name == ufrlList.team_Leader && f.isTeamLeader == true)
                    .OrderByDescending(log => log.Timestamp)
                    .ToListAsync();

                return View(logs);
            }

            return View(new List<ActionLog>());
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> UploadProgReport1(string id)
        {
            if(id == null)
            {
                return NotFound();
            }

            var ufrw = await _context.UniversityFundedResearches.FindAsync(id);
            var efrw = await _context.ExternallyFundedResearches.FindAsync(id);
            var ufrl = await _context.UniversityFundedResearchLoads.FindAsync(id);

            if(ufrw != null)
            {
                ViewBag.Id = ufrw.ufrw_Id;
                ViewBag.Research = ufrw.research_Title;
                ViewBag.Lead = ufrw.team_Leader;
                ViewBag.Members = ufrw.team_Members;
                var docu = await _context.GeneratedForms.Where(d => d.fra_Id == ufrw.fra_Id && d.FileName.Contains("Progress-Report")).ToListAsync();
                return View(docu);
            }
            else if(efrw != null)
            {
                ViewBag.Id = efrw.efrw_Id;
                ViewBag.Research = efrw.research_Title;
                ViewBag.Lead = efrw.team_Leader;
                ViewBag.Members = efrw.team_Members;
                var docu = await _context.GeneratedForms.Where(d => d.fra_Id == efrw.fra_Id && d.FileName.Contains("Progress-Report")).ToListAsync();
                return View(docu);
            }
            else if(ufrl != null)
            {
                ViewBag.Id = ufrl.ufrl_Id;
                ViewBag.Research = ufrl.research_Title;
                ViewBag.Lead = ufrl.team_Leader;
                ViewBag.Members = ufrl.team_Members;
                var docu = await _context.GeneratedForms.Where(d => d.fra_Id == ufrl.fra_Id && d.FileName.Contains("Progress-Report")).ToListAsync();
                return View(docu);
            }

            return View(new List<GeneratedForm>());
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
    }
}
