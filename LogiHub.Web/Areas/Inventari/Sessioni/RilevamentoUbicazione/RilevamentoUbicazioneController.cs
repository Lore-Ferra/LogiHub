using LogiHub.Services;
using LogiHub.Services.Inventari.Sessioni;
using LogiHub.Services.Inventari.Sessioni.Query;
using LogiHub.Web.Features.SearchCard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LogiHub.Web.Areas.Inventari.Sessioni;

namespace LogiHub.Web.Areas.Inventari;

[Area("Inventari")]
public partial class RilevamentoUbicazioneController : AuthenticatedBaseController
{
    private readonly ISessioniService _service;
    private readonly TemplateDbContext _context;

    public RilevamentoUbicazioneController(ISessioniService service, TemplateDbContext context)
    {
        _service = service;
        _context = context;
    }


    [HttpGet]
    [Route("Inventari/Sessioni/{sessioneId}/Rilevamento/{ubicazioneId}")]
    public virtual async Task<IActionResult> Index(
        Guid sessioneId,
        Guid ubicazioneId,
        [FromQuery] string query,
        int page = 1,
        int pageSize = 25)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        try
        {
            await _service.BloccaUbicazioneAsync(sessioneId, ubicazioneId, userId);
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Index", "Dettaglio", new { area = "Inventari", id = sessioneId });
        }

        var status = await _service.OttieniStatusUbicazioneAsync(sessioneId, ubicazioneId);
        var sessione = await _service.OttieniDettaglioSessioneAsync(sessioneId);

        var infoUbicazione = await _context.SessioniUbicazioni
            .Include(su => su.Ubicazione)
            .Where(su => su.SessioneInventarioId == sessioneId && su.UbicazioneId == ubicazioneId)
            .Select(su => new { Nome = su.Ubicazione.Posizione })
            .FirstOrDefaultAsync();

        if (infoUbicazione == null) return NotFound("Ubicazione non trovata.");

        bool isSolaLettura = status.GiaCompletata;

        var datiPezzi = await _service.OttieniPezziUbicazioneAsync(new PezziUbicazioneQuery
        {
            SessioneId = sessioneId,
            UbicazioneId = ubicazioneId
        });

        // --- COSTRUZIONE BOTTONI ---
        
        var bottoneEsci = new SearchCardButton
        {
            Text = "Rilascia e Esci",
            CssClass = "btn-secondary",
            IconClass = "fa-solid fa-door-open",
            Type = "button",
            HtmlAttributes = new Dictionary<string, string>
            {
                { "data-confirm-trigger", "true" },
                {
                    "data-url",
                    Url.Action("Abbandona", "RilevamentoUbicazione",
                        new { area = "Inventari", sessioneId, ubicazioneId })
                },
                { "data-method", "POST" },
                { "data-type", "form" },
                { "data-message", "Vuoi rilasciare l'ubicazione? Altri operatori potranno lavorarci." }
            }
        };

        var attributiConcludi = new Dictionary<string, string>
        {
            { "onclick", "gestisciClickConcludi(event)" }
        };


        var bottoneConcludi = new SearchCardButton
        {
            Text = "Concludi",
            CssClass = "btn-primary d-none d-md-inline-block",
            IconClass = "fa-solid fa-check",
            Type = "button",
            HtmlAttributes = attributiConcludi
        };

        var bottoneAggiungi = new SearchCardButton
        {
            Text = "Aggiungi Extra",
            CssClass = "btn-outline-primary d-none d-md-inline-block",
            IconClass = "fa-solid fa-plus",
            Type = "button",
            HtmlAttributes = new Dictionary<string, string>
            {
                { "data-bs-toggle", "modal" },
                { "data-bs-target", "#modalAggiungiExtra" }
            }
        };

        var bottoneBloccato = new SearchCardButton
        {
            Text = "Concludi",
            CssClass = "btn-secondary d-none d-md-inline-block",
            IconClass = "fa-solid fa-lock",
            Type = "button",
            HtmlAttributes = new Dictionary<string, string>
            {
                { "disabled", "disabled" },
                { "title", "Ubicazione già completata" },
                { "style", "cursor: not-allowed;" }
            }
        };

