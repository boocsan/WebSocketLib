using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketLib
{
    public class Server
    {
        /// <summary>
        /// WebSocket インスタンス (クライアント)
        /// </summary>
        private static readonly List<WebSocket> ClientList = new List<WebSocket>();

        /// <summary>
        /// WebSocket サーバー起動
        /// </summary>
        /// <param name="prefixes">接続待ち受け URI 配列 (e.g. http://localhost:8080/ )</param>
        public async void Start(string[] prefixes)
        {
            // 接続待ち受けリスナー
            var httpListener = new HttpListener();
            foreach (var prefix in prefixes) httpListener.Prefixes.Add(prefix);

            // 待ち受け開始
            httpListener.Start();

            while (true)
            {
                var listenerContext = await httpListener.GetContextAsync(); // 接続待機
                if (listenerContext.Request.IsWebSocketRequest) // WebSocket か判定
                {
                    // 認証を実装する場合はここに挿入
                    ProcessRequest(listenerContext);
                }
                else // WebSocketではなかった場合 (HTTP 400で切断)
                {
                    listenerContext.Response.StatusCode = 400;
                    listenerContext.Response.Close();
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        /// <summary>
        /// クライアント全切断
        /// </summary>
        public void CloseAllSession()
        {
            Parallel.ForEach(ClientList, p =>
            {
                if (p.State == WebSocketState.Open)
                    p.CloseAsync(WebSocketCloseStatus.NormalClosure, "", System.Threading.CancellationToken.None);
            });
        }

        /// <summary>
        /// クライアントにメッセージ配信
        /// </summary>
        /// <param name="data">クライアントに送信するデータ</param>
        /// <returns>送信結果(成功:true/失敗:false)</returns>
        public async Task<bool> SendMessage(string data)
        {
            var sendTask = Task.Run(() =>
            {
                try
                {
                    Parallel.ForEach(ClientList, p => p.SendAsync(new ArraySegment<byte>(Encoding.GetEncoding("UTF-8").GetBytes(data.ToArray())), WebSocketMessageType.Text, true, System.Threading.CancellationToken.None));
                    return true;
                }
                catch
                {
                    return false;
                }
            });

            var result = await sendTask;
            return result;
        }

        /// <summary>
        /// WebSocket 接続毎の処理
        /// </summary>
        /// <param name="listenerContext">WebSocket リスナーのコンテキスト</param>
        private static async void ProcessRequest(HttpListenerContext listenerContext)
        {
            // WebSocket の接続完了を待機して WebSocket オブジェクトを取得する
            var ws = (await listenerContext.AcceptWebSocketAsync(null)).WebSocket;
            // クライアント追加
            ClientList.Add(ws);
            // クライアント接続時に Ping 送信
            var now = DateTime.UtcNow;
            var unixTime = (long) (now - new DateTime(1970, 1, 1)).TotalSeconds;
            //var timebase64 = "{\"ping\":\"" + Convert.ToBase64String(Encoding.GetEncoding("UTF-8").GetBytes(unixTime.ToString().ToArray())) + "\"}";
            var pingJson = "{\"ping\":\"" + unixTime + "\"}";
            var pingUnixTime = Encoding.GetEncoding("UTF-8").GetBytes(pingJson);
            await ws.SendAsync(new ArraySegment<byte>(pingUnixTime), WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);

            //メッセージ送受信
            while (ws.State == WebSocketState.Open)
            {
                try
                {
                    var buff = new ArraySegment<byte>(new byte[1024]);

                    //受信待機
                    var ret = await ws.ReceiveAsync(buff, System.Threading.CancellationToken.None);

                    //テキスト受信
                    if (ret.MessageType == WebSocketMessageType.Text)
                    {
                        if (listenerContext.Request.RemoteEndPoint != null)
                            Console.WriteLine("{0}:String Received:{1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), listenerContext.Request.RemoteEndPoint.Address);
                        Console.WriteLine("Message={0}", Encoding.UTF8.GetString(buff.Take(ret.Count).ToArray()));
                    }
                    //クライアント側から切断
                    else if (ret.MessageType == WebSocketMessageType.Close) break;
                }
                catch
                {
                    //クライアント異常切断
                    break;
                }
            }

            //クライアント除外
            ClientList.Remove(ws);
            ws.Dispose();
        }
    }
}
