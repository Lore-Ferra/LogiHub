namespace LogiHub.Web.Features.Modals;

public class ConfirmModalViewModel
{
    public string ModalId { get; set; } = "confirmModal";
    public string Title { get; set; } = "Conferma operazione";
    public string Message { get; set; } = "Sei sicuro di voler procedere?";
    public string ConfirmButtonText { get; set; } = "Conferma";
    public string ConfirmButtonClass { get; set; } = "btn-primary";
    public string IconClass { get; set; } = "bi bi-exclamation-triangle-fill text-warning";
}