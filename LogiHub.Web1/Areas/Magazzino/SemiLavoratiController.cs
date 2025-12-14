using LogiHub.Services.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LogiHub.Web.Areas.Magazzino
{
    [Area("Magazzino")]
    public partial class SemiLavoratiController : Controller
    {
        private readonly SharedService _queries;
        private readonly ISemiLavoratoService _service;
        public SemiLavoratiController(SharedService queries, ISemiLavoratoService service)
        {
            _queries = queries;
            _service = service;
        }

        [HttpGet]
        public virtual async Task<IActionResult> Index(SemilavoratiIndexQuery query)
        {
            var dto = await _queries.Query(query);
            var model = new IndexViewModel();
            model.SetData(dto);
            return View(model);
        }
    }
}
