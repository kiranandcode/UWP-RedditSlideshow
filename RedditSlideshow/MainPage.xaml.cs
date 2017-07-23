using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
        
        public ObservableCollection<Link> LinkList;

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


            dialog.ShowAsync();
        }
    }
}
