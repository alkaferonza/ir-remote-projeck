using System;
using System.Diagnostics;
using System.IO.Ports;
using InputSimulatorStandard;
using InputSimulatorStandard.Native;

class Program
{   
    static void Main(string[] args)
    {
        string comPort = args.Length > 0 ? args[0] : "COM3";
        int baudRate = args.Length > 1 && int.TryParse(args[1], out int parsedBaud)
            ? parsedBaud
            : 115200;

        using SerialPort arduino = new(comPort, baudRate)
        {
            ReadTimeout = 500,
            WriteTimeout = 500,
            DtrEnable = true,
            RtsEnable = true
        };

        try
        {
            arduino.Open();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Gagal membuka {comPort}: {ex.Message}");
            Console.WriteLine("Port tersedia:");

            foreach (string port in SerialPort.GetPortNames())
                Console.WriteLine("- " + port);

            Console.WriteLine("\nContoh: program.exe COM5 115200");
            return;
        }

        Console.WriteLine($"Port {comPort} terbuka pada {baudRate} baud.");
        Console.WriteLine("Waiting for remote...");
        Console.WriteLine("\n=== KONTROL VOLUME/MEDIA ===");
        Console.WriteLine("88BDC17E = Play/Pause");
        Console.WriteLine("BA45FF00 = Close Window (Alt+F4)");
        Console.WriteLine("BF40FF00 = Volume Up");
        Console.WriteLine("BB44FF00 = Volume Down");
        Console.WriteLine("BC43FF00 = Mute");
        Console.WriteLine("EA15FF00 = Next");
        Console.WriteLine("F807FF00 = Previous");
        Console.WriteLine("\n=== FILE EXPLORER ===");
        Console.WriteLine("00FF6897 = Buka File Explorer");
        Console.WriteLine("00FF629D = Panah atas");
        Console.WriteLine("00FFA857 = Panah bawah");
        Console.WriteLine("00FF22DD = Panah kiri");
        Console.WriteLine("00FFC23D = Panah kanan");
        Console.WriteLine("00FF02FD = Enter / Pilih");
        Console.WriteLine("BD42FF00 = Tab");
        Console.WriteLine("B946FF00 = Back (Alt+Kiri)");
        Console.WriteLine("\n=== BUKA APLIKASI ===");
        Console.WriteLine("(Gunakan tombol remote lain untuk membuka app)\n");

        var inputSimulator = new InputSimulator();

        while (true)
        {
            try
            {
                string data = NormalizeIrCode(arduino.ReadLine());
                Console.WriteLine("Button: " + data);

                // ===== VOLUME & MEDIA =====
                // Play/Pause
                if (data == "B847FF00")
                {
                    Console.WriteLine("→ Play/Pause");
                    inputSimulator.Keyboard.KeyPress(VirtualKeyCode.MEDIA_PLAY_PAUSE);
                }
                // Close active window (Alt+F4)
                else if (data == "BA45FF00")
                {
                    Console.WriteLine("→ Menutup jendela aktif (Alt+F4)");
                    inputSimulator.Keyboard.ModifiedKeyStroke(
                        VirtualKeyCode.MENU,
                        VirtualKeyCode.F4);
                }
                // Volume Up
                else if (data == "BF40FF00")
                {
                    Console.WriteLine("→ Volume Up");
                    inputSimulator.Keyboard.KeyPress(VirtualKeyCode.VOLUME_UP);
                }
                // Volume Down
                else if (data == "BB44FF00")
                {
                    Console.WriteLine("→ Volume Down");
                    inputSimulator.Keyboard.KeyPress(VirtualKeyCode.VOLUME_DOWN);
                }
                // Mute
                else if (data == "BC43FF00")
                {
                    Console.WriteLine("→ Mute");
                    inputSimulator.Keyboard.KeyPress(VirtualKeyCode.VOLUME_MUTE);
                }

                // ===== FILE EXPLORER NAVIGASI KEYBOARD =====
                if (data == "F708FF00")
                {
                    Console.WriteLine("→ Membuka File Explorer");
                    OpenFileExplorer();
                }
                else if (data == "AD52FF00")
                {
                    inputSimulator.Keyboard.KeyPress(VirtualKeyCode.UP);
                }
                else if (data == "B54AFF00")
                {
                    inputSimulator.Keyboard.KeyPress(VirtualKeyCode.DOWN);
                }
                else if (data == "E916FF00")
                {
                    inputSimulator.Keyboard.KeyPress(VirtualKeyCode.LEFT);
                }
                else if (data == "E619FF00")
                {
                    inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RIGHT);
                }
                else if (data == "F609FF00")
                {
                    inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                }
                else if (data == "BD42FF00")
                {
                    Console.WriteLine("→ Tab");
                    inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
                }
                else if (data == "B946FF00")
                {
                    Console.WriteLine("→ Kembali (Alt+Kiri)");
                    inputSimulator.Keyboard.ModifiedKeyStroke(
                        VirtualKeyCode.MENU,
                        VirtualKeyCode.LEFT);
                }
                // ===== APLIKASI & WEBSITE =====
                // Spotify
                else if (data == "E718FF00")
                {
                    Console.WriteLine("→ Membuka Spotify...");
                    LaunchApplication("spotify", "C:\\Users\\MSI Thin\\AppData\\Roaming\\Spotify\\Spotify.exe");
                }
                // VLC
                else if (data == "F20DFF00")
                {
                    Console.WriteLine("→ Membuka Media Player...");
                    LaunchWindowsApp("Microsoft.ZuneMusic_8wekyb3d8bbwe!Microsoft.ZuneMusic");
                }
                // Google Chrome
                else if (data == "F30CFF00")
                {
                    Console.WriteLine("→ Membuka Chrome...");
                    LaunchApplication("chrome", "https://www.google.com");
                }
                // YouTube
                else if (data == "A15EFF00")
                {
                    Console.WriteLine("→ Membuka shortcut YouTube di Desktop...");
                    OpenYouTubeShortcut();
                }
                // Next Track
                else if (data == "EA15FF00")
                {
                    Console.WriteLine("→ Next Track");
                    inputSimulator.Keyboard.KeyPress(VirtualKeyCode.MEDIA_NEXT_TRACK);
                }
                // Previous Track (Kembali)
                else if (data == "F807FF00")
                {
                    Console.WriteLine("→ Previous Track / Kembali");
                    inputSimulator.Keyboard.KeyPress(VirtualKeyCode.MEDIA_PREV_TRACK);
                }
                else
                {
                    Console.WriteLine("→ Tombol tidak dikenali");
                }
            }
            catch (TimeoutException)
            {
                // Tidak ada data IR; terus tunggu tombol berikutnya.
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("Port serial terputus.");
                return;
            }
        }
    }

    static string NormalizeIrCode(string code)
    {
        string normalized = code.Trim();
        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[2..];

        return normalized.PadLeft(8, '0').ToUpperInvariant();
    }

    static void OpenFileExplorer()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Gagal membuka File Explorer: {ex.Message}");
        }
    }

    static void OpenYouTubeShortcut()
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string shortcutPath = Path.Combine(desktopPath, "YouTube.lnk");

        try
        {
            if (File.Exists(shortcutPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = shortcutPath,
                    UseShellExecute = true
                });
                return;
            }

            Console.WriteLine($"Shortcut tidak ditemukan: {shortcutPath}");
            Console.WriteLine("Membuka YouTube melalui browser default...");
            OpenWebsite("https://www.youtube.com/");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Gagal membuka shortcut YouTube: {ex.Message}");
        }
    }

    // Fungsi untuk membuka aplikasi
    static void LaunchApplication(string appName, string url = "")
    {
        try
        {
            // Coba buka dari registry/PATH
            Process.Start(new ProcessStartInfo
            {
                FileName = appName + ".exe",
                UseShellExecute = true
            });
        }
        catch
        {
            // Jika gagal, coba buka melalui website
            if (!string.IsNullOrEmpty(url))
            {
                Console.WriteLine($"  Aplikasi tidak ditemukan, membuka website...");
                OpenWebsite(url);
            }
            else
            {
                Console.WriteLine($"  Gagal membuka {appName}");
            }
        }
    }

    static void LaunchWindowsApp(string appId)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"shell:AppsFolder\\{appId}",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Gagal membuka aplikasi Windows: {ex.Message}");
        }
    }

    // Fungsi untuk membuka website
    static void OpenWebsite(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error: {ex.Message}");
        }
    }
}
