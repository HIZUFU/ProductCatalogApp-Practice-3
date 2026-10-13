using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProductCatalogApp.Data;
using ProductCatalogApp.Models;

namespace ProductCatalogApp.Controllers
{
    [Authorize]
    public class SensorsController : Controller
    {
        private const int HistorySize = 20;

        private readonly ApplicationDbContext _context;

        public SensorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Sensors
        public async Task<IActionResult> Index(int? equipmentId)
        {
            var sensorsQuery = _context.Sensors
                .Include(sensor => sensor.Equipment)
                    .ThenInclude(equipment => equipment!.ProductionSite)
                .AsQueryable();

            if (equipmentId.HasValue)
            {
                sensorsQuery = sensorsQuery
                    .Where(sensor => sensor.EquipmentId == equipmentId.Value);
            }

            await LoadEquipmentAsync(equipmentId);

            var sensors = await sensorsQuery
                .OrderBy(sensor => sensor.Equipment!.Name)
                .ThenBy(sensor => sensor.Name)
                .ToListAsync();

            return View(sensors);
        }

        // GET: Sensors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sensor = await _context.Sensors
                .Include(sensor => sensor.Equipment)
                    .ThenInclude(equipment => equipment!.ProductionSite)
                .Include(sensor => sensor.Readings
                    .OrderByDescending(reading => reading.Id)
                    .Take(HistorySize))
                .FirstOrDefaultAsync(sensor => sensor.Id == id);

            if (sensor == null)
            {
                return NotFound();
            }

            return View(sensor);
        }

        // GET: Sensors/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            await LoadEquipmentAsync();

            return View();
        }

        // POST: Sensors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            [Bind("Name,Unit,MinValue,MaxValue,EquipmentId")]
            Sensor sensor)
        {
            if (ModelState.IsValid)
            {
                _context.Sensors.Add(sensor);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await LoadEquipmentAsync(sensor.EquipmentId);

            return View(sensor);
        }

        // GET: Sensors/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sensor = await _context.Sensors.FindAsync(id);

            if (sensor == null)
            {
                return NotFound();
            }

            await LoadEquipmentAsync(sensor.EquipmentId);

            return View(sensor);
        }

        // POST: Sensors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Name,Unit,MinValue,MaxValue,EquipmentId")]
            Sensor sensor)
        {
            if (id != sensor.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(sensor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Sensors.AnyAsync(s => s.Id == sensor.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            await LoadEquipmentAsync(sensor.EquipmentId);

            return View(sensor);
        }

        // GET: Sensors/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sensor = await _context.Sensors
                .Include(sensor => sensor.Equipment)
                    .ThenInclude(equipment => equipment!.ProductionSite)
                .FirstOrDefaultAsync(sensor => sensor.Id == id);

            if (sensor == null)
            {
                return NotFound();
            }

            ViewData["ReadingsCount"] = await _context.SensorReadings
                .CountAsync(reading => reading.SensorId == sensor.Id);

            return View(sensor);
        }

        // POST: Sensors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sensor = await _context.Sensors.FindAsync(id);

            if (sensor != null)
            {
                _context.Sensors.Remove(sensor);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadEquipmentAsync(int? selectedId = null)
        {
            var equipmentItems = await _context.EquipmentItems
                .Include(equipment => equipment.ProductionSite)
                .OrderBy(equipment => equipment.Name)
                .ToListAsync();

            var items = equipmentItems.Select(equipment => new
            {
                equipment.Id,
                Title = equipment.ProductionSite == null
                    ? equipment.Name
                    : $"{equipment.Name} ({equipment.ProductionSite.Name})"
            });

            ViewData["EquipmentId"] = new SelectList(
                items,
                "Id",
                "Title",
                selectedId);
        }
    }
}
