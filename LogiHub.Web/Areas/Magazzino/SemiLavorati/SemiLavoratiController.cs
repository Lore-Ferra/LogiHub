using System;
using System.Security.Claims;
using LogiHub.Services;
using LogiHub.Services.Shared.SemiLavorati;
using LogiHub.Web.Areas.Magazzino.SemiLavorati;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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
        public virtual async Task<IActionResult> Dettagli(Guid id)
        {
            var query = new SemiLavoratiDetailsQuery { Id = id };
            var dto = await _queries.GetSemiLavoratoDetailsAsync(query);

            if (dto == null) return NotFound();

            return PartialView("DettagliSemiLavorato", dto);
        }
        
        
        [HttpGet]
        public virtual IActionResult AggiungiSemilavorato()
        {
            var model = new AggiungiSemiLavoratoViewModel
            {
                UbicazioniList = _context.Ubicazioni
                    .Select(u => new SelectListItem { Value = u.UbicazioneId.ToString(), Text = u.Posizione })
                    .ToList(),
            };

            return View("AggiungiSemilavorato", model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> AggiungiSemilavorato(AggiungiSemiLavoratoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.UbicazioniList = _context.Ubicazioni
                    .Select(u => new SelectListItem { Value = u.UbicazioneId.ToString(), Text = u.Posizione })
                    .ToList();

                return View("AggiungiSemilavorato", model);
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var dto = new AggiungiSemiLavoratoDTO
            {
                Barcode = model.Barcode,
                Descrizione = model.Descrizione,
                UbicazioneId = model.UbicazioneId,
                UserId = userId,
            };

            await _service.AggiungiSemiLavoratoAsync(dto);

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
        public virtual async Task<IActionResult> Modifica(Guid id)
        {
            var dto = await _queries.GetSemiLavoratoDetailsAsync(
                new SemiLavoratiDetailsQuery { Id = id });

            if (dto == null)
                return NotFound();

            var model = new ModificaSemiLavoratoViewModel
            {
                Id = dto.Id,
                Barcode = dto.Barcode,
                Descrizione = dto.Descrizione,
                UbicazioneId = dto.UbicazioneId,
                AziendaEsternaId = dto.AziendaEsternaId,
                Uscito = dto.AziendaEsternaId != null,

                UbicazioniList = _context.Ubicazioni
                    .Select(u => new SelectListItem
                    {
                        Value = u.UbicazioneId.ToString(),
                        Text = u.Posizione
                    })
                    .ToList(),

                AziendeList = _context.AziendeEsterne
                    .Select(a => new SelectListItem
                    {
                        Value = a.Id.ToString(),
                        Text = a.Nome
                    })
                    .ToList()
            };

            return PartialView("ModificaSemilavorato", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> Modifica(ModificaSemiLavoratoViewModel model)
        {
            model.Uscito = model.AziendaEsternaId != null;

            if (model.Uscito && model.AziendaEsternaId == null)
            {
                ModelState.AddModelError(
                    nameof(model.AziendaEsternaId),
                    "Se il semilavorato è uscito, seleziona un'azienda."
                );
            }

            if (!ModelState.IsValid)
            {
                model.UbicazioniList = _context.Ubicazioni
                    .Select(u => new SelectListItem
                    {
                        Value = u.UbicazioneId.ToString(),
                        Text = u.Posizione
                    })
                    .ToList();

                model.AziendeList = _context.AziendeEsterne
                    .Select(a => new SelectListItem
                    {
                        Value = a.Id.ToString(),
                        Text = a.Nome
                    })
                    .ToList();

                return PartialView("ModificaSemilavorato", model);
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var dto = new ModificaSemiLavoratoDTO
            {
                Id = model.Id,
                Barcode = model.Barcode,
                Descrizione = model.Descrizione,
                UbicazioneId = model.UbicazioneId,
                AziendaEsternaId = model.AziendaEsternaId,

                Uscito = model.AziendaEsternaId != null,

                UserId = userId
            };

            var success = await _service.ModificaSemiLavorato(dto);
            if (!success)
                return NotFound();

            return RedirectToAction(nameof(Index));
        }
        
        [HttpGet]
        [Route("/Magazzino/SemiLavorati/GeneraBarcodeUnivoco")]
        public virtual async Task<IActionResult> GeneraBarcodeUnivoco()
        {
            string barcode;
            do
            {
                barcode = "#" + Random.Shared.Next(0001, 9999);
            }
            while (await _context.SemiLavorati.AnyAsync(x => x.Barcode == barcode));

            return Json(barcode);
        }

        [HttpGet]
        [Route("/Magazzino/SemiLavorati/VerificaEsistenzaBarcode")]
        public virtual async Task<IActionResult> VerificaEsistenzaBarcode(string barcode)
        {
            var esiste = await _context.SemiLavorati
                .AnyAsync(x => x.Barcode == barcode && !x.Eliminato);

            return Json(new { esiste });
        }
    }
}
