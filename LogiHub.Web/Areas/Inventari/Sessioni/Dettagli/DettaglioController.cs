using LogiHub.Services.Inventari.Sessioni;
using LogiHub.Web.Features.SearchCard;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LogiHub.Web.Areas.Inventari.Sessioni;

namespace LogiHub.Web.Areas.Inventari;

[Area("Inventari")]
public partial class DettaglioController : AuthenticatedBaseController
{
    private readonly ISessioniService _service;

    public DettaglioController(ISessioniService service)
    {
        _service = service;
    }

    [HttpGet]
    public virtual async Task<IActionResult> Index(
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
        var discrepanze = await _service.OttieniDiscrepanzeAsync(id);

        bool ciSonoDiscrepanzeAperte = discrepanze.Any(d => d.Stato == StatoDiscrepanza.Aperta);
        bool ciSonoUbicazioniAperte = data.Ubicazioni.Any(u => !u.Completata);

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
            Title = "📋 "+ data.NomeSessione,
            Placeholder = "Cerca Ubicazione...",
            Query = query,
            Filters = Filters,
            ShowFilters = false,
            ShowUscitoFilter = false,
            ShowSearchInColumns = false,
            HeaderButtons = new List<SearchCardButton>
            {
                new SearchCardButton
                {
                    Text = "Discrepanze",
                    CssClass = "btn-warning",
                    IconClass = "fa-solid fa-triangle-exclamation",
                    Type = "button",
                    HtmlAttributes = new Dictionary<string, string>
                    {
                        { "onclick", $"location.href='{Url.Action("Index", "Discrepanze", new { area = "Inventari", id = id })}'" }
                    }
                }
            }
        };

        // Se la sessione non è chiusa, aggiungi il pulsante di chiusura
        if (!data.Chiuso)
        {
            if (ciSonoDiscrepanzeAperte || ciSonoUbicazioniAperte)
            {
                // BOTTONE DISABILITATO
                searchCardModel.HeaderButtons.Add(new SearchCardButton
                {
                    Text = "Termina",
                    CssClass = "btn-secondary disabled d-none d-md-inline-flex align-items-center",
                    IconClass = "fa-solid fa-lock",
                    Type = "button",
                    HtmlAttributes = new Dictionary<string, string>
                    {
                        { "disabled", "disabled" },
                        { "title", "Completa tutte le ubicazioni e risolvi le discrepanze prima di terminare." },
                        { "style", "cursor: not-allowed;" }
                    }
                });
            }
            else
            {
                // BOTTONE ABILITATO
                searchCardModel.HeaderButtons.Add(new SearchCardButton
                {
                    Text = "Termina",
                    CssClass = "btn-primary d-none d-md-inline-flex align-items-center",
                    IconClass = "fa-solid fa-check",
                    Type = "button",
                    HtmlAttributes = new Dictionary<string, string>
                    {
                        { "onclick", "chiudiInventario(event)" },
                        { "data-url", Url.Action("ChiudiSessione", "Dettaglio", new { area = "Inventari", id }) },
                        { "data-message", $"Vuoi davvero chiudere l’inventario <b>{data.NomeSessione}</b>?" }
                    }
                });
            }
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
            TotalItems = totalItems,
            CiSonoDiscrepanzeAperte = ciSonoDiscrepanzeAperte,
            CiSonoUbicazioniAperte = ciSonoUbicazioniAperte
        };
        
        SetBreadcrumb(
            ("Inventari", Url.Action("Index", "Sessioni")),
            (data.NomeSessione, "")
        );
        return View("Dettaglio", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> ChiudiSessione(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _service.ChiudiSessioneAsync(id, userId);
            return Json(new { success = true, redirectUrl = Url.Action("Index", "Sessioni") });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}