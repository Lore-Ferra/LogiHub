using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LogiHub.Models.Shared;

public class Inventario
{
    [Key]
    public Guid Id { get; set; }

    public Guid UbicazioneId { get; set; }
    [ForeignKey(nameof(UbicazioneId))]
    public Ubicazione Ubicazione { get; set; }

    public string SemiLavoratoId { get; set; }
    [ForeignKey(nameof(SemiLavoratoId))]
    public SemiLavorato SemiLavorato { get; set; }
    public bool Presente { get; set; }

    public DateTime DataInventario { get; set; } = DateTime.Now;

    public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; }

    public string? Note { get; set; }
}
