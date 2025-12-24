using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LogiHub.Models.Shared;

public class SemiLavorato
{
    [Key] 
    public Guid Id { get; set; }
    
    [Required]
    public string Barcode { get; set; }
    [Required]
    public string Descrizione { get; set; } = string.Empty;

    public Guid? UbicazioneId { get; set; }
    [ForeignKey(nameof(UbicazioneId))]
    public Ubicazione Ubicazione { get; set; }

    public Guid? AziendaEsternaId { get; set; }
    [ForeignKey(nameof(AziendaEsternaId))]
    public AziendaEsterna? AziendaEsterna { get; set; }

    public bool Eliminato { get; set; } = false;
    
    public bool Uscito {get; set;} = false;

    public DateTime DataCreazione { get; set; }
    public DateTime UltimaModifica { get; set; }

    [InverseProperty(nameof(Azione.SemiLavorato))]
    public ICollection<Azione> Azioni { get; set; } = new List<Azione>();
}
