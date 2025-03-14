using Avalonia.Controls;
using PokeAppi.ViewModels;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Reactive;
using ReactiveUI;
using System.IO;
using System.Linq;
using System.Text.Json;
using PokeAppi.Models;
using System.Collections.ObjectModel;
using Avalonia.Metadata;
using Avalonia.Media.Imaging;
using System.Threading;
using System.Reactive.Linq;
using System.Collections.Generic;
using DynamicData;

namespace PokeAppi.ViewModels;
public class MainViewModel : ViewModelBase
{
    private readonly HttpClient _httpClient = new();
    private const int MaxRetries = 3;
    private const int InitialDelayMilliseconds = 2000;
    private CancellationTokenSource _cancellationTokenSource = new();
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(5);

    private string _searchInput;
    public string SearchInput
    {
        get => _searchInput;
        set => this.RaiseAndSetIfChanged(ref _searchInput, value);
    }

    private string _resultText;
    public string ResultText
    {
        get => _resultText;
        set => this.RaiseAndSetIfChanged(ref _resultText, value);
    }

    private string _textHandler;
    public string TextHandler
    {
        get => _textHandler;
        set => this.RaiseAndSetIfChanged(ref _textHandler, value);
    }

    private Bitmap _pokemonImage;
    public Bitmap PokemonImage
    {
        get => _pokemonImage;
        set => this.RaiseAndSetIfChanged(ref _pokemonImage, value);
    }

    public ObservableCollection<Pokemon> Pokemons { get; set; } = new();

    public ReactiveCommand<Unit, Unit> SearchCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public MainViewModel()
    {
        SearchCommand = ReactiveCommand.CreateFromTask(FetchPokeApi);
        CancelCommand = ReactiveCommand.Create(CancelSearch);


        this.WhenAnyValue(x => x.SearchInput)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .DistinctUntilChanged()
            .Where(query => !string.IsNullOrWhiteSpace(query))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async query => await FetchPokeApi());
    }

    public void CancelSearch()
    {
        if (!_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
            TextHandler = "Search cancelled.";
            ResultText = "";
            Pokemons.Clear();

            while (_semaphore.CurrentCount < 5)
            {
                _semaphore.Release();
            }
        }
    }
    public async Task FetchPokeApi()
    {
        if (string.IsNullOrWhiteSpace(SearchInput))
        {
            ResultText = "Please enter Pokémon names or IDs separated by commas.";
            TextHandler = "";
            return;
        }

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        ResultText = "";
        TextHandler = "Searching...";
        PokemonImage = null;
        Pokemons.Clear();

        var pokemonNames = SearchInput
            .ToLower()
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(name => name.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .ToList();

        if (!pokemonNames.Any())
        {
            TextHandler = "No valid Pokémon names provided.";
            return;
        }

        var pokemonResults = new List<Pokemon>();

        foreach (var name in pokemonNames)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var pokemon = await GetPokemonDetails(name, cancellationToken);
                if (pokemon != null)
                    if (pokemon != null)
                    {
                        pokemonResults.Add(pokemon);               
                    }
            }
            finally
            {
                _semaphore.Release(); // Libera el semáforo
            }
        }

        if (pokemonResults.Any())
        {
            PokemonImage = await LoadImageFromUrl(pokemonResults.First().Sprites.Front_Default);
            Pokemons.AddRange(pokemonResults);
            ResultText = string.Join("\n", pokemonResults.Select(p => $"ID: {p.ID}, Name: {p.Name}, Weight: {p.Weight}"));
            TextHandler = "Pokémon found!";
        }
        else
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                PokemonImage = null;
                Pokemons.Clear();
            });
            TextHandler = "No Pokémon found after multiple attempts.";
        }
    }

    private async Task<Pokemon> GetPokemonDetails(string name, CancellationToken cancellationToken)
    {
        int attempt = 0;
        int delay = InitialDelayMilliseconds;

        while (attempt < MaxRetries)
        {
            try
            {
                string url = $"https://pokeapi.co/api/v2/pokemon/{name}";
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(delay);
                var response = await _httpClient.GetAsync(url, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Error {response.StatusCode}");
                }

                string json = await response.Content.ReadAsStringAsync();
                var pokemon = JsonSerializer.Deserialize<Pokemon>(json);

                return pokemon;
            }
            catch (HttpRequestException ex)
            {
                TextHandler = $"Error fetching Pokémon '{name}': {ex.Message}";
            }
            catch (JsonException ex)
            {
                TextHandler = $"Error parsing Pokémon data for '{name}': {ex.Message}";
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch
            {
                attempt++;
                int retriesLeft = MaxRetries - attempt;

                if (retriesLeft > 0)
                {
                    TextHandler = $"Pokémon '{name}' not found. Retrying in {delay / 1000}s... ({retriesLeft} retries left)";
                    await Task.Delay(delay, cancellationToken);
                    delay *= 2;
                }
                else
                {
                    TextHandler = $"Pokémon '{name}' not found after multiple attempts.";
                }
            }
        }
        return null;
    }

    private async Task<Bitmap> LoadImageFromUrl(string url)
    {
        var response = await _httpClient.GetByteArrayAsync(url);
        using var stream = new MemoryStream(response);
        return new Bitmap(stream);
    }
}
