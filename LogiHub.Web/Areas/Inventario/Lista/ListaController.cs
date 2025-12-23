using Microsoft.AspNetCore.Mvc;

namespace LogiHub.Web.Areas.Inventario;

[Area("Inventario")]
public partial class ListaController : AuthenticatedBaseController
{
    public virtual IActionResult Index(string filter, int page = 1, int pageSize = 10)
    {
        var model = new ListaIndexViewModel
        {
            Filter = filter ?? string.Empty,
            Page = page,
            PageSize = pageSize,
            TotalItems = 0
        };

        return View(model);
    }
}