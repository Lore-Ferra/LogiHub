using LogiHub.Services.Shared;
using LogiHub.Web.Areas.Magazzino.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using LogiHub.Web1.Areas;

namespace LogiHub.Web.Areas.Magazzino
{
    [Area("Magazzino")]
    public partial class SemiLavoratiController : AuthenticatedBaseController
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

        [HttpGet]
        public virtual async Task<IActionResult> Details(string id)
        {
            var query = new SemiLavoratiDetailsQuery { Id = id };
            var dto = await _queries.Query(query);

            if (dto == null) return NotFound();

            return PartialView("Details", dto);
        }
    }
}
