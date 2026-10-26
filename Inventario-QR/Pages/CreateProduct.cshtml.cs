using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Inventario_QR.Data;
using Inventario_QR.Models;
using System;
using System.Collections.Generic;
using System.IO;
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

        // Propiedad para capturar la foto subida desde el formulario
        [BindProperty]
        public IFormFile? FotoArchivo { get; set; }

        public SelectList CategoriesSL { get; set; } = default!;

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

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            await LoadDataAsync();

            if (id.HasValue && id.Value > 0)
            {
                Product = await _context.Products.FindAsync(id.Value) ?? new();
                if (Product.Id == 0)
                {
                    return RedirectToPage("./Index");
                }

                // Cargar los detalles existentes de este producto
                var productDetails = await _context.ProductDetails
                    .Where(pd => pd.ProductId == id.Value)
                    .ToListAsync();

                foreach (var item in AllCatalogItems)
                {
                    var existing = productDetails.FirstOrDefault(pd => pd.DetailsId == item.DetailsId);
                    if (existing != null)
                    {
                        item.IsSelected = true;
                        item.DetailsValue = existing.DetailsValue ?? string.Empty;
                        item.Amount = existing.Amount > 0 ? existing.Amount : 1;
                    }
                }
            }
            else
            {
                Product.Code = "INV-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
                Product.Qr = "QR-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                Product.Active = true;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            ModelState.Remove("Product.Category");
            ModelState.Remove("FotoArchivo");
            ModelState.Remove("Product.ImagePath");

            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            string? uniqueFileName = null;
            if (FotoArchivo != null && FotoArchivo.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string extension = Path.GetExtension(FotoArchivo.FileName);
                uniqueFileName = $"{Product.Code.Replace("/", "_")}_{Guid.NewGuid().ToString().Substring(0, 6)}{extension}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await FotoArchivo.CopyToAsync(fileStream);
                }

                uniqueFileName = $"/uploads/products/{uniqueFileName}";
            }

            if (id.HasValue && id.Value > 0)
            {
                // MODO EDICIÓN
                var productInDb = await _context.Products.FindAsync(id.Value);
                if (productInDb == null)
                {
                    return NotFound();
                }

                productInDb.CategoryId = Product.CategoryId;
                productInDb.Code = Product.Code;
                productInDb.ProductName = Product.ProductName;
                productInDb.Active = Product.Active;

                // Actualizar la ruta de la imagen solo si el usuario subió una nueva foto
                if (uniqueFileName != null)
                {
                    productInDb.ImagePath = uniqueFileName;
                }

                // Obtener detalles actuales en BD
                var existingDetails = await _context.ProductDetails
                    .Where(pd => pd.ProductId == id.Value)
                    .ToListAsync();

                // Procesar cada elemento enviado desde la vista
                foreach (var item in SelectedCatalogItems)
                {
                    var dbDetail = existingDetails.FirstOrDefault(pd => pd.DetailsId == item.DetailsId);

                    if (item.IsSelected)
                    {
                        if (dbDetail != null)
                        {
                            // Actualizar el valor si ya existía
                            dbDetail.DetailsValue = item.DetailsValue ?? string.Empty;
                            dbDetail.Amount = item.Amount > 0 ? item.Amount : 1;
                        }
                        else
                        {
                            // Crear nuevo registro si fue marcado ahora
                            _context.ProductDetails.Add(new ProductDetail
                            {
                                ProductId = id.Value,
                                DetailsId = item.DetailsId,
                                Amount = item.Amount > 0 ? item.Amount : 1,
                                DetailsValue = item.DetailsValue ?? string.Empty
                            });
                        }
                    }
                    else
                    {
                        // Si se desmarcó y existía, lo eliminamos
                        if (dbDetail != null)
                        {
                            _context.ProductDetails.Remove(dbDetail);
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                // MODO CREACIÓN
                if (uniqueFileName != null)
                {
                    Product.ImagePath = uniqueFileName;
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
                            Amount = item.Amount > 0 ? item.Amount : 1,
                            DetailsValue = item.DetailsValue ?? string.Empty
                        });
                    }
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostRegisterCatalogItemAsync([FromBody] Detail detail)
        {
            if (string.IsNullOrWhiteSpace(detail.DetailName))
            {
                return BadRequest(new { message = "El nombre del detalle es obligatorio." });
            }

            detail.Complement = false;
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

            var relatedProductDetails = _context.ProductDetails.Where(pd => pd.DetailsId == detail.Id);
            _context.ProductDetails.RemoveRange(relatedProductDetails);

            _context.Details.Remove(detailDb);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, id = detail.Id });
        }

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
                IsSelected = false,
                Amount = 1,
                DetailsValue = string.Empty
            }).ToList();
        }
    }
}