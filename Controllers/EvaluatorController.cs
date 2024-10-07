using Microsoft.AspNetCore.Mvc;

namespace RemcSys.Controllers
{
    public class EvaluatorController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult EvaluationForm()
        {
            return View();
        }

        public IActionResult EvaluatorEvaluated()
        {
            return View();
        }
    }
}
