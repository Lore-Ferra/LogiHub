using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LogiHub.Web.Areas.Magazzino.SemiLavorati;

public class ModificaSemiLavoratoViewModel
{
    public Guid Id { get; set; }
    public string Barcode { get; set; }
    public string Descrizione { get; set; }

    [Required(ErrorMessage = "Ubicazione obbligatoria")]
    public Guid? UbicazioneId { get; set; }
    public IEnumerable<SelectListItem> UbicazioniList { get; set; } = new List<SelectListItem>();

    [Required(ErrorMessage = "Azienda obbligatoria")]
    public Guid? AziendaEsternaId { get; set; }
    public IEnumerable<SelectListItem> AziendeList { get; set; } = new List<SelectListItem>();

    public bool Uscito { get; set; }
    
    public bool Rientrato { get; set; }
}