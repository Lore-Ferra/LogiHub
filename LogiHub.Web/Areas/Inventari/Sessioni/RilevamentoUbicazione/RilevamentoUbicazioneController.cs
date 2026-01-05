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
    public virtual async Task<IActionResult> Index(
        Guid sessioneId,
        Guid ubicazioneId,
        [FromQuery] string query,
        int page = 1,
        int pageSize = 25)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // 1. Lock dell'ubicazione
        // (Grazie alla modifica fatta nel Service, questo NON lancia più eccezione se è già completata)
        await _service.BloccaUbicazioneAsync(sessioneId, ubicazioneId, userId);

        // 2. Recupero Info Stato (Nome e se è Completata)
        // Interroghiamo SessioniUbicazioni per avere lo stato preciso
        var infoUbicazione = await _context.SessioniUbicazioni
            .Include(su => su.Ubicazione)
            .Where(su => su.SessioneInventarioId == sessioneId && su.UbicazioneId == ubicazioneId)
            .Select(su => new
            {
                Nome = su.Ubicazione.Posizione,
                Completata = su.Completata
            })
            .FirstOrDefaultAsync();

        if (infoUbicazione == null) return NotFound("Ubicazione non trovata nella sessione.");

        // Flag Sola Lettura
        bool isSolaLettura = infoUbicazione.Completata;

        // 3. Recupero dati pezzi
        var datiPezzi = await _service.OttieniPezziUbicazioneAsync(new PezziUbicazioneQuery
        {
            SessioneId = sessioneId,
            UbicazioneId = ubicazioneId
        });

        
        var bottoneEsci = new SearchCardButton
        {
            Text = "Rilascia e Esci",
            CssClass = "btn-outline-secondary",
            IconClass = "fa-solid fa-door-open",
            Type = "button",
            HtmlAttributes = new Dictionary<string, string>
            {
                { "data-post-action", "true" },
                { "data-url", Url.Action("Abbandona", "RilevamentoUbicazione", new { area = "Inventari", sessioneId, ubicazioneId }) },
                { "data-confirm", "Vuoi rilasciare l'ubicazione? Altri operatori potranno lavorarci." }
            }
        };

        // Bottone Indietro semplice (se è già Sola Lettura)
        var bottoneIndietro = new SearchCardButton
        {
            Text = "Indietro",
            CssClass = "btn-outline-secondary",
            IconClass = "fa-solid fa-arrow-left",
            Type = "button",
            HtmlAttributes = new Dictionary<string, string>
            {
                { "onclick", $"location.href='{Url.Action("Index", "Dettaglio", new { area = "Inventari", id = sessioneId })}'" }            }
        };
        
        
        var bottoneConcludi = new SearchCardButton
        {
            Text = "Concludi",
            CssClass = "btn-primary d-none d-md-inline-block",
            IconClass = "fa-solid fa-check-double",
            Type = "button",
            HtmlAttributes = new Dictionary<string, string>
            {
                { "data-post-action", "true" },
                { "data-url", Url.Action("ConcludiUbicazione", "RilevamentoUbicazione", new { area = "Inventari", sessioneId, ubicazioneId }) },
                { "data-confirm",
                    "Sicuro di voler chiudere questa ubicazione? I pezzi non segnati saranno messi come mancanti."
                }
            }
        };

// 2. Definizione Bottone DISABILITATO (Bloccato)
        var bottoneBloccato = new SearchCardButton
        {
            Text = "Concludi",
            // Uso btn-success con opacity per mantenere coerenza cromatica ma mostrare che è spento
            CssClass = "btn-primary d-none d-md-inline-block",
            IconClass = "fa-solid fa-lock",
            Type = "button",
            HtmlAttributes = new Dictionary<string, string>
            {
                { "disabled", "disabled" },
                { "title", "Ubicazione già completata (Sola lettura)" },
                { "style", "cursor: not-allowed; pointer-events: auto !important;" }
            }
        };

// 3. Gestione Messaggio (opzionale, fuori dalla costruzione dell'oggetto)
        if (isSolaLettura)
        {
            TempData["WarningMessage"] = "Questa ubicazione è già stata completata. Modalità sola lettura.";
        }

       // 4. Costruzione SearchCard con Logica Condizionale
        var searchCard = new SearchCardViewModel
        {
            Title = $"Rilevamento: {infoUbicazione.Nome}",
            Placeholder = "Cerca barcode o pezzo...",
            Query = query,
            ShowFilters = false,
            HeaderButtons = new List<SearchCardButton>
            {
                isSolaLettura ? bottoneIndietro : bottoneEsci,
                isSolaLettura ? bottoneBloccato : bottoneConcludi
            }
        };

        // 6. Creazione ViewModel
        var model = new RilevamentoUbicazioneViewModel
        {
            SessioneId = sessioneId,
            UbicazioneId = ubicazioneId,
            NomeUbicazione = infoUbicazione.Nome,

            // Assegnazione Flag fondamentale
            IsSolaLettura = isSolaLettura,

            SearchCard = searchCard,
            Pezzi = datiPezzi
                .Where(p => string.IsNullOrEmpty(query) || p.Barcode.Contains(query) ||
                            p.Descrizione.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Skip((page - 1) * pageSize)
                .Take(pageSize),
            Page = page,
            PageSize = pageSize,
            TotalItems = datiPezzi.Count
        };

        return View("RilevamentoUbicazione", model);
        
    }

    [HttpPost]
    public virtual async Task<IActionResult> SegnaPresente(Guid rigaId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
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
    public virtual async Task<IActionResult> ConcludiUbicazione(Guid sessioneId, Guid ubicazioneId)
    {
        await _service.CompletaUbicazioneAsync(sessioneId, ubicazioneId);
        return RedirectToAction("Index", "Dettaglio", new { area = "Inventari", id = sessioneId });
        
    }
    
    [HttpPost]
    public virtual async Task<IActionResult> Abbandona(Guid sessioneId, Guid ubicazioneId)
    {
        await _service.RilasciaUbicazioneAsync(sessioneId, ubicazioneId);
        return RedirectToAction("Index", "Dettaglio", new { area = "Inventari", id = sessioneId });    }
}