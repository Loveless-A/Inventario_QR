using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Inventario_QR.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Inventario_QR.Pages
{
    public class ProductAssignmentModel : PageModel
    {
        private readonly AppDbContext _context;

        public ProductAssignmentModel(AppDbContext context)
        {
            _context = context;
        }

        public class AssignmentItemVm
        {
            public int RowNumber { get; set; }
            public int ProductId { get; set; }
            public string Code { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public string AssignedToName { get; set; } = "No asignado";
            public string Localization { get; set; } = string.Empty;
            public List<DetailRowVm> Characteristics { get; set; } = new();
            public List<DetailRowVm> Complements { get; set; } = new();
        }

        public class DetailRowVm
        {
            public string DetailName { get; set; } = string.Empty;
            public string DetailValue { get; set; } = string.Empty;
            public int Amount { get; set; }
            public bool IsComplement { get; set; }
        }

        public IList<AssignmentItemVm> AssignmentList { get; set; } = new List<AssignmentItemVm>();

        [BindProperty(SupportsGet = true)]
        public string? SelectedDepartment { get; set; }

        public List<string> Departments { get; set; } = new()
        {
            "La Paz",
            "Santa Cruz",
            "Cochabamba",
            "Oruro",
            "Potosí",
            "Tarija",
            "Chuquisaca",
            "Beni",
            "Pando"
        };

        public async Task OnGetAsync()
        {
            var products = await _context.Products
                .Where(p => p.Active)
                .OrderBy(p => p.Id)
                .ToListAsync();

            var list = new List<AssignmentItemVm>();
            int rowIndex = 1;

            foreach (var p in products)
            {
                var characteristicRecord = await _context.Characteristics
                    .Include(c => c.Person)
                    .FirstOrDefaultAsync(c => c.ProductId == p.Id && c.Active);

                string assignedName = "No asignado";
                string localization = string.Empty;

                if (characteristicRecord?.Person != null)
                {
                    var person = characteristicRecord.Person;
                    assignedName = $"{person.Identification} - {person.LastName}".Trim();
                    localization = characteristicRecord.Localization ?? string.Empty;
                }

                var detailsQuery = from pd in _context.ProductDetails
                                   join d in _context.Details on pd.DetailsId equals d.Id
                                   where pd.ProductId == p.Id
                                   select new DetailRowVm
                                   {
                                       DetailName = d.DetailName ?? "Sin nombre",
                                       DetailValue = pd.DetailsValue ?? string.Empty,
                                       Amount = pd.Amount,
                                       IsComplement = d.Complement
                                   };

                var allDetails = await detailsQuery.ToListAsync();

                var characteristics = allDetails.Where(d => !d.IsComplement).ToList();
                var complements = allDetails.Where(d => d.IsComplement).ToList();

                list.Add(new AssignmentItemVm
                {
                    RowNumber = rowIndex++,
                    ProductId = p.Id,
                    Code = p.Code ?? string.Empty,
                    ProductName = p.ProductName ?? string.Empty,
                    AssignedToName = assignedName,
                    Localization = localization,
                    Characteristics = characteristics,
                    Complements = complements
                });
            }

            // Aplicar filtro por departamento seleccionado si existe
            if (!string.IsNullOrEmpty(SelectedDepartment))
            {
                list = list.Where(x => !string.IsNullOrEmpty(x.Localization) &&
                                       x.Localization.Contains(SelectedDepartment, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Reenumerar filas de forma consecutiva tras el filtrado
            int finalIndex = 1;
            foreach (var item in list)
            {
                item.RowNumber = finalIndex++;
            }

            AssignmentList = list;
        }
    }
}