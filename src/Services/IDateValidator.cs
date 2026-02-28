namespace EdsMediaArchiver.Services;

public interface IDateValidator
{
    bool IsValid(DateTimeOffset? date);
}
