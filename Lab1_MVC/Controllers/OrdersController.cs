using Lab1_MVC.Data;
using Lab1_MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lab1_MVC.Controllers;

public class OrdersController(LibraryContext context) : Controller
{
    public async Task<IActionResult> Index(int? customerId)
    {
        ViewData["CustomerID"] = new SelectList(await context.Customer.AsNoTracking().OrderBy(c => c.Name).ToListAsync(), "CustomerID", "Name", customerId);
        var orders = context.Order.AsNoTracking().Include(o => o.Customer).Include(o => o.Book).AsQueryable();
        if (customerId.HasValue) orders = orders.Where(o => o.CustomerID == customerId);
        return View(await orders.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.OrderID).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        var order = await context.Order.AsNoTracking().Include(o => o.Customer).Include(o => o.Book).FirstOrDefaultAsync(o => o.OrderID == id);
        return order is null ? NotFound() : View(order);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateLists();
        return View(new OrderEditViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderEditViewModel vm)
    {
        await ValidateReferences(vm);
        if (ModelState.IsValid)
        {
            // Only the editable values are copied. Posted OrderID/navigation values are ignored.
            context.Order.Add(new Order { BookID = vm.BookID, CustomerID = vm.CustomerID, OrderDate = vm.OrderDate!.Value });
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        await PopulateLists(vm);
        return View(vm);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        var order = await context.Order.FindAsync(id);
        if (order is null) return NotFound();
        var vm = new OrderEditViewModel { OrderID = order.OrderID, BookID = order.BookID, CustomerID = order.CustomerID, OrderDate = order.OrderDate };
        await PopulateLists(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OrderEditViewModel vm)
    {
        if (id != vm.OrderID) return NotFound();
        var order = await context.Order.FindAsync(id);
        if (order is null) return NotFound();
        await ValidateReferences(vm);
        if (ModelState.IsValid)
        {
            order.BookID = vm.BookID;
            order.CustomerID = vm.CustomerID;
            order.OrderDate = vm.OrderDate!.Value;
            try { await context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!await context.Order.AnyAsync(o => o.OrderID == id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        await PopulateLists(vm);
        return View(vm);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        var order = await context.Order.AsNoTracking().Include(o => o.Customer).Include(o => o.Book).FirstOrDefaultAsync(o => o.OrderID == id);
        return order is null ? NotFound() : View(order);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var order = await context.Order.FindAsync(id);
        if (order is null) return NotFound();
        context.Order.Remove(order);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLists(OrderEditViewModel? vm = null)
    {
        ViewData["BookID"] = new SelectList(await context.Book.AsNoTracking().OrderBy(b => b.Title).ToListAsync(), "ID", "Title", vm?.BookID);
        ViewData["CustomerID"] = new SelectList(await context.Customer.AsNoTracking().OrderBy(c => c.Name).ToListAsync(), "CustomerID", "Name", vm?.CustomerID);
    }

    private async Task ValidateReferences(OrderEditViewModel vm)
    {
        if (!await context.Book.AnyAsync(b => b.ID == vm.BookID)) ModelState.AddModelError(nameof(vm.BookID), "Selectează o carte existentă.");
        if (!await context.Customer.AnyAsync(c => c.CustomerID == vm.CustomerID)) ModelState.AddModelError(nameof(vm.CustomerID), "Selectează un cititor existent.");
    }
}
