# 🎵 BreakPlayer

Program do automatycznego odtwarzania muzyki podczas przerw szkolnych.
Gra muzykę według harmonogramu, różne gatunki w różne dni tygodnia.

---

## Wymagania

- .NET 10
- mpg123

Instalacja mpg123 na Fedorze:
```bash
sudo dnf install mpg123
```

---

## Struktura folderów
```
bin/Debug/net10.0/
├── BreakPlayer.exe
├── harmonogram.json
├── dni.json
└── muzyka/
    ├── pop/
    │   ├── piosenka1.mp3
    │   └── piosenka2.mp3
    ├── rock/
    │   └── piosenka3.mp3
    └── disco/
        └── piosenka4.mp3
```

---

## Konfiguracja

### harmonogram.json
Lista przerw — godziny startu i końca:
```json
[
    { "Start": "09:55", "End": "10:10" },
    { "Start": "11:45", "End": "12:00" },
    { "Start": "13:30", "End": "13:45" }
]
```

### dni.json
Przypisanie gatunku muzyki do dnia tygodnia:
```json
{
    "Monday":    "pop",
    "Tuesday":   "rock",
    "Wednesday": "disco",
    "Thursday":  "pop",
    "Friday":    "rock"
}
```

> Nazwy dni muszą być po angielsku. Nazwa gatunku musi zgadzać się z nazwą folderu w `muzyka/`.

---

## Uruchomienie
```bash
cd ~/Projects/BreakPlayer/BreakPlayer
dotnet run
```

---

## Jak działa

- Co sekundę sprawdza czy trwa jakaś przerwa z harmonogramu
- Jeśli tak — zaczyna grać muzykę z folderu przypisanego do dzisiejszego dnia
- Piosenki lecą po kolei (posortowane alfabetycznie)
- Program pamięta którą piosenkę skończył na poprzedniej przerwie i kontynuuje od następnej
- Na początku nowego dnia indeks resetuje się do pierwszej piosenki
- Gdy przerwa się kończy głośność stopniowo spada do zera i muzyka się zatrzymuje
- Oba pliki JSON są odczytywane co sekundę — można je edytować bez restartu programu

---

## Zmiana głośności

Wpisz liczbę od 0 do 100 w konsoli podczas działania programu:
```
80
🔊 Głośność: 80
```

---

## Częste problemy

| Problem | Rozwiązanie |
|---|---|
| `mpg123: nie znaleziono polecenia` | `sudo dnf install mpg123` |
| `Brak folderu: .../muzyka/pop` | Utwórz folder i wrzuć pliki mp3 |
| Nie gra w danym dniu | Sprawdź czy dzień jest po angielsku w dni.json |
| Błąd wczytywania JSON | Sprawdź czy JSON jest poprawny — brak przecinka złamie plik |
