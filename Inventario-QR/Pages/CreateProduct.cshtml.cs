using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Inventario_QR.Data;
using Inventario_QR.Models;
using System;
using System.Threading.Tasks;

namespace Inventario_QR.Pages
{
    public class CreateProductModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateProductModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Product Product { get; set; } = new();

        public IActionResult OnGet()
        {
            // Código autogenerado inicial (pero editable en la vista)
            Product.Code = "INV-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper();

            // Identificador QR único y automático para la etiqueta física
            Product.Qr = "QR-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            Product.Active = true;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Products.Add(Product);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}