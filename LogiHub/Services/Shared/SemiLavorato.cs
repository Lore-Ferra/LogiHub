using LogiHub.Services.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class SemiLavorato
{
    [Key]
    public int Id { get; set; }

    public string Descrizione { get; set; } = string.Empty;

    public Ubicazione Ubicazione { get; set; }

    public string Reparto { get; set; } = string.Empty;

    public AziendaEsterna? AziendaEsterna { get; set; }

    public User User { get; set; }

    public DateTime DataCreazione { get; set; }

    public DateTime UltimaModifica { get; set; }

    [InverseProperty(nameof(Azione.SemiLavorato))]
    public ICollection<Azione> Azioni { get; set; }
}
