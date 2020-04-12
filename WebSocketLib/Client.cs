using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace WebSocketLib
{
    public class Client : IDisposable
    {
        private readonly int _messageBufferSize;
        private ClientWebSocket _cws;

        public Client(int bufferSize)
        {
            _messageBufferSize = bufferSize;
        }

        /// <summary>
        /// Connect to WebSocketServer.
        /// </summary>
        public async void Open(string uri)
        {
            try
            {
                _cws = new ClientWebSocket();

                if (_cws.State == WebSocketState.Open) return;
                await _cws.ConnectAsync(new Uri(uri), CancellationToken.None);
                Opened?.Invoke();

                while (_cws.State == WebSocketState.Open)
                {
                    var buff = new ArraySegment<byte>(new byte[_messageBufferSize]);
                    var ret = await _cws.ReceiveAsync(buff, CancellationToken.None);
                    OnMessage?.Invoke(new UTF8Encoding().GetString(buff.Take(ret.Count).ToArray()));
                }

                Closed?.Invoke();
            }
            catch (Exception e)
            {
                Error?.Invoke(e);
            }
        }

        public void Send(string s) =>
            _cws.SendAsync(new ArraySegment<byte>(Encoding.GetEncoding("UTF-8").GetBytes(s.ToArray())), WebSocketMessageType.Text, true, CancellationToken.None);

        /// <summary>
        /// Opened WebSocket Event.
        /// </summary>
        public event OpenedEventHandler Opened;
        public delegate void OpenedEventHandler();

        /// <summary>
        /// Message Receive Event.
        /// </summary>
        public event ReceiveEventHandler OnMessage;
        public delegate void ReceiveEventHandler(string message);

        /// <summary>
        /// Closed WebSocket Event.
        /// </summary>
        public event CloseEventHandler Closed;
        public delegate void CloseEventHandler();

        /// <summary>
        /// Message Receive Event.
        /// </summary>
        public event ErrorEventHandler Error;
        public delegate void ErrorEventHandler(Exception exception);

        /// <summary>
        /// Disconnect from WebSocketServer.
        /// </summary>
        public void Close() => _cws.CloseAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);

        public void Dispose()
        {
            if (_cws.State == WebSocketState.Open) Close();
            _cws?.Dispose();
        }
    }
}
