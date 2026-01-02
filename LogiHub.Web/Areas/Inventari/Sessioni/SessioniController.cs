using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LogiHub.Services;
using LogiHub.Services.Inventari.Sessioni;
using LogiHub.Services.Inventari.Sessioni.DTO;
using LogiHub.Web.Areas.Inventari.Sessioni;
using LogiHub.Web.Features.SearchCard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    public virtual async Task<IActionResult> Dashboard(Guid id)
    {
        // Placeholder per il prossimo step: La Dashboard operativa
        // Qui caricheremo le zone (Ubicazioni) e lo stato di avanzamento
        return View(); 
    }
}