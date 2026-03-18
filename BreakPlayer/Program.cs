using NetCoreAudio;
using System.Text.Json;

namespace BreakPlayer;

class Program
{
    // aktualny indeks piosenki - pamietamy miedzy przerwami
    static int _indeks = 0;
    // zapamietujemy dzien tygodnia zeby wiedziec kiedy resetowac indeks
    static string _ostatniFolder = "";

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // sciezka do folderu gdzie jest program
        var bazowa = AppDomain.CurrentDomain.BaseDirectory;
        var player = new Player();

        // wczytaj pliki konfiguracyjne
        var harmonogram = WczytajHarmonogram(bazowa);
        var dni = WczytajDni(bazowa);

        Console.WriteLine("🎵 BreakPlayer uruchomiony!");
        Console.WriteLine("💡 Wpisz 0-100 aby zmienić głośność");

        int aktualnaGlosnosc = 80;
        await player.SetVolume((byte)aktualnaGlosnosc);

        // osobny watek ktory caly czas czeka na wpisanie glosnosci
        // dzieki temu muzyka gra i mozna wpisywac jednoczesnie
        _ = Task.Run(async () =>
        {
            while (true)
            {
                var linia = Console.ReadLine();
                if (int.TryParse(linia, out int poziom) && poziom >= 0 && poziom <= 100)
                {
                    aktualnaGlosnosc = poziom;
                    await player.SetVolume((byte)aktualnaGlosnosc);
                    Console.WriteLine($"🔊 Głośność: {poziom}");
                }
            }
        });

        // czy w tej chwili trwa lub trwala przerwa
        bool bylaPrezerwa = false;

        while (true)
        {
            // odswiezamy konfiguracje co sekunde - mozna zmienic json bez restartu
            harmonogram = WczytajHarmonogram(bazowa);
            dni = WczytajDni(bazowa);

            var teraz = TimeOnly.FromDateTime(DateTime.Now);
            var dzisiaj = DateTime.Now.DayOfWeek.ToString();

            // jezeli zmienil sie dzien to zaczynamy piosenki od nowa
            if (_ostatniFolder != dzisiaj)
            {
                _indeks = 0;
                _ostatniFolder = dzisiaj;
                Console.WriteLine("🌅 Nowy dzień — reset kolejności piosenek");
            }

            // jezeli dzis nie ma przypisanej kategorii to nic nie gramy
            if (!dni.ContainsKey(dzisiaj))
            {
                if (player.Playing) await StopujStopniowo(player, aktualnaGlosnosc);
                await Task.Delay(1000);
                continue;
            }

            // budujemy sciezke do folderu z muzyka na dzis np. muzyka/pop
            var folder = Path.Combine(bazowa, "muzyka", dni[dzisiaj]);

            if (!Directory.Exists(folder))
            {
                Console.WriteLine($"⚠️ Brak folderu: {folder}");
                await Task.Delay(1000);
                continue;
            }

            // pobieramy posortowane pliki mp3 - OrderBy zapewnia stala kolejnosc
            var pliki = Directory.GetFiles(folder, "*.mp3").OrderBy(f => f).ToArray();

            if (pliki.Length == 0)
            {
                Console.WriteLine($"⚠️ Brak plików MP3 w: {folder}");
                await Task.Delay(1000);
                continue;
            }

            // sprawdzamy czy teraz jest ktoras z przerw z harmonogramu
            var przerwa = harmonogram.FirstOrDefault(p =>
                teraz >= TimeOnly.Parse(p.Start) && teraz < TimeOnly.Parse(p.End));

            if (przerwa != null)
            {
                bylaPrezerwa = true;

                // jezeli przerwa trwa ale nic nie gra - odpal nastepna piosenke
                if (!player.Playing)
                {
                    // przywroc glosnosc bo mogla byc sciszona przez poprzednia przerwe
                    await player.SetVolume((byte)aktualnaGlosnosc);

                    // modulo zeby po ostatniej piosence wracac do pierwszej
                    var plik = pliki[_indeks % pliki.Length];
                    Console.WriteLine($"▶️ [{_indeks % pliki.Length + 1}/{pliki.Length}] {Path.GetFileName(plik)}");
                    await player.Play(plik);
                    _indeks++;
                }
            }
            else if (bylaPrezerwa && player.Playing)
            {
                // przerwa sie skonczyla - sciszamy stopniowo zamiast ucinac
                Console.WriteLine("⏹️ Koniec przerwy — ściszam...");
                await StopujStopniowo(player, aktualnaGlosnosc);
                bylaPrezerwa = false;
            }

            // czekamy sekunde przed kolejnym sprawdzeniem
            await Task.Delay(1000);
        }
    }

    // sciszamy o 5 co 200ms az do zera, potem zatrzymujemy
    static async Task StopujStopniowo(Player player, int aktualnaGlosnosc)
    {
        for (int g = aktualnaGlosnosc; g >= 0; g -= 5)
        {
            await player.SetVolume((byte)Math.Max(g, 0));
            await Task.Delay(200);
        }
        await player.Stop();
        Console.WriteLine("⏹️ Zatrzymano");
    }

    // deserializujemy liste przerw z harmonogram.json
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

    // deserializujemy slownik dzien->folder z dni.json
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

