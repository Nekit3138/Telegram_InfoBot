using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using HtmlParser;
using Newtonsoft.Json.Linq;

class Program
{
    public static Int64 counter = 0;
    public static Parser parser;
    public static string link = "https://www.google.com/search?q=";

    static void Main(string[] args)
    {
        parser = new Parser();
        Console.WriteLine("Бот запущен");
        Console.WriteLine("Счетчик сообщений: 0");
        Bot.RunBot();
    }
}

class Bot
{
    private static TelegramBotClient client;
    private static bool is_weather = false;

    private static string startText =
        "Приветствуем вас от всей команды InfoBot!)\n" +
        "/weather для информации о погоде\n" +
        "/ranks для вывода курсов валют";


    public static void RunBot()
    {
        client = new TelegramBotClient("1674625177:AAFyt68T5RjX0JTCCgjwyMEEgD2nFFX7cdM");
        client.OnMessage += BotOnMessageReceived;
        client.OnMessageEdited += BotOnMessageReceived;
        client.StartReceiving();
        Console.ReadKey();
        client.StopReceiving();
    }

    private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
    {
        Program.counter++;
        Console.Clear();
        Console.WriteLine($"Счетчик сообщений: {Program.counter}\n\"Enter\" для завершения работы");
        var message = messageEventArgs.Message;
        if (message?.Type == MessageType.Text)
        {
            if (message.Text == "/start")
            {
                await client.SendTextMessageAsync(message.Chat.Id, startText);
                is_weather = false;
            }
            else if (message.Text == "/weather")
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Введите город");
                is_weather = true;
            }
            else if (message.Text == "/ranks")
            {
                await client.SendTextMessageAsync(message.Chat.Id, BeautyDictToString(GetRanks()));
                is_weather = false;
            }
            else if (is_weather)
            {
                Dictionary<string, string> res;
                try
                {
                    res = GetWeather(message.Text);
                    await client.SendTextMessageAsync(message.Chat.Id, BeautyDictToString(res));
                }
                catch (Exception e)
                {
                    await client.SendTextMessageAsync(message.Chat.Id, "Неверный город");
                }
            }
            else
            {
                client.SendTextMessageAsync(message.Chat.Id, "Неверная команда!");
            }
        }
    }

    public static Dictionary<string, string> GetWeather(string city)
    {
        var client = new HttpClient();
        var response = client.GetAsync("http://api.openweathermap.org/" +
                                       $"data/2.5/find?q={city}&type=like&lang=ru&units=metric&APPID=" +
                                       "632776fe531822c1da881d1ca73aba40").Result.Content.ReadAsStringAsync().Result;
        var json = JToken.Parse(response).SelectToken("list").First;
        var result = new Dictionary<string, string>
        {
            {"Название", json.SelectToken("name").ToString()},
            {"Температура", json.SelectToken("main.temp") + " ℃"},
            {"Ощущается", json.SelectToken("main.feels_like") + " ℃"},
            {
                "Давление",
                Math.Round(float.Parse(json.SelectToken("main.pressure").ToString()) / 133 * 100, 2) + " мм рт.ст."
            },
            {"Влажность", json.SelectToken("main.humidity") + "%"},
            {"Скорость ветра", json.SelectToken("wind.speed") + " м/c"},
            {"Направление ветра", json.SelectToken("wind.deg") + "°"},
            {
                "Дождь",
                json.SelectToken("rain").ToString() == ""
                    ? "Нет"
                    : float.Parse(json.SelectToken("rain.1h").ToString()) * 100 + "%"
            },
            {
                "Снег",
                json.SelectToken("snow").ToString() == ""
                    ? "Нет"
                    : float.Parse(json.SelectToken("snow.1h").ToString()) * 100 + "%"
            },
            {"Облачность", json.SelectToken("clouds.all") + "%"}
        };
        return result;
    }

    private static Dictionary<string, string> GetRanks()
    {
        var result = new Dictionary<string, string>
        {
            {"Доллар к рублю", ""},
            {"Евро к рублю", ""},
            {"Евро к доллару", ""}
        };
        var names = new List<string>
        {
            "Курс доллара к рублю".Replace(" ", "+"),
            "Курс евро к рублю".Replace(" ", "+"),
            "Курс евро к доллару".Replace(" ", "+")
        };
        for (var i = 0; i != 3; i++)
        {
            Program.parser.SetPage(Program.link + names[i]);
            result[result.Keys.ToList()[i]] = Program.parser.Find("class", "DFlfde SwHCTb");
        }

        return result;
    }

    public static string BeautyDictToString(Dictionary<string, string> dictionary)
    {
        return dictionary.Keys.Aggregate("", (current, i) => current + $"{i}:  {dictionary[i]}\n");
    }
}