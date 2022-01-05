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
        private string game = "b4f2sYXl";

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
                case "giveup": action = "desisto";
                    break;

                case "takeback": action = "engano";
                    break;

                case "accept": action = "accengano";
                    break;

                case "refuse": action = "rejengano";
                    break;

                case "clap": action = "envia";
                    break;
            }

            if (action  == "desisto")
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
            else if (action == "engano")
            {
                string message = "yes";
                Task<String> send_msg = Task.Run(() => TakeBack(game, message));
                String result = send_msg.Result;
                string auxok = @"""ok""";
                string auxtrue = "true";
                string aux = "{" + auxok + ":" + auxtrue + "}";
                if (result.Equals(aux))
                {
                    result = "{'ok' : 'Pedido enviado'}";
                }
                await mmic.Send(lce.NewContextRequest());
                var exNot = lce.ExtensionNotification(0 + "", 0 + "", 1, result);
                await mmic.Send(exNot);
            }
            else if (action == "accengano")
            {
                string message = "yes";
                Task<String> send_msg = Task.Run(() => TakeBack(game, message));
                String result = send_msg.Result;
                string auxok = @"""ok""";
                string auxtrue = "true";
                string aux = "{" + auxok + ":" + auxtrue + "}";
                if (result.Equals(aux))
                {
                    result = "{'ok' : 'Pedido aceite'}";
                }
                await mmic.Send(lce.NewContextRequest());
                var exNot = lce.ExtensionNotification(0 + "", 0 + "", 1, result);
                await mmic.Send(exNot);
            }
            else if (action == "rejengano")
            {
                string message = "no";
                Task<String> send_msg = Task.Run(() => TakeBack(game, message));
                String result = send_msg.Result;
                string auxok = @"""ok""";
                string auxtrue = "true";
                string aux = "{" + auxok + ":" + auxtrue + "}";
                if (result.Equals(aux))
                {
                    result = "{'ok' : 'Pedido rejeitado'}";
                }
                await mmic.Send(lce.NewContextRequest());
                var exNot = lce.ExtensionNotification(0 + "", 0 + "", 1, result);
                await mmic.Send(exNot);
            }
            else if (action == "envia")
            {
                string message = "Excelente jogada";
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

    }
}
