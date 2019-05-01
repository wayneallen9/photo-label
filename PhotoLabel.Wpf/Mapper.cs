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
                config.CreateMap<RecentlyUsedDirectoryViewModel, Directory>();
            }).CreateMapper();
        }

        public static TDestination Map<TDestination>(object source)
        {
            return AutoMapper.Map<TDestination>(source);
        }
    }
}