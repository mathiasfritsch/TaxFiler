namespace TaxFiler.Service;

public static class Common
{
    public static DateTime GetYearMonth(string yearMonth)
    {
        var parts = yearMonth.Split('-');
        var year = int.Parse(parts[0]);
        var month = int.Parse(parts[1]);
        
        return new DateTime(year, month, 1);
    }
}