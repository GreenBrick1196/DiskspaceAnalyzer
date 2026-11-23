using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.Messaging;
using RPZ_DiskspaceAnalyzer.ViewModels;
using System;

namespace RPZ_DiskspaceAnalyzer.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            WindowNotificationManager notificationManager = new WindowNotificationManager(this)
            {
                Position = NotificationPosition.BottomRight,
                MaxItems = 3
            };

            DataContext = new MainWindowViewModel(notificationManager);
        }
    }
}