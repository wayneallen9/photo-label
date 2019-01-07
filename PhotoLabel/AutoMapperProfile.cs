using AutoMapper;
using System.Drawing;

namespace PhotoLabel
{
    // ReSharper disable once UnusedMember.Global
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Services.Models.Metadata, Models.ImageModel>()
                .ForMember(d => d.BackgroundColour, o => o.MapFrom(s => Color.FromArgb(s.BackgroundColour)))
                .ForMember(d => d.Colour, o => o.MapFrom(s => Color.FromArgb(s.Colour)))
                .ForMember(d => d.ExifLoaded, o => o.Ignore())
                .ForMember(d => d.MetadataExists, o => o.UseValue(true))
                .ForMember(d => d.MetadataLoaded, o => o.Ignore())
                .ForMember(d => d.Saved, o => o.Ignore());

            CreateMap<Models.DirectoryModel, Services.Models.DirectoryModel>()
                .ReverseMap();
        }
    }
}