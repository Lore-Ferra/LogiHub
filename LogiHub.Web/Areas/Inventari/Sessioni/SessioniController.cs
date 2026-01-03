using LogiHub.Services;
using LogiHub.Services.Inventari.Sessioni;
using LogiHub.Services.Inventari.Sessioni.DTO;
using LogiHub.Web.Areas.Inventari.Sessioni;
using LogiHub.Web.Features.SearchCard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LogiHub.Services.Inventari.Sessioni.Query;

namespace LogiHub.Web.Areas.Inventari;

[Area("Inventari")]
public partial class SessioniController : AuthenticatedBaseController
{
    private readonly ISessioniService _service;
    private readonly TemplateDbContext _context;

    public SessioniController(ISessioniService service, TemplateDbContext context)
    {
        _service = service;
        _context = context;
    }

    [HttpGet]
    public virtual async Task<IActionResult> Index(
        [FromQuery] string Query,
        [FromQuery] SearchCardFiltersViewModel Filters,
        int page = 1,
        int pageSize = 25
    )
    {
        // 1. Filtri
        if (Filters == null)
        {
            Filters = new SearchCardFiltersViewModel();
        }

        // 2. Controllo se esiste già un inventario aperto (per la logica del bottone)
        bool inventarioAttivo = await _context.SessioniInventario.AnyAsync(s => !s.Chiuso);

        // 3. Costruzione della SearchCard
        var searchCardModel = new SearchCardViewModel
        {
            Title = "📋 Storico Inventari",
            Placeholder = "Cerca sessione...",
            Query = Query,
            Filters = Filters,
            HeaderButtons = new List<SearchCardButton>()
        };


        searchCardModel.HeaderButtons.Add(new SearchCardButton
        {
            Text = "Crea Inventario",
            CssClass = "btn-success",
            IconClass = "fa-solid fa-play",
            Type = "button",
            HtmlAttributes = new Dictionary<string, string>
            {
                { "data-post-action", "true" },
                { "data-url", Url.Action("AggiungiSessioneInventario", "Sessioni", new { area = "Inventari" }) },
                { "data-confirm", "Vuoi avviare una nuova sessione? Il magazzino verrà bloccato." }
            }
        });


        // 4. Query per recuperare i dati 
        var queryBase = _context.SessioniInventario.AsNoTracking();

        if (!string.IsNullOrEmpty(Query))
        {
            queryBase = queryBase.Where(x => x.NomeSessione.Contains(Query));
        }

        // Paginazione
        var totalItems = await queryBase.CountAsync();
        var items = await queryBase
            .OrderByDescending(x => x.DataCreazione)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SessioniIndexDTO.RigaSessione
            {
                Id = x.Id,
                NomeSessione = x.NomeSessione,
                DataCreazione = x.DataCreazione,
                Chiuso = x.Chiuso,
            })
            .ToListAsync();

