namespace LogiHub.Services.Shared
{
    public partial class SharedService
    {
        private readonly TemplateDbContext _dbContext;

        public SharedService(TemplateDbContext dbContext)
        {
            _dbContext = dbContext;
        }
    }
}
