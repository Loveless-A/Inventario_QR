using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Inventario_QR.Data;
using Inventario_QR.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Inventario_QR.Pages
{
    public class GroupAssignModel : PageModel
    {
        private readonly AppDbContext _context;

        public GroupAssignModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? Ids { get; set; }

        [BindProperty]
        public List<int> SelectedIds { get; set; } = new();

        [BindProperty]
        public int SelectedPersonId { get; set; }

        [BindProperty]
        public string Localization { get; set; } = string.Empty;

        [BindProperty]
        public string AssignmentComment { get; set; } = string.Empty;

        public List<ProductGroupItemVM> ProductGroups { get; set; } = new();
        public List<Person> AllPersons { get; set; } = new();
        public List<string> DepartamentosBolivia { get; set; } = new()
        {
            "La Paz", "Cochabamba", "Santa Cruz", "Oruro", "Potosí", "Tarija", "Chuquisaca", "Beni", "Pando"
        };

        public class ProductGroupItemVM
        {
            public int ProductId { get; set; }
            public string Code { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public string Qr { get; set; } = string.Empty;
            public List<ChecklistDetailVM> Details { get; set; } = new();
        }

        public class ChecklistDetailVM
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
            if (!string.IsNullOrEmpty(Ids))
            {
                SelectedIds = Ids.Split(',').Select(int.Parse).Distinct().ToList();
            }

            if (!SelectedIds.Any())
            {
                return RedirectToPage("./Index");
            }

            await LoadGroupDataAsync();
            AllPersons = await _context.Persons.Where(p => p.Active).ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (SelectedIds == null || !SelectedIds.Any() || SelectedPersonId <= 0 || string.IsNullOrEmpty(Localization))
            {
                ModelState.AddModelError(string.Empty, "Debe seleccionar una persona y un departamento de destino válidos.");
                await LoadGroupDataAsync();
                AllPersons = await _context.Persons.Where(p => p.Active).ToListAsync();
                return Page();
            }

            foreach (var productId in SelectedIds)
            {
                var characteristic = new Characteristic
                {
                    PersonId = SelectedPersonId,
                    ProductId = productId,
                    Localization = Localization,
                    Coment = AssignmentComment,
                    Date = DateTime.UtcNow,
                    Active = true
                };

                _context.Characteristics.Add(characteristic);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }

        // Handler AJAX para Registrar Complemento
        public async Task<IActionResult> OnPostAddComplementAsync(string complementName)
        {
            if (string.IsNullOrWhiteSpace(complementName))
            {
                return new JsonResult(new { success = false, message = "El nombre no puede estar vacío." });
            }

            var newDetail = new Detail
            {
                DetailName = complementName.Trim(),
                Complement = true
            };

            _context.Details.Add(newDetail);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, id = newDetail.Id, name = newDetail.DetailName });
        }

        // Handler AJAX para Modificar Catálogo
        public async Task<IActionResult> OnPostUpdateCatalogDetailAsync(int detailId, string newName)
        {
            if (detailId <= 0 || string.IsNullOrWhiteSpace(newName))
            {
                return new JsonResult(new { success = false, message = "Datos inválidos." });
            }

            var detail = await _context.Details.FindAsync(detailId);
            if (detail == null)
            {
                return new JsonResult(new { success = false, message = "Elemento no encontrado." });
            }

            detail.DetailName = newName.Trim();
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        // Handler AJAX para Eliminar del Catálogo
        public async Task<IActionResult> OnPostDeleteCatalogDetailAsync(int detailId)
        {
            if (detailId <= 0)
            {
                return new JsonResult(new { success = false, message = "ID inválido." });
            }

            var detail = await _context.Details.FindAsync(detailId);
            if (detail == null)
            {
                return new JsonResult(new { success = false, message = "Elemento no encontrado." });
            }

            var prodDetails = _context.ProductDetails.Where(pd => pd.DetailsId == detailId);
            _context.ProductDetails.RemoveRange(prodDetails);

            _context.Details.Remove(detail);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        private async Task LoadGroupDataAsync()
        {
            var products = await _context.Products
                .Where(p => SelectedIds.Contains(p.Id))
                .ToListAsync();

            var catalogDetails = await _context.Details.ToListAsync();
            ProductGroups = new List<ProductGroupItemVM>();

            foreach (var prod in products)
            {
                // Obtenemos unicamente los detalles que están vinculados en la tabla intermedia para este producto
                var existingProductDetails = await _context.ProductDetails
                    .Where(pd => pd.ProductId == prod.Id)
                    .ToListAsync();

                var vmDetails = new List<ChecklistDetailVM>();

                foreach (var d in catalogDetails)
                {
                    var match = existingProductDetails.FirstOrDefault(ed => ed.DetailsId == d.Id);

                    if (!d.Complement)
                    {
                        // FILTRO ESTRICTO: Solo incluimos características principales si están explícitamente registradas y tienen contenido o valor válido
                        if (match != null && !string.IsNullOrWhiteSpace(match.DetailsValue))
                        {
                            vmDetails.Add(new ChecklistDetailVM
                            {
                                DetailsId = d.Id,
                                DetailName = d.DetailName,
                                Complement = false,
                                IsSelected = true,
                                Amount = 1,
                                DetailsValue = match.DetailsValue
                            });
                        }
                    }
                    else
                    {
                        // Para complementos, listamos todo el catálogo para permitir seleccionarlos o gestionarlos
                        vmDetails.Add(new ChecklistDetailVM
                        {
                            DetailsId = d.Id,
                            DetailName = d.DetailName,
                            Complement = true,
                            IsSelected = match != null,
                            Amount = match?.Amount > 0 ? match.Amount : 1,
                            DetailsValue = match?.DetailsValue ?? string.Empty
                        });
                    }
                }

                ProductGroups.Add(new ProductGroupItemVM
                {
                    ProductId = prod.Id,
                    Code = prod.Code,
                    ProductName = prod.ProductName,
                    Qr = prod.Qr,
                    Details = vmDetails
                });
            }
        }
    }
}