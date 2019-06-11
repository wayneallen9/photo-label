using AutoMapper;
using PhotoLabel.Services.Models;
using System.Collections.Generic;

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
                config.CreateMap<FolderViewModel, Folder>()
                    .ForMember(d => d.SelectedSubFolders, o => o.MapFrom((folderViewModel, folder) =>
                    {
                        var list = new List<string>();

                        // populate the list
                        list.AddRange(GetSelectedSubFolders(folderViewModel.SubFolders));

                        return list;
                    }))
                    .ReverseMap()
                    .AfterMap((folder, folderViewModel) =>
                        {
                            SetSelectedSubFolders(folderViewModel.SubFolders, folder.SelectedSubFolders);
                        });
            }).CreateMapper();
        }

        private static IEnumerable<string> GetSelectedSubFolders(IEnumerable<IFolderViewModel> folderViewModels)
        {
            var list = new List<string>();

            foreach (var folderViewModel in folderViewModels)
            {
                if (folderViewModel.IsSelected) list.Add(folderViewModel.Path);

                list.AddRange(GetSelectedSubFolders(folderViewModel.SubFolders));
            }

            return list;
        }

        public static TDestination Map<TDestination>(object source)
        {
            return AutoMapper.Map<TDestination>(source);
        }

        private static void SetSelectedSubFolders(IEnumerable<IFolderViewModel> subFolderViewModels,
            ICollection<string> selectedSubFolders)
        {
            foreach (var subFolderViewModel in subFolderViewModels)
            {
                subFolderViewModel.IsSelected = selectedSubFolders.Contains(subFolderViewModel.Path);

                SetSelectedSubFolders(subFolderViewModel.SubFolders, selectedSubFolders);
            }
        }
    }
}