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

        if ((Filters.SearchInColumns == null || !Filters.SearchInColumns.Any())
            && page == 1
            && string.IsNullOrWhiteSpace(Query))
        {
            Filters.SearchInColumns = new List<string>
            {
                "Barcode",
                "Descrizione"
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
            CssClass = "btn-secondary d-none d-md-inline-block",
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
            Placeholder = "Cerca…",
            Query = Query,
            Filters = Filters,
            HeaderButtons = new List<SearchCardButton>
            {
                isBloccato ? bottoneDisabilitato : bottoneAttivo
            },
        
            AlertTitle = isBloccato ? "Magazzino Bloccato: " : null,
            Message = isBloccato ? "Inventario in corso. Inserimento, modifica ed eliminazione disabilitati." : null,
            MessageType = isBloccato ? SearchCardMessageType.Warning : SearchCardMessageType.None,
            
            DefaultSearchInColumnKey = "Barcode",
        
            SearchInColumns = new()
            {
                new() { Key = "Barcode", Label = "Barcode" },
                new() { Key = "Descrizione", Label = "Descrizione" },
                new() { Key = "Ubicazione", Label = "Ubicazione" },
                new() { Key = "UltimaModifica", Label = "Ultima modifica" },
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

        // Carico le liste per i modali di azione rapida (Sposta / Movimenta)
        ViewData["UbicazioniList"] = await _context.Ubicazioni
            .OrderBy(u => u.Posizione)
            .Select(u => new SelectListItem { Value = u.UbicazioneId.ToString(), Text = u.Posizione })
            .ToListAsync();

        ViewData["AziendeList"] = await _context.AziendeEsterne
            .OrderBy(a => a.Nome)
            .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Nome })
            .ToListAsync();

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
    public virtual async Task<IActionResult> Elimina(
        Guid? semiLavoratoId,
        EliminaSemiLavoratoDTO? dto)
    {
        if (await _bloccoService.IsBloccatoAsync())
            return Json(new { success = false, message = "Magazzino bloccato per inventario." });

        Guid id;
        if (dto is not null && dto.SemiLavoratoId != Guid.Empty)
        {
            id = dto.SemiLavoratoId;
        }
        else if (semiLavoratoId.HasValue && semiLavoratoId.Value != Guid.Empty)
        {
            id = semiLavoratoId.Value;
            dto = new EliminaSemiLavoratoDTO { SemiLavoratoId = id };
        }
        else
        {
            return Json(new { success = false, message = "Id mancante." });
        }

        dto.UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var success = await _service.EliminaSemiLavoratoAsync(dto);

        if (!success) return Json(new { success = false, message = "Errore durante l'eliminazione." });

        return Json(new { success = true });
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
            barcode = "#" + Random.Shared.Next(0001, 9999);
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

    [HttpGet]
    public virtual async Task<IActionResult> GetSpostaModal(Guid id)
    {
        if (await _bloccoService.IsBloccatoAsync())
            return BadRequest("Magazzino bloccato.");

        var sl = await _context.SemiLavorati
            .Include(s => s.Ubicazione)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (sl == null) return NotFound();

        var ubicazioni = await _context.Ubicazioni
            .OrderBy(u => u.Posizione)
            .Select(u => new SelectListItem { Value = u.UbicazioneId.ToString(), Text = u.Posizione })
            .ToListAsync();

        if (sl.UbicazioneId.HasValue)
        {
            var currentIdStr = sl.UbicazioneId.Value.ToString();
            ubicazioni = ubicazioni
                .Where(x => x.Value != currentIdStr)
                .ToList();
        }

        var model = new SpostaSemiLavoratoViewModel
        {
            Id = sl.Id,
            Barcode = sl.Barcode,
            Descrizione = sl.Descrizione,
            UbicazioneAttuale = sl.Ubicazione?.Posizione ?? "N/D",
            UbicazioniList = ubicazioni
        };

        return PartialView("_SpostaModal", model);
    }

    [HttpGet]
    public virtual async Task<IActionResult> GetMovimentaModal(Guid id)
    {
        if (await _bloccoService.IsBloccatoAsync())
            return BadRequest("Magazzino bloccato.");

        var sl = await _context.SemiLavorati.FindAsync(id);
        if (sl == null) return NotFound();

        var model = new MovimentoSemiLavoratoViewModel
        {
            Id = sl.Id,
            Barcode = sl.Barcode,
            Descrizione = sl.Descrizione,
            Uscito = !sl.Uscito,
            UbicazioniList = await _context.Ubicazioni
                .OrderBy(u => u.Posizione)
                .Select(u => new SelectListItem { Value = u.UbicazioneId.ToString(), Text = u.Posizione })
                .ToListAsync(),
            AziendeList = await _context.AziendeEsterne
                .OrderBy(a => a.Nome)
                .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Nome })
                .ToListAsync()
        };

        ViewData["OriginalUscito"] = sl.Uscito;
        return PartialView("_MovimentoModal", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> Sposta(SpostaSemiLavoratoViewModel model)
    {
        if (await _bloccoService.IsBloccatoAsync())
            return Json(new { success = false, message = "Magazzino bloccato per inventario." });

        var sl = await _context.SemiLavorati.Include(x => x.Ubicazione).FirstOrDefaultAsync(x => x.Id == model.Id);
        if (sl == null)
            return Json(new { success = false, message = "Semilavorato non trovato." });

        // Validazione
        if (model.NuovaUbicazioneId == Guid.Empty || model.NuovaUbicazioneId == null)
            ModelState.AddModelError(nameof(model.NuovaUbicazioneId), "Seleziona un'ubicazione valida.");
        
        if (model.NuovaUbicazioneId == sl.UbicazioneId)
            ModelState.AddModelError(nameof(model.NuovaUbicazioneId), "Seleziona una nuova ubicazione diversa da quella attuale.");

        if (!ModelState.IsValid)
        {
            model.Barcode = sl.Barcode;
            model.Descrizione = sl.Descrizione;
            model.UbicazioneAttuale = sl.Ubicazione?.Posizione ?? "N/D";
            var ubicazioni = await _context.Ubicazioni
                .OrderBy(u => u.Posizione)
                .Select(u => new SelectListItem { Value = u.UbicazioneId.ToString(), Text = u.Posizione })
                .ToListAsync();

            if (sl.UbicazioneId.HasValue)
            {
                var currentIdStr = sl.UbicazioneId.Value.ToString();
                ubicazioni = ubicazioni.Where(x => x.Value != currentIdStr).ToList();
            }

            model.UbicazioniList = ubicazioni;
            return PartialView("_SpostaModal", model);
        }

        var dto = new ModificaSemiLavoratoDTO
        {
            Id = sl.Id,
            Barcode = sl.Barcode,
            Descrizione = sl.Descrizione,
            UbicazioneId = model.NuovaUbicazioneId,
            AziendaEsternaId = null, // Se sposto internamente, non è uscito
            Uscito = false,
            UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
        };

        var success = await _service.ModificaSemiLavorato(dto);
        
        if (success)
            return Json(new { success = true, message = "Spostamento effettuato con successo!" });
        
        return Json(new { success = false, message = "Errore durante lo spostamento." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> CambiaStato(MovimentoSemiLavoratoViewModel model)
    {
        if (await _bloccoService.IsBloccatoAsync())
            return Json(new { success = false, message = "Magazzino bloccato per inventario." });

        var sl = await _context.SemiLavorati.FindAsync(model.Id);
        if (sl == null)
            return Json(new { success = false, message = "Semilavorato non trovato." });

        // Validazione
        if (model.Uscito)
        {
            if (model.AziendaId == null || model.AziendaId == Guid.Empty)
                ModelState.AddModelError(nameof(model.AziendaId), "Seleziona l'azienda destinataria.");
            
            if (sl.Uscito && sl.AziendaEsternaId == model.AziendaId)
                ModelState.AddModelError(nameof(model.AziendaId), "Seleziona un'azienda diversa.");
        }
        else
        {
            if (model.UbicazioneId == null || model.UbicazioneId == Guid.Empty)
                ModelState.AddModelError(nameof(model.UbicazioneId), "Seleziona l'ubicazione di rientro.");
            
            if (!sl.Uscito)
                ModelState.AddModelError(nameof(model.Uscito), "Il semilavorato è già in magazzino.");
        }

        if (!ModelState.IsValid)
        {
            model.Barcode = sl.Barcode;
            model.Descrizione = sl.Descrizione;
            model.UbicazioniList = await _context.Ubicazioni.OrderBy(u => u.Posizione).Select(u => new SelectListItem { Value = u.UbicazioneId.ToString(), Text = u.Posizione }).ToListAsync();
            model.AziendeList = await _context.AziendeEsterne.OrderBy(a => a.Nome).Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Nome }).ToListAsync();
            ViewData["OriginalUscito"] = sl.Uscito;
            return PartialView("_MovimentoModal", model);
        }

        var dto = new ModificaSemiLavoratoDTO
        {
            Id = sl.Id,
            Barcode = sl.Barcode,
            Descrizione = sl.Descrizione,
            UbicazioneId = !model.Uscito ? model.UbicazioneId : null,
            AziendaEsternaId = model.Uscito ? model.AziendaId : null,
            Uscito = model.Uscito,
            UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
        };

        var success = await _service.ModificaSemiLavorato(dto);
        
        if (success) 
            return Json(new { success = true, message = "Stato aggiornato con successo!" });
            
        return Json(new { success = false, message = "Errore durante l'aggiornamento." });
    }
}