namespace PhotoLabel.Services
{
    public interface IPercentageService
    {
        float ConvertToFloat(string percentage);
        string ConvertToString(float percentage);
    }
}