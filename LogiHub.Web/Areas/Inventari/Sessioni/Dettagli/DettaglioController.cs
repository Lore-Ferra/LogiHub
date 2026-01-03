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
                        { "href", Url.Action("Index", "Discrepanze", new { area = "Inventari", id = id }) }
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
                    { "data-url", Url.Action("ChiudiSessione", "Dettaglio", new { area = "Inventari", id = id }) }
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

        return View("Dettaglio", model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> ChiudiSessione(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _service.ChiudiSessioneAsync(id, userId);
            return RedirectToAction("Index", "Sessioni");
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index), new { id });
        }
    }
}