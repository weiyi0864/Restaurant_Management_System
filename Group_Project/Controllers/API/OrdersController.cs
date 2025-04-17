//using Microsoft.AspNetCore.Authentication.JwtBearer;
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

namespace RestaurantManagementSystem.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // If admin, return all orders
            if (User.IsInRole("Admin"))
            {
                return await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.MenuItem)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();
            }

            // Otherwise return only the user's orders
            return await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Reservation)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if the user is authorized to view this order
            if (order.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return order;
        }

        // POST: api/Orders
        [HttpPost]
        public async Task<ActionResult<Order>> PostOrder(OrderViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Validate the reservation if provided
            if (model.ReservationId.HasValue)
            {
                var reservation = await _context.Reservations.FindAsync(model.ReservationId.Value);
                if (reservation == null)
                {
                    return BadRequest(new { message = "Invalid reservation" });
                }

                if (reservation.UserId != userId)
                {
                    return Forbid();
                }
            }

            // Validate the menu items
            var menuItemIds = model.Items.Select(i => i.MenuItemId).ToList();
            var menuItems = await _context.MenuItems
                .Where(mi => menuItemIds.Contains(mi.Id) && mi.IsAvailable)
                .ToDictionaryAsync(mi => mi.Id, mi => mi);

            if (menuItems.Count != menuItemIds.Count)
            {
                return BadRequest(new { message = "One or more menu items are invalid or unavailable" });
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

            var orderResponse = new
            {
                order.Id,
                order.UserId,
                order.ReservationId,
                order.Status,
                order.TotalAmount,
                order.CreatedAt,
                OrderItems = order.OrderItems.Select(oi => new
                {
                    oi.Id,
                    oi.MenuItemId,
                    oi.Quantity,
                    oi.Price
                }).ToList()
            };

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, orderResponse);
        }

        // PUT: api/Orders/5/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Only allow admins to update status (except cancellation)
            if (status != OrderStatus.Cancelled && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // For cancellation, check if it's the user's own order and if it's not already processed
            if (status == OrderStatus.Cancelled)
            {
                if (order.UserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                if (order.Status != OrderStatus.Created)
                {
                    return BadRequest(new { message = "Cannot cancel an order that is already being processed" });
                }
            }

            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if the user is authorized to delete this order
            if (order.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Only allow cancellation if the order is still in 'Created' status
            if (order.Status == OrderStatus.Created)
            {
                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return NoContent();
            }

            return BadRequest(new { message = "Cannot cancel an order that is already being processed" });
        }
    }
}