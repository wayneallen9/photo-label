namespace PhotoLabel.Services
{
    public interface ILocaleService
    {
        bool PercentageTryParse(string text, out decimal percentage);
    }
}