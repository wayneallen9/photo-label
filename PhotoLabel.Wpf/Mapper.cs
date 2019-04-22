using PhotoLabel.Services.Models;

namespace PhotoLabel.Wpf
{
    public static class Mapper
    {
        #region variables
        private static readonly AutoMapper.IMapper _mapper;
        #endregion

        static Mapper()
        {
            // create the mapper
            _mapper = new AutoMapper.MapperConfiguration(config =>
            {
                config.CreateMap<RecentlyUsedDirectoryViewModel, Directory>();
            }).CreateMapper();
        }

        public static TDestination Map<TDestination>(object source)
        {
            return _mapper.Map<TDestination>(source);
        }
    }
}
