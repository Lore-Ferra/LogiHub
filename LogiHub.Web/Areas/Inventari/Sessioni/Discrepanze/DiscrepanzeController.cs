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


        var btnRisolviAttivo = new SearchCardButton
        {
            Text = "Risolvi Tutto",
            CssClass = "btn-primary",
            IconClass = "fa-solid fa-wand-magic-sparkles",
            Type = "button",
            HtmlAttributes = new Dictionary<string, string>
            {
                { "data-confirm-trigger", "true" },
                { "data-url", Url.Action("RisolviTutto", "Discrepanze", new { area = "Inventari", id = id }) },
                { "data-method", "POST" },
                { "data-type", "form" },
                { "data-message", "Sei sicuro di voler allineare tutto il magazzino?" }
            }
        };

        // 3. Bottone RISOLVI DISATTIVATO (Lucchetto)
        var btnRisolviDisattivato = new SearchCardButton
        {
            Text = isSolaLettura ? "Risoluzione Bloccata" : "Tutto Risolto",
            CssClass = "btn-secondary disabled ",
            IconClass = isSolaLettura ? "fa-solid fa-lock" : "fa-solid fa-check-double",
            Type = "button",
            HtmlAttributes = new Dictionary<string, string>
            {
                { "title", isSolaLettura ? "Completa prima tutte le ubicazioni" : "Nessuna discrepanza aperta" }
            }
        };

        // Filtro di ricerca (eseguito dopo il controllo haAperte per coerenza globale)
        if (!string.IsNullOrWhiteSpace(query))
        {
            tutte = tutte.Where(d =>
                (d.Barcode?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (d.Descrizione?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (d.UbicazioneSnapshot?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (d.UbicazioneRilevata?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
        }

        var headerButtons = new List<SearchCardButton>();
        if (isSolaLettura)
        {
            headerButtons.Add(btnRisolviDisattivato);
        }
        else if (haAperte)
        {
            headerButtons.Add(btnRisolviAttivo);
        }

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
            dataGestione = DateTime.Now.ToString("dd/MM HH:mm")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> RisolviTutto(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        await _service.RisolviTuttoAsync(id, userId);

        return RedirectToAction("Index", new { area = "Inventari", id });
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
            dataGestione = DateTime.Now.ToString("dd/MM HH:mm")
        });
    }
}