using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Inventario_QR.Data;
using Inventario_QR.Models;

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
        public string Mode { get; set; } // "assign" o "details"

        public Product Product { get; set; }
        public List<Person> AllPersons { get; set; } = new List<Person>();
        public List<Position> AllPositions { get; set; } = new List<Position>();
        public List<Dependence> AllDependences { get; set; } = new List<Dependence>();

        [BindProperty]
        public int? SelectedPersonId { get; set; }

        [BindProperty]
        public string Localization { get; set; }

        [BindProperty]
        public string AssignmentComment { get; set; }

        [BindProperty]
        public List<ChecklistItemVM> AllChecklistItems { get; set; } = new List<ChecklistItemVM>();

        [BindProperty]
        public string NewCatalogDetailName { get; set; }

        [BindProperty]
        public bool NewCatalogIsComplement { get; set; }

        // Propiedades para Modal de Persona
        [BindProperty]
        public int? ModalPersonId { get; set; }

        [BindProperty]
        public string ModalNewPersonLastName { get; set; }

        [BindProperty]
        public string ModalNewPersonIdentification { get; set; }

        [BindProperty]
        public string ModalNewPersonNumbers { get; set; }

        [BindProperty]
        public bool ModalNewPersonIsActive { get; set; } = true;

        [BindProperty]
        public int? ModalNewPersonPositionId { get; set; }

        [BindProperty]
        public int? ModalNewPersonDependenceId { get; set; }

        public string AssignedPersonDisplayValue { get; set; }
        public bool IsAlreadyAssigned { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        [BindProperty]
        public int? ModalPositionId { get; set; }

        [BindProperty]
        public string NewPositionName { get; set; }

        [BindProperty]
        public int? ModalDependenceId { get; set; }

        [BindProperty]
        public string NewDependenceName { get; set; }

        // ==========================================
        // HANDLERS PARA REGISTRAR O MODIFICAR CARGO Y DEPENDENCIA
        // ==========================================
        // ==========================================
        // HANDLERS AJAX PARA CARGO Y DEPENDENCIA
        // ==========================================
        public async Task<IActionResult> OnPostRegisterPositionAsync()
        {
            if (string.IsNullOrWhiteSpace(NewPositionName))
            {
                return new JsonResult(new { success = false, message = "El nombre del cargo no puede estar vacío." });
            }

            Position positionObj;

            if (ModalPositionId.HasValue && ModalPositionId.Value > 0)
            {
                positionObj = await _context.Positions.FindAsync(ModalPositionId.Value);
                if (positionObj != null)
                {
                    positionObj.PositionName = NewPositionName.Trim();
                }
            }
            else
            {
                positionObj = new Position
                {
                    PositionName = NewPositionName.Trim(),
                    Active = true
                };
                _context.Positions.Add(positionObj);
            }

            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                id = positionObj.Id,
                name = positionObj.PositionName,
                isEdit = ModalPositionId.HasValue && ModalPositionId.Value > 0
            });
        }

        public async Task<IActionResult> OnPostRegisterDependenceAsync()
        {
            if (string.IsNullOrWhiteSpace(NewDependenceName))
            {
                return new JsonResult(new { success = false, message = "El nombre de la dependencia no puede estar vacío." });
            }

            Dependence dependenceObj;

            if (ModalDependenceId.HasValue && ModalDependenceId.Value > 0)
            {
                dependenceObj = await _context.Dependences.FindAsync(ModalDependenceId.Value);
                if (dependenceObj != null)
                {
                    dependenceObj.DependenceName = NewDependenceName.Trim();
                }
            }
            else
            {
                dependenceObj = new Dependence
                {
                    DependenceName = NewDependenceName.Trim(),
                    Active = true
                };
                _context.Dependences.Add(dependenceObj);
            }

            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                id = dependenceObj.Id,
                name = dependenceObj.DependenceName,
                isEdit = ModalDependenceId.HasValue && ModalDependenceId.Value > 0
            });
        }

        public readonly List<string> DepartamentosBolivia = new List<string>
        {
            "Beni", "Chuquisaca", "Cochabamba", "La Paz", "Oruro", "Pando", "Potosí", "Santa Cruz", "Tarija"
        };

        public class ChecklistItemVM
        {
            public int DetailsId { get; set; }
            public string DetailName { get; set; }
            public bool Complement { get; set; }
            public bool IsSelected { get; set; }
            public int Amount { get; set; } = 1;
            public string DetailsValue { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == Id);

            if (Product == null)
            {
                return NotFound();
            }

            await LoadMasterDataAsync();

            // Cargar asignación existente
            var activeChar = await _context.Characteristics
                .Include(c => c.Person)
                .FirstOrDefaultAsync(c => c.ProductId == Id && c.Active);

            if (activeChar != null)
            {
                IsAlreadyAssigned = true;
                SelectedPersonId = activeChar.PersonId;
                Localization = activeChar.Localization;
                AssignmentComment = activeChar.Coment;
                if (activeChar.Person != null)
                {
                    AssignedPersonDisplayValue = $"{activeChar.Person.LastName} (CI: {activeChar.Person.Identification})";
                }
            }

            // Cargar items del catálogo y asociar con ProductDetail
            var catalogDetails = await _context.Details.ToListAsync();
            var existingProductDetails = await _context.ProductDetails
                .Where(pd => pd.ProductId == Id)
                .ToListAsync();

            AllChecklistItems = catalogDetails.Select(d =>
            {
                var pd = existingProductDetails.FirstOrDefault(x => x.DetailsId == d.Id);
                return new ChecklistItemVM
                {
                    DetailsId = d.Id,
                    DetailName = d.DetailName,
                    Complement = d.Complement,
                    IsSelected = pd != null,
                    Amount = pd?.Amount ?? 1,
                    DetailsValue = pd?.DetailsValue ?? ""
                };
            }).ToList();

            return Page();
        }

        private async Task LoadMasterDataAsync()
        {
            AllPersons = await _context.Persons.Where(p => p.Active).ToListAsync();

            // Nombres de propiedades tomados de tu modelo Position y Dependence
            AllPositions = await _context.Positions.Where(p => p.Active).OrderBy(p => p.PositionName).ToListAsync();
            AllDependences = await _context.Dependences.Where(d => d.Active).OrderBy(d => d.DependenceName).ToListAsync();
        }

        public async Task<IActionResult> OnPostSaveAssignAsync()
        {
            if (SelectedPersonId == null || SelectedPersonId == 0)
            {
                ErrorMessage = "Debe seleccionar una persona activa para la asignación.";
                return await OnGetAsync();
            }

            // ============================================================
            // BUSCAR ASIGNACIÓN EXISTENTE DEL PRODUCTO
            // ============================================================

            var existingChar = await _context.Characteristics
                .FirstOrDefaultAsync(c => c.ProductId == Id && c.Active);

            bool esNuevaAsignacion = existingChar == null;

            // ============================================================
            // SI EXISTE -> MODIFICAR
            // SI NO EXISTE -> CREAR
            // ============================================================

            if (existingChar != null)
            {
                // MODIFICAR LA FILA EXISTENTE
                existingChar.PersonId = SelectedPersonId.Value;
                existingChar.Localization = Localization;
                existingChar.Coment = AssignmentComment;
                existingChar.Date = DateTime.UtcNow;
                existingChar.Active = true;
            }
            else
            {
                // CREAR NUEVA ASIGNACIÓN
                existingChar = new Characteristic
                {
                    ProductId = Id,
                    PersonId = SelectedPersonId.Value,
                    Localization = Localization,
                    Coment = AssignmentComment,
                    Date = DateTime.UtcNow,
                    Active = true
                };

                _context.Characteristics.Add(existingChar);
            }

            // ============================================================
            // COMPLEMENTOS
            // ============================================================

            var currentProductDetails = await _context.ProductDetails
                .Where(pd => pd.ProductId == Id)
                .ToListAsync();

            foreach (var item in AllChecklistItems.Where(i => i.Complement))
            {
                var existingPd = currentProductDetails
                    .FirstOrDefault(pd => pd.DetailsId == item.DetailsId);

                if (item.IsSelected)
                {
                    if (existingPd != null)
                    {
                        // MODIFICAR COMPLEMENTO EXISTENTE
                        existingPd.Amount = item.Amount;
                        existingPd.DetailsValue = item.DetailsValue;
                    }
                    else
                    {
                        // AGREGAR NUEVO COMPLEMENTO
                        _context.ProductDetails.Add(new ProductDetail
                        {
                            ProductId = Id,
                            DetailsId = item.DetailsId,
                            Amount = item.Amount,
                            DetailsValue = item.DetailsValue
                        });
                    }
                }
                else if (existingPd != null)
                {
                    // QUITAR COMPLEMENTO
                    _context.ProductDetails.Remove(existingPd);
                }
            }

            await _context.SaveChangesAsync();

            // ============================================================
            // MENSAJE
            // ============================================================

            StatusMessage = esNuevaAsignacion
                ? "Asignación guardada correctamente."
                : "Asignación modificada correctamente.";

            return RedirectToPage(new
            {
                id = Id,
                mode = "assign"
            });
        }

        public async Task<IActionResult> OnPostModifyDetailsAsync()
        {
            var currentProductDetails = await _context.ProductDetails.Where(pd => pd.ProductId == Id).ToListAsync();

            foreach (var item in AllChecklistItems.Where(i => !i.Complement))
            {
                var existingPd = currentProductDetails.FirstOrDefault(pd => pd.DetailsId == item.DetailsId);

                if (item.IsSelected)
                {
                    if (existingPd != null)
                    {
                        existingPd.Amount = item.Amount;
                        existingPd.DetailsValue = item.DetailsValue;
                    }
                    else
                    {
                        _context.ProductDetails.Add(new ProductDetail
                        {
                            ProductId = Id,
                            DetailsId = item.DetailsId,
                            Amount = item.Amount,
                            DetailsValue = item.DetailsValue
                        });
                    }
                }
                else if (existingPd != null)
                {
                    _context.ProductDetails.Remove(existingPd);
                }
            }

            await _context.SaveChangesAsync();
            StatusMessage = "Detalles del producto modificados correctamente.";
            return RedirectToPage(new { id = Id, mode = "details" });
        }

        public async Task<IActionResult> OnPostRegisterCatalogItemAsync()
        {
            if (!string.IsNullOrWhiteSpace(NewCatalogDetailName))
            {
                var detail = new Detail
                {
                    DetailName = NewCatalogDetailName.Trim(),
                    Complement = NewCatalogIsComplement
                };
                _context.Details.Add(detail);
                await _context.SaveChangesAsync();
                StatusMessage = "Elemento registrado en el catálogo.";
            }

            return RedirectToPage(new { id = Id, mode = Mode });
        }

        public async Task<IActionResult> OnPostDeleteCatalogItemAsync(int detailId)
        {
            var detail = await _context.Details.FindAsync(detailId);
            if (detail != null)
            {
                var relatedProductDetails = _context.ProductDetails.Where(pd => pd.DetailsId == detailId);
                _context.ProductDetails.RemoveRange(relatedProductDetails);
                _context.Details.Remove(detail);
                await _context.SaveChangesAsync();
                StatusMessage = "Elemento eliminado del catálogo.";
            }

            return RedirectToPage(new { id = Id, mode = Mode });
        }

        public async Task<IActionResult> OnPostRegisterPersonAsync()
        {
            if (!string.IsNullOrWhiteSpace(ModalNewPersonLastName) && !string.IsNullOrWhiteSpace(ModalNewPersonIdentification))
            {
                Person person;
                if (ModalPersonId.HasValue && ModalPersonId.Value > 0)
                {
                    person = await _context.Persons.FindAsync(ModalPersonId.Value);
                    if (person != null)
                    {
                        person.LastName = ModalNewPersonLastName.Trim();
                        person.Identification = ModalNewPersonIdentification.Trim();
                        person.Numbers = ModalNewPersonNumbers?.Trim();
                        person.Active = ModalNewPersonIsActive;
                        person.PositionId = ModalNewPersonPositionId;
                        person.DependenceId = ModalNewPersonDependenceId;
                        StatusMessage = "Persona actualizada con éxito.";
                    }
                }
                else
                {
                    person = new Person
                    {
                        LastName = ModalNewPersonLastName.Trim(),
                        Identification = ModalNewPersonIdentification.Trim(),
                        Numbers = ModalNewPersonNumbers?.Trim(),
                        Active = ModalNewPersonIsActive,
                        PositionId = ModalNewPersonPositionId,
                        DependenceId = ModalNewPersonDependenceId
                    };
                    _context.Persons.Add(person);
                    await _context.SaveChangesAsync();
                    StatusMessage = "Persona registrada con éxito.";
                }

                await _context.SaveChangesAsync();
                if (person != null)
                {
                    SelectedPersonId = person.Id;
                }
            }

            return RedirectToPage(new { id = Id, mode = "assign" });
        }
    }
}