        // 5. Costruzione ViewModel
        var model = new SessioniIndexViewModel
        {
            SearchCard = searchCardModel,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            Sessioni = items
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> AggiungiSessioneInventario()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var dto = new AggiungiSessioneInventarioDTO { UserId = userId };
        var sessione = await _service.AggiungiSessioneAsync(dto);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public virtual async Task<IActionResult> Dettaglio(
        Guid id,
        [FromQuery] string query,
        [FromQuery] SearchCardFiltersViewModel Filters,
        int page = 1,
        int pageSize = 25)
    {
        if (Filters == null)
        {
            Filters = new SearchCardFiltersViewModel();
        }

        var data = await _service.OttieniDettaglioSessioneAsync(id);

        var ubicazioniFiltrate = data.Ubicazioni;

        if (!string.IsNullOrWhiteSpace(query))
        {
            ubicazioniFiltrate = ubicazioniFiltrate
                .Where(u => u.Posizione.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Calcola la paginazione
        var totalItems = ubicazioniFiltrate.Count();
        var ubicazioniPaginate = ubicazioniFiltrate
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Costruzione della SearchCard
        var searchCardModel = new SearchCardViewModel
        {
            Title = "📋 Inventario",
            Placeholder = "Cerca Ubicazione...",
            Query = query,
            Filters = Filters,
            HeaderButtons = new List<SearchCardButton>
            {
                new SearchCardButton
                {
                    Text = "Torna allo Storico",
                    CssClass = "btn-outline-secondary",
                    IconClass = "fa-solid fa-arrow-left",
                    Type = "button",
                    HtmlAttributes = new Dictionary<string, string>
                    {
                        { "onclick", $"location.href='{Url.Action("Index", "Sessioni", new { area = "Inventari" })}'" }
                    }
                },
                new SearchCardButton
                {
                    Text = "Visualizza Discrepanze",
                    CssClass = "btn-primary",
                    IconClass = "fa-solid fa-triangle-exclamation",
                    Type = "link",
                    HtmlAttributes = new Dictionary<string, string>
                    {
                        { "href", Url.Action("Discrepanze", "Sessioni", new { area = "Inventari", id = id }) }
                    }
                }
            }
        };

        // Se la sessione non è chiusa, aggiungi il pulsante di chiusura
        if (!data.Chiuso)
        {
            searchCardModel.HeaderButtons.Add(new SearchCardButton
            {
                Text = "Chiudi Inventario",
                CssClass = "btn-danger",
                IconClass = "fa-solid fa-lock",
                Type = "button",
                HtmlAttributes = new Dictionary<string, string>
                {
                    { "data-post-action", "true" },
                    { "data-url", Url.Action("ChiudiSessione", "Sessioni", new { area = "Inventari", id = id }) }
                }
            });
        }

        var model = new DettaglioSessioneViewModel
        {
            SessioneId = data.SessioneId,
            NomeSessione = data.NomeSessione,
            Chiuso = data.Chiuso,
            SearchCard = searchCardModel,
            SearchQuery = query,
            UbicazioniFiltrate = ubicazioniPaginate,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return View(model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> ChiudiSessione(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _service.ChiudiSessioneAsync(id, userId);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Dettaglio), new { id });
        }
    }

    [HttpGet]
    public virtual async Task<IActionResult> RilevamentoUbicazione(
        Guid sessioneId,
        Guid ubicazioneId,
        [FromQuery] string query,
        int page = 1,
        int pageSize = 25)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // 1. Lock dell'ubicazione
        await _service.BloccaUbicazioneAsync(sessioneId, ubicazioneId, userId);

        // 2. Recupero dati pezzi
        var datiPezzi = await _service.OttieniPezziUbicazioneAsync(new PezziUbicazioneQuery
        {
            SessioneId = sessioneId,
            UbicazioneId = ubicazioneId
        });

        // OPTIONAL: Recupera il nome dell'ubicazione dal DB o dal DTO se lo hai aggiunto
        var nomeUbi = await _context.Ubicazioni
            .Where(u => u.UbicazioneId == ubicazioneId)
            .Select(u => u.Posizione)
            .FirstOrDefaultAsync() ?? "Ubicazione Ignota";

        // 3. Costruzione SearchCard
        var searchCard = new SearchCardViewModel
        {
            Title = $"Rilevamento: {nomeUbi}",
            Placeholder = "Cerca barcode o pezzo...",
            Query = query,
            HeaderButtons = new List<SearchCardButton>
            {
                new SearchCardButton
                {
                    Text = "Concludi",
                    CssClass = "btn-success",
                    IconClass = "fa-solid fa-check-double",
                    Type = "button",
                    HtmlAttributes = new Dictionary<string, string>
                    {
                        { "data-post-action", "true" },
                        { "data-url", Url.Action("ConcludiUbicazione", new { sessioneId, ubicazioneId }) },
                        {
                            "data-confirm",
                            "Sicuro di voler chiudere questa ubicazione? I pezzi non segnati saranno messi come mancanti."
                        }
                    }
                }
            }
        };

        var model = new RilevamentoUbicazioneViewModel
        {
            SessioneId = sessioneId,
            UbicazioneId = ubicazioneId,
            NomeUbicazione = nomeUbi,
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

        return View(model);
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
        // Torna al dettaglio della sessione (elenco ubicazioni)
        return RedirectToAction(nameof(Dettaglio), new { id = sessioneId });
    }
}