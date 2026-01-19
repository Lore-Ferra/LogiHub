using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LogiHub.Services.Inventari.Sessioni;
using LogiHub.Web.Features.SearchCard;
using Microsoft.AspNetCore.Mvc;
using LogiHub.Services;

namespace LogiHub.Web.Areas.Inventari.Sessioni.Discrepanze;

[Area("Inventari")]
public partial class DiscrepanzeController : AuthenticatedBaseController
{
    private readonly ISessioniService _service;
    private readonly TemplateDbContext _context;

    public DiscrepanzeController(ISessioniService service, TemplateDbContext context)
    {
        _service = service;
        _context = context;
    }

    [HttpGet]
    public virtual async Task<IActionResult> Index(
        Guid id,
        [FromQuery] string query,
        [FromQuery] SearchCardFiltersViewModel Filters,
        int page = 1,
        int pageSize = 25)
    {
        if (id == Guid.Empty) return NotFound("ID Sessione non valido.");
        Filters ??= new SearchCardFiltersViewModel();

        var tutte = await _service.OttieniDiscrepanzeAsync(id);
        var sessione = await _service.OttieniDettaglioSessioneAsync(id);

        bool isSolaLettura = sessione.Ubicazioni.Any(u => !u.Completata);
        bool haAperte = tutte.Any(x => x.Stato == StatoDiscrepanza.Aperta);

        bool canRisolviTutto = !isSolaLettura && haAperte;

        var headerButtons = new List<SearchCardButton>();

        var btnRisolvi = new SearchCardButton
        {
            Text = isSolaLettura ? "Risoluzione Bloccata" : "Risolvi Tutto",
            CssClass = (isSolaLettura ? "btn-secondary disabled" : "btn-primary") + 
                       (haAperte ? " d-none d-md-inline-block" : " d-none"), 
            IconClass = isSolaLettura ? "fa-solid fa-lock" : "fa-solid fa-wand-magic-sparkles",
            Type = "button",
            HtmlAttributes = new Dictionary<string, string>
            {
                { "id", "btnRisolviTutto" },
                { "data-confirm-trigger", (!isSolaLettura && haAperte).ToString().ToLower() },
                { "data-url", Url.Action("RisolviTutto", "Discrepanze", new { area = "Inventari", id }) },
                { "data-url-original", Url.Action("RisolviTutto", "Discrepanze", new { area = "Inventari", id }) },
                { "data-is-readonly", isSolaLettura.ToString().ToLower() },
                { "data-message", "Sei sicuro di voler allineare tutto il magazzino?" }
            }
        };

        headerButtons.Add(btnRisolvi);

        var searchCard = new SearchCardViewModel
        {
            Title = "⚠️ Analisi Discrepanze",
            Placeholder = "Cerca barcode o ubicazione...",
            Query = query,
            Filters = Filters,
            ShowFilters = false,
            ShowUscitoFilter = false,
            ShowSearchInColumns = false,
            HeaderButtons = headerButtons
        };

        var model = new DiscrepanzeViewModel
        {
            SessioneId = id,
            IsSolaLettura = isSolaLettura,
            SearchCard = searchCard,
            SearchQuery = query,
            DaSpostare = tutte.Where(x => x.Tipo == TipoDiscrepanzaOperativa.Spostato).ToList(),
            DaAggiungere = tutte.Where(x => x.Tipo == TipoDiscrepanzaOperativa.Extra).ToList(),
            DaRimuovere = tutte.Where(x => x.Tipo == TipoDiscrepanzaOperativa.Mancante).ToList(),
            TotalItems = tutte.Count,
            Page = page,
            PageSize = pageSize
        };
        SetBreadcrumb(
            ("Inventari", Url.Action("Index", "Sessioni", new { area = "Inventari" })),
            (sessione.NomeSessione, Url.Action("Index", "Dettaglio", new { area = "Inventari", id = id })),
            ("Discrepanze", "")
        );
        return View("Discrepanze", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> Risolvi(Guid id, string barcode, TipoRisoluzione tipo)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var tutte = await _service.OttieniDiscrepanzeAsync(id);
        var d = tutte.FirstOrDefault(x => x.Barcode == barcode);

        if (d != null)
            await _service.RisolviDiscrepanzaAsync(id, d, tipo, userId);

        var user = await _context.Users.FindAsync(userId);
        var nomeOperatore = user != null ? $"{user.FirstName} {user.LastName}" : User.Identity?.Name ?? "Operatore";

        return Json(new
        {
            success = true,
            stato = StatoDiscrepanza.Risolta.ToString(),
            gestitaDa = nomeOperatore,
            dataGestione = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> RisolviTutto(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var tutte = await _service.OttieniDiscrepanzeAsync(id);
        var sessione = await _service.OttieniDettaglioSessioneAsync(id);

        bool isSolaLettura = sessione.Ubicazioni.Any(u => !u.Completata);
        bool haAperte = tutte.Any(x => x.Stato == StatoDiscrepanza.Aperta);

        if (isSolaLettura || !haAperte)
            return Json(new { success = false, message = "Operazione non disponibile: inventario non completato o nessuna discrepanza aperta." });

        await _service.RisolviTuttoAsync(id, userId);

        return Json(new
        {
            success = true,
            redirectUrl = Url.Action("Index", "Dettaglio", new { area = "Inventari", id })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> AnnullaDiscrepanza(Guid id, string barcode)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        await _service.AnnullaDiscrepanzaAsync(id, barcode, userId);

        var user = await _context.Users.FindAsync(userId);
        var nomeOperatore = user != null ? $"{user.FirstName} {user.LastName}" : User.Identity?.Name ?? "Operatore";

        return Json(new
        {
            success = true,
            stato = StatoDiscrepanza.Annullata.ToString(),
            gestitaDa = nomeOperatore,
            dataGestione = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
        });
    }
}