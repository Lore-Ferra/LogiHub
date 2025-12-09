using System;
using System.ComponentModel.DataAnnotations;

public class Ubicazione
{
    [Key]
    public Guid? UbicazioneId { get; set; }
    public string Posizione { get; set; } = string.Empty;
}