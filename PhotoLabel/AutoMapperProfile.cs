using AutoMapper;
using System.Drawing;

namespace PhotoLabel
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Services.Models.Metadata, Models.ImageModel>()
                .ForMember(d => d.Colour, o => o.MapFrom(s => Color.FromArgb(s.Colour)))
                .ForMember(d => d.Font, o => o.MapFrom(s => new Font(s.FontFamily, s.FontSize, s.FontBold ? FontStyle.Bold : FontStyle.Regular)))
                .ForMember(d => d.ExifLoaded, o => o.Ignore())
                .ForMember(d => d.MetadataExists, o => o.UseValue(true))
                .ForMember(d => d.MetadataLoaded, o => o.Ignore())
                .ForMember(d => d.Saved, o => o.Ignore());

            CreateMap<Models.DirectoryModel, Services.Models.FolderModel>()
                .ReverseMap();
        }
    }
}