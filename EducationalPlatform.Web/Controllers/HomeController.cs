using EducationalPlatform.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace EducationalPlatform.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {

            if (User.IsInRole("Admin"))
                return RedirectToAction("Index", "Admin");

            if (User.IsInRole("Assistant"))
                return RedirectToAction("Index", "AssistantDashboard");

            if (User.IsInRole("Student"))
                return RedirectToAction("Index", "Student");

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
    }
}
