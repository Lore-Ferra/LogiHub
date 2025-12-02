public class SemiLavorato
{
    public int Id { get; set; }

    public string Descrizione { get; set; } = string.Empty;

    public Ubicazione Ubicazione { get; set; }

    public string Reparto { get; set; } = string.Empty;

    public AziendaEsterna? AziendaEsterna { get; set; }

    public DateTime DataCreazione { get; set; }

    public DateTime UltimaModifica { get; set; }
}
