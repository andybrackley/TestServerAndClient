using System;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientServerSockets
{
  public interface IClientMessage
  {
    TcpClient Client { get; }
  }

  public static class ClientMessages
  {
    public class ClientConnected : IClientMessage
    {
      public ClientConnected(TcpClient client)
      {
        Client = client;
      }

      public TcpClient Client { get; }
    }


    public class ClientDisconnected : IClientMessage
    {
      public ClientDisconnected(TcpClient client)
      {
        Client = client;
      }

      public TcpClient Client { get; }
    }

    public class ClientMessage : IClientMessage
    {
      public ClientMessage(TcpClient fromClient, string message)
      {
        Client = fromClient;
        Message = message;
      }
      public TcpClient Client { get; }
      public string Message { get; }
    }
  }

  public static class ClientMessageHandler
  {
    public static IObservable<IClientMessage> FromClient(TcpClient client)
    {
      return Observable.Create<IClientMessage>(obs =>
      {
        Task.Factory.StartNew(() =>
        {
          try
          {
            byte[] empty = new byte[0];
            bool connected = client.Client.Connected;
            while (client.Client.Connected && connected)
            {
              // Block waiting for message
              int bytesRead = client.Client.Receive(empty, 0, 0, SocketFlags.None);
              connected = client.Client.Available > 0;

              if (connected)
              {
                StringBuilder builder = new StringBuilder();
                int bytesAvailable = client.Client.Available;
                byte[] buffer = new byte[bytesAvailable];

                bytesRead = client.Client.Receive(buffer, 0, bytesAvailable, SocketFlags.None);
                builder.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));

                string str = builder.ToString();
                if (str.ToUpper() == "LOGOUT")
                {
                  obs.OnNext(new ClientMessages.ClientDisconnected(client));
                  connected = false;
                }
                else
                {
                  obs.OnNext(new ClientMessages.ClientMessage(client, builder.ToString()));
                }
              }
            }
            obs.OnCompleted();
          }

          catch (Exception err)
          {
            obs.OnError(err);
          }
        });

        return () =>
        {
          obs.OnNext(new ClientMessages.ClientDisconnected(client));
          client.Dispose();
        };
      });
    }
  }

  public static class ClientConnectionHandler
  {
    public static IObservable<TcpClient> FromListener(TcpListener listener)
    {
      return Observable.Create<TcpClient>(obs =>
      {
        listener.Start();

        Task.Factory.StartNew(() =>
        {
          while (true)
          {
            listener.BeginAcceptTcpClient(
              result =>
              {
                try
                {
                  var client = listener.EndAcceptTcpClient(result);
                  obs.OnNext(client);
                }
                catch (Exception err)
                {
                  Console.WriteLine($"Error in FromListener: {err}");
                }
              }, null);

          }
        });
        
        return () =>
        {
          listener.Stop();
        };
      });
    }
  }
}
