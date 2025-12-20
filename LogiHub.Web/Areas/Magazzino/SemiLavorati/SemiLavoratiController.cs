using System;
using System.Security.Claims;
using LogiHub.Services;
using LogiHub.Services.Shared.SemiLavorati;
using LogiHub.Web.Areas.Magazzino.SemiLavorati;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LogiHub.Web.Areas.Magazzino
{
    using LogiHub.Services.Shared;
    using LogiHub.Web.Areas.Magazzino.Models;
    using Web.Areas;
    using Microsoft.AspNetCore.Mvc;
    using System.Linq;
    using System.Threading.Tasks;

    [Area("Magazzino")]
    public partial class SemiLavoratiController : AuthenticatedBaseController
    {
        private readonly SharedService _queries;
        private readonly TemplateDbContext _context;

        private readonly ISemiLavoratoService _service;
        public SemiLavoratiController(SharedService queries, ISemiLavoratoService service, TemplateDbContext context)
        {
            _queries = queries;
            _service = service;
            _context = context;
        }

        [HttpGet]
        public virtual async Task<IActionResult> Index(
            string filter,
            int page = 1,
            int pageSize = 25
        )
        {
            var query = new SemilavoratiIndexQuery
            {
                Filter = filter,
                Page = page,
                PageSize = pageSize
            };

            var dto = await _queries.GetSemiLavoratiListAsync(query);
            

            var model = new SemiLavoratiIndexViewModel
            {
                Filter = filter,
                Page = page,
                PageSize = pageSize,
                TotalItems = dto.TotalCount,
                SemiLavorati = dto.Items
            };

            return View(model);
        }

        [HttpGet]
        public virtual async Task<IActionResult> Dettagli(string id)
        {
            var query = new SemiLavoratiDetailsQuery { Id = id };
            var dto = await _queries.GetSemiLavoratoDetailsAsync(query);

            if (dto == null) return NotFound();

            return PartialView("DettagliSemiLavorato", dto);
        }
        
        
        [HttpGet]
        public virtual IActionResult CreaSemilavorato()
        {
            var model = new CreaSemiLavoratoViewModel
            {
                UbicazioniList = _context.Ubicazioni
                    .Select(u => new SelectListItem { Value = u.UbicazioneId.ToString(), Text = u.Posizione })
                    .ToList(),
            };

            return View("CreaSemilavorato", model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> CreaSemilavorato(CreaSemiLavoratoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.UbicazioniList = _context.Ubicazioni
                    .Select(u => new SelectListItem { Value = u.UbicazioneId.ToString(), Text = u.Posizione })
                    .ToList();

                return View("CreaSemilavorato", model);
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var dto = new CreaSemiLavoratoDTO
            {
                Id = model.Id,
                Descrizione = model.Descrizione,
                UbicazioneId = model.UbicazioneId,
                UserId = userId,
            };

            await _service.CreaSemiLavoratoAsync(dto);

            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        public virtual async Task<IActionResult> Elimina([FromBody] EliminaSemiLavoratoDTO dto)
        {
            dto.UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); 
            var success = await _service.EliminaAsync(dto);
            return success ? Ok() : BadRequest();
        }
        
        [HttpGet]
        public virtual async Task<IActionResult> Modifica(string id)
        {
            var dto = await _queries.GetSemiLavoratoDetailsAsync(new SemiLavoratiDetailsQuery { Id = id });
            if (dto == null) return NotFound();

            var model = new ModificaSemiLavoratoViewModel
            {
                Id = dto.Id,
                Descrizione = dto.Descrizione,
                UbicazioneId = dto.UbicazioneId,
                AziendaEsternaId = dto.AziendaEsternaId,
                Uscito = dto.Uscito,
                UbicazioniList = _context.Ubicazioni
                    .Select(u => new SelectListItem { Value = u.UbicazioneId.ToString(), Text = u.Posizione })
                    .ToList(),
                AziendeList = _context.AziendeEsterne
                    .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Nome })
                    .ToList()
            };

            return PartialView("ModificaSemiLavorato", model);
        }
    }
}
