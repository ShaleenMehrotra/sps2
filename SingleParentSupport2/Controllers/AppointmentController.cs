using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SingleParentSupport2.Models;

namespace SingleParentSupport2.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var appointments = await _context.Appointments
                .Include(a => a.Volunteer)
                .Where(a => a.UserId == user.Id)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            if (appointments.Count != 0)
            {
                return View(appointments);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Schedule(AppointmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Log validation errors for debugging
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                return View(model);
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                var appointment = new Appointment
                {
                    UserId = user.Id,
                    //VolunteerId = model.VolunteerId.ToString(),
                    Purpose = model.Purpose,
                    AppointmentDate = model.AppointmentDate,
                    Status = "Scheduled"
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                return RedirectToAction("Confirmation");
            }

            // reload upcoming appointments if invalid
            var userAppointments = await _context.Appointments
                .Include(a => a.Volunteer)
                .Where(a => a.UserId == _userManager.GetUserId(User) && a.AppointmentDate >= DateTime.Today)
                .ToListAsync();

            return View("Index", userAppointments);
        }

        public IActionResult Confirmation()
        {
            return View();
        }

        public IActionResult Reschedule(int id)
        {
            // Logic to get appointment by id would go here
            return View();
        }

        public async Task<IActionResult> Cancel(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
