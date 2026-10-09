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

        public List<CatalogItemVM> AllCatalogItems { get; set; } = new();

        [BindProperty]
        public List<CatalogItemVM> SelectedCatalogItems { get; set; } = new();

        public class CatalogItemVM
        {
            public int DetailsId { get; set; }
            public string DetailName { get; set; } = string.Empty;
            public bool Complement { get; set; }
            public bool IsSelected { get; set; }
            public int Amount { get; set; } = 1;
            public string DetailsValue { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDataAsync();

            Product.Code = "INV-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            Product.Qr = "QR-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            Product.Active = true;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Product.Category");

            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            _context.Products.Add(Product);
            await _context.SaveChangesAsync();

            if (SelectedCatalogItems != null && SelectedCatalogItems.Any())
            {
                foreach (var item in SelectedCatalogItems.Where(i => i.IsSelected))
                {
                    _context.ProductDetails.Add(new ProductDetail
                    {
                        ProductId = Product.Id,
                        DetailsId = item.DetailsId,
                        Amount = item.Amount,
                        DetailsValue = item.DetailsValue ?? string.Empty
                    });
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }

        // --- AJAX: REGISTRAR DETALLE EN EL CATÁLOGO ---
        public async Task<IActionResult> OnPostRegisterCatalogItemAsync([FromBody] Detail detail)
        {
            if (string.IsNullOrWhiteSpace(detail.DetailName))
            {
                return BadRequest(new { message = "El nombre del detalle es obligatorio." });
            }

            detail.Complement = false; // Forzar que sea detalle principal
            _context.Details.Add(detail);
            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                id = detail.Id,
                name = detail.DetailName,
                complement = detail.Complement
            });
        }

        // --- AJAX: MODIFICAR DETALLE DEL CATÁLOGO ---
        public async Task<IActionResult> OnPostUpdateCatalogItemAsync([FromBody] Detail detail)
        {
            if (detail.Id <= 0 || string.IsNullOrWhiteSpace(detail.DetailName))
            {
                return BadRequest(new { message = "Datos inválidos para actualizar el detalle." });
            }

            var detailDb = await _context.Details.FindAsync(detail.Id);
            if (detailDb == null)
            {
                return NotFound(new { message = "El detalle no existe en el catálogo." });
            }

            detailDb.DetailName = detail.DetailName.Trim();
            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                id = detailDb.Id,
                name = detailDb.DetailName
            });
        }

        // --- AJAX: ELIMINAR DETALLE DEL CATÁLOGO ---
        public async Task<IActionResult> OnPostDeleteCatalogItemAsync([FromBody] Detail detail)
        {
            if (detail.Id <= 0)
            {
                return BadRequest(new { message = "ID de detalle inválido." });
            }

            var detailDb = await _context.Details.FindAsync(detail.Id);
            if (detailDb == null)
            {
                return NotFound(new { message = "El detalle no existe." });
            }

            // Eliminar dependencias en ProductDetails si existieran
            var relatedProductDetails = _context.ProductDetails.Where(pd => pd.DetailsId == detail.Id);
            _context.ProductDetails.RemoveRange(relatedProductDetails);

            _context.Details.Remove(detailDb);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, id = detail.Id });
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

        private async Task LoadDataAsync()
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

            var catalogDetails = await _context.Details.ToListAsync();
            AllCatalogItems = catalogDetails.Select(d => new CatalogItemVM
            {
                DetailsId = d.Id,
                DetailName = d.DetailName,
                Complement = d.Complement,
                IsSelected = !d.Complement,
                Amount = 1,
                DetailsValue = string.Empty
            }).ToList();
        }
    }
}