using AutoMapper;
using PhotoLabel.Services.Models;

namespace PhotoLabel.Wpf
{
    public static class Mapper
    {
        #region variables
        private static readonly IMapper AutoMapper;
        #endregion

        static Mapper()
        {
            // create the mapper
            AutoMapper = new MapperConfiguration(config =>
            {
                config.CreateMap<FolderViewModel, Folder>();
                config.CreateMap<SubFolderViewModel, SubFolder>()
                    .ReverseMap();
            }).CreateMapper();
        }

        public static TDestination Map<TDestination>(object source)
        {
            return AutoMapper.Map<TDestination>(source);
        }

        public static void Map(object source, object target)
        {
            AutoMapper.Map(source, target);
        }
    }
}