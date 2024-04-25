using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Bot.AnonymousChat
{
    public class Program
    {
        private readonly ServiceProvider _serviceProvider = new ServiceCollection().BuildServiceProvider();

        // Botun tokenini buraya yazın
        private const string Token = "----------";

        // Botun komut ön eki
        private const string Prefix = ".";

        // Botun bağlı olduğu istemci
        private DiscordSocketClient _client;

        // Komut servisi
        private CommandService _commands;

        // Sohbet eden kullanıcıların eşleşmelerini tutan sözlük
        private readonly Dictionary<ulong, ulong> _matches = new();

        // Sohbet eden kullanıcıların son mesaj attıkları zamanı tutan sözlük
        private readonly Dictionary<ulong, DateTime> _lastMessages = new();

        // Sohbet bitirme ve şikayet etme komutlarının isimleri
        private const string EndCommand = "bitir";
        private const string ReportCommand = "şikayet";

        // Sohbetin otomatik olarak bitmesi için gereken süre (dakika cinsinden)
        private const int Timeout = 15;

        // Şikayet edilen mesajların atılacağı kanalın ID'si
        private const ulong ReportChannelId = 1110598110058643637;

        private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();

            // Olayları tanımla
            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += MessageReceivedAsync;
            _client.InteractionCreated += InteractionCreatedAsync;

            // Token ile giriş yap
            await _client.LoginAsync(TokenType.Bot, Token);
            await _client.StartAsync();

            // Sonsuza kadar çalış
            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private async Task ReadyAsync()
        {
            Console.WriteLine($"{_client.CurrentUser} is connected!");

            // Her 1 dakikada bir sohbetleri kontrol et
            var timer = new System.Timers.Timer(60000);
            timer.Elapsed += CheckChatsAsync;
            timer.Start();

            await Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Channel is not SocketDMChannel channel || channel.Users.All(x => x.Id != 1110535253396828251) || message.Source != MessageSource.User) return;

            if (_matches.ContainsKey(message.Author.Id) || _matches.ContainsValue(message.Author.Id)) // Eğer kullanıcı eşleşmişse
            {
                var partnerId = _matches.ContainsKey(message.Author.Id) ? _matches[message.Author.Id] : _matches.FirstOrDefault(x => x.Value == message.Author.Id).Key; // Eşleştiği kişinin ID'sini al
                var partner = _client.GetUser(partnerId); // Eşleştiği kişiyi bul

                if (partner == null) // Eğer eşleştiği kişi bulunamazsa
                {
                    await message.Channel.SendMessageAsync("Eşleştiğiniz kişi artık mevcut değil. Sohbet sonlandırıldı.");
                    EndChat(message.Author.Id); // Sohbeti bitir
                    return;
                }

                if (message.Content.StartsWith(Prefix)) // Eğer mesaj bir komut ise
                {
                    if (message.Content.StartsWith(Prefix + EndCommand)) // Eğer komut sohbet bitirme komutu ise
                    {
                        await message.Channel.SendMessageAsync("**Sohbeti bitirdiniz.**");
                        await partner.SendMessageAsync("**Eşleştiğiniz kişi sohbeti bitirdi.**");
                        EndChat(message.Author.Id); // Sohbeti bitir
                        return;
                    }

                    if (message.Content.StartsWith(Prefix + ReportCommand)) // Eğer komut şikayet etme komutu ise
                    {
                        await message.Channel.SendMessageAsync("**Şikayetiniz alındı.** Teşekkürler. Sohbet bitirildi!");
                        await partner.SendMessageAsync("**Eşleştiğiniz kişi sizi şikayet etti.** Sohbet bitirildi!");

                        EndChat(message.Author.Id); // Sohbeti bitir

                        if (_client.GetChannel(ReportChannelId) is SocketTextChannel reportChannel)
                            await reportChannel.SendMessageAsync($"<@&1052249602599956592> buraya bak aloooo şikayet var!!!!\r\nŞikayet eden: <@{message.Author.Id}>\r\nŞikayet edilen: <@{partner.Id}>");
                        return;
                    }

                    // Eğer komut tanınmayan bir komut ise
                    await message.Channel.SendMessageAsync("Geçersiz komut.");
                    return;
                }

                await partner.SendMessageAsync($"{message.Content}"); // Eşleştiği kişiye mesajı ilet
                if (_client.GetChannel(ReportChannelId) is SocketTextChannel report)
                    await report.SendMessageAsync(embed: new EmbedBuilder().WithDescription($"<@{message.Author.Id}>, <@{partner.Id}>'e **{message.Content}** yazdı.").Build());

                _lastMessages[message.Author.Id] = DateTime.Now; // Son mesaj zamanını güncelle

                return;
            }

            if (message.Content.StartsWith(Prefix)) // Eğer mesaj bir komut ise
            {
                var argPos = 0;
                var context = new SocketCommandContext(_client, message as SocketUserMessage);

                if (!(((SocketUserMessage)message).HasStringPrefix(Prefix, ref argPos) || ((SocketUserMessage)message).HasMentionPrefix(_client.CurrentUser, ref argPos)))
                    return;

                var result = await _commands.ExecuteAsync(context, argPos, _serviceProvider);

                if (!result.IsSuccess)
                    await context.Channel.SendMessageAsync(result.ErrorReason);
            }
        }

        private async Task InteractionCreatedAsync(SocketInteraction interaction)
        {
            if (interaction is SocketMessageComponent component) // Eğer etkileşim bir bileşen ise
            {
                if (component.Data.CustomId == "start_chat") // Eğer bileşen sohbet başlat butonu ise
                {
                    if (_matches.ContainsKey(component.User.Id) || _matches.ContainsValue(component.User.Id)) // Eğer kullanıcı zaten eşleşmişse
                    {
                        await component.RespondAsync("Zaten bir sohbette bulunuyorsunuz.", ephemeral: true);
                        return;
                    }

                    try
                    {
                        await component.User.SendMessageAsync("Anonim sohbete hoş geldiniz. Biriyle eşleştiğinizde bildirim alacaksınız!\r\n\r\nEşleştikten sonra sohbet bitirmek isterseniz **.bitir**, eğer şikayet etmek isterseniz **.şikayet** yazın.\r\n\r\n" +
                                                              "Sohbet başladıktan sonra 15 dakika boyunca mesaj atmazsanız sohbet otomatik olarak bitirilecektir.\r\n⁣");

                        var availableUsers = _matches.Keys.Where(x => _matches[x] == 0).ToList(); // Eşleşmemiş kullanıcıları bul

                        if (availableUsers.Count == 0) // Eğer hiç eşleşmemiş kullanıcı yoksa
                        {
                            _matches.Add(component.User.Id, 0); // Kullanıcıyı eşleştirilmeyi bekleyenler listesine ekle
                            return;
                        }

                        var randomUser = availableUsers[new Random().Next(availableUsers.Count)]; // Rastgele bir eşleştirilmeyi bekleyen kullanıcı seç

                        _matches[randomUser] = component.User.Id; // Kullanıcıları eşleştir

                        var partner1 = component.User;
                        var partner2 = _client.GetUser(randomUser);

                        await partner1.SendMessageAsync("**EŞLEŞME BULUNDU** :partying_face: Yazdığınız mesajlar eşleştiğiniz kişiye anonim bir şekilde iletilecek.\r\n\r\nİlk mesajı atmaya ne dersin?\r\n⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯");
                        await partner2.SendMessageAsync("**EŞLEŞME BULUNDU** :partying_face: Yazdığınız mesajlar eşleştiğiniz kişiye anonim bir şekilde iletilecek.\r\n\r\nİlk mesajı atmaya ne dersin?\r\n⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯");

                        _lastMessages[partner1.Id] = DateTime.Now;
                        _lastMessages[partner2.Id] = DateTime.Now; // Son mesaj zamanlarını güncelle
                    }
                    catch { }
                }
            }
        }

        private void CheckChatsAsync(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (var pair in _matches.ToList()) // Eşleşmeleri döngüye al
            {
                if (pair.Value == 0) continue; // Eşleşmemiş kullanıcıları atla

                var partner1 = _client.GetUser(pair.Key);
                var partner2 = _client.GetUser(pair.Value);

                if (partner1 == null || partner2 == null) // Eğer kullanıcılardan biri bulunamazsa
                {
                    EndChat(pair.Key); // Sohbeti bitir
                    continue;
                }

                if ((DateTime.Now - _lastMessages[pair.Key]).TotalMinutes > Timeout || (DateTime.Now - _lastMessages[pair.Value]).TotalMinutes > Timeout) // Eğer son mesaj zamanından beri belirlenen süre geçtiyse
                {
                    partner1.SendMessageAsync("**Sohbet otomatik olarak sonlandırıldı.**");
                    partner2.SendMessageAsync("**Sohbet otomatik olarak sonlandırıldı.**");
                    EndChat(pair.Key); // Sohbeti bitir
                }
            }
        }

        private void EndChat(ulong userId) // Sohbeti bitiren fonksiyon
        {
            if (_matches.ContainsKey(userId) || _matches.ContainsValue(userId)) // Eğer kullanıcı eşleşmişse
            {
                _matches.Remove(_matches.ContainsKey(userId)
                    ? userId
                    : _matches.FirstOrDefault(x => x.Value == userId).Key);
            }
        }
    }
}