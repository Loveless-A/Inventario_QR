using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Inventario_QR.Data;
using Inventario_QR.Helpers;
using Inventario_QR.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Inventario_QR.Pages
{
    public class AssignmentFormModel : PageModel
    {
        private readonly AppDbContext _context;

        public AssignmentFormModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public VisualizationsModel.ProductInfo Product { get; set; } = new();
        public VisualizationsModel.CharacteristicInfo Characteristic { get; set; } = new();
        public List<VisualizationsModel.ChecklistDetailItem> Items { get; set; } = new();
        public string QrImage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            if (Id <= 0) return RedirectToPage("./Index");

            var productEntity = await _context.Products.FindAsync(Id);
            if (productEntity == null) return RedirectToPage("./Index");

            Product = new VisualizationsModel.ProductInfo
            {
                Id = productEntity.Id,
                Code = productEntity.Code ?? string.Empty,
                ProductName = productEntity.ProductName ?? string.Empty,
                Qr = productEntity.Qr ?? string.Empty
            };

            var charData = await _context.Characteristics
                .Include(c => c.Person)
                .Where(c => c.ProductId == Id && c.Active)
                .OrderByDescending(c => c.Date)
                .FirstOrDefaultAsync();

            if (charData != null)
            {
                Characteristic = new VisualizationsModel.CharacteristicInfo
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

            QrImage = QrGenerator.ToSvgDataUri($"{Request.Scheme}://{Request.Host}/Visualizations/{Id}", 4);

            var productDetailsList = await _context.ProductDetails
                .Include(pd => pd.Detail)
                .Where(pd => pd.ProductId == Id)
                .ToListAsync();

            Items = productDetailsList.Select(pd => new VisualizationsModel.ChecklistDetailItem
            {
                DetailName = pd.Detail != null ? (pd.Detail.DetailName ?? string.Empty) : string.Empty,
                Complement = pd.Detail != null ? pd.Detail.Complement : false,
                IsSelected = true,
                Amount = pd.Amount,
                DetailsValue = pd.DetailsValue ?? string.Empty
            }).ToList();

            return Page();
        }
    }
}