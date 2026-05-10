using NAudio.Wave;
using System.Text.Json;

namespace BreakPlayer;

class Program
{
    static int _indeks = 0;
    static string _ostatniFolder = "";

    static WaveOutEvent? outputDevice;
    static AudioFileReader? audioFile;

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var bazowa = AppDomain.CurrentDomain.BaseDirectory;

        var harmonogram = WczytajHarmonogram(bazowa);
        var dni = WczytajDni(bazowa);

        Console.WriteLine("🎵 BreakPlayer uruchomiony!");
        Console.WriteLine("💡 Wpisz 0-100 aby zmienić głośność");

        int aktualnaGlosnosc = 20;

        _ = Task.Run(() =>
        {
            while (true)
            {
                var linia = Console.ReadLine();

                if (int.TryParse(linia, out int poziom) && poziom >= 0 && poziom <= 100)
                {
                    aktualnaGlosnosc = poziom;

                    if (audioFile != null)
                        audioFile.Volume = aktualnaGlosnosc / 100f;

                    Console.WriteLine($"🔊 Głośność: {poziom}");
                }
                else if(linia == "next")
                {
                    outputDevice.Stop();
                }
                
            }
        });

        bool bylaPrzerwa = false;

        while (true)
        {
            harmonogram = WczytajHarmonogram(bazowa);
            dni = WczytajDni(bazowa);

            var teraz = TimeOnly.FromDateTime(DateTime.Now);
            var dzisiaj = DateTime.Now.DayOfWeek.ToString();

            if (_ostatniFolder != dzisiaj)
            {
                _indeks = 0;
                _ostatniFolder = dzisiaj;
                Console.WriteLine("🌅 Nowy dzień — reset kolejności piosenek");
            }

            if (!dni.ContainsKey(dzisiaj))
            {
               
                await Task.Delay(1000);
                continue;
            }

            var folder = Path.Combine(bazowa, "muzyka", dni[dzisiaj]);

            if (!Directory.Exists(folder))
            {
                Console.WriteLine($"⚠️ Brak folderu: {folder}");
                await Task.Delay(1000);
                continue;
            }

            var pliki = Directory.GetFiles(folder, "*.mp3").OrderBy(f => f).ToArray();

            if (pliki.Length == 0)
            {
                Console.WriteLine($"⚠️ Brak plików MP3 w: {folder}");
                await Task.Delay(1000);
                continue;
            }

            var przerwa = harmonogram.FirstOrDefault(p =>
                teraz >= TimeOnly.Parse(p.Start) && teraz < TimeOnly.Parse(p.End));

            if (przerwa != null)
            {
                bylaPrzerwa = true;

                if (outputDevice == null || outputDevice.PlaybackState != PlaybackState.Playing)
                {
                    Play(pliki[_indeks % pliki.Length], aktualnaGlosnosc);

                    Console.WriteLine($"▶️ {_indeks % pliki.Length + 1}/{pliki.Length} {Path.GetFileName(pliki[_indeks % pliki.Length])}");

                    _indeks++;
                }
            }
            else if (bylaPrzerwa && outputDevice != null)
            {
                Console.WriteLine("⏹️ Koniec przerwy — ściszam...");
                await StopujStopniowo(aktualnaGlosnosc);
                bylaPrzerwa = false;
            }

            await Task.Delay(1000);
        }
    }

    static void Play(string plik, int glosnosc)
    {
        StopImmediate();

        audioFile = new AudioFileReader(plik);
        audioFile.Volume = glosnosc / 100f;

        outputDevice = new WaveOutEvent();
        outputDevice.Init(audioFile);
        outputDevice.Play();
    }

    static async Task StopujStopniowo(int glosnosc)
    {
        if (audioFile == null) return;

        for (int g = glosnosc; g >= 0; g -= 2)
        {
            audioFile.Volume = g / 100f;
            await Task.Delay(200);
        }

        StopImmediate();
        Console.WriteLine("⏹️ Zatrzymano");
    }

    static void StopImmediate()
    {
        outputDevice?.Stop();
        outputDevice?.Dispose();
        audioFile?.Dispose();

        outputDevice = null;
        audioFile = null;
    }

    static List<Przerwa> WczytajHarmonogram(string bazowa)
    {
        try
        {
            var json = File.ReadAllText(Path.Combine(bazowa, "harmonogram.json"));
            return JsonSerializer.Deserialize<List<Przerwa>>(json) ?? new();
        }
        catch
        {
            Console.WriteLine("⚠️ Błąd wczytywania harmonogram.json");
            return new();
        }
    }

    static Dictionary<string, string> WczytajDni(string bazowa)
    {
        try
        {
            var json = File.ReadAllText(Path.Combine(bazowa, "dni.json"));
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        catch
        {
            Console.WriteLine("⚠️ Błąd wczytywania dni.json");
            return new();
        }
    }
}