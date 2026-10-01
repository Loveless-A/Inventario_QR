using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Inventario_QR.Data;
using Inventario_QR.Helpers;
using Inventario_QR.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Inventario_QR.Pages
{
    public class PrintQRsModel : PageModel
    {
        private readonly AppDbContext _context;

        public PrintQRsModel(AppDbContext context)
        {
            _context = context;
        }

        public class PrintProductVm
        {
            public int Id { get; set; }
            public string Code { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public string Qr { get; set; } = string.Empty;
            public string TargetUrl { get; set; } = string.Empty;
            public string QrImage { get; set; } = string.Empty;
        }

        public IList<PrintProductVm> Products { get; set; } = new List<PrintProductVm>();

        public async Task<IActionResult> OnGetAsync(string ids)
        {
            if (string.IsNullOrEmpty(ids))
            {
                return RedirectToPage("./Index");
            }

            var idList = ids.Split(',')
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .ToList();

            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            var list = await _context.Products
                .Where(p => idList.Contains(p.Id))
                .ToListAsync();

            Products = list.Select(p => new PrintProductVm
            {
                Id = p.Id,
                Code = p.Code ?? string.Empty,
                ProductName = p.ProductName ?? string.Empty,
                Qr = p.Qr ?? string.Empty,
                TargetUrl = $"{baseUrl}/Visualizations/{p.Id}",
                QrImage = QrGenerator.ToSvgDataUri($"{baseUrl}/Visualizations/{p.Id}")
            }).ToList();

            return Page();
        }
    }
}