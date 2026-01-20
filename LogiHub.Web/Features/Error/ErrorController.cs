using System;
using Microsoft.AspNetCore.Mvc;

namespace LogiHub.Web.Features.Error
{
    public partial class ErrorController : Controller
    {
        public virtual IActionResult NotFound() => View();
        public virtual IActionResult Error() => View();
        // Naviga su /Error/Trigger500 per testare la pagina
        public virtual IActionResult Trigger500() 
        {
            throw new Exception("Errore di test");
        }
    }
}