using Microsoft.AspNetCore.Mvc;

namespace LogiHub.Web1.Areas.Inventario.Lista;

[Area("Inventario")]
public partial class ListaController : AuthenticatedBaseController
{
    public virtual IActionResult Index()
    {

        return View("/Areas/Inventario/Lista/Index.cshtml");
    }
}