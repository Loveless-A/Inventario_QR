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
    public class ProductChecklistModel : PageModel
    {
        private readonly AppDbContext _context;

        public ProductChecklistModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Mode { get; set; } // "assign" o "details"

        public Product Product { get; set; } = default!;

        // Propiedades de Asignación de Persona
        [BindProperty]
        public int? SelectedPersonId { get; set; }
        public List<Person> AllPersons { get; set; } = new();

        [BindProperty]
        public string? AssignmentComment { get; set; }

        // Propiedad para el Departamento de Destino (Localization)
        [BindProperty]
        public string? Localization { get; set; }

        // Lista de los 9 Departamentos de Bolivia para el Select
        public List<string> DepartamentosBolivia => new()
        {
            "Chuquisaca",
            "La Paz",
            "Cochabamba",
            "Oruro",
            "Potosí",
            "Tarija",
            "Santa Cruz",
            "Beni",
            "Pando"
        };

        [BindProperty]
        public bool ModalNewPersonIsActive { get; set; } = true;

        // Propiedades para Modal de Nueva Persona
        [BindProperty]
        public string? ModalNewPersonLastName { get; set; }
        [BindProperty]
        public string? ModalNewPersonIdentification { get; set; }
        [BindProperty]
        public string? ModalNewPersonNumbers { get; set; }

        // Propiedades para Catálogo de Componentes/Complementos
        [BindProperty]
        public string? NewCatalogDetailName { get; set; }
        [BindProperty]
        public bool NewCatalogIsComplement { get; set; }

        [BindProperty]
        public int EditCatalogId { get; set; }
        [BindProperty]
        public string? EditCatalogName { get; set; }
        [BindProperty]
        public bool EditCatalogComplement { get; set; }

        [BindProperty]
        public List<ChecklistRowInput> AllChecklistItems { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }
        [TempData]
        public string? ErrorMessage { get; set; }

        public bool IsAlreadyAssigned { get; set; }
        public string? AssignedPersonDisplayValue { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Id = id;
            Product = await _context.Products.FindAsync(Id);
            if (Product == null) return NotFound();

            await LoadDataAsync();
            return Page();
        }

        private async Task LoadDataAsync()
        {
            AllPersons = await _context.Persons.OrderBy(p => p.LastName).ToListAsync();

            var activeCharacteristic = await _context.Characteristics
                .Include(c => c.Person)
                .FirstOrDefaultAsync(c => c.ProductId == Id && c.Active);

            if (activeCharacteristic?.Person != null)
            {
                SelectedPersonId = activeCharacteristic.PersonId;
                AssignmentComment = activeCharacteristic.Coment;
                Localization = activeCharacteristic.Localization;

                // Marcamos que ya está asignado y formateamos el texto para el input
                IsAlreadyAssigned = true;
                AssignedPersonDisplayValue = $"{activeCharacteristic.Person.LastName} (CI: {activeCharacteristic.Person.Identification})";
            }
            else
            {
                IsAlreadyAssigned = false;
                AssignedPersonDisplayValue = string.Empty;
            }

            var allDetails = await _context.Details.ToListAsync();
            var existingProductDetails = await _context.ProductDetails
                .Where(pd => pd.ProductId == Id)
                .ToListAsync();

            AllChecklistItems = allDetails.Select(d => {
                var existing = existingProductDetails.FirstOrDefault(ed => ed.DetailsId == d.Id);
                return new ChecklistRowInput
                {
                    DetailsId = d.Id,
                    DetailName = d.DetailName,
                    Complement = d.Complement,
                    IsSelected = existing != null,
                    Amount = existing?.Amount > 0 ? existing.Amount : 1,
                    DetailsValue = existing?.DetailsValue ?? string.Empty
                };
            }).ToList();
        }

        public async Task<IActionResult> OnPostRegisterPersonAsync()
        {
            if (!string.IsNullOrWhiteSpace(ModalNewPersonLastName) && !string.IsNullOrWhiteSpace(ModalNewPersonIdentification))
            {
                var newPerson = new Person
                {
                    LastName = ModalNewPersonLastName.Trim(),
                    Identification = ModalNewPersonIdentification.Trim(),
                    Numbers = ModalNewPersonNumbers?.Trim(),
                    Active = ModalNewPersonIsActive
                };
                _context.Persons.Add(newPerson);
                await _context.SaveChangesAsync();
                SelectedPersonId = newPerson.Id;
                StatusMessage = "Persona registrada y seleccionada exitosamente.";
            }
            else
            {
                ErrorMessage = "Complete los campos obligatorios para registrar a la persona.";
            }

            return RedirectToPage(new { id = Id, mode = "assign" });
        }

        public async Task<IActionResult> OnPostSaveAssignAsync()
        {
            Product = await _context.Products.FindAsync(Id);
            if (Product == null) return NotFound();

            var characteristic = await _context.Characteristics
                .FirstOrDefaultAsync(c => c.ProductId == Id && c.Active);

            if (SelectedPersonId.HasValue && SelectedPersonId.Value > 0)
            {
                if (characteristic != null)
                {
                    characteristic.PersonId = SelectedPersonId.Value;
                    characteristic.Coment = AssignmentComment;
                    characteristic.Localization = Localization; // Actualiza el departamento
                }
                else
                {
                    _context.Characteristics.Add(new Characteristic
                    {
                        ProductId = Id,
                        PersonId = SelectedPersonId.Value,
                        Active = true,
                        Date = DateTime.UtcNow, // CORREGIDO A UTC para evitar error en PostgreSQL
                        Localization = Localization ?? "La Paz", // Guarda el departamento seleccionado
                        Coment = AssignmentComment
                    });
                }
            }
            else
            {
                if (characteristic != null)
                {
                    characteristic.Active = false;
                }
            }

            var existingProductDetails = await _context.ProductDetails
                .Where(pd => pd.ProductId == Id)
                .ToListAsync();

            var complementDetailIds = await _context.Details.Where(d => d.Complement).Select(d => d.Id).ToListAsync();
            var currentComplements = existingProductDetails.Where(pd => complementDetailIds.Contains(pd.DetailsId)).ToList();

            _context.ProductDetails.RemoveRange(currentComplements);

            foreach (var item in AllChecklistItems.Where(i => i.IsSelected && i.Complement))
            {
                _context.ProductDetails.Add(new ProductDetail
                {
                    ProductId = Id,
                    DetailsId = item.DetailsId,
                    Amount = item.Amount > 0 ? item.Amount : 1,
                    DetailsValue = item.DetailsValue ?? string.Empty
                });
            }

            await _context.SaveChangesAsync();
            StatusMessage = "Asignación y complementos guardados correctamente.";
            return RedirectToPage(new { id = Id, mode = "assign" });
        }

        public async Task<IActionResult> OnPostRegisterCatalogItemAsync()
        {
            if (!string.IsNullOrWhiteSpace(NewCatalogDetailName))
            {
                var newDetail = new Detail
                {
                    DetailName = NewCatalogDetailName.Trim(),
                    Complement = NewCatalogIsComplement
                };
                _context.Details.Add(newDetail);
                await _context.SaveChangesAsync();
                StatusMessage = "Elemento agregado al catálogo exitosamente.";
            }
            else
            {
                ErrorMessage = "El nombre del elemento no puede estar vacío.";
            }

            return RedirectToPage(new { id = Id, mode = "details" });
        }

        public async Task<IActionResult> OnPostEditCatalogItemAsync()
        {
            var detail = await _context.Details.FindAsync(EditCatalogId);
            if (detail != null && !string.IsNullOrWhiteSpace(EditCatalogName))
            {
                int usageCount = await _context.ProductDetails.CountAsync(pd => pd.DetailsId == EditCatalogId);

                detail.DetailName = EditCatalogName.Trim();
                detail.Complement = EditCatalogComplement;
                await _context.SaveChangesAsync();

                StatusMessage = $"Elemento actualizado correctamente. (Vinculado a {usageCount} producto(s)).";
            }
            else
            {
                ErrorMessage = "No se pudo actualizar el elemento del catálogo.";
            }

            return RedirectToPage(new { id = Id, mode = "details" });
        }

        public async Task<IActionResult> OnPostDeleteCatalogItemAsync(int detailId)
        {
            var detail = await _context.Details.FindAsync(detailId);
            if (detail != null)
            {
                var relatedProductDetails = await _context.ProductDetails.Where(pd => pd.DetailsId == detailId).ToListAsync();

                if (relatedProductDetails.Any())
                {
                    ErrorMessage = $"No se puede eliminar '{detail.DetailName}' porque está siendo utilizado en otros productos/registros de la auditoría.";
                    return RedirectToPage(new { id = Id, mode = "details" });
                }

                _context.Details.Remove(detail);
                await _context.SaveChangesAsync();
                StatusMessage = "Elemento eliminado del catálogo exitosamente.";
            }

            return RedirectToPage(new { id = Id, mode = "details" });
        }

        public async Task<IActionResult> OnPostModifyDetailsAsync()
        {
            Product = await _context.Products.FindAsync(Id);
            if (Product == null) return NotFound();

            var existingProductDetails = await _context.ProductDetails
                .Where(pd => pd.ProductId == Id)
                .ToListAsync();

            _context.ProductDetails.RemoveRange(existingProductDetails);

            foreach (var item in AllChecklistItems.Where(i => i.IsSelected))
            {
                _context.ProductDetails.Add(new ProductDetail
                {
                    ProductId = Id,
                    DetailsId = item.DetailsId,
                    Amount = item.Amount > 0 ? item.Amount : 1,
                    DetailsValue = item.DetailsValue ?? string.Empty
                });
            }

            await _context.SaveChangesAsync();
            StatusMessage = "Detalles modificados correctamente.";
            return RedirectToPage(new { id = Id, mode = "details" });
        }
    }

    public class ChecklistRowInput
    {
        public int DetailsId { get; set; }
        public string DetailName { get; set; } = string.Empty;
        public bool Complement { get; set; }
        public bool IsSelected { get; set; }
        public int Amount { get; set; } = 1;
        public string DetailsValue { get; set; } = string.Empty;
    }
}