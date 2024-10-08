using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RemcSys.Controllers
{
    public class EvaluatorController : Controller
    {
        [Authorize(Roles ="Evaluator")]
        public IActionResult EvaluatorPending()
        {
            return View();
        }

        public IActionResult EvaluatorMissed()
        {
            return View();
        }

        public IActionResult EvaluatorEvaluated()
        {
            return View();
        }

        public IActionResult EvaluationForm()
        {
            return View();
        }
    }
}
