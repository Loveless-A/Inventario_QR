using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Inventario_QR.Data;
using Inventario_QR.Helpers;
using Inventario_QR.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Inventario_QR.Pages
{
    public class VisualizationsModel : PageModel
    {
        private readonly AppDbContext _context;

        public VisualizationsModel(AppDbContext context)
        {
            _context = context;
        }

        public class CharacteristicInfo
        {
            public int Id { get; set; }
            public string Localization { get; set; } = string.Empty;
            public DateTime Date { get; set; }
            public bool Active { get; set; }
            public string PersonName { get; set; } = string.Empty;
            public string PersonIdentification { get; set; } = string.Empty;
            public string PersonNumbers { get; set; } = string.Empty;
            public string Coment { get; set; } = string.Empty;
        }

        public class ProductInfo
        {
            public int Id { get; set; }
            public string Code { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public string Qr { get; set; } = string.Empty;
        }

        public class ChecklistDetailItem
        {
            public string DetailName { get; set; } = string.Empty;
            public bool Complement { get; set; }
            public bool IsSelected { get; set; }
            public int Amount { get; set; }
            public string DetailsValue { get; set; } = string.Empty;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public ProductInfo Product { get; set; } = new();
        public CharacteristicInfo Characteristic { get; set; } = new();
        public List<ChecklistDetailItem> Items { get; set; } = new();
        public string QrImage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            if (Id <= 0) return RedirectToPage("./Index");

            // 1. Obtener datos del producto usando el modelo y DbSet de EF
            var productEntity = await _context.Products.FindAsync(Id);
            if (productEntity == null)
            {
                return RedirectToPage("./Index");
            }

            Product = new ProductInfo
            {
                Id = productEntity.Id,
                Code = productEntity.Code ?? string.Empty,
                ProductName = productEntity.ProductName ?? string.Empty, // Apunta correctamente a [Column("product")]
                Qr = productEntity.Qr ?? string.Empty
            };

            // 2. Obtener datos de Characteristic y la persona asociada usando las relaciones de EF
            var charData = await _context.Characteristics
                .Include(c => c.Person)
                .Where(c => c.ProductId == Id && c.Active)
                .OrderByDescending(c => c.Date)
                .FirstOrDefaultAsync();

            if (charData != null)
            {
                Characteristic = new CharacteristicInfo
                {
                    Id = charData.Id,
                    Localization = charData.Localization ?? "No especificada",
                    Date = charData.Date,
                    Active = charData.Active,
                    PersonName = charData.Person != null ? (charData.Person.LastName ?? "") : "Sin asignar",
                    PersonIdentification = charData.Person != null ? (charData.Person.Identification ?? "") : "",
                    PersonNumbers = charData.Person != null ? (charData.Person.Numbers ?? "") : "",
                    Coment = charData.Coment ?? string.Empty
                };
            }

            // 3. Generar el QR de esta misma pagina en el servidor (antes lo hacia
            //    una libreria JS cargada desde un CDN, que requeria internet).
            QrImage = QrGenerator.ToSvgDataUri(
                $"{Request.Scheme}://{Request.Host}/Visualizations/{Id}", 4);

            // 4. Obtener componentes y detalles asociados al producto desde product_details
            var productDetailsList = await _context.ProductDetails
                .Include(pd => pd.Detail)
                .Where(pd => pd.ProductId == Id)
                .ToListAsync();

            Items = productDetailsList.Select(pd => new ChecklistDetailItem
            {
                DetailName = pd.Detail != null ? (pd.Detail.DetailName ?? string.Empty) : string.Empty,
                Complement = pd.Detail != null ? pd.Detail.Complement : false,
                IsSelected = true, // Los registros en product_details representan los elementos seleccionados/entregados
                Amount = pd.Amount,
                DetailsValue = pd.DetailsValue ?? string.Empty
            }).ToList();

            return Page();
        }
    }
}