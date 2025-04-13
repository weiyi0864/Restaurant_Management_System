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
    public class ReservationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReservationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Reservations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Reservation>>> GetReservations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // If admin, return all reservations
            if (User.IsInRole("Admin"))
            {
                return await _context.Reservations
                    .Include(r => r.User)
                    .OrderByDescending(r => r.ReservationTime)
                    .ToListAsync();
            }

            // Otherwise return only the user's reservations
            return await _context.Reservations
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.ReservationTime)
                .ToListAsync();
        }

        // GET: api/Reservations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Reservation>> GetReservation(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if the user is authorized to view this reservation
            if (reservation.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return reservation;
        }

        // GET: api/Reservations/available-times/2023-04-15
        [HttpGet("available-times/{date}")]
        [AllowAnonymous]
        public ActionResult<IEnumerable<DateTime>> GetAvailableTimes(DateTime date)
        {
            // Simplified availability logic - just returning time slots
            // In a real system, this would check capacity and existing reservations

            var openingTime = new TimeSpan(11, 0, 0); // 11:00 AM
            var closingTime = new TimeSpan(22, 0, 0); // 10:00 PM
            var interval = new TimeSpan(0, 30, 0); // 30 minutes

            var availableTimes = new List<DateTime>();
            var currentTime = date.Date.Add(openingTime);
            var endTime = date.Date.Add(closingTime);

            while (currentTime < endTime)
            {
                availableTimes.Add(currentTime);
                currentTime = currentTime.Add(interval);
            }

            return availableTimes;
        }

        // POST: api/Reservations
        [HttpPost]
        public async Task<ActionResult<Reservation>> PostReservation(ReservationViewModel model)
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

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, reservation);
        }

        // PUT: api/Reservations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReservation(int id, ReservationViewModel model)
        {
            var reservation = await _context.Reservations.FindAsync(id);

            if (reservation == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if the user is authorized to update this reservation
            if (reservation.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Update fields
            reservation.ReservationTime = model.ReservationTime;
            reservation.PartySize = model.PartySize;
            reservation.SpecialRequests = model.SpecialRequests;
            reservation.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReservationExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // PUT: api/Reservations/5/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateReservationStatus(int id, [FromBody] ReservationStatus status)
        {
            var reservation = await _context.Reservations.FindAsync(id);

            if (reservation == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Only allow admins to update status (except cancellation)
            if (status != ReservationStatus.Cancelled && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // For cancellation, check if it's the user's own reservation
            if (status == ReservationStatus.Cancelled && reservation.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            reservation.Status = status;
            reservation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Reservations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if the user is authorized to delete this reservation
            if (reservation.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // If the reservation is in the future, allow cancellation
            if (reservation.ReservationTime > DateTime.UtcNow)
            {
                reservation.Status = ReservationStatus.Cancelled;
                reservation.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return NoContent();
            }

            return BadRequest(new { message = "Cannot cancel a past reservation" });
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.Id == id);
        }
    }
}
