using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Inventario_QR.Data;
using Inventario_QR.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Inventario_QR.Pages
{
    // IgnoreAntiforgeryToken permite evitar fallos de Token al enviar peticiones JSON por Fetch AJAX
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class CreateProductModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateProductModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Product Product { get; set; } = new();

        public SelectList CategoriesSL { get; set; }

        public List<Category> CategoryList { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadCategoriesAsync();

            Product.Code = "INV-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            Product.Qr = "QR-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            Product.Active = true;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Remover la validación de la entidad Category ligada si existe como propiedad de navegación
            ModelState.Remove("Product.Category");

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return Page();
            }

            _context.Products.Add(Product);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        // --- AJAX: CREAR CATEGORÍA ---
        public async Task<IActionResult> OnPostCreateCategoryAsync([FromBody] Category category)
        {
            if (string.IsNullOrWhiteSpace(category.CategoryName) || string.IsNullOrWhiteSpace(category.Abbreviation))
            {
                return BadRequest(new { message = "El nombre y la abreviatura son obligatorios." });
            }

            category.Active = true;
            category.Abbreviation = category.Abbreviation.ToUpper().Trim();

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                id = category.Id,
                name = category.CategoryName,
                abbreviation = category.Abbreviation,
                comment = category.Comment ?? ""
            });
        }

        // --- AJAX: EDITAR CATEGORÍA ---
        public async Task<IActionResult> OnPostUpdateCategoryAsync([FromBody] Category category)
        {
            if (category.Id <= 0 || string.IsNullOrWhiteSpace(category.CategoryName) || string.IsNullOrWhiteSpace(category.Abbreviation))
            {
                return BadRequest(new { message = "Datos inválidos para actualizar la categoría." });
            }

            var catDb = await _context.Categories.FindAsync(category.Id);
            if (catDb == null)
            {
                return NotFound(new { message = "La categoría no existe." });
            }

            catDb.CategoryName = category.CategoryName;
            catDb.Abbreviation = category.Abbreviation.ToUpper().Trim();
            catDb.Comment = category.Comment;

            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                id = catDb.Id,
                name = catDb.CategoryName,
                abbreviation = catDb.Abbreviation,
                comment = catDb.Comment ?? ""
            });
        }

        private async Task LoadCategoriesAsync()
        {
            CategoryList = await _context.Categories
                .Where(c => c.Active)
                .ToListAsync();

            var selectData = CategoryList.Select(c => new
            {
                c.Id,
                DisplayText = $"{c.CategoryName} ({c.Abbreviation})"
            });

            CategoriesSL = new SelectList(selectData, "Id", "DisplayText");
        }
    }
}