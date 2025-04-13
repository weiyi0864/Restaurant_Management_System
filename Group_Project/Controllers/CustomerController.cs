using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Models;
using RestaurantManagementSystem.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RestaurantManagementSystem.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get upcoming reservations
            var upcomingReservations = await _context.Reservations
                .Where(r => r.UserId == userId && r.ReservationTime > DateTime.UtcNow && r.Status != ReservationStatus.Cancelled)
                .OrderBy(r => r.ReservationTime)
                .ToListAsync();

            // Get recent orders
            var recentOrders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewData["UpcomingReservations"] = upcomingReservations;
            ViewData["RecentOrders"] = recentOrders;

            return View();
        }

        public async Task<IActionResult> Reservations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var reservations = await _context.Reservations
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.ReservationTime)
                .ToListAsync();

            return View(reservations);
        }

        public IActionResult CreateReservation(DateTime? reservationTime = null, int? partySize = null, string specialRequests = null)
        {
            // If parameters exist, create a prefilled model
            if (reservationTime.HasValue && partySize.HasValue)
            {
                var model = new ReservationViewModel
                {
                    ReservationTime = reservationTime.Value,
                    PartySize = partySize.Value,
                    SpecialRequests = specialRequests
                };
                return View(model);
            }

            // Otherwise return an empty model
            return View(new ReservationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReservation(ReservationViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var reservation = new Reservation
                {
                    UserId = userId,
                    ReservationTime = model.ReservationTime,
                    PartySize = model.PartySize,
                    SpecialRequests = model.SpecialRequests,
                    Status = ReservationStatus.Confirmed // Auto-confirm for simplicity
                };

                _context.Add(reservation);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Reservations));
            }

            return View(model);
        }

        public async Task<IActionResult> ReservationDetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var reservation = await _context.Reservations
                .Include(r => r.Orders)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReservation(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reservation == null)
            {
                return NotFound();
            }

            // Can only cancel if the reservation is in the future
            if (reservation.ReservationTime > DateTime.UtcNow)
            {
                reservation.Status = ReservationStatus.Cancelled;
                await _context.SaveChangesAsync();
            }
            else
            {
                ModelState.AddModelError("", "Cannot cancel a past reservation");
                return View("ReservationDetails", reservation);
            }

            return RedirectToAction(nameof(Reservations));
        }

        public async Task<IActionResult> Orders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Reservation)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.Orders
                .Include(o => o.Reservation)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        public IActionResult CreateOrder(int? reservationId)
        {
            ViewData["ReservationId"] = reservationId;

            var menuItems = _context.MenuItems
                .Where(mi => mi.IsAvailable)
                .OrderBy(mi => mi.Category)
                .ThenBy(mi => mi.Name)
                .ToList();

            ViewData["MenuItems"] = menuItems;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder(OrderViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Validate the reservation if provided
            if (model.ReservationId.HasValue)
            {
                var reservation = await _context.Reservations.FindAsync(model.ReservationId.Value);
                if (reservation == null || reservation.UserId != userId)
                {
                    ModelState.AddModelError("", "Invalid reservation");
                    return View(model);
                }
            }

            // Validate the menu items
            if (model.Items == null || !model.Items.Any())
            {
                ModelState.AddModelError("", "Order must contain at least one item");
                return View(model);
            }

            var menuItemIds = model.Items.Select(i => i.MenuItemId).ToList();
            var menuItems = await _context.MenuItems
                .Where(mi => menuItemIds.Contains(mi.Id) && mi.IsAvailable)
                .ToDictionaryAsync(mi => mi.Id, mi => mi);

            if (menuItems.Count != menuItemIds.Count)
            {
                ModelState.AddModelError("", "One or more menu items are invalid or unavailable");
                return View(model);
            }

            // Create the order
            var order = new Order
            {
                UserId = userId,
                ReservationId = model.ReservationId,
                Status = OrderStatus.Created,
                TotalAmount = 0, // Will be calculated below
                OrderItems = new List<OrderItem>()
            };

            // Add order items
            foreach (var item in model.Items)
            {
                var menuItem = menuItems[item.MenuItemId];
                var orderItem = new OrderItem
                {
                    MenuItemId = menuItem.Id,
                    Quantity = item.Quantity,
                    Price = menuItem.Price
                };

                order.OrderItems.Add(orderItem);
                order.TotalAmount += orderItem.Price * orderItem.Quantity;
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(OrderDetails), new { id = order.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            // Can only cancel if the order is still in Created status
            if (order.Status == OrderStatus.Created)
            {
                order.Status = OrderStatus.Cancelled;
                await _context.SaveChangesAsync();
            }
            else
            {
                ModelState.AddModelError("", "Cannot cancel an order that is already being processed");
                return View("OrderDetails", order);
            }

            return RedirectToAction(nameof(Orders));
        }
    }
}