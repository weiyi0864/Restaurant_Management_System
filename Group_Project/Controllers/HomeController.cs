using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Menu()
        {
            var menuItems = _context.MenuItems
                .Where(m => m.IsAvailable)
                .OrderBy(m => m.Category)
                .ToList();

            return View(menuItems);
        }

        public IActionResult Reservation()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> AvailableTimes(DateTime date)
        {
            // Simplified availability logic - just returning time slots
            // In a real system, this would check capacity and existing reservations

            var openingTime = new TimeSpan(11, 0, 0); // 11:00 AM
            var closingTime = new TimeSpan(22, 0, 0); // 10:00 PM
            var interval = new TimeSpan(0, 30, 0); // 30 minutes

            var currentTime = date.Date.Add(openingTime);
            var endTime = date.Date.Add(closingTime);

            var availableTimes = Enumerable.Range(0, (int)((endTime - currentTime).TotalMinutes / interval.TotalMinutes))
                .Select(i => currentTime.AddMinutes(i * interval.TotalMinutes))
                .ToList();

            return Json(availableTimes);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}