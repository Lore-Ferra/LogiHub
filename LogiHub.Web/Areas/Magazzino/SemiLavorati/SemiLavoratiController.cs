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

            if (dto == null) return NotFound();

            var model = new ModificaSemiLavoratoViewModel
            {
                Id = dto.Id,
                Barcode = dto.Barcode,
                Descrizione = dto.Descrizione,
                UbicazioneId = dto.UbicazioneId,
                AziendaEsternaId = dto.AziendaEsternaId,
                
                // Imposta lo stato iniziale per i Radio Button
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
            // LOGICA DI PULIZIA E VALIDAZIONE STATO
            if (model.Uscito)
            {
                // CASO 1: Il pezzo è USCITO
                // Deve avere un'azienda, ma NON deve avere un'ubicazione in magazzino
                if (model.AziendaEsternaId == null)
                {
                    ModelState.AddModelError(nameof(model.AziendaEsternaId), "Seleziona l'azienda destinataria.");
                }
                
                // Pulisco i dati incoerenti
                model.UbicazioneId = null;
                
                // Rimuovo errori di validazione su Ubicazione (se presenti da DataAnnotations)
                ModelState.Remove(nameof(model.UbicazioneId)); 
            }
            else
            {
                // CASO 2: Il pezzo è IN MAGAZZINO
                // Deve avere un'ubicazione, ma NON deve avere un'azienda
                if (model.UbicazioneId == null)
                {
                    ModelState.AddModelError(nameof(model.UbicazioneId), "Seleziona un'ubicazione in magazzino.");
                }

                // Pulisco i dati incoerenti
                model.AziendaEsternaId = null;

                // Rimuovo errori di validazione su Azienda
                ModelState.Remove(nameof(model.AziendaEsternaId));
            }

            if (!ModelState.IsValid)
            {
                // Ricarico le liste per la view
                model.UbicazioniList = _context.Ubicazioni
                    .Select(u => new SelectListItem { Value = u.UbicazioneId.ToString(), Text = u.Posizione })
                    .ToList();

                model.AziendeList = _context.AziendeEsterne
                    .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Nome })
                    .ToList();

                return PartialView("ModificaSemilavorato", model);
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var dto = new ModificaSemiLavoratoDTO
            {
                Id = model.Id,
                Barcode = model.Barcode,
                Descrizione = model.Descrizione,
                UbicazioneId = model.UbicazioneId,         // Sarà null se Uscito
                AziendaEsternaId = model.AziendaEsternaId, // Sarà null se In Magazzino
                Uscito = model.Uscito,
                UserId = userId
            };

            var success = await _service.ModificaSemiLavorato(dto);
            
            if (!success) return NotFound();

            // Poiché siamo in un offcanvas/modale ajax, qui dovresti ritornare un JSON di successo o un redirect gestito dal client.
            // Se usi la logica standard MVC:
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
