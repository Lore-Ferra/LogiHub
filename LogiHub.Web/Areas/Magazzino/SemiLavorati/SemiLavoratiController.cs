namespace LogiHub.Web.Areas.Magazzino
{
    using LogiHub.Services.Shared;
    using LogiHub.Web.Areas.Magazzino.Models;
    using Web.Areas;
    using Microsoft.AspNetCore.Mvc;
    using System.Linq;
    using System.Threading.Tasks;

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
        public virtual async Task<IActionResult> Index(
            string filter,
            int page = 1,
            int pageSize = 25
        )
        {
            var query = new SemilavoratiIndexQuery
            {
                Filter = filter
            };

            var dto = await _queries.Query(query);

            var items = dto.Items
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var model = new SemiLavoratiIndexViewModel
            {
                Filter = filter,
                Page = page,
                PageSize = pageSize,
                TotalItems = dto.Items.Count(),
                SemiLavorati = items
            };

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
