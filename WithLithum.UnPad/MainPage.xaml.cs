using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.System;
using Windows.UI.Core.Preview;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace WithLithum.UnPad
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool changed;
        private StorageFile knownFile;

        public MainPage()
        {
            this.InitializeComponent();
            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested
                += this.MainPage_CloseRequested;
        }

        private async void MainPage_CloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            var defferal = e.GetDeferral();
            if (changed)
            {
                var res = await PromptChanged("Your work was not saved. If you insist, your work will be lost.");
                if (!res) e.Handled = true;
            }
            defferal.Complete();
        }

        private void Page_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Tab)
            {
                e.Handled = true;
            }
        }

        private void AboutClicked(object sender, RoutedEventArgs e)
        {
            var md = new MessageDialog($"UnPad v{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build} (Rev {Package.Current.Id.Version.Revision}) \r\nby WithLithum")
            {
                Commands = { new UICommand("OK", _ => { }, 1) },
                Title = "About"
            };

            _ = md.ShowAsync();
        }

        private void EditMain_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Reserved
        }

        private async Task<bool> PromptChanged(string text)
        {
            if (!changed) return true;

            var dlg = new ContentDialog()
            {
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                CloseButtonText = "Cancel",
                Content = text,
                Title = "Unsaved Content"
            };

            var result = await dlg.ShowAsync();
            switch (result)
            {
                case ContentDialogResult.Primary:
                    await AutoSave();
                    return true;
                case ContentDialogResult.Secondary:
                    return true;

                default:
                    return false;
            }
        }

        private async Task Save(StorageFile file)
        {
            if (file != null)
            {
                CachedFileManager.DeferUpdates(file);

                EditMain.Document.GetText(TextGetOptions.FormatRtf, out var fin);

                await FileIO.WriteTextAsync(file, fin);
                TextStatus.Text = "File saved";
                var updateStat = await CachedFileManager.CompleteUpdatesAsync(file);
                if (updateStat != FileUpdateStatus.Complete)
                {
                    var errorBox =
                new MessageDialog("File " + file.Name + " couldn't be saved.");
                    await errorBox.ShowAsync();
                }

                knownFile = file;
            }
        }

        private async Task AutoSave()
        {
            if (knownFile == null)
            {
                await PromptToSave();
            }
            else
            {
                await Save(knownFile);
            }
        }

        private async Task PromptToSave()
        {
            var picker = new FileSavePicker();
            picker.FileTypeChoices.Add("Rich Text Document", new List<string>() { ".rtf", ".updr" });
            picker.SuggestedFileName = "Document";

            var file = await picker.PickSaveFileAsync();
            await Save(file);
        }

        private async void ItemNew_Click(object sender, RoutedEventArgs e)
        {
            var rsl = await PromptChanged("If you do not save, your work will be lost if you continue.");
            if (!rsl) return;

            EditMain.Document.SetText(TextSetOptions.None, "");
            changed = false;
            knownFile = null;
        }

        private void EditMain_TextChanged(object sender, RoutedEventArgs e)
        {
            changed = true;
        }

        private async void ItemSave_Click(object sender, RoutedEventArgs e)
        {
            await AutoSave();
        }

        private async void ItemSaveAs_Click(object sender, RoutedEventArgs e)
        {
            await PromptToSave();
        }

        private async void ItemOpen_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".rtf");
            picker.FileTypeFilter.Add(".udpr");
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            var file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                using (var stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    EditMain.Document.LoadFromStream(TextSetOptions.FormatRtf, stream);
                }

                knownFile = file;
            }
        }

        private async void ItemExit_Click(object sender, RoutedEventArgs e)
        {
            if (changed)
            {
                var res = await PromptChanged("Your work was not saved. If you insist, your work will be lost.");
                if (!res)
                {
                    return;
                }
                Application.Current.Exit();
            }
        }
    }
}
