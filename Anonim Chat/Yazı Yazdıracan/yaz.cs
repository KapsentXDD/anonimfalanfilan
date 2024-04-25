using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

public class Program
{
    private DiscordSocketClient _client;

    public static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

    public async Task RunBotAsync()
    {
        _client = new DiscordSocketClient();
        _client.Log += Log;

        await _client.LoginAsync(TokenType.Bot, "");

        await _client.StartAsync();

        _client.Ready += ClientReady;

        await Task.Delay(-1);
    }

    private Task Log(LogMessage arg)
    {
        Console.WriteLine(arg);
        return Task.CompletedTask;
    }

    private async Task ClientReady()
    {
        ulong guildId = ; // Your guild ID "" girme salak gibi
        ulong channelId = ; // Your channel ID

        var guild = _client.GetGuild(guildId);
        var channel = guild.GetTextChannel(channelId);

        var builder = new ComponentBuilder()
            .WithButton("EŞLEŞME BUL", "start_chat");

        await channel.SendMessageAsync("**ANONİM SOHBET NEDİR?**\r\n⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯\r\n" +
            "Tüm konuşmanızın gizli olduğu bir ortamda tanımadığınız kişilerle eşleşip keyifli sohbetler yapabileceksiniz.\r\n" +
            "İster uzun ve anlamlı bir sohbet, ister kısa ve keyifli bir muhabbet olsun, burada her türlü sohbete yer var.\r\n\r\n" +
            "**Tabii ki bazı kuralları da var:**\r\n\r\n" +
            ":exclamation:  Öncelikle, sohbet ettiğiniz kişiye kesinlikle saygılı olmalısınız.\r\n" +
            ":exclamation:  Küfür, argo, cinsel konuşmalar kesinlikle yapmamalısınız.\r\n" +
            ":exclamation:  Karşı tarafın rahatsız olacağı davranışlar içine girmemelisiniz.\r\n\r\n" +
            ":exclamation:  Eğer ki bir şikayetiniz varsa ve rahatsız edici şeyler görürseniz, sohbetin her hangi bir anında **.şikayet** yazıp görüşmeyi sonlandırabilir ve sohbet geçmişini bize iletebilirsiniz.\r\n" +
            ":exclamation:  Konuşmalarınız incelenip destek ekibi tarafından gerekli işlemler uygulanacaktır. Bunun dışında kesinlikle hiç kimse ne konuştuğunuzu göremez.\r\n\r\n" +
            "Aşağıdaki **EŞLEŞME BUL** butonuna basarak bekleme sırasına girebilirsiniz. Keyifli sohbetler :wink:\r\n" +
            "⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯", components: builder.Build());
    }
}
