using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LogiHub.Web.Areas.Magazzino.SemiLavorati;

public class ModificaSemiLavoratoViewModel
{
    public Guid Id { get; set; }
    public string Barcode { get; set; }
    public string Descrizione { get; set; }

    public Guid? UbicazioneId { get; set; }
    public IEnumerable<SelectListItem> UbicazioniList { get; set; } = new List<SelectListItem>();

    public Guid? AziendaEsternaId { get; set; }
    public IEnumerable<SelectListItem> AziendeList { get; set; } = new List<SelectListItem>();

    public bool Uscito { get; set; }
}