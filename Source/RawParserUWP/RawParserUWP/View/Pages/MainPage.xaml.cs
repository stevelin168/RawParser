﻿using RawParser.Model.Format;
using RawParser.Model.ImageDisplay;
using RawParser.Model.Parser;
using RawParserUWP.Model.Exception;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace RawParserUWP
{
    /// <summary>
    /// The main class of the appliation
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private RawImage currentRawImage { set; get; }
        private bool imageSelected { set; get; }

        public MainPage()
        {
            InitializeComponent();

            NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            appBarImageChoose.Click += new RoutedEventHandler(appBarImageChooseClick);
            imageSelected = false;
        }

        private async void appBarImageChooseClick(object sender, RoutedEventArgs e)
        {
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.ViewMode = PickerViewMode.Thumbnail;
            filePicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            filePicker.FileTypeFilter.Add(".nef");
            filePicker.FileTypeFilter.Add(".tiff");
            filePicker.FileTypeFilter.Add(".dng");
            StorageFile file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file
                try
                {
                    OpenFile(file);
                }
                catch (Exception ex)
                {
                    ExceptionDisplay.display(ex.Message + ex.StackTrace);
                }
            }
            else
            {
                //TODO
            }
        }

        private void emptyImage()
        {
            //empty the previous image data
            currentRawImage = null;
            //empty the image display
            imageBox.Source = null;
            imageBox.UpdateLayout();
            //empty the exif data
            exifDisplay.ItemsSource = null;
            //empty the histogram

        }

        private async void OpenFile(StorageFile file)
        {
            //Open the file with the correct parser
            Parser parser;
            switch (file.FileType.ToUpper())
            {
                case ".NEF":
                    parser = new NEFParser();
                    break;
                case ".DNG":
                    parser = new DNGParser();
                    break;
                case ".TIFF":
                    parser = new DNGParser();
                    break;
                default: throw new Exception("File not supported");//todo change exception types
            }

            //TODO Add a loading screen
            progressDisplay.IsActive = true;
            progressDisplay.Visibility = Visibility.Visible;
            emptyImage();
            Stream stream = (await file.OpenReadAsync()).AsStreamForRead();
            Task t = Task.Run(async () =>
            {
                currentRawImage = parser.parse(stream);
                SoftwareBitmap image = currentRawImage.getImageAsBitmap();
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    //Do some UI-code that must be run on the UI thread.
                    //display the image
                    WriteableBitmap bitmap = new WriteableBitmap(image.PixelWidth, image.PixelHeight);
                    image.CopyToBuffer(bitmap.PixelBuffer);
                    imageBox.Source = bitmap;
                    //set exif datasource
                    exifDisplay.ItemsSource = currentRawImage.exif.Values;
                    //TODO Hide the loading screen
                    progressDisplay.Visibility = Visibility.Collapsed;
                    progressDisplay.IsActive = false;
                });
            });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var args = e.Parameter as Windows.ApplicationModel.Activation.IActivatedEventArgs;
            if (args != null)
            {
                if (args.Kind == Windows.ApplicationModel.Activation.ActivationKind.File)
                {
                    var fileArgs = args as Windows.ApplicationModel.Activation.FileActivatedEventArgs;
                    string strFilePath = fileArgs.Files[0].Path;
                    var file = (StorageFile)fileArgs.Files[0];
                    if (file != null)
                    {
                        // Application now has read/write access to the picked file
                        try
                        {
                            OpenFile(file);
                        }
                        catch (Exception ex)
                        {
                            ExceptionDisplay.display(ex.Message + ex.StackTrace);
                        }
                    }
                    else
                    {
                        //TODO
                    }

                }
            }
        }


        private void appbarAboutClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(View.Pages.About), null);
        }

        private void appbarSettingClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(View.Pages.Settings), null);
        }

        private void appbarShowSplitClick(object sender, RoutedEventArgs e)
        {
            splitView.IsPaneOpen = !splitView.IsPaneOpen;

        }
    }
}
