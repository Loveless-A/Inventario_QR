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
    public class GroupAssignmentFormModel : PageModel
    {
        private readonly AppDbContext _context;

        public GroupAssignmentFormModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string Ids { get; set; }

        public class ActaGroupItemVm
        {
            public VisualizationsModel.ProductInfo Product { get; set; } = new();
            public VisualizationsModel.CharacteristicInfo Characteristic { get; set; } = new();
            public List<VisualizationsModel.ChecklistDetailItem> Items { get; set; } = new();
            public string QrImage { get; set; } = string.Empty;
        }

        public List<ActaGroupItemVm> Actas { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(Ids)) return RedirectToPage("./Index");

            var idList = Ids.Split(',').Select(int.Parse).ToList();
            var products = await _context.Products.Where(p => idList.Contains(p.Id)).ToListAsync();

            foreach (var productEntity in products)
            {
                var productInfo = new VisualizationsModel.ProductInfo
                {
                    Id = productEntity.Id,
                    Code = productEntity.Code ?? string.Empty,
                    ProductName = productEntity.ProductName ?? string.Empty,
                    Qr = productEntity.Qr ?? string.Empty
                };

                var charData = await _context.Characteristics
                    .Include(c => c.Person)
                    .Where(c => c.ProductId == productEntity.Id && c.Active)
                    .OrderByDescending(c => c.Date)
                    .FirstOrDefaultAsync();

                var characteristicInfo = new VisualizationsModel.CharacteristicInfo();
                if (charData != null)
                {
                    characteristicInfo = new VisualizationsModel.CharacteristicInfo
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

                string qrImage = QrGenerator.ToSvgDataUri($"{Request.Scheme}://{Request.Host}/Visualizations/{productEntity.Id}", 4);

                var productDetailsList = await _context.ProductDetails
                    .Include(pd => pd.Detail)
                    .Where(pd => pd.ProductId == productEntity.Id)
                    .ToListAsync();

                var items = productDetailsList.Select(pd => new VisualizationsModel.ChecklistDetailItem
                {
                    DetailName = pd.Detail != null ? (pd.Detail.DetailName ?? string.Empty) : string.Empty,
                    Complement = pd.Detail != null ? pd.Detail.Complement : false,
                    IsSelected = true,
                    Amount = pd.Amount,
                    DetailsValue = pd.DetailsValue ?? string.Empty
                }).ToList();

                Actas.Add(new ActaGroupItemVm
                {
                    Product = productInfo,
                    Characteristic = characteristicInfo,
                    Items = items,
                    QrImage = qrImage
                });
            }

            return Page();
        }
    }
}