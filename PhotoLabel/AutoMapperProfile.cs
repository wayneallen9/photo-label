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
                .ForMember(d => d.BackgroundColour, o => o.MapFrom(s => s.BackgroundColour == null ? (Color?)null : Color.FromArgb(s.BackgroundColour.Value)))
                .ForMember(d => d.Colour, o => o.MapFrom(s => s.Colour == null ? (Color?)null : Color.FromArgb(s.Colour.Value)))
                .ForMember(d => d.Filename, o => o.Ignore())
                .ForMember(d => d.FontName, o => o.MapFrom(s => s.FontFamily))
                .ForMember(d => d.IsExifLoaded, o => o.Ignore())
                .ForMember(d => d.IsMetadataLoaded, o=>o.Ignore())
                .ForMember(d => d.IsPreviewLoaded, o => o.Ignore())
                .ForMember(d => d.IsSaved, o => o.Ignore())
                .ReverseMap()
                    .ForMember(d => d.BackgroundColour, o => o.MapFrom(s => s.BackgroundColour == null ? (int?)null : s.BackgroundColour.Value.ToArgb()))
                    .ForMember(d => d.Colour, o => o.MapFrom(s => s.Colour == null ? (int?)null : s.Colour.Value.ToArgb()))
                    .ForMember(d => d.FontFamily, o=>o.MapFrom(s => s.FontName));

            CreateMap<Models.Directory, Services.Models.Directory>()
                .ReverseMap();
        }
    }
}