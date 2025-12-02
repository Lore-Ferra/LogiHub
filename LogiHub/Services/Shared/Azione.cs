using LogiHub.Services.Shared;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Azione
{
    [Key]
    public Guid Id { get; set; }

    public Guid? SemiLavoratoId { get; set; }

    [ForeignKey(nameof(SemiLavoratoId))]
    public SemiLavorato SemiLavorato { get; set; }

    public string TipoOperazione { get; set; } = string.Empty;

    [ForeignKey(nameof(User.Id))]
    public User User { get; set; }

    public DateTime DataOperazione { get; set; } = DateTime.Now;

    public string Dettagli { get; set; }
}
