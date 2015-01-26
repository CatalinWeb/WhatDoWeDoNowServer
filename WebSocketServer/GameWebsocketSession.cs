using Microsoft.Xna.Framework;
using SuperWebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatDoWeDoNow;

namespace WebSocketServer
{
    public class GameWebsocketSession : WebSocketSession<GameWebsocketSession>
    {
        public WhatDoWeDoNow.Player Player;
        protected override void OnSessionStarted()
        {
            if (Path != "/")
            {
                throw new Exception("what is this connection?");
            }


            Player = new Player1(new Vector2(300, 400), new Vector2(50, 50));

            WhatDoWeDoNow.TheGame.Instance.gameObjects.AddGameObject(Player);
        }

        protected override void OnSessionClosed(SuperSocket.SocketBase.CloseReason reason)
        {
            //let the users know that the player isn't there anymore
            TheGame.Instance.gameObjects.RemoveGameObject(Player);

            base.OnSessionClosed(reason);
        }

        private static int messagesSent = 0;
        public override void Send(string message)
        {

            Console.WriteLine("Sent #" + messagesSent);
            ++messagesSent;

            base.Send(message);
        }
    }
}
