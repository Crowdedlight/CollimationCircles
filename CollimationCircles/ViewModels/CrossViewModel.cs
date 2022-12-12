﻿using CollimationCircles.Messages;
using CollimationCircles.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.ComponentModel;

namespace CollimationCircles.ViewModels
{
    public partial class CrossViewModel : ObservableValidator, ICross
    {
        [ObservableProperty]
        public double rotation = 45;
        [ObservableProperty]
        public double size = 4;
        [ObservableProperty]
        public Guid id = Guid.NewGuid();
        [ObservableProperty]
        public string color = ItemColor.Yellow;        
        [ObservableProperty]
        public string label = "Cross";
        [ObservableProperty]
        public int thickness = 1;
        [ObservableProperty]
        public double radius = 300;
        [ObservableProperty]
        public bool visibility = true;
        [ObservableProperty]
        public bool isRotatable = true;
        [ObservableProperty]
        public bool isSizeable = true;
        [ObservableProperty]
        public bool isEditable = true;

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new ItemChangedMessage(this));
        }
    }
}
