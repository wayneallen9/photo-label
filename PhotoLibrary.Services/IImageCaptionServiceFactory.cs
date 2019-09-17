namespace PhotoLabel.Services
{
    public interface IImageCaptionServiceFactory
    {
        IImageCaptionService Create(bool useCanvas, int? canvasWidth, int? canvasHeight);
    }
}