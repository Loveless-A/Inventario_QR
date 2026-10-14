using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Inventario_QR.Data;
using Inventario_QR.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Inventario_QR.Pages
{
    public class ProductHistoryModel : PageModel
    {
        private readonly AppDbContext _context;

        public ProductHistoryModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public Product Product { get; set; }

        // Características Principales del Equipo (RAM, Procesador, etc.)
        public List<MainCharacteristicVm> MainCharacteristics { get; set; } = new List<MainCharacteristicVm>();

        // Complementos / Accesorios actuales del equipo
        public List<ProductAccessoryVm> AssignedAccessories { get; set; } = new List<ProductAccessoryVm>();

        public class MainCharacteristicVm
        {
            public string DetailName { get; set; }
            public string DetailsValue { get; set; }
        }

        public class ProductAccessoryVm
        {
            public string DetailName { get; set; }
            public int Amount { get; set; }
            public string DetailsValue { get; set; }
        }

        public class HistoryItemVm
        {
            public int CharacteristicId { get; set; }
            public string PersonName { get; set; }
            public string Identification { get; set; }
            public string PositionName { get; set; }
            public string DependenceName { get; set; }
            public string Localization { get; set; }
            public DateTime AssignmentDate { get; set; }
            public string Comment { get; set; }
            public bool IsActive { get; set; }
        }

        public List<HistoryItemVm> HistoryItems { get; set; } = new List<HistoryItemVm>();

        public async Task<IActionResult> OnGetAsync()
        {
            Product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == Id);

            if (Product == null)
            {
                return NotFound();
            }

            // 1. Obtener características principales (no complementos) que tienen valor registrado
            var catalogDetails = await _context.Details.Where(d => !d.Complement).ToListAsync();
            var existingProductDetails = await _context.ProductDetails
                .Where(pd => pd.ProductId == Id)
                .ToListAsync();

            MainCharacteristics = catalogDetails
                .Select(d => {
                    var pd = existingProductDetails.FirstOrDefault(x => x.DetailsId == d.Id);
                    return new MainCharacteristicVm
                    {
                        DetailName = d.DetailName,
                        DetailsValue = pd?.DetailsValue ?? ""
                    };
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.DetailsValue))
                .ToList();

            // 2. Obtener los accesorios/complementos asignados al equipo
            var complementCatalog = await _context.Details.Where(d => d.Complement).ToListAsync();
            AssignedAccessories = complementCatalog
                .Select(d => {
                    var pd = existingProductDetails.FirstOrDefault(x => x.DetailsId == d.Id);
                    if (pd != null)
                    {
                        return new ProductAccessoryVm
                        {
                            DetailName = d.DetailName,
                            Amount = pd.Amount,
                            DetailsValue = pd.DetailsValue
                        };
                    }
                    return null;
                })
                .Where(x => x != null)
                .ToList();

            // 3. Historial de personas que tuvieron el equipo
            var characteristics = await _context.Characteristics
                .Include(c => c.Person)
                    .ThenInclude(p => p.Position)
                .Include(c => c.Person)
                    .ThenInclude(p => p.Dependence)
                .Where(c => c.ProductId == Id)
                .OrderByDescending(c => c.Date)
                .ToListAsync();

            HistoryItems = characteristics.Select(c => new HistoryItemVm
            {
                CharacteristicId = c.Id,
                PersonName = c.Person != null ? c.Person.LastName : "Desconocido",
                Identification = c.Person != null ? c.Person.Identification : "S/N",
                PositionName = c.Person?.Position?.PositionName ?? "Sin Cargo",
                DependenceName = c.Person?.Dependence?.DependenceName ?? "Sin Dependencia",
                Localization = c.Localization ?? "No especificada",
                AssignmentDate = c.Date,
                Comment = c.Coment ?? "Sin observaciones",
                IsActive = c.Active
            }).ToList();

            return Page();
        }
    }
}