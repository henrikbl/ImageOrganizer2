﻿using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using Template10.Mvvm;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using ImageOrganizer.Model;
using System.Windows.Input;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage.Search;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using ImageOrganizer.GUI.DataSource;
using System.IO;
using Windows.UI.Xaml.Media;

namespace ImageOrganizer.GUI.ViewModels
{

    public class MainPageViewModel : ViewModelBase, INotifyPropertyChanged
    {
        // path to currently picked folder.
        private string _PathToFolder;
        public string PathToFolder
        {
            get => _PathToFolder;
            set
            {
                if (value != _PathToFolder)
                {
                    _PathToFolder = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // Message board textinput
        private string _MessageBoardText;
        public string MessageBoardText
        {
            get => _MessageBoardText;
            set
            {
                if(value != _MessageBoardText)
                {
                    _MessageBoardText = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // title of the selected picture
        private string _CurrentPictureTitle;
        public string CurrentPictureTitle
        {
            get => _CurrentPictureTitle;
            set
            {
                if (value != _CurrentPictureTitle)
                {
                    _CurrentPictureTitle = value;
                    SelectedPicture.Title = CurrentPictureTitle;
                    NotifyPropertyChanged();
                }
            }
        }

        // picture which is currently showing.
        private PictureForView _SelectedPicture;
        public PictureForView SelectedPicture
        {
            get => _SelectedPicture;
            set
            {
                if(value != _SelectedPicture)
                {
                    _SelectedPicture = value;
                    if(SelectedPicture != null)
                    {
                        CurrentPictureTitle = _SelectedPicture.Title;
                    }
                    
                    NotifyPropertyChanged();
                }
            }
        }

        // group which is currently selected.
        private Group _SelectedGroup;
        public Group SelectedGroup
        {
            get => _SelectedGroup;
            set
            {
                if(value != _SelectedGroup)
                {
                    _SelectedGroup = value;
                    NotifyPropertyChanged();
                }
            }
        }

        ObservableCollection<Group> groupList;
        public ObservableCollection<Group> GroupList { get { return groupList; } set { Set(ref groupList, value); } }

        ObservableCollection<PictureForView> pictureList;
        public ObservableCollection<PictureForView> PictureList { get { return pictureList; } set { Set(ref pictureList, value); } }

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

        public MainPageViewModel()
        {
            _PathToFolder = "Directory";
            _MessageBoardText = "Message board";

            PictureList = new ObservableCollection<PictureForView>();
        }

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            if(GroupList == null)
            {
                GroupList = new ObservableCollection<Group>(await DataSource.Groups.Instance.GetGroups());
            }

            if(PictureList == null)
            {
                PictureList = new ObservableCollection<PictureForView>();
            }
            
            if(state.Any())
            {

            }
            await Task.CompletedTask;
        } 

        public ICommand FindFolderCommand { get { return new FindFolderCommand(async ()=> await FindFolderAsync()); }}
        public ICommand AddPictureCommand { get { return new AddPicture(async () => await AddPictureAsync()); } }

        // Add picture to database and group.
        public async Task AddPictureAsync()
        {
            var picture = new Picture(CurrentPictureTitle, SelectedPicture.base64ImageString);

            if (SelectedGroup != null)
            {
                int selectedGroupId = SelectedGroup.GroupId;

                try
                {
                    await DataSource.Pictures.Instance.AddPicture(picture);

                    await DataSource.Pictures.Instance.AddPictureToGroup(Pictures.CurrentPictureId, selectedGroupId);

                    MessageBoardText = String.Format("yay....{0} is added to {1}!", picture.Title, SelectedGroup.Name); 
                }
                catch (Exception)
                {

                    throw;
                }
            }
            
            else
            {
                MessageBoardText = "Oops.. you did not set a group to add picture to!";
            }       
        }

        // Folder picker method which creates a list of storagefiles with .png, .jpg and .jpeg extensions.
        public async Task FindFolderAsync()
        {
            var folderPicker = new FolderPicker();
            folderPicker.ViewMode = PickerViewMode.List;
            folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                QueryOptions options;

                options = new QueryOptions(CommonFileQuery.DefaultQuery, new[] { ".png", ".jpg", ".jpeg" });
                StorageFileQueryResult sfqr = folder.CreateFileQueryWithOptions(options);

                IReadOnlyList<StorageFile> picture_files = await sfqr.GetFilesAsync();
                await CreatePictureListFromFolderAsync(picture_files);

                Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                PathToFolder = folder.Path;
            }
        }

        // creates a list of pictures from computer for viewing.
        public async Task CreatePictureListFromFolderAsync(IReadOnlyList<StorageFile> files)
        {
            PictureList.Clear();

            foreach(var f in files)
            {
                string imageString = await ConvertToBase64String(f);
                Picture p = new Picture(f.DisplayName.ToString(), imageString);

                BitmapImage image = new BitmapImage();
                image = await ConvertToBitmapImageAsync(imageString);

                PictureForView pfv = new PictureForView(p, image);
                PictureList.Add(pfv);  
            }
        }

        // Convert image to a base64String. Method is found at https://blogs.msdn.microsoft.com/msgulfcommunity/2014/11/02/c-encoding-and-decoding-base-64-strings/
        public async Task<string> ConvertToBase64String(StorageFile file)
        {
            var bitmapImage = new BitmapImage();
            var stream = (FileRandomAccessStream)await file.OpenAsync(FileAccessMode.Read);

            bitmapImage.SetSource(stream);

            Byte[] pictureAttachment = new Byte[0];
            var reader = new DataReader(stream.GetInputStreamAt(0));
            pictureAttachment = new Byte[stream.Size];

            await reader.LoadAsync((uint)stream.Size);
            reader.ReadBytes(pictureAttachment);

            string imageAsBase64String = Convert.ToBase64String(pictureAttachment);
            return imageAsBase64String;
        }

        // Convert base64String to BitmapImage. Method found at https://social.msdn.microsoft.com/Forums/sqlserver/en-US/09a07285-c03d-42e9-901f-e7ded45944b7/wp81chow-to-convert-base64-string-to-image-or-bitmap-image-in-windows-phone-81-store-app?forum=wpdevelop
        public async Task<BitmapImage> ConvertToBitmapImageAsync(string base64String)
        {
            var byteArray = Convert.FromBase64String(base64String);
            var stream = new InMemoryRandomAccessStream();

            var dataWriter = new DataWriter(stream);
            dataWriter.WriteBytes(byteArray);
            await dataWriter.StoreAsync();
            stream.Seek(0);

            BitmapImage image = new BitmapImage();
            image.SetSource(stream);

            return image;
        }

        // method for updating UI elements using INotifyPropertyChanged
        private void NotifyPropertyChanged([CallerMemberName] String propertyName="")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /*
     * 
     * Commands 
     * 
     */

    // Command for adding a picture to database.
    public class AddPicture : ICommand
    {
        private Action _action;

        public AddPicture(Action action)
        {
            _action = action;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            this._action();
        }
    }

    // Command for finding a folder with images.
    public class FindFolderCommand : ICommand
    {
        private Action _action;

        public FindFolderCommand(Action action)
        {
            _action = action;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            this._action();
        }
    }
}
