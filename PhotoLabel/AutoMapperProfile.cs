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
                .ForMember(d => d.BackgroundColour, o => o.MapFrom(s => s.BackgroundColour < -1 ? (Color?)null : Color.FromArgb(s.BackgroundColour)))
                .ForMember(d => d.Colour, o => o.MapFrom(s => s.Colour < -1 ? (Color?)null : Color.FromArgb(s.Colour)))
                .ForMember(d => d.FontName, o => o.MapFrom(s => s.FontFamily))
                .ForMember(d => d.FontSize, o=>o.ResolveUsing(s => s.FontSize <= 0 ? (float?)null : s.FontSize))
                .ForMember(d => d.IsExifLoaded, o => o.Ignore())
                .ForMember(d => d.IsSaved, o => o.Ignore())
                .ReverseMap()
                    .ForMember(d => d.BackgroundColour, o => o.MapFrom(s => s.BackgroundColour.HasValue ? s.BackgroundColour.Value.ToArgb() : -2))
                    .ForMember(d => d.Colour, o => o.MapFrom(s => s.Colour.HasValue ? s.Colour.Value.ToArgb() : -2))
                    .ForMember(d => d.FontFamily, o=>o.MapFrom(s => s.FontName));

            CreateMap<Models.Directory, Services.Models.Directory>()
                .ReverseMap();
        }
    }
}