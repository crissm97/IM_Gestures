using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using mmisharp;
using Newtonsoft.Json;

namespace AppGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MmiCommunication mmiC;

        private MmiCommunication mmiSender;
        private LifeCycleEvents lce;
        private MmiCommunication mmic;
        private string game = "";
        
        // Browser to deploy the game
        private string browser = "firefox.exe";
        //private string browser = "chrome.exe";

        // Lichess token - pako25
        static public string token = "lip_I0iYfH1quLT2GUWlxrAq";

        // Lichess token - TestarCaderno
        //static public string token = "lip_I0iYfH1quLT2GUWlxrAq";

        public MainWindow()
        {
            InitializeComponent();

            mmiC = new MmiCommunication("localhost",8000, "User1", "GUI");
            mmiC.Message += MmiC_Message;
            mmiC.Start();

            // NEW 16 april 2020
            //init LifeCycleEvents..
            lce = new LifeCycleEvents("APP", "TTS", "User1", "na", "command"); // LifeCycleEvents(string source, string target, string id, string medium, string mode
            // MmiCommunication(string IMhost, int portIM, string UserOD, string thisModalityName)
            mmic = new MmiCommunication("localhost", 8000, "User1", "GUI");
        }

        private async void MmiC_Message(object sender, MmiEventArgs e)
        {
            var doc = XDocument.Parse(e.Message);
            var com = doc.Descendants("command").FirstOrDefault().Value;
            dynamic json = JsonConvert.DeserializeObject(com);
            Console.WriteLine(json);
            String action = null;
            switch ((string)json.recognized[1].ToString())
            {
                case "resign": action = "resign";
                    break;

                case "draw": action = "draw";
                    break;

                case "reject": action = "reject";
                    break;

                case "sit": action = "sit";
                    break;

                case "clapping": action = "clap";
                    break;
            }

            // If I pretend to give up
            if (action  == "resign")
            {
                Task<String> resign = Task.Run(() => Resign(game));
                String result = resign.Result;
                string auxok = @"""ok""";
                string auxtrue = "true";
                string aux = "{" + auxok + ":" + auxtrue + "}";
                if (result.Equals(aux))
                {
                    result = "{'ok' : 'Partida terminada'}";
                }
                await mmic.Send(lce.NewContextRequest());
                var exNot = lce.ExtensionNotification(0 + "", 0 + "", 1, result);
                await mmic.Send(exNot);
            }
            // If I pretend to propose a draw or accept a draw proposal
            else if (action == "draw")
            {
                string message = "yes";
                Task<String> send_msg = Task.Run(() => Draw(game, message));
                String result = send_msg.Result;
                string auxok = @"""ok""";
                string auxtrue = "true";
                string aux = "{" + auxok + ":" + auxtrue + "}";
                if (result.Equals(aux))
                {
                    result = "{'ok' : 'Empate'}";
                }
                await mmic.Send(lce.NewContextRequest());
                var exNot = lce.ExtensionNotification(0 + "", 0 + "", 1, result);
                await mmic.Send(exNot);
            }
            // If I pretend to decline a draw proposal
            else if (action == "reject")
            {
                string message = "no";
                Task<String> send_msg = Task.Run(() => Draw(game, message));
                String result = send_msg.Result;
                string auxok = @"""ok""";
                string auxtrue = "true";
                string aux = "{" + auxok + ":" + auxtrue + "}";
                if (result.Equals(aux))
                {
                    result = "{'ok' : 'Empate'}";
                }
                await mmic.Send(lce.NewContextRequest());
                var exNot = lce.ExtensionNotification(0 + "", 0 + "", 1, result);
                await mmic.Send(exNot);
            }
            // If I pretend to send a cheering chat message
            else if (action == "clap") 
            {
                string message = "Excelente jogada!";
                Task<String> send_msg = Task.Run(() => SendMessage(game, message));
                String result = send_msg.Result;
                string auxok = @"""ok""";
                string auxtrue = "true";
                string aux = "{" + auxok + ":" + auxtrue + "}";
                if (result.Equals(aux))
                {
                    result = "{'ok' : 'Mensagem enviada'}";
                }
                await mmic.Send(lce.NewContextRequest());
                var exNot = lce.ExtensionNotification(0 + "", 0 + "", 1, result);
                await mmic.Send(exNot);
            }
            // If I pretend to challenge someone or accept challenge requests
            else if (action == "sit")
            {
                Task<String> list = Task.Run(() => ListChallenges());
                String result = list.Result;
                Console.WriteLine(result);

                string data = "";
                if (result.Contains(@"""in"":[]"))
                {
                    data = "";
                }
                else
                {
                    data = getBetween(result, "id", "url");
                    char[] removeStuff = { '"', ':', ',' };
                    data = data.TrimStart(removeStuff);
                    data = data.TrimEnd(removeStuff);
                    game = data;
                }

                if (String.IsNullOrEmpty(data))
                {
                    Task<String> currentgames = Task.Run(() => SeeCurrentGames());
                    String current = currentgames.Result;
                    if (current.Contains(@"""nowPlaying"":[]"))
                    {
                        string user = "TestarCaderno";
                        Task<String> challenge = Task.Run(() => Challenge(user));
                        string challengeresult = challenge.Result;
                        string challengedata = getBetween(challengeresult, "url", "status");
                        char[] removeStuff = { '"', ':', ',' };
                        string auxgame = getBetween(challengeresult, "id", "url");
                        string newgame = auxgame.TrimEnd(removeStuff);
                        game = newgame.TrimStart(removeStuff);
                        string auxdata = challengedata.TrimEnd(removeStuff);
                        string newdata = auxdata.TrimStart(removeStuff);
                        System.Diagnostics.Process.Start(browser, newdata);
                        await mmic.Send(lce.NewContextRequest());
                        var exNot = lce.ExtensionNotification(0 + "", 0 + "", 1, challengeresult);
                        await mmic.Send(exNot);
                    }
                }
                else
                {
                    Task<String> accept = Task.Run(() => Accept(game));
                    string result2 = accept.Result;
                    string auxok = @"""ok""";
                    string auxtrue = "true";
                    string aux = "{" + auxok + ":" + auxtrue + "}";
                    if (result2.Equals(aux))
                    {
                        result2 = "{'ok' : 'Desafio aceite'}";
                    }
                    System.Diagnostics.Process.Start(browser, "https://lichess.org/" + game);
                    await mmic.Send(lce.NewContextRequest());
                    var exNot = lce.ExtensionNotification(0 + "", 0 + "", 1, result);
                    await mmic.Send(exNot);
                }
            }
        }
        
        // Function that given a string, a starting point and a ending point, returns the charachters in-between
        public static string getBetween(string strSource, string strStart, string strEnd)
        {
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                int Start, End;
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }

            return "";
        }
        
        // Series of Tasks that communicate with the Lichess API to...
        //    ...  move a piece from one to another of given positions
        static async Task<String> MovePiece(String Game, String Move)
        {
            var client = new HttpClient();
            var url = new Uri("https://lichess.org/api/board/game/" + Game + "/move/" + Move);
            var values = new Dictionary<string, string>()
            {
                {"", "" }
            };
            var content = new FormUrlEncodedContent(values);
            client.DefaultRequestHeaders.Authorization
                            = new AuthenticationHeaderValue("Bearer", token);
            var result = await client.PostAsync(url, content);
            string resultContent = await result.Content.ReadAsStringAsync();
            return resultContent;
        }

        //    ... send to the chat a given message
        static async Task<String> SendMessage(String Game, String Message)
        {
            var client = new HttpClient();
            var url = new Uri("https://lichess.org/api/board/game/" + Game + "/chat");
            var values = new Dictionary<string, string>()
            {
                {"text", Message },
                {"room", "player" }
            };
            var content = new FormUrlEncodedContent(values);
            client.DefaultRequestHeaders.Authorization
                            = new AuthenticationHeaderValue("Bearer", token);
            var result = await client.PostAsync(url, content);
            string resultContent = await result.Content.ReadAsStringAsync();
            return resultContent;
        }

        //      ... give up the game
        static async Task<String> Resign(String Game)
        {
            var client = new HttpClient();
            var url = new Uri("https://lichess.org/api/board/game/" + Game + "/resign");
            var values = new Dictionary<string, string>()
            {
                {"", "" }
            };
            var content = new FormUrlEncodedContent(values);
            client.DefaultRequestHeaders.Authorization
                            = new AuthenticationHeaderValue("Bearer", token);
            var result = await client.PostAsync(url, content);
            string resultContent = await result.Content.ReadAsStringAsync();
            return resultContent;
        }

        //      ... invite a given opponent
        static async Task<String> Challenge(String User)
        {
            var client = new HttpClient();
            var url = new Uri("https://lichess.org/api/challenge/" + User);
            var values = new Dictionary<string, string>()
            {
                {"", "" }
            };
            var content = new FormUrlEncodedContent(values);
            client.DefaultRequestHeaders.Authorization
                            = new AuthenticationHeaderValue("Bearer", token);
            var result = await client.PostAsync(url, content);
            string resultContent = await result.Content.ReadAsStringAsync();
            return resultContent;
        }

        //      ... propose a takeback
        static async Task<String> TakeBack(String Game, String Message)
        {
            var client = new HttpClient();
            var url = new Uri("https://lichess.org/api/board/game/" + Game + "/takeback/" + Message);
            var values = new Dictionary<string, string>()
            {
                {"", "" },
            };
            var content = new FormUrlEncodedContent(values);
            client.DefaultRequestHeaders.Authorization
                            = new AuthenticationHeaderValue("Bearer", token);
            var result = await client.PostAsync(url, content);
            string resultContent = await result.Content.ReadAsStringAsync();
            return resultContent;
        }

        //      ... know if there is an active invite to play
        static async Task<String> ListChallenges()
        {
            var client = new HttpClient();
            var url = new Uri("https://lichess.org/api/challenge");
            client.DefaultRequestHeaders.Authorization
                            = new AuthenticationHeaderValue("Bearer", token);
            var result = await client.GetAsync(url);
            string resultContent = await result.Content.ReadAsStringAsync();
            return resultContent;
        }

        //      ... accept an invite
        static async Task<String> Accept(String Accept)
        {
            var client = new HttpClient();
            var url = new Uri("https://lichess.org/api/challenge/" + Accept + "/accept");
            var values = new Dictionary<string, string>()
            {
                {"", "" }
            };
            var content = new FormUrlEncodedContent(values);
            client.DefaultRequestHeaders.Authorization
                            = new AuthenticationHeaderValue("Bearer", token);
            var result = await client.PostAsync(url, content);
            string resultContent = await result.Content.ReadAsStringAsync();
            return resultContent;
        }

        //      ... decline an invite
        static async Task<String> Decline(String Decline)
        {
            var client = new HttpClient();
            var url = new Uri("https://lichess.org/api/challenge/" + Decline + "/decline");
            var values = new Dictionary<string, string>()
            {
                {"", "" }
            };
            var content = new FormUrlEncodedContent(values);
            client.DefaultRequestHeaders.Authorization
                            = new AuthenticationHeaderValue("Bearer", token);
            var result = await client.PostAsync(url, content);
            string resultContent = await result.Content.ReadAsStringAsync();
            return resultContent;
        }

        //      ... cancel a sent invitation
        static async Task<String> Cancel(String Cancel)
        {
            var client = new HttpClient();
            var url = new Uri("https://lichess.org/api/challenge/" + Cancel + "/cancel");
            var values = new Dictionary<string, string>()
            {
                {"", "" }
            };
            var content = new FormUrlEncodedContent(values);
            client.DefaultRequestHeaders.Authorization
                            = new AuthenticationHeaderValue("Bearer", token);
            var result = await client.PostAsync(url, content);
            string resultContent = await result.Content.ReadAsStringAsync();
            return resultContent;
        }

        //      ... propose a draw
        static async Task<String> Draw(String Game, String Message)
        {
            var client = new HttpClient();
            var url = new Uri("https://lichess.org/api/board/game/" + Game + "/draw/" + Message);
            var values = new Dictionary<string, string>()
            {
                {"", "" },
            };
            var content = new FormUrlEncodedContent(values);
            client.DefaultRequestHeaders.Authorization
                            = new AuthenticationHeaderValue("Bearer", token);
            var result = await client.PostAsync(url, content);
            string resultContent = await result.Content.ReadAsStringAsync();
            return resultContent;
        }

        //     ... see if there are games ongoing
        static async Task<String> SeeCurrentGames()
        {
            var client = new HttpClient();
            var url = new Uri("https://lichess.org/api/account/playing");
            client.DefaultRequestHeaders.Authorization
                            = new AuthenticationHeaderValue("Bearer", token);
            var result = await client.GetAsync(url);
            string resultContent = await result.Content.ReadAsStringAsync();
            Console.WriteLine(resultContent);
            return resultContent;
        }

    }
}
