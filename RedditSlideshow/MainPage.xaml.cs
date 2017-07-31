using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RedditSlideshow
{
    public class Link {
        private string url;
        public string Url {
            get {
                return url;
            }
            set {
                url = value;
            }
        }
        public Link()
        {
            url = "";
        }
    }



    public sealed partial class MainPage : Page
    {
        static ContentDialog err_dialog;
        public ObservableCollection<Link> LinkList;

        Task currentFileTask;

        public MainPage()
        {
            // Maintain state between pages
            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            
            LinkList = new ObservableCollection<Link>();

            // Show one entry at startup
            LinkList.Add(new Link());

            this.InitializeComponent();

            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

        }

        private void addLink_Click(object sender, RoutedEventArgs e)
        {
            LinkList.Add(new Link());

        }

        private void removeLink_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            LinkList.Remove((RedditSlideshow.Link)button.DataContext);

        }

        private void generateSlideShow_Click(object sender, RoutedEventArgs e)
        {
            
            List<string> urls = LinkList.Where(item => !String.IsNullOrEmpty(item.Url)).Select(item => item.Url).ToList();

            if(urls.Count != 0)
                this.Frame.Navigate(typeof(RedditSlideshow.Views.Slideshow), urls);
            else
            {
                ShowMessageDialog("Could not generate Slideshow", "At least one valid reddit url is required to generate a slideshow.");
            }
        }


        private void textBoxLostFocusEventHandler(object sender, RoutedEventArgs e)
        {
            TextBox textbox = sender as TextBox;

            string text = textbox.Text;

            // Auto format entry to compatible format..
            Regex url_rx = new Regex(@"(?:^(?:https?:\/\/)?(?:www.)?(?:(?:np|np-dk).)?(?:reddit\.com)\/r\/([a-zA-Z0-9_]+)\/?|^(?:\/?r\/)?([a-zA-Z0-9_]+))", RegexOptions.Compiled);
            MatchCollection extracted = url_rx.Matches(text);

            if(extracted.Count != 0)
            {
                StringBuilder sb = new StringBuilder();
                Match match = extracted[0];
                sb.Append("r/");
                if (!String.IsNullOrEmpty(match.Groups[1].ToString()))
                    sb.Append(match.Groups[1]);
                else if (!String.IsNullOrEmpty(match.Groups[2].ToString()))
                    sb.Append(match.Groups[2]);
                textbox.Text = sb.ToString();
            }
            else
            {
                ShowMessageDialog("Url Format Error", "The url submitted was not a valid reddit url.\n" +
                "Urls can be of the form reddit.com/r/[subreddit name] or even r/[subreddit name].");
                textbox.Text = "";
            }


            textbox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private static void ShowMessageDialog(string title, string content)
        {
            if (err_dialog != null) return;

            var dialog = new ContentDialog() {
                Title = title,
                Background = Application.Current.Resources["MainTheme_color_highlight"] as SolidColorBrush,
            };
            var panel = new StackPanel()
            {
            };
            panel.Children.Add(new TextBlock
            {
                Text = content,
                TextWrapping = TextWrapping.Wrap,
                                
            });

            dialog.Content = panel;

            dialog.PrimaryButtonText = "Ok";
            dialog.PrimaryButtonClick += (s, e) =>
            {
                err_dialog = null;
            };
            err_dialog = dialog;

            dialog.ShowAsync();
        }

        private  void opensubreddits_click(object sender, RoutedEventArgs e)
        {
            if (currentFileTask == null || currentFileTask.IsCompleted || currentFileTask.IsCanceled || currentFileTask.IsFaulted)
            {
                currentFileTask = opensubreddits_clickasync();
            }
            else
            {
                currentFileTask = currentFileTask.ContinueWith(async (task) => { await opensubreddits_clickasync(); });
            }
        }

        private void savesubreddits_click(object sender, RoutedEventArgs e)
        {
            if(currentFileTask == null || currentFileTask.IsCompleted || currentFileTask.IsCanceled || currentFileTask.IsFaulted)
            {
                currentFileTask = savesubreddits_clickasync();
            } else
            {
                currentFileTask = currentFileTask.ContinueWith(async (task) => { await savesubreddits_clickasync(); });
            }
        }

        private async Task opensubreddits_clickasync()
        {
            Regex url_rx = new Regex(@"(?:^(?:https?:\/\/)?(?:www.)?(?:(?:np|np-dk).)?(?:reddit\.com)\/r\/([a-zA-Z0-9_]+)\/?|^(?:\/?r\/)?([a-zA-Z0-9_]+))", RegexOptions.Compiled);
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".txt");
            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if(file != null)
            {
                try
                {
                    Stream x = await file.OpenStreamForReadAsync();
                    StreamReader reader = new StreamReader(x, Encoding.UTF8);
                    try
                    {
                        while(!reader.EndOfStream)
                        {
                            string line = await reader.ReadLineAsync();
                             MatchCollection extracted = url_rx.Matches(line);

                            if (extracted.Count != 0)
                            {
                                StringBuilder sb = new StringBuilder();
                                Match match = extracted[0];
                                sb.Append("r/");
                                if (!String.IsNullOrEmpty(match.Groups[1].ToString()))
                                    sb.Append(match.Groups[1]);
                                else if (!String.IsNullOrEmpty(match.Groups[2].ToString()))
                                    sb.Append(match.Groups[2]);
                                Link link = new Link();
                                link.Url = sb.ToString();
                                LinkList.Add(link);
                            }
                        }
                    }
                    finally
                    {

                        reader.Dispose();
                        x.Dispose();
                    }

                }
                catch (PathTooLongException)
                {
                    ShowMessageDialog("File Opening Error", "Unfortunately we couldn't open the file as the path was too long. Please try using a shorter path.");
                    return;
                }
                catch (NotSupportedException)
                {
                    ShowMessageDialog("File Opening Error", "Unfortunately we couldn't open the file as file access is not supported on this platform. Apologies for the inconvenience.");
                    return;
                }
                catch (DirectoryNotFoundException)
                {
                    ShowMessageDialog("File Opening Error", "Unfortunately we couldn't open the file as we couldn't find the directory. Please try using a different directory.");
                    return;
                }
                catch (SecurityException)
                {
                    ShowMessageDialog("File Opening Error", "Unfortunately we couldn't open the file due to security issues. Please send the error code SEC1 to the developer for support.");
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    ShowMessageDialog("File Opening Error", "Unfortunately we couldn't open the file as access to that file is not authorized on this account. Please try again with higher priveleges or open a different file.");
                    return;
                }
                catch (IOException)
                {
                    ShowMessageDialog("File Opening Error", "Unfortunately we couldn't open the file. Please send the error code IOE1 to the developer for support.");
                    return;
                }
            }
        }

        private async Task savesubreddits_clickasync()
        {
            Regex url_rx = new Regex(@"(?:^(?:https?:\/\/)?(?:www.)?(?:(?:np|np-dk).)?(?:reddit\.com)\/r\/([a-zA-Z0-9_]+)\/?|^(?:\/?r\/)?([a-zA-Z0-9_]+))", RegexOptions.Compiled);

            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
            savePicker.SuggestedFileName = "RedditSlideshow Subreddit List " + new DateTime();

            StorageFile file = await savePicker.PickSaveFileAsync();
            Stream x;
            StreamWriter writer;
            if (file != null)
            {
                try
                {
                        x = await file.OpenStreamForWriteAsync();
                        writer = new StreamWriter(x, Encoding.UTF8);
                    try
                    {
                        foreach (String s in LinkList.Where((Link link) => { return url_rx.IsMatch(link.Url); }).Select((Link a) => { return a.Url; }))
                        {
                            await writer.WriteLineAsync(s);
                        }
                    await writer.FlushAsync();
                    }
                    finally
                    {

                        writer.Dispose();
                        x.Dispose();
                    }

                }
                catch (PathTooLongException)
                {
                    ShowMessageDialog("File Saving Error", "Unfortunately we couldn't save the file as the path was too long. Please try using a shorter path.");
                    return;
                }
                catch (NotSupportedException)
                {
                    ShowMessageDialog("File Saving Error", "Unfortunately we couldn't save the file as file saving is not supported on this platform. Apologies for the inconvenience.");
                    return;
                }
                catch (DirectoryNotFoundException)
                {
                    ShowMessageDialog("File Saving Error", "Unfortunately we couldn't save the file as we couldn't find the directory. Please try using a different directory.");
                    return;
                }
                catch (SecurityException)
                {
                    ShowMessageDialog("File Saving Error", "Unfortunately we couldn't save the file due to security issues. Please send the error code SEC1 to the developer for support.");
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    ShowMessageDialog("File Saving Error", "Unfortunately we couldn't save the file as access to that file is not authorized on this account. Please try again with higher priveleges or save to a different directory.");
                    return;
                }
                catch (IOException)
                {
                    ShowMessageDialog("File Saving Error", "Unfortunately we couldn't save the file. Please send the error code IOE1 to the developer for support.");
                    return;
                } 
                ShowMessageDialog("Subreddit list Saved!", "We have saved your subreddit list to " + file.Path + ".");
            }
        }
    }
}
