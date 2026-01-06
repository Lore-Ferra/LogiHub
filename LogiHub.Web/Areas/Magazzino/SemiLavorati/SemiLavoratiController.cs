using System;
using System.Collections.Generic;
using System.Security.Claims;
using LogiHub.Services;
using LogiHub.Services.Magazzino.SemiLavorati.DTO;
using LogiHub.Services.Shared.SemiLavorati;
using LogiHub.Web.Areas.Magazzino.SemiLavorati;
using LogiHub.Web.Features.SearchCard;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LogiHub.Services.Shared;
using LogiHub.Web.Areas.Magazzino.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace LogiHub.Web.Areas.Magazzino;

[Area("Magazzino")]
public partial class SemiLavoratiController : AuthenticatedBaseController
{
    private readonly SharedService _queries;
    private readonly TemplateDbContext _context;

    private readonly ISemiLavoratoService _service;
    private readonly IBloccoMagazzinoService _bloccoService;

    public SemiLavoratiController(
        SharedService queries,
        ISemiLavoratoService service,
        IBloccoMagazzinoService bloccoService,
        TemplateDbContext context)
    {
        _queries = queries;
        _service = service;
        _context = context;
        _bloccoService = bloccoService;
    }

    [HttpGet]
    public virtual async Task<IActionResult> Index(
        [FromQuery] string Query,
        [FromQuery] SearchCardFiltersViewModel Filters,
        int page = 1,
        int? pageSize = null
    )
    {
        const string pageSizeKey = "Magazzino_SemiLavorati_PageSize";

        int effectivePageSize;
        if (pageSize.HasValue)
        {
            effectivePageSize = pageSize.Value;
            HttpContext.Session.SetInt32(pageSizeKey, effectivePageSize);
        }
        else
        {
            effectivePageSize = HttpContext.Session.GetInt32(pageSizeKey) ?? 25;
        }

        bool isBloccato = await _bloccoService.IsBloccatoAsync();

        if (Filters == null)
        {
            Filters = new SearchCardFiltersViewModel
            {
                Uscito = TriState.All
            };
        }

        var serviceQuery = new SemilavoratiIndexQuery
        {
            SearchText = Query,
            Uscito = Filters.Uscito,
            SearchInColumns = Filters.SearchInColumns,
            Page = page,
            PageSize = effectivePageSize
        };

        var dto = await _queries.GetSemiLavoratiListAsync(serviceQuery);

        var bottoneAttivo = new SearchCardButton
        {
            Text = "Aggiungi",
            CssClass = "btn-primary d-none d-md-inline-block",
            IconClass = "fa-solid fa-plus",
            Type = "button",
            HtmlAttributes = new Dictionary<string, string>
            {
                { "data-offcanvas", "" },
                { "data-id", "offcanvasAggiungi" },
                { "data-url", Url.Action("AggiungiSemilavorato") },
                { "data-title", "Aggiungi Semilavorato" }
            }
        };

        var bottoneDisabilitato = new SearchCardButton
        {
            Text = "Aggiungi",
            CssClass = "btn-primary d-none d-md-inline-block",
            IconClass = "fa-solid fa-lock",
            Type = "button",
            HtmlAttributes = new Dictionary<string, string>
            {
                { "disabled", "disabled" },
                { "title", "Magazzino bloccato per inventario" },
                { "style", "cursor: not-allowed; pointer-events: auto !important;" }
            }
        };

        var searchCardModel = new SearchCardViewModel
        {
            Title = "📦 Magazzino Semilavorati",
            Placeholder = "Cerca barcode, descrizione...",
            Query = Query,
            Filters = Filters,
            HeaderButtons = new List<SearchCardButton>
            {
                isBloccato ? bottoneDisabilitato : bottoneAttivo
            },

            SearchInColumns = new()
            {
                new() { Key = "Barcode", Label = "Barcode", DefaultSelected = true },
                new() { Key = "Descrizione", Label = "Descrizione", DefaultSelected = true },
                new() { Key = "Ubicazione", Label = "Ubicazione", DefaultSelected = true },
                new() { Key = "UltimaModifica", Label = "Ultima modifica", DefaultSelected = true },
            }
        };

        var model = new SemiLavoratiIndexViewModel
        {
            SearchCard = searchCardModel,
            Page = page,
            PageSize = effectivePageSize,
            TotalItems = dto.TotalCount,
            SemiLavorati = dto.Items
        };

        SetBreadcrumb(
            ("Magazzino", "") 
        );

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
    public virtual async Task<IActionResult> AggiungiSemilavorato()
    {
        if (await _bloccoService.IsBloccatoAsync())
        {
            return RedirectToAction(nameof(Index));
        }

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
        if (await _bloccoService.IsBloccatoAsync())
        {
            ModelState.AddModelError(string.Empty, "Magazzino bloccato per inventario.");

            model.UbicazioniList = _context.Ubicazioni
                .Select(u => new SelectListItem { Value = u.UbicazioneId.ToString(), Text = u.Posizione })
                .ToList();
            return View("AggiungiSemilavorato", model);
        }

        bool esisteGia = await _context.SemiLavorati
            .AnyAsync(x => x.Barcode == model.Barcode && !x.Eliminato);

        if (esisteGia)
        {
            ModelState.AddModelError(nameof(model.Barcode), "Attenzione: questo Barcode esiste già a sistema.");
        }

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
        if (await _bloccoService.IsBloccatoAsync())
        {
            return BadRequest("Magazzino bloccato per inventario.");
        }

        dto.UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var success = await _service.EliminaSemiLavoratoAsync(dto);
        return success ? Ok() : BadRequest();
    }

    [HttpGet]
    public virtual async Task<IActionResult> Modifica(Guid id)
    {
        if (await _bloccoService.IsBloccatoAsync())
        {
            return BadRequest("Magazzino bloccato per inventario.");
        }

        var dto = await _queries.GetSemiLavoratoDetailsAsync(new SemiLavoratiDetailsQuery { Id = id });
        if (dto == null) return NotFound();

        var model = new ModificaSemiLavoratoViewModel
        {
            Id = dto.Id,
            Barcode = dto.Barcode,
            Descrizione = dto.Descrizione,
            UbicazioneId = dto.UbicazioneId,
            AziendaEsternaId = dto.AziendaEsternaId,
            Uscito = dto.Uscito,

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
        if (await _bloccoService.IsBloccatoAsync())
        {
            return BadRequest("Magazzino bloccato per inventario.");
        }

        bool duplicato = await _context.SemiLavorati
            .AnyAsync(x => x.Barcode == model.Barcode && x.Id != model.Id && !x.Eliminato);

        if (duplicato)
        {
            ModelState.AddModelError(nameof(model.Barcode), "Barcode già presente nel sistema!");
        }

        if (model.Uscito)
        {
            if (model.AziendaEsternaId == null)
            {
                ModelState.AddModelError(nameof(model.AziendaEsternaId), "Seleziona l'azienda destinataria.");
            }

            model.UbicazioneId = null;

            ModelState.Remove(nameof(model.UbicazioneId));
        }
        else
        {
            if (model.UbicazioneId == null)
            {
                ModelState.AddModelError(nameof(model.UbicazioneId), "Seleziona un'ubicazione in magazzino.");
            }

            model.AziendaEsternaId = null;

            ModelState.Remove(nameof(model.AziendaEsternaId));
        }

        if (!ModelState.IsValid)
        {
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
            UbicazioneId = model.UbicazioneId,
            AziendaEsternaId = model.AziendaEsternaId,
            Uscito = model.Uscito,
            UserId = userId
        };

        var success = await _service.ModificaSemiLavorato(dto);

        if (!success) return NotFound();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Route("/Magazzino/SemiLavorati/GeneraBarcodeUnivoco")]
    public virtual async Task<IActionResult> GeneraBarcodeUnivoco()
    {
        string barcode;
        do
        {
            barcode = "PZ-" + Random.Shared.Next(0001, 9999);
        } while (await _context.SemiLavorati.AnyAsync(x => x.Barcode == barcode));

        return Json(barcode);
    }

    [HttpGet]
    [Route("/Magazzino/SemiLavorati/VerificaEsistenzaBarcode")]
    public virtual async Task<IActionResult> VerificaEsistenzaBarcode(string barcode, Guid? idEscluso)
    {
        var query = _context.SemiLavorati
            .Where(x => x.Barcode == barcode && !x.Eliminato);

        if (idEscluso.HasValue)
        {
            query = query.Where(x => x.Id != idEscluso.Value);
        }

        var esiste = await query.AnyAsync();

        return Json(new { esiste });
    }
}