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
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public class ProductIndexVm
        {
            public int Id { get; set; }
            public string Code { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public string Qr { get; set; } = string.Empty;
            public bool IsAssigned { get; set; }
            public string AssignedToName { get; set; } = "No asignado";
            public string TargetUrl { get; set; } = string.Empty;
        }

        public IList<ProductIndexVm> Products { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10; // Hacemos dinámico el tamaño de página

        public int TotalPages { get; set; } = 1;

        [BindProperty]
        public List<int> SelectedProductIds { get; set; } = new();

        public async Task OnGetAsync()
        {
            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            var productsQuery = _context.Products.Where(p => p.Active);

            var list = await productsQuery
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            var vmList = new List<ProductIndexVm>();

            foreach (var p in list)
            {
                var characteristic = await _context.Characteristics
                    .Include(c => c.Person)
                    .FirstOrDefaultAsync(c => c.ProductId == p.Id && c.Active);

                bool isAssigned = characteristic != null;
                string assignedName = "No asignado";

                if (isAssigned && characteristic?.Person != null)
                {
                    var person = characteristic.Person;
                    assignedName = $"{person.Identification} - {person.LastName}".Trim();
                    if (string.IsNullOrEmpty(assignedName) || assignedName == "-") assignedName = "Asignado";
                }

                vmList.Add(new ProductIndexVm
                {
                    Id = p.Id,
                    Code = p.Code ?? string.Empty,
                    ProductName = p.ProductName ?? string.Empty,
                    Qr = p.Qr ?? string.Empty,
                    IsAssigned = isAssigned,
                    AssignedToName = assignedName,
                    TargetUrl = $"{baseUrl}/Visualizations/{p.Id}"
                });
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                vmList = vmList.Where(p =>
                    p.Code.ToLower().Contains(term) ||
                    p.Qr.ToLower().Contains(term) ||
                    p.ProductName.ToLower().Contains(term) ||
                    p.AssignedToName.ToLower().Contains(term)
                ).ToList();
            }

            // Validar que el PageSize sea uno de los permitidos por seguridad
            int[] allowedSizes = { 10, 20, 30, 50, 100 };
            if (!allowedSizes.Contains(PageSize))
            {
                PageSize = 10;
            }

            int totalItems = vmList.Count;
            TotalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));

            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;

            Products = vmList
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }

        public IActionResult OnPostPrintQRs()
        {
            if (SelectedProductIds == null || !SelectedProductIds.Any())
            {
                return RedirectToPage(new { CurrentPage, SearchTerm, PageSize });
            }

            string ids = string.Join(",", SelectedProductIds);
            return RedirectToPage("./PrintQRs", new { ids = ids });
        }
    }
}