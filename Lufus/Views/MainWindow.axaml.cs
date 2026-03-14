using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System;
using System.Diagnostics;
using Avalonia.Controls.Shapes;
using System.Text.RegularExpressions;
using Avalonia.Threading;



namespace Lufus.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddItemCombobox();
    }

    private async void btnIso_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        string? selectedPath = await FileDialog();

        if (selectedPath != null)
        {
            txtIso.Text = selectedPath;
        }


        //txbProg.Text = "Merhaba Dünya";
    }

    private void AddItemCombobox()
    {
        cmbUsb.Items.Clear();

        var psi = new ProcessStartInfo("lsblk", "-dn -o NAME,TYPE,SIZE")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using var proc = Process.Start(psi);
        var output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var cleanLine = System.Text.RegularExpressions.Regex.Replace(line.Trim(), @"\s+", " ");
            var parts = cleanLine.Split(' ');

            if (parts.Length >= 3 && parts[1] == "disk" && parts[0].StartsWith("sd"))
            {
                var name = parts[0];
                var size = parts[2];

                if (name != "sda")
                    cmbUsb.Items.Add($"{name}-{size}");
            }
        }

        if (cmbUsb.Items.Count == 0)
            cmbUsb.Items.Add("Usb takılı değil");


    }

    

    private async Task<string?> FileDialog()
    {
        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel.StorageProvider == null) return null;

        var filter = new FilePickerFileType("Iso Dosyaları")
        {
            Patterns = new[] { "*.iso" },
        };

        var allFiles = FilePickerFileTypes.All;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Iso Seç",
            AllowMultiple = false,
            FileTypeFilter = new[] { filter, allFiles }
        });

        if (files != null && files.Count > 0)
            return files[0].Path.LocalPath;

        return null;
    }

    private async void btnYaz_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var msgmngr = MessageBoxManager.GetMessageBoxStandard("Uyarı", "Diskiniz tamamen silinecektir. Bunu yapmak istediğinizden emin misiniz?", ButtonEnum.YesNo);
        var dialog = await msgmngr.ShowAsync();
        string iso=txtIso.Text;
        string usb=cmbUsb.SelectedItem.ToString();

        if (dialog == ButtonResult.Yes)
        {// btnYaz_Click içinde şu kısmı bul ve değiştir:
            await WriteOnDisk(usb, iso, (p, mesaj) => // Artik iki parametre alıyor: p (yüzde), mesaj (metin)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    progress.Value = p;
                    txbProg.Text = mesaj; // Metodun içinden gelen "Sync ediliyor" gibi mesajlar burada görünür
                });
            });

        }
        }catch(Exception ex)
        {
            var errorBox=MessageBoxManager.GetMessageBoxStandard("hata",ex.Message);
            await errorBox.ShowAsync();
        }
    }


  private async Task WriteOnDisk(string device, string iso, Action<double, string> progress)
    {
        if (!File.Exists(iso)) throw new FileNotFoundException("Iso Dosyası Seçmediniz");
        long totalBytes = new FileInfo(iso).Length;
        string deviceName = "/dev/" + device.Split('-')[0];

        // 1. Adım: Unmount (Sessizce)
        progress(0, "Hazırlanıyor...");
        var unmountProcess = Process.Start("pkexec", $"sh -c \"umount {deviceName}* 2>/dev/null || true\"");
        await unmountProcess.WaitForExitAsync();

        // 2. Adım: Yazma İşlemi (dd)
        var startInfo = new ProcessStartInfo
        {
            FileName = "pkexec",
            Arguments = $"stdbuf -oL dd if=\"{iso}\" of={deviceName} bs=4M status=progress",
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var startProcess = new Process { StartInfo = startInfo };
        startProcess.Start();

        using (var reader = startProcess.StandardError)
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var match = Regex.Match(line, @"(\d+)\s+byte");
                if (match.Success && long.TryParse(match.Groups[1].Value, out long bytesWritten))
                {
                    double myProgress = (double)bytesWritten / totalBytes * 100;
                    // %99'da sabitleyelim ki 'Sync' aşamasına geçince %100 diyelim
                    progress(Math.Min(myProgress, 99.0), $"Yazdırılıyor %{myProgress:F1}");
                }
            }
        }
        await startProcess.WaitForExitAsync();

        // 3. Adım: Senkronizasyon (Gerçekten hazır olduğundan emin olalım)
        progress(99.5, "Diske son veriler yazılıyor (Sync)...");
        var syncProcess = Process.Start("pkexec", "sync");
        await syncProcess.WaitForExitAsync();

        if (startProcess.ExitCode == 0)
        {
            progress(100, "Tamamlandı! Güvenle çıkarabilirsiniz.");
        }
        else
        {
            throw new Exception("Yazma hatası oluştu.");
        }
    }

}
