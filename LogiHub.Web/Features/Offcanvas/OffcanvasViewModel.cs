namespace LogiHub.Web.Features.Offcanvas
{
    public class OffcanvasViewModel
    {
        public string Id { get; set; } = "offcanvas";
        public string Title { get; set; } = "";
        public string Width { get; set; } = "500px";

        public string Id_Label => $"{Id}_Label";
    }
}
