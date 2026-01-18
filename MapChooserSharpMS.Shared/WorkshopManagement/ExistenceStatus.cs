namespace MapChooserSharpMS.Shared.WorkshopManagement;

public enum ExistenceStatus
{
    FoundInMemoryConfig,
    FoundInWorkshop,
    FailedToFetchHttpError,
    FailedToFetchUnknown,
}