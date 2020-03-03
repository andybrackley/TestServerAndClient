using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ClientServerSockets;

namespace TestServer
{
  class Program
  {
    private static void SendMessage(TcpClient client, string message)
    {
      byte[] outStream = System.Text.Encoding.ASCII.GetBytes(message);

      var s = client.GetStream();
      s.Write(outStream);
      s.Flush();
    }

    private static void ProcessClientMessage(IClientMessage message)
    {
      switch (message)
      {
        case ClientMessages.ClientMessage m:
          {
            Console.WriteLine($"Received Message From: '{m.Client.Client.RemoteEndPoint}'='{m.Message}'");
            SendMessage(message.Client, $"Responding to: {m.Message}");
            break;
          }

        case ClientMessages.ClientConnected m:
          {
            break;
          }

        case ClientMessages.ClientDisconnected m:
          {
            Console.WriteLine($"Client: '{m.Client.Client.RemoteEndPoint}' has disconnected");
            break;
          }
      }
    }


    static void Main(string[] args)
    {
      int port = 8888;
      IPAddress ipAdd = IPAddress.Parse("127.0.0.1");
      TcpListener listener = new TcpListener(ipAdd, port);

      var clients = ClientConnectionHandler.FromListener(listener);
      var disp =
        clients
        .Do(cl => 
        {
          Console.WriteLine($"Connection From: '{cl.Client.RemoteEndPoint}'");          
          SendMessage(cl, $"Hello: {cl.Client.RemoteEndPoint}");
        })
        .Select(cl => ClientMessageHandler.FromClient(cl))
        .Merge()
        .Do(msg => 
        { 
          ProcessClientMessage(msg); 
        },
        err =>
        {
          Console.WriteLine($"Error Occurred: {err.Message}");
        })
        .Retry()
        .Subscribe();


      Console.WriteLine($"Waiting for connection on: {port}");
      Console.WriteLine("Enter 'q' to quit");
      Console.ReadLine();

      disp.Dispose();

    }
  }
}
