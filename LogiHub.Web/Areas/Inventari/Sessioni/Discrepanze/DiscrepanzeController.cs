using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
        [FromQuery] SearchCardFiltersViewModel Filters,
        int page = 1,
        int pageSize = 25)
    {
        Filters ??= new SearchCardFiltersViewModel();

        // 1. Il service ora restituisce già i DTO con il Tipo (Spostato, Extra, Mancante)
        var tutte = await _service.OttieniDiscrepanzeAsync(id);

        // 2. Filtro di ricerca
        if (!string.IsNullOrWhiteSpace(query))
        {
            tutte = tutte.Where(d =>
                (d.Barcode?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (d.Descrizione?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (d.UbicazioneSnapshot?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (d.UbicazioneRilevata?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
        }

        // 3. Configurazione UI con tasto "Risolvi Tutto"
        var searchCard = new SearchCardViewModel
        {
            Title = "⚠️ Analisi Discrepanze",
            Placeholder = "Cerca barcode o ubicazione...",
            Query = query,
            Filters = Filters,
            ShowFilters = false,
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
                        {
                            "onclick",
                            $"location.href='{Url.Action("Dettaglio", "Sessioni", new { area = "Inventari", id = id })}'"
                        }
                    }
                },
                new SearchCardButton
                {
                    Text = "Risolvi Tutto",
                    CssClass = "btn-success",
                    IconClass = "fa-solid fa-wand-magic-sparkles",
                    Type = "button",
                    HtmlAttributes = new Dictionary<string, string>
                    {
                        { "data-post-action", Url.Action("RisolviTutto", "Discrepanze", new { area = "Inventari", id = id }) },
                        { "data-confirm", "Sei sicuro di voler allineare tutto il magazzino?" }
                    }
                }
            }
        };

        var model = new DiscrepanzeViewModel
        {
            SessioneId = id,
            SearchCard = searchCard,
            SearchQuery = query,
            DaSpostare = tutte.Where(x => x.Tipo == TipoDiscrepanzaOperativa.Spostato).ToList(),
            DaAggiungere = tutte.Where(x => x.Tipo == TipoDiscrepanzaOperativa.Extra).ToList(),
            DaRimuovere = tutte.Where(x => x.Tipo == TipoDiscrepanzaOperativa.Mancante).ToList(),
            TotalItems = tutte.Count,
            Page = page,
            PageSize = pageSize
        };

        return View("Discrepanze", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> Risolvi(Guid id, string barcode, TipoRisoluzione tipo)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var tutte = await _service.OttieniDiscrepanzeAsync(id);
        var d = tutte.FirstOrDefault(x => x.Barcode == barcode);

        if (d == null) return Json(new { success = false, message = "Pezzo non trovato." });

        await _service.RisolviDiscrepanzaAsync(id, d, tipo, userId);
        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> RisolviTutto(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        await _service.RisolviTuttoAsync(id, userId);

        return Json(new { 
            success = true, 
            redirectUrl = Url.Action("Index", "Discrepanze", new { area = "Inventari", id = id }) 
        });
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> AnnullaDiscrepanza(Guid id, string barcode)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    
        await _service.AnnullaDiscrepanzaAsync(id, barcode, userId);
    
        return Json(new { success = true });
    }
}