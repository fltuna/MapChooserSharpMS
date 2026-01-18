namespace MapChooserSharpMS.Shared.WorkshopManagement;

public interface IWorkshopFetchResult
{
    ExistenceStatus ExistenceStatus { get; }
    
    string? MapName { get; }
    
    long? WorkshopId { get; }
}