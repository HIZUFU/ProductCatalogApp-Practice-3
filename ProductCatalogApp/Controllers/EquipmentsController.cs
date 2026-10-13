using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProductCatalogApp.Data;
using ProductCatalogApp.Models;
using Microsoft.AspNetCore.Authorization;

[Authorize]
public class EquipmentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public EquipmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Equipments
    public async Task<IActionResult> Index(int? productionSiteId)
    {
        var equipmentQuery = _context.EquipmentItems
            .Include(e => e.ProductionSite)
            .AsQueryable();

        if (productionSiteId.HasValue)
        {
            equipmentQuery = equipmentQuery
                .Where(e => e.ProductionSiteId == productionSiteId.Value);
        }

        await LoadProductionSitesAsync(productionSiteId);

        return View(await equipmentQuery.ToListAsync());
    }

    // GET: Equipments/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var equipment = await _context.EquipmentItems
            .Include(e => e.ProductionSite)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (equipment == null)
        {
            return NotFound();
        }

        return View(equipment);
    }

    // GET: Equipments/Create
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        await LoadProductionSitesAsync();

        return View();
    }

    // POST: Equipments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [Bind("Id,Name,Model,InventoryNumber,ProductionSiteId")]
        Equipment equipment)
    {
        if (ModelState.IsValid)
        {
            _context.Add(equipment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        await LoadProductionSitesAsync(equipment.ProductionSiteId);

        return View(equipment);
    }

    // GET: Equipments/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var equipment = await _context.EquipmentItems.FindAsync(id);

        if (equipment == null)
        {
            return NotFound();
        }

        await LoadProductionSitesAsync(equipment.ProductionSiteId);

        return View(equipment);
    }

    // POST: Equipments/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(
        int? id,
        [Bind("Id,Name,Model,InventoryNumber,ProductionSiteId")]
        Equipment equipment)
    {
        if (id != equipment.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(equipment);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EquipmentExists(equipment.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        await LoadProductionSitesAsync(equipment.ProductionSiteId);

        return View(equipment);
    }

    // GET: Equipments/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var equipment = await _context.EquipmentItems
            .Include(e => e.ProductionSite)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (equipment == null)
        {
            return NotFound();
        }

        return View(equipment);
    }

    // POST: Equipments/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var equipment = await _context.EquipmentItems.FindAsync(id);

        if (equipment != null)
        {
            _context.EquipmentItems.Remove(equipment);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool EquipmentExists(int id)
    {
        return _context.EquipmentItems.Any(e => e.Id == id);
    }

    private async Task LoadProductionSitesAsync(int? selectedId = null)
    {
        var productionSites = await _context.ProductionSites
            .OrderBy(site => site.Name)
            .ToListAsync();

        ViewData["ProductionSiteId"] = new SelectList(
            productionSites,
            "Id",
            "Name",
            selectedId);
    }
}