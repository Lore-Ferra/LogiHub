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
using LogiHub.Services.Shared;

namespace LogiHub.Web.Areas.Inventari;

[Area("Inventari")]
public partial class SessioniController : AuthenticatedBaseController
{
    private readonly ISessioniService _service;
    private readonly TemplateDbContext _context;
    private readonly IBloccoMagazzinoService _bloccoService;

    public SessioniController(
        ISessioniService service,
        IBloccoMagazzinoService bloccoService,
        TemplateDbContext context)
    {
        _service = service;
        _context = context;
        _bloccoService = bloccoService;
    }

    [HttpGet]
    public virtual async Task<IActionResult> Index(
        [FromQuery] string Query,
        [FromQuery] SearchCardFiltersViewModel Filters,
        int page = 1,
        int pageSize = 25
    )
    {
        bool isBloccato = await _bloccoService.IsBloccatoAsync();
        
        if (Filters == null)
            Filters = new SearchCardFiltersViewModel();
        if ((Filters.SearchInColumns == null || !Filters.SearchInColumns.Any())
            && page == 1
            && string.IsNullOrWhiteSpace(Query))
        {
            Filters.SearchInColumns = new List<string> { "NomeSessione" };
        }
        
        bool inventarioAttivo = await _context.SessioniInventario.AnyAsync(s => !s.Chiuso);

        var bottoneAttivo = new SearchCardButton
        {
            Text = "Avvia Inventario",
            CssClass = "btn-primary d-none d-md-inline-block",
            IconClass = "fa-solid fa-clipboard-check",
            Type = "button",
            HtmlAttributes = new Dictionary<string, string>
            {
                { "onclick", "creaNuovaSessione(event)" },
                { "data-url", Url.Action("AggiungiSessioneInventario", "Sessioni", new { area = "Inventari" }) },
                { "data-message", "Vuoi avviare una nuova sessione? Il magazzino verrà bloccato." }
            }
        };
        
        var bottoneDisabilitato = new SearchCardButton
        {
            Text = "Avvia Inventario",
            CssClass = "btn-secondary disabled d-none d-md-inline-block",
            IconClass = "fa-solid fa-lock",
            Type = "button",
            HtmlAttributes = new Dictionary<string, string>
            {
                { "disabled", "disabled" },
                { "style", "cursor: not-allowed; pointer-events: auto !important;" }
            }
        };
        
        var searchCardModel = new SearchCardViewModel
        {
            Title = "📋 Storico Inventari",
            Placeholder = "Cerca sessione...",
            Query = Query,
            Filters = Filters,

            ShowUscitoFilter = false,
            ShowSearchInColumns = true,
            DefaultSearchInColumnKey = "NomeSessione",

            SearchInColumns = new()
            {
                new() { Key = "NomeSessione", Label = "Nome sessione"},
                new() { Key = "DataCreazione", Label = "Data creazione"},
                new() { Key = "DataCompletamento", Label = "Data completamento"}
            },

            HeaderButtons = new List<SearchCardButton>
            {
                isBloccato ? bottoneDisabilitato : bottoneAttivo
            }
        };
        
        var queryBase = _context.SessioniInventario.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(Query))
        {
            var q = Query.Trim();
            var cols = Filters.SearchInColumns ?? new List<string>();

            if (cols.Count == 0)
                cols = new List<string> { "NomeSessione", "DataCreazione", "DataCompletamento" };

            bool hasFullDate = DateTime.TryParseExact(
                q,
                "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var fullDate);

            bool hasDayMonth = DateTime.TryParseExact(
                q,
                "dd/MM",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var dayMonth);

            int day = 0;

            bool hasDayOnly =
                q.Length == 2 &&
                int.TryParse(q, out day) &&
                day >= 1 && day <= 31;
            
            bool isNumeric = int.TryParse(q, out int numericValue);

            bool hasYear =
                isNumeric &&
                (
                    numericValue >= 1900 && numericValue <= 2100
                    || (numericValue >= 0 && numericValue <= 99)
                );

            int resolvedYear =
                numericValue < 100 ? 2000 + numericValue : numericValue;

            queryBase = queryBase.Where(x =>
                (cols.Contains("NomeSessione") &&
                 x.NomeSessione != null &&
                 x.NomeSessione.ToLower().Contains(q.ToLower()))

                ||

                (cols.Contains("DataCreazione") &&
                 (
                     (hasFullDate &&
                      x.DataCreazione >= fullDate.Date &&
                      x.DataCreazione < fullDate.Date.AddDays(1))

                     ||

                     (hasDayMonth &&
                      x.DataCreazione.Day == dayMonth.Day &&
                      x.DataCreazione.Month == dayMonth.Month)

                     ||

                     (hasDayOnly && x.DataCreazione.Day == numericValue)

                     ||

                     (hasYear && x.DataCreazione.Year == resolvedYear)
                 ))
                ||

                (cols.Contains("DataCompletamento") &&
                 x.DataCompletamento.HasValue &&
                 (
                     (hasFullDate &&
                      x.DataCompletamento.Value >= fullDate.Date &&
                      x.DataCompletamento.Value < fullDate.Date.AddDays(1))

                     ||

                     (hasDayMonth &&
                      x.DataCompletamento.Value.Day == dayMonth.Day &&
                      x.DataCompletamento.Value.Month == dayMonth.Month)

                     ||

                     (hasDayOnly && x.DataCompletamento.Value.Day == numericValue)

                     ||

                     (hasYear && x.DataCompletamento.Value.Year == resolvedYear)
                 ))
            );
        }

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
                DataCompletamento = x.DataCompletamento,
                Chiuso = x.Chiuso,
            })
            .ToListAsync();

        var model = new SessioniIndexViewModel
        {
            SearchCard = searchCardModel,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            Sessioni = items
        };
        
        SetBreadcrumb(
            ("Inventari", "") 
        );

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> AggiungiSessioneInventario()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var dto = new AggiungiSessioneInventarioDTO { UserId = userId };
            await _service.AggiungiSessioneAsync(dto);
            
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}