        var headerButtons = new List<SearchCardButton>();
        if (!isSolaLettura)
        {
            headerButtons.Add(bottoneAggiungi);
            headerButtons.Add(bottoneEsci);
        }

        headerButtons.Add(isSolaLettura ? bottoneBloccato : bottoneConcludi);

        if (isSolaLettura)
        {
            TempData["WarningMessage"] = "Ubicazione completata. Sola lettura.";
        }

        var model = new RilevamentoUbicazioneViewModel
        {
            SessioneId = sessioneId,
            UbicazioneId = ubicazioneId,
            NomeUbicazione = infoUbicazione.Nome,
            IsSolaLettura = isSolaLettura,

            TotaliPezzi = status.Totali,
            PezziRilevati = status.Rilevati,
            ConteggioExtra = status.ConteggioExtra,

            SearchCard = new SearchCardViewModel
            {
                Title = $"Rilevamento: {infoUbicazione.Nome}",
                Placeholder = "Cerca barcode...",
                Query = query,
                ShowFilters = false,
                ShowUscitoFilter = false,
                ShowSearchInColumns = false,
                HeaderButtons = headerButtons
            },
            Pezzi = datiPezzi
                .Where(p => string.IsNullOrEmpty(query) || p.Barcode.Contains(query) ||
                            p.Descrizione.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Skip((page - 1) * pageSize)
                .Take(pageSize),
            Page = page,
            PageSize = pageSize,
            TotalItems = datiPezzi.Count
        };

        SetBreadcrumb(
            ("Inventari", Url.Action("Index", "Sessioni", new { area = "Inventari" })),
            (sessione.NomeSessione, Url.Action("Index", "Dettaglio", new { area = "Inventari", id = sessioneId })),
            ($"Ubicazione {infoUbicazione.Nome}", "")
        );

        return View("RilevamentoUbicazione", model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> SegnaPresente(Guid rigaId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var extraId = await _service.OttieniConflittoExtraAsync(rigaId);
        if (extraId.HasValue)
        {
            return Json(new { success = false, conflict = true, extraId = extraId.Value });
        }

        await _service.SegnaPresenteAsync(rigaId, userId);
        return Json(new { success = true });
    }

    [HttpPost]
    public virtual async Task<IActionResult> SegnaMancante(Guid rigaId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        await _service.SegnaMancanteAsync(rigaId, userId);
        return Json(new { success = true });
    }

    [HttpPost]
    public virtual async Task<IActionResult> AggiungiPezzo(Guid sessioneId, Guid ubicazioneId, string barcode,
        string descrizione)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _service.AggiungiExtraAsync(sessioneId, ubicazioneId, barcode, descrizione, userId);
            return Json(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public virtual async Task<IActionResult> RisolviConflittoPresente(Guid rigaId, Guid extraId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        await _service.RimuoviExtraAsync(extraId);
        await _service.SegnaPresenteAsync(rigaId, userId);
        return Json(new { success = true });
    }

    [HttpPost]
    public virtual async Task<IActionResult> ConcludiUbicazione(Guid sessioneId, Guid ubicazioneId)
    {
        await _service.CompletaUbicazioneAsync(sessioneId, ubicazioneId);
        TempData["SuccessMessage"] = "Ubicazione chiusa con successo.";

        return Json(new { success = true, redirectUrl = Url.Action("Index", "Dettaglio", new { area = "Inventari", id = sessioneId }) });
    }

    [HttpPost]
    public virtual async Task<IActionResult> Abbandona(Guid sessioneId, Guid ubicazioneId)
    {
        await _service.RilasciaUbicazioneAsync(sessioneId, ubicazioneId);
        return RedirectToAction("Index", "Dettaglio", new { area = "Inventari", id = sessioneId });
    }
}