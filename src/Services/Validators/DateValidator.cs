namespace EdsMediaArchiver.Services.Validators;

public interface IDateValidator
{
    bool IsValid(DateTimeOffset? date);
}

public class DateValidator() : IDateValidator
{
    public bool IsValid(DateTimeOffset? date)
    {
        if (date.HasValue == false)
            return false;

        return date.Value.Year > 1700 && date <= DateTimeOffset.Now.AddDays(1);
    }
}
