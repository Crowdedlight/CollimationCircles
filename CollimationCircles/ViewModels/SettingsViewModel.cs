﻿using Avalonia;
using Avalonia.Media;
using CollimationCircles.Messages;
using CollimationCircles.Models;
using CollimationCircles.Resources.Strings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DynamicData;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.FrameworkDialogs;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CollimationCircles.ViewModels
{
    [JsonObject(MemberSerialization.OptIn)]
    public partial class SettingsViewModel : BaseViewModel, IViewClosed
    {
        private readonly IDialogService dialogService;

        [ObservableProperty]
        private INotifyPropertyChanged? dialogViewModel;

        [JsonProperty]
        [ObservableProperty]
        public PixelPoint position = new(100, 100);

        [JsonProperty]
        [ObservableProperty]
        public double width = 650;

        [JsonProperty]
        [ObservableProperty]
        public double height = 650;

        [JsonProperty]
        [ObservableProperty]
        public double scale = 1.0;

        [JsonProperty]
        [ObservableProperty]
        public double rotationAngle = 0;

        [JsonProperty]
        [ObservableProperty]
        public bool showLabels = true;

        [JsonProperty]
        [ObservableProperty]
        public ObservableCollection<CollimationHelper> items = new();

        [JsonProperty]
        [ObservableProperty]
        public ObservableCollection<Color> colorList = new();

        [ObservableProperty]
        public CollimationHelper selectedItem = new();

        [ObservableProperty]
        public bool isSelectedItem = false;

        public SettingsViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;

            InitializeColors();
            InitializeDefaults();
            InitializeMessages();
        }

        private void InitializeMessages()
        {
            WeakReferenceMessenger.Default.Register<ItemChangedMessage>(this, (r, m) =>
            {
                var item = Items?.SingleOrDefault(x => x.Id == m.Value.Id);

                if (item != null)
                {
                    item = m.Value;
                }

                WeakReferenceMessenger.Default.Send(new SettingsChangedMessage(this));
            });
        }

        private void InitializeColors()
        {
            List<Color> c = new()
            {
                Colors.Red,
                Colors.Green,
                Colors.Blue,
                Colors.Orange,
                Colors.LightBlue,
                Colors.LightGreen,
                Colors.Yellow,
                Colors.Fuchsia,
                Colors.Cyan,
                Colors.Lime,
                Colors.Gold,
                Colors.White,
                Colors.Black
            };

            ColorList = new ObservableCollection<Color>(c);
        }

        private void InitializeDefaults()
        {
            if (Items is not null)
            {
                List<CollimationHelper> list = new()
                {
                    // Circles
                    new CircleViewModel() { ItemColor = Colors.LightGreen, Radius = 100, Thickness = 2, Label = Text.Inner },
                    new CircleViewModel() { ItemColor = Colors.LightBlue, Radius = 250, Thickness = 3, Label = Text.PrimaryOuter },

                    // Spider
                    new SpiderViewModel(),

                    // Screws
                    new ScrewViewModel(),

                    // Primary Clip
                    new PrimaryClipViewModel()
                };

                Items.Clear();
                Items.AddRange(list);

                Items.CollectionChanged += Items_CollectionChanged;

                SelectedItem = Items?.FirstOrDefault()!;
            }

            RotationAngle = 0;
            Scale = 1;
            ShowLabels = true;
        }

        private void Items_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new SettingsChangedMessage(this));
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new SettingsChangedMessage(this));
        }

        [RelayCommand]
        internal void ShowSettings()
        {
            if (DialogViewModel is null)
            {
                DialogViewModel = dialogService.CreateViewModel<SettingsViewModel>();
                dialogService.Show(null, DialogViewModel);
            }
        }

        [RelayCommand]
        internal void CloseSettings()
        {
            dialogService.Close(DialogViewModel!);
            DialogViewModel = null;
        }

        [RelayCommand]
        internal void AddCircle()
        {
            Items?.Add(new CircleViewModel());
        }

        [RelayCommand]
        internal void AddScrew()
        {
            Items?.Add(new ScrewViewModel());
        }

        [RelayCommand]
        internal void AddClip()
        {
            Items?.Add(new PrimaryClipViewModel());
        }

        [RelayCommand]
        internal void AddSpider()
        {
            Items?.Add(new SpiderViewModel());
        }

        [RelayCommand]
        internal void RemoveItem(CollimationHelper item)
        {
            Items?.Remove(item);
        }

        [RelayCommand]
        internal void ResetList()
        {
            InitializeDefaults();
        }

        [RelayCommand]
        internal async Task SaveList()
        {
            string jsonString = JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });

            var settings = new SaveFileDialogSettings
            {
                Title = Text.SaveFile,
                InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                Filters = new List<FileFilter>()
                {
                    new FileFilter(Text.JSONDocuments, Text.StarJson),
                    new FileFilter(Text.AllFiles, Text.StarChar)
                },
                DefaultExtension = Text.StarJson
            };

            var result = await dialogService.ShowSaveFileDialogAsync(this, settings);

            var path = result?.Path?.LocalPath;

            if (!string.IsNullOrWhiteSpace(path))
            {
                File.WriteAllText(path, jsonString, System.Text.Encoding.UTF8);
            }
        }

        [RelayCommand]
        internal async Task LoadList()
        {
            var settings = new OpenFileDialogSettings
            {
                Title = Text.OpenFile,
                InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                Filters = new List<FileFilter>()
                {
                    new FileFilter(Text.JSONDocuments, Text.StarJson),
                    new FileFilter(Text.AllFiles, Text.StarChar),
                }
            };

            var result = await dialogService.ShowOpenFileDialogAsync(this, settings);

            string? path = result?.Path?.LocalPath;

            if (!string.IsNullOrWhiteSpace(path))
            {
                string content = File.ReadAllText(path, System.Text.Encoding.UTF8);

                if (!string.IsNullOrWhiteSpace(content))
                {
                    var jss = new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto,
                        NullValueHandling = NullValueHandling.Ignore,
                    };

                    try
                    {
                        SettingsViewModel? vm = JsonConvert.DeserializeObject<SettingsViewModel>(content, jss);

                        if (vm != null && vm.Items != null)
                        {
                            Position = vm.Position;
                            Width = vm.Width;
                            Height = vm.Height;
                            Scale = vm.Scale;
                            RotationAngle = vm.RotationAngle;
                            ShowLabels = vm.ShowLabels;
                            ColorList = vm.ColorList;

                            Items?.Clear();
                            Items?.AddRange(vm.Items);
                        }
                        else
                        {
                            await dialogService.ShowMessageBoxAsync(this, Text.UnableToOpenFile, Text.Error);
                        }
                    }
                    catch// (Exception exc)
                    {
                        // TODO: log exception
                        await dialogService.ShowMessageBoxAsync(this, Text.UnableToParseJsonFile, Text.Error);
                    }
                }
            }
        }

        partial void OnSelectedItemChanged(CollimationHelper value)
        {
            IsSelectedItem = SelectedItem is not null;
        }

        public void OnClosed()
        {
            DialogViewModel = null;
        }
    }
}
