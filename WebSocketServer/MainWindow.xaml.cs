using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using SuperWebSocket;

using WhatDoWeDoNow;
using Microsoft.Xna.Framework;
using System.Threading;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace WebSocketServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        GameWebsocketServer appServer;

        Player1 player1;

        public MainWindow()
        {
            InitializeComponent();

            InitWebsocketServer();


            TheGame.Instance.gameObjects = new GameObjects();
            TheGame.Instance.players = new List<Player>();
            TheGame.Instance.gameState = GameState.Game;
            TheGame.Instance.map = new Map();
            TheGame.Instance.map.Size = new Vector2(500, 500);


            //add a platform

            TheGame.Instance.platform = new Platform(
                    new Vector2(0, 500),
                    new Vector2(4000, 100));
            TheGame.Instance.gameObjects.AddGameObject(TheGame.Instance.platform);



            Vector2 velocity = new Vector2(1, 1) * 5;


            Thread thread = new Thread(() =>
            {
                TimeSpan totalGameTime = new TimeSpan();
                TimeSpan lastUpdateTimeStamp = new TimeSpan();
                bool lastUpdateTimeStampCreated = false;
                while (true)
                {
                    //game server loop





                    //update with take time?

                    
                    TimeSpan elapsedGameTime = new TimeSpan();

                    elapsedGameTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
                    totalGameTime += elapsedGameTime;


                    GameTime gameTime = new GameTime(totalGameTime, elapsedGameTime);

                    foreach (GameWebsocketSession session in appServer.GetAllSessions())
                    {
                        session.Player.Update(gameTime);
                    }

                    if (lastUpdateTimeStampCreated == false || (totalGameTime.TotalSeconds - lastUpdateTimeStamp.TotalSeconds > 0.1))
                    {
                        foreach (GameWebsocketSession session in appServer.GetAllSessions())
                        {
                            syncPlayerPosition(session);
                        }
                        lastUpdateTimeStamp = totalGameTime;
                        lastUpdateTimeStampCreated = true;
                    }





                    //if (loops > 1000000) loops = 0;
                    //++loops;

                    //come from corner to corner.
                    /*
                    player1.setPos(player1.getPos() + velocity);

                    if (player1.getPos().X < 0)
                        velocity.X *= -1;
                    if (player1.getPos().Y < 0)
                        velocity.Y *= -1;

                    if (player1.getPos().X + player1.getWorldRectangle().Width > TheGame.Instance.map.Size.X)
                        velocity.X *= -1;
                    if (player1.getPos().Y + player1.getWorldRectangle().Height > TheGame.Instance.map.Size.Y)
                        velocity.Y *= -1;


                    foreach (GameWebsocketSession session in appServer.GetAllSessions())
                    {
                        session.Send((int)player1.getPos().X + " " + (int)player1.getPos().Y);
                    }
                     * 
                     */


                    Thread.Sleep(15);
                }
            });
            thread.IsBackground = true;
            thread.Start();

            

        }

        private void InitWebsocketServer()
        {
            Console.WriteLine("WebSocketServer is starting!");
            appServer = new GameWebsocketServer();

            SuperSocket.SocketBase.Config.RootConfig r = new SuperSocket.SocketBase.Config.RootConfig();
            SuperSocket.SocketBase.Config.ServerConfig s = new SuperSocket.SocketBase.Config.ServerConfig();
            s.Name = "SuperWebSocket";
            s.ServerTypeName = "WebSocketService";
            s.Ip = "Any";
            s.Port = 2012;
            s.MaxRequestLength = 1024 * 1000 * 1000;
            SuperSocket.SocketEngine.SocketServerFactory f = new SuperSocket.SocketEngine.SocketServerFactory();

            appServer.Setup(r, s, f);

            appServer.NewMessageReceived +=
                new SuperSocket.SocketBase.SessionHandler<GameWebsocketSession, string>(
                    appServer_NewMessageReceived);
            appServer.NewSessionConnected +=
                new SuperSocket.SocketBase.SessionHandler<GameWebsocketSession>(
                    appServer_NewSessionConnected
                    );


            Console.WriteLine();

            //Try to start the appServer
            if (!appServer.Start())
            {
                Console.WriteLine("Failed to start!");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("The server started successfully!");
        }

        private void appServer_NewSessionConnected(GameWebsocketSession session)
        {
            //@TODO collision objects shouldn't be sent!


            //send all others who you are first

            foreach (GameWebsocketSession session2 in appServer.GetAllSessions())
            {
                if (session == session2) continue;

                session2.Send(Newtonsoft.Json.JsonConvert.SerializeObject(GameObjectFactory.to(TheGame.Instance.gameObjects.getIdFor(session.Player), session.Player)));
            }


            //sync all the objects
            foreach (KeyValuePair<int, GameObject> keyValuePair in TheGame.Instance.gameObjects)
            {
                session.Send(Newtonsoft.Json.JsonConvert.SerializeObject(GameObjectFactory.to(keyValuePair.Key, keyValuePair.Value)));
            }

            //let him now which one is him
            session.Send(Newtonsoft.Json.JsonConvert.SerializeObject(
                new WhatDoWeDoNow.JSON.GameObjectYouAre {Id = TheGame.Instance.gameObjects.getIdFor(session.Player)}
            ));

        }

        private static int messagesReceived = 0;
        private void appServer_NewMessageReceived(GameWebsocketSession session, string value)
        {
            dynamic d = JObject.Parse(value);

            Console.WriteLine("Received #" + messagesReceived + " received and type is: " + d.Type);
            ++messagesReceived;

            if (d.Type == (new WhatDoWeDoNow.JSON.GameObjectPositionChange()).Type)
            {
                WhatDoWeDoNow.JSON.GameObjectPositionChange obj = JsonConvert.DeserializeObject<WhatDoWeDoNow.JSON.GameObjectPositionChange>(value);

                foreach (GameWebsocketSession session2 in appServer.GetAllSessions())
                {
                    if (session == session2) continue;

                    session2.Send(value);
                }
            }
            if (d.Type == (new WhatDoWeDoNow.JSON.PlayerChange("bla")).Type)
            {
                WhatDoWeDoNow.JSON.PlayerChange obj = JsonConvert.DeserializeObject<WhatDoWeDoNow.JSON.PlayerChange>(value);

                //apply it on user
                if (obj.Change == "Down")
                    session.Player.playerDownCommand();
                if (obj.Change == "DownEnd")
                    session.Player.playerDownCommandEnd();
                if (obj.Change == "Up")
                    session.Player.playerUpCommand();
                if (obj.Change == "UpEnd")
                    session.Player.playerUpCommandEnd();
                if (obj.Change == "Left")
                    session.Player.playerLeftCommand();
                if (obj.Change == "LeftEnd")
                    session.Player.playerLeftCommandEnd();
                if (obj.Change == "Right")
                    session.Player.playerRightCommand();
                if (obj.Change == "RightEnd")
                    session.Player.playerRightCommandEnd();
                if (obj.Change == "Jump")
                    session.Player.playerJumpCommand();
                if (obj.Change == "JumpEnd")
                    session.Player.playerJumpCommandEnd();


            }
        }

        private void syncPlayerPosition(GameWebsocketSession session)
        {
            if (session.Player.lastPositionTransmitted == session.Player.getPos()) return;

            string toSend = TheGame.Instance.makeGameobjectPositionChangeFromGameObject(session.Player);
            foreach (GameWebsocketSession session2 in appServer.GetAllSessions())
            {
                session2.Send(toSend);
            }

            session.Player.lastPositionTransmitted = session.Player.getPos();
        }
    }
}
