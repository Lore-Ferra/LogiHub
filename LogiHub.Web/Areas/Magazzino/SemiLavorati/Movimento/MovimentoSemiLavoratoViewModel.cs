using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LogiHub.Web.Areas.Magazzino.SemiLavorati;

public class MovimentoSemiLavoratoViewModel
{
    public Guid Id { get; set; }
    public string Barcode { get; set; }
    public string Descrizione { get; set; }
    
    public bool Uscito { get; set; }
    
    public Guid? UbicazioneId { get; set; } // Per il rientro
    public Guid? AziendaId { get; set; }    // Per l'uscita
    
    public List<SelectListItem> UbicazioniList { get; set; } = new();
    public List<SelectListItem> AziendeList { get; set; } = new();
}