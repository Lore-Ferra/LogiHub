using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LogiHub.Web.Areas.Magazzino.SemiLavorati
{
    public class CreaSemiLavoratoViewModel
    {
        public string Id { get; set; }
        public Guid? UbicazioneId { get; set; }
        public IEnumerable<SelectListItem> UbicazioniList { get; set; } = new List<SelectListItem>();
        public string Descrizione { get; set; }
    }
}