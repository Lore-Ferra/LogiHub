namespace LogiHub.Services.Inventari.Sessioni.DTO;

public class UbicazioneStatusDTO
{
    public int Totali { get; set; }
    public int Rilevati { get; set; }
    public int InAttesa => Totali - Rilevati;
    public bool GiaCompletata { get; set; }
    public string PercentualeCompletamento => Totali > 0 
        ? $"{(double)Rilevati / Totali * 100:0}%" 
        : "0%";
}