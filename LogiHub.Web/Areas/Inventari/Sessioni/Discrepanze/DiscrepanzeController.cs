using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogiHub.Services.Inventari.Sessioni;
using LogiHub.Web.Features.SearchCard;
using Microsoft.AspNetCore.Mvc;

namespace LogiHub.Web.Areas.Inventari.Sessioni.Discrepanze;

[Area("Inventari")]
public partial class DiscrepanzeController : AuthenticatedBaseController
{
    private readonly ISessioniService _service;

    public DiscrepanzeController(ISessioniService service)
    {
        _service = service;
    }

    [HttpGet]
    public virtual async Task<IActionResult> Index(
        Guid id,
        [FromQuery] string query,
        int page = 1,
        int pageSize = 25)
    {
        var discrepanze = await _service.OttieniDiscrepanzeAsync(id);

        if (!string.IsNullOrWhiteSpace(query))
        {
            discrepanze = discrepanze
                .Where(d => d.Barcode.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            d.Descrizione.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var searchCard = new SearchCardViewModel
        {
            Title = "⚠️ Discrepanze Rilevate",
            Placeholder = "Cerca barcode...",
            Query = query,
            HeaderButtons = new List<SearchCardButton>
            {
                new SearchCardButton
                {
                    Text = "Torna al Dettaglio",
                    CssClass = "btn-outline-secondary",
                    IconClass = "fa-solid fa-arrow-left",
                    Type = "button",
                    HtmlAttributes = new Dictionary<string, string>
                    {
                        { "onclick", $"location.href='{Url.Action("Index", "Dettaglio", new { area = "Inventari", id = id })}'" }
                    }
                }
            }
        };

        var model = new DiscrepanzeViewModel
        {
            SessioneId = id,
            SearchCard = searchCard,
            Discrepanze = discrepanze, // Paginazione potrebbe essere fatta qui se la lista è lunga
            Page = page,
            PageSize = pageSize,
            TotalItems = discrepanze.Count
        };

        return View("Discrepanze", model);
    }
}