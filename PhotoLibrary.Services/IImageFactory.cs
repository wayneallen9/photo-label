namespace PhotoLabel.Services
{
    public interface IImageFactory
    {
        IImageService Create(bool useCanvas, int? canvasWidth, int? canvasHeight);
    }
}