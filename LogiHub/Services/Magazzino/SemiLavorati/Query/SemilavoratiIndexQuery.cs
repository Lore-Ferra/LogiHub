namespace LogiHub.Services.Shared.SemiLavorati
{
    public class SemilavoratiIndexQuery
    {
        public string Filter { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}