using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using System.IO.Compression;
using System.Net.Http.Headers;

namespace Dispatch;

public class DispatchPlugin : BasePlugin, IPluginConfig<DispatchConfig>
{
    public override string ModuleName => "Dispatch";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "AI";
    public override string ModuleDescription => "Belirtilen plugin klasörünü zip'leyip upload eder ve indirme linkini konsola yazar.";

    public DispatchConfig Config { get; set; } = new();

    public void OnConfigParsed(DispatchConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        Console.WriteLine($"[Dispatch] Plugin yüklendi. Hedef: '{Config.PluginAdi}'");
        Console.WriteLine($"[Dispatch] Komutlar: !dispatch (chat) | css_dispatch (konsol)");

        AddCommand("css_dispatch", "Plugin klasörünü zip'leyip upload eder", OnDispatchCommand);
        AddCommand("dispatch", "Plugin klasörünü zip'leyip upload eder", OnDispatchCommand);
    }

    [RequiresPermissions("@css/root")]
    public void OnDispatchCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player != null && !AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            command.ReplyToCommand("[Dispatch] Bu komutu kullanma yetkiniz yok.");
            return;
        }

        command.ReplyToCommand("[Dispatch] Islem baslatiliyor...");
        Task.Run(() => RunDispatchAsync(replyToPlayer: player));
    }

    private async Task RunDispatchAsync(CCSPlayerController? replyToPlayer)
    {
        string pluginAdi = Config.PluginAdi;

        if (string.IsNullOrWhiteSpace(pluginAdi))
        {
            PrintResult(replyToPlayer, "[Dispatch] HATA: Config'de 'PluginAdi' bos birakılmıs!");
            return;
        }

        // ModuleDirectory = .../addons/counterstrikesharp/plugins/Dispatch
        // Bir üst klasör  = .../addons/counterstrikesharp/plugins/
        string pluginsDir = Path.GetDirectoryName(ModuleDirectory) ?? ModuleDirectory;
        string hedefKlasor = Path.Combine(pluginsDir, pluginAdi);

        if (!Directory.Exists(hedefKlasor))
        {
            PrintResult(replyToPlayer, $"[Dispatch] HATA: Klasor bulunamadi: {hedefKlasor}");
            return;
        }

        PrintResult(replyToPlayer, $"[Dispatch] '{pluginAdi}' klasoru zip'leniyor...");

        try
        {
            // RAM'de zip oluştur
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                ZipDirectory(archive, hedefKlasor);
            }

            memoryStream.Position = 0;
            byte[] zipBytes = memoryStream.ToArray();

            PrintResult(replyToPlayer, $"[Dispatch] Zip olusturuldu ({zipBytes.Length / 1024} KB). Yukleniyor...");

            // filebin.net'e yükle
            string binId = Guid.NewGuid().ToString("N")[..12];
            string fileName = $"{pluginAdi}.zip";
            string uploadUrl = $"https://filebin.net/{binId}/{fileName}";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "curl/7.68.0");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.Timeout = TimeSpan.FromSeconds(60);

            using var content = new ByteArrayContent(zipBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var response = await httpClient.PostAsync(uploadUrl, content);

            if (response.IsSuccessStatusCode)
            {
                PrintResult(replyToPlayer, "[Dispatch] ========================================");
                PrintResult(replyToPlayer, $"[Dispatch] BASARILI! '{pluginAdi}' yuklendi.");
                PrintResult(replyToPlayer, "[Dispatch] Indirme linki:");
                PrintResult(replyToPlayer, $"[Dispatch] {uploadUrl}");
                PrintResult(replyToPlayer, "[Dispatch] (Link yaklasik 1 gun gecerlidir)");
                PrintResult(replyToPlayer, "[Dispatch] ========================================");
            }
            else
            {
                string body = await response.Content.ReadAsStringAsync();
                PrintResult(replyToPlayer, $"[Dispatch] HATA: Upload basarisiz. HTTP {(int)response.StatusCode}: {body[..Math.Min(body.Length, 200)]}");
            }
        }
        catch (Exception ex)
        {
            PrintResult(replyToPlayer, $"[Dispatch] HATA: {ex.Message}");
            Console.WriteLine($"[Dispatch] Tam hata: {ex}");
        }
    }

    /// <summary>
    /// Verilen klasörü ve tüm alt dosyalarını ZipArchive'e ekler.
    /// Zip içindeki yol: KlasorAdi/dosya.dll vb.
    /// </summary>
    private static void ZipDirectory(ZipArchive archive, string dirPath)
    {
        string? parentDir = Path.GetDirectoryName(dirPath);
        if (parentDir == null) return;

        foreach (string file in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
        {
            string entryName = Path.GetRelativePath(parentDir, file);
            archive.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);
        }
    }

    /// <summary>
    /// Mesajı server console'a yazar; oyuncu mevcutsa konsoluna da iletir.
    /// Server.NextFrame ile thread güvenliği sağlanır.
    /// </summary>
    private void PrintResult(CCSPlayerController? player, string message)
    {
        Console.WriteLine(message);
        if (player != null && player.IsValid)
        {
            Server.NextFrame(() =>
            {
                if (player.IsValid)
                    player.PrintToConsole(message);
            });
        }
    }
}
