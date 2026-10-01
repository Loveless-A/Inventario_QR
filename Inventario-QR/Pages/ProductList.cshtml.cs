using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Inventario_QR.Data;
using Inventario_QR.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Inventario_QR.Pages
{
    public class ProductList : PageModel
    {
        private readonly AppDbContext _context;

        public ProductList(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? SelectedDepartment { get; set; }

        public List<string> Departments { get; set; } = new();

        public class InventoryItemVm
        {
            public int RowNumber { get; set; }
            public int ProductId { get; set; }
            public string Code { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public List<DetailRowVm> Characteristics { get; set; } = new();
            public List<DetailRowVm> Complements { get; set; } = new();
        }

        public class DetailRowVm
        {
            public string DetailName { get; set; } = string.Empty;
            public int Amount { get; set; }
            public bool IsComplement { get; set; }
        }

        public IList<InventoryItemVm> InventoryList { get; set; } = new List<InventoryItemVm>();

        public async Task OnGetAsync()
        {
            // Cargar los departamentos disponibles desde la base de datos
            Departments = await _context.Characteristics
                .Where(c => c.Active && !string.IsNullOrEmpty(c.Localization))
                .Select(c => c.Localization!)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            var query = _context.Products.Where(p => p.Active).AsQueryable();

            // Filtrar por departamento si se selecciona uno
            if (!string.IsNullOrEmpty(SelectedDepartment))
            {
                var productIdsInDept = await _context.Characteristics
                    .Where(c => c.Active && c.Localization == SelectedDepartment)
                    .Select(c => c.ProductId)
                    .Distinct()
                    .ToListAsync();

                query = query.Where(p => productIdsInDept.Contains(p.Id));
            }

            var products = await query.OrderBy(p => p.Id).ToListAsync();

            var list = new List<InventoryItemVm>();
            int rowIndex = 1;

            foreach (var p in products)
            {
                var detailsQuery = from pd in _context.ProductDetails
                                   join d in _context.Details on pd.DetailsId equals d.Id
                                   where pd.ProductId == p.Id
                                   select new DetailRowVm
                                   {
                                       DetailName = d.DetailName ?? "Sin nombre",
                                       Amount = pd.Amount,
                                       IsComplement = d.Complement
                                   };

                var allDetails = await detailsQuery.ToListAsync();

                list.Add(new InventoryItemVm
                {
                    RowNumber = rowIndex++,
                    ProductId = p.Id,
                    Code = p.Code ?? string.Empty,
                    ProductName = p.ProductName ?? string.Empty,
                    Characteristics = allDetails.Where(d => !d.IsComplement).ToList(),
                    Complements = allDetails.Where(d => d.IsComplement).ToList()
                });
            }

            InventoryList = list;
        }
    }
}