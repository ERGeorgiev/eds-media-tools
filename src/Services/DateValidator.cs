namespace EdsMediaArchiver.Services;

public class DateValidator() : IDateValidator
{
    public bool IsValid(DateTimeOffset? date)
    {
        if (date.HasValue == false)
            return false;

        return date.Value.Year > 1700 && date <= DateTimeOffset.Now.AddDays(1);
    }
}
