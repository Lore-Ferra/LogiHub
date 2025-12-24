namespace LogiHub.Web.Features.Modals;
public class ConfirmDeleteModalViewModel
{
    public string ModalId { get; set; } = "confirmDeleteModal";
    public string Title { get; set; } = "Conferma eliminazione";
    public string Message { get; set; } = "Sei sicuro di voler eliminare";
    public string Endpoint { get; set; } = "";
}