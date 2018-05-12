using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NZSmartIDReader.Api.Models;
using NZSmartIDReader.Core;

namespace NZSmartIDReader.Api.Controllers
{
    public class HomeController : Controller
    {
        private readonly ISmartIdReader SmartIdReader;

        public HomeController(ISmartIdReader smartIdReader)
        {
            SmartIdReader = smartIdReader;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public IActionResult Upload(IFormFile drivingLicence)
        {
            var result = SmartIdReader.IsValid(drivingLicence.OpenReadStream());

            ViewData["Message"] = result ? "Your picture is from a NZ driver licence" : "Your picture is NOT from a NZ driver licence";

            return View("Index");
        }
    }
}
