using LogiHub.Services.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class SemiLavorato
{
    [Key]
    public Guid Id { get; set; }

    public string Descrizione { get; set; } = string.Empty;

    public Ubicazione Ubicazione { get; set; }

    public AziendaEsterna? AziendaEsterna { get; set; }

    public DateTime DataCreazione { get; set; }

    public DateTime UltimaModifica { get; set; }

    [InverseProperty(nameof(Azione.SemiLavorato))]
    public ICollection<Azione> Azioni { get; set; }
}
