using System.Collections.Generic;
using System.Threading.Tasks;
using LogiHub.Web.Areas.Inventario.Models;
using LogiHub.Web.Features.SearchCard;
using Microsoft.AspNetCore.Mvc;

namespace LogiHub.Web.Areas.Inventario;

[Area("Inventario")]
public partial class SessioniController : AuthenticatedBaseController
{
    [HttpGet]
    public virtual async Task<IActionResult> Index(
        [FromQuery] string Query,
        [FromQuery] SearchCardFiltersViewModel Filters,
        int page = 1,
        int pageSize = 25)
    {
        if (Filters == null)
        {
            Filters = new SearchCardFiltersViewModel
            {
                SearchInColumns = new List<string> { "NomeSessione", "CreatoDa" }
            };
        }

        var searchCardModel = new SearchCardViewModel
        {
            Title = "📋 Gestione Inventario",
            Placeholder = "Cerca sessione...",
            Query = Query,
            Filters = Filters
        };

        // 3. Costruisci il ViewModel finale
        var model = new SessioniIndexViewModel
        {
            SearchCard = searchCardModel,
            Filter = Query,
            Page = page,
            PageSize = pageSize,
            TotalItems = 0
        };

        return View(model);
    }
}