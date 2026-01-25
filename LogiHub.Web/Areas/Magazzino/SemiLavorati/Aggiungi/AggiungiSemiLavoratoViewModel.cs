using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LogiHub.Web.Areas.Magazzino.SemiLavorati
{
    public class AggiungiSemiLavoratoViewModel
    {
        public string Barcode { get; set; }
        public string Descrizione { get; set; }
        public Guid? UbicazioneId { get; set; }
        public IEnumerable<SelectListItem> UbicazioniList { get; set; } = new List<SelectListItem>();
        
    }
}