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
        await _service.BloccaUbicazioneAsync(sessioneId, ubicazioneId, userId);

        // 2. Recupero dati pezzi
        var datiPezzi = await _service.OttieniPezziUbicazioneAsync(new PezziUbicazioneQuery
        {
            SessioneId = sessioneId,
            UbicazioneId = ubicazioneId
        });

        // OPTIONAL: Recupera il nome dell'ubicazione dal DB
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
            ShowFilters = false,
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
        // Torna al dettaglio della sessione (elenco ubicazioni)
        return RedirectToAction("Index", "Dettaglio", new { id = sessioneId });
    }
}