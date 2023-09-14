using System.IO.Compression;
using System.Text;
using Artisan.V.Client.JsInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Artisan.V.Client.Pages;

public partial class CharacterSheet
{
    private const long MaxPortraitSize = 0xFFFFF; // 1MB
    private string GetPortraitData() => $"data:image/jpeg;base64,{_portraitBase64}";
    private string _portraitBase64 = DefaultPortraitBase64;
    private string? _charsheetFront;
    private string? _charsheetBack;

    private bool _includeFront = true;
    private bool _includeBack = true;

    private string _fileName = string.Empty;

    [Inject]
    public HttpClient HttpClient { get; set; } = null!;
    [Inject]
    public DownloadJsInterop DownloadJsInterop { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _charsheetFront = await HttpClient.GetStringAsync("img/svg/Charsheet-Front.svg");
        _charsheetBack = await HttpClient.GetStringAsync("img/svg/Charsheet-Back.svg");
    }

    private async Task HandleFile(InputFileChangeEventArgs e)
    {
        var formattedImage = await e.File.RequestImageFileAsync("jpeg", 512, 512);
        var imageStream = formattedImage.OpenReadStream(MaxPortraitSize);
        await SetImageAsync(imageStream);
    }

    private async Task SetImageAsync(Stream jpegImageStream)
    {
        var imageStream = new MemoryStream();
        await jpegImageStream.CopyToAsync(imageStream);
        _portraitBase64 = Convert.ToBase64String(imageStream.ToArray());
    }

    private void DropImage()
    {
        _portraitBase64 = DefaultPortraitBase64;
    }

    private async Task DownloadFile()
    {
        if (_charsheetFront is null || _charsheetBack is null)
            return;

        (Stream Content, string Name) downloaded = (_includeFront, _includeBack) switch
        {
            (true, true) => (await CreateBothPageStream(), $"{NormalizedFileName}.zip"),
            (true, false) => (CreatePageStream(CreateFrontPage()), $"{NormalizedFileName}_front.svg"),
            (false, true) => (CreatePageStream(CreateBackPage()), $"{NormalizedFileName}_back.svg"),
            _ => throw new ArgumentOutOfRangeException()
        };

        await DownloadJsInterop.DownloadAsync(downloaded.Content, downloaded. Name);
    }

    private string NormalizedFileName => string.IsNullOrWhiteSpace(_fileName)
        ? "charsheet"
        : _fileName;
    
    private string CreateFrontPage()
        => _charsheetFront!.Replace(DefaultPortraitBase64, _portraitBase64);

    private string CreateBackPage()
        => _charsheetBack!;
    
    private static Stream CreatePageStream(string page)
        => new MemoryStream(Encoding.UTF8.GetBytes(page));

    private async Task<Stream> CreateBothPageStream()
    {
        var resultStream = new MemoryStream();

        var zip = new ZipArchive(resultStream, ZipArchiveMode.Update, leaveOpen: true);
        var frontEntry = zip.CreateEntry($"{NormalizedFileName}_front.svg");
        var frontPage = CreateFrontPage();
        await using (var writer = new StreamWriter(frontEntry.Open()))
        {
            await writer.WriteAsync(frontPage);
        }

        var backEntry = zip.CreateEntry($"{NormalizedFileName}_back.svg");
        var backPage = CreateBackPage();
        await using (var writer = new StreamWriter(backEntry.Open()))
        {
            await writer.WriteAsync(backPage);
        }
        
        zip.Dispose();
        resultStream.Seek(0, SeekOrigin.Begin);

        return resultStream;
    }

    private async Task ChooseDefaultPortrait(ChangeEventArgs e)
    {
        if (e.Value is not string portraitFileName) 
            return;
        
        var imageStream = await HttpClient.GetStreamAsync($"img/jpg/portraits/{portraitFileName}");
        await SetImageAsync(imageStream);
    }

    private static readonly Dictionary<string, string> DefaultPortraitUrls = new()
    {
        { "Человек ♂️", "Человек.М.jpg" }, { "Человек ♀️", "Человек.Ж.jpg" },
        { "Эльф ♂️", "Эльф.М.jpg" }, { "Эльф ♀️", "Эльф.Ж.jpg" },
        { "Дварф ♂️", "Дварф.М.jpg" }, { "Дварф ♀️", "Дварф.Ж.jpg" },
        { "Кицуне ♂️", "Кицуне.М.jpg" }, { "Кицуне ♀️", "Кицуне.Ж.jpg" },
        { "Минас ♂️", "Минас.М.jpg" }, { "Минас ♀️", "Минас.Ж.jpg" },
        { "Серпент ♂️", "Серпент.М.jpg" }, { "Серпент ♀️", "Серпент.Ж.jpg" }
    };

    private const string DefaultPortraitBase64 = // solid white 1 px
        "/9j/4AAQSkZJRgABAQEAYABgAAD/4QBmRXhpZgAATU0AKgAAAAgABAEaAAUAAAABAAAAPgEbAAUAAAABAAAARgEoAAMAAAABAAIAAAExAAIAAAA" +
        "QAAAATgAAAAAAAABgAAAAAQAAAGAAAAABcGFpbnQubmV0IDUuMC45AP/bAEMAAwICAwICAwMDAwQDAwQFCAUFBAQFCgcHBggMCgwMCwoLCw0OEh" +
        "ANDhEOCwsQFhARExQVFRUMDxcYFhQYEhQVFP/bAEMBAwQEBQQFCQUFCRQNCw0UFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUF" +
        "BQUFBQUFBQUFBQUFP/AABEIAAEAAQMBEgACEQEDEQH/xAAfAAABBQEBAQEBAQAAAAAAAAAAAQIDBAUGBwgJCgv/xAC1EAACAQMDAgQDBQUEBAAA" +
        "AX0BAgMABBEFEiExQQYTUWEHInEUMoGRoQgjQrHBFVLR8CQzYnKCCQoWFxgZGiUmJygpKjQ1Njc4OTpDREVGR0hJSlNUVVZXWFlaY2RlZmdoaWp" +
        "zdHV2d3h5eoOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4eLj5OXm5+jp6vHy8/T19vf4+fr/xA" +
        "AfAQADAQEBAQEBAQEBAAAAAAAAAQIDBAUGBwgJCgv/xAC1EQACAQIEBAMEBwUEBAABAncAAQIDEQQFITEGEkFRB2FxEyIygQgUQpGhscEJIzNS8" +
        "BVictEKFiQ04SXxFxgZGiYnKCkqNTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqCg4SFhoeIiYqSk5SVlpeYmZqio6Slpqeo" +
        "qaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2dri4+Tl5ufo6ery8/T19vf4+fr/2gAMAwEAAhEDEQA/AP1TooA//9k=";
}