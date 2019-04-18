using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using PhotoLabel.Services;

namespace PhotoLabel
{
    public static class ImageManager
    {
        private static Image AddCaptionToImage(Image image, Models.ImageModel imageModel, CancellationToken cancellationToken)
        {
            ILogService logService = null;

            try
            {
                // create the dependencies
                var configurationService = NinjectKernel.Get<IConfigurationService>();
                var imageService = NinjectKernel.Get<IImageService>();
                logService = NinjectKernel.Get<ILogService>();

                logService.TraceEnter();

                if (cancellationToken.IsCancellationRequested) return null;
                logService.Trace("Populating values to use for caption...");
                var backgroundColour = imageModel.BackgroundColour ?? configurationService.BackgroundColour;
                var captionAlignment = imageModel.CaptionAlignment ?? configurationService.CaptionAlignment;
                var colour = imageModel.Colour ?? configurationService.Colour;
                var fontBold = imageModel.FontBold ?? configurationService.FontBold;
                var fontName = imageModel.FontName ?? configurationService.FontName;
                var fontSize = imageModel.FontSize ?? configurationService.FontSize;
                var fontType = imageModel.FontType ?? configurationService.FontType;

                // what is the caption?
                if (cancellationToken.IsCancellationRequested) return null;
                var captionBuilder = new StringBuilder(imageModel.Caption);

                // is there a date taken?
                if (cancellationToken.IsCancellationRequested) return null;
                logService.Trace($@"Checking if ""{imageModel.Filename}"" has a date taken set...");
                if (imageModel.DateTaken != null &&
                    (imageModel.AppendDateTakenToCaption ?? configurationService.AppendDateTakenToCaption))
                {
                    if (captionBuilder.Length > 0) captionBuilder.Append(" - ");

                    captionBuilder.Append(imageModel.DateTaken);
                }

                var caption = captionBuilder.ToString();

                // create the caption
                if (cancellationToken.IsCancellationRequested) return null;
                logService.Trace($@"Caption for ""{imageModel.Filename}"" is ""{caption}"".  Creating image...");
                return imageService.Caption(image, caption, captionAlignment, fontName, fontSize,
                    fontType, fontBold, new SolidBrush(colour), backgroundColour, cancellationToken);
            }
            finally
            {
                logService?.TraceExit();
            }
        }

        public static Image Caption(Image originalImage, Models.ImageModel captionDetails, CancellationToken cancellationToken)
        {
            ILogService logService = null;

            try
            {
                // create dependencies
                logService = NinjectKernel.Get<ILogService>();
                var quickCaptionService = NinjectKernel.Get<IQuickCaptionService>();

                logService.TraceEnter();

                if (cancellationToken.IsCancellationRequested) return null;
                logService.Trace("Adding caption to background image...");
                var captionedImage = AddCaptionToImage(originalImage, captionDetails, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    captionedImage?.Dispose();

                    return null;
                }

                if (cancellationToken.IsCancellationRequested) return null;
                logService.Trace("Mapping to service layer...");
                var metadata = Mapper.Map<Services.Models.Metadata>(captionDetails);

                if (cancellationToken.IsCancellationRequested) return null;
                logService.Trace($@"Loading new list of quick captions for ""{captionDetails.Filename}""...");
                quickCaptionService.Switch(captionDetails.Filename, metadata);

                return captionedImage;
            }
            finally
            {
                logService?.TraceExit();
            }
        }

        public static void Save(string originalFilename, string captionedFilename, Models.ImageModel captionDetails)
        {
            ILogService logService = null;

            try
            {
                // create the dependencies for this thread
                var imageMetadataService = NinjectKernel.Get<IImageMetadataService>();
                var imageService = NinjectKernel.Get<IImageService>();
                logService = NinjectKernel.Get<ILogService>();
                var quickCaptionService = NinjectKernel.Get<IQuickCaptionService>();

                logService.Trace($@"Getting ""{originalFilename}""...");
                var image = imageService.Get(originalFilename);

                logService.Trace($@"Captioning ""{originalFilename}""...");
                var captionedImage = Caption(image, captionDetails, new CancellationTokenSource().Token);

                logService.Trace($@"Saving ""{originalFilename}"" to ""{captionedFilename}""...");
                var metadata = Mapper.Map<Services.Models.Metadata>(captionDetails);
                imageService.Save(image, captionedFilename, metadata.ImageFormat ?? ImageFormat.Png);

                logService.Trace($@"Saving metadata for ""{originalFilename}""...");
                imageMetadataService.Save(metadata, originalFilename);

                // save the output file for the image
                captionDetails.OutputFilename = captionedFilename;

                // save the quick caption
                quickCaptionService.Add(captionedFilename, metadata);

                // flag that the current image has metadata
                captionDetails.IsMetadataLoaded = true;

                // do we need to flag it as saved?
                if (captionDetails.IsSaved) return;
                
                // flag that the current image has been saved
                captionDetails.IsSaved = true;

                // flag that the current preview needs to be reloaded
                captionDetails.IsPreviewLoaded = false;

                logService.Trace("Reloading preview...");
                PreviewThread(_current, _openCancellationTokenSource.Token);
            }
            finally
            {
                logService?.TraceExit();
            }
        }
    }
}
