using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiHub.Models.Shared;
public enum TipoOperazione
{
    Creazione,
    CambioUbicazione,
    Uscita,
    Entrata,
    Eliminazione,
}
public class Azione
{
    [Key]
    public Guid Id { get; set; }

    public Guid SemiLavoratoId { get; set; }

    [ForeignKey(nameof(SemiLavoratoId))]
    public SemiLavorato SemiLavorato { get; set; }

    public TipoOperazione TipoOperazione { get; set; }
    public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; }

    public DateTime DataOperazione { get; set; } = DateTime.Now;

    public string Dettagli { get; set; }
}