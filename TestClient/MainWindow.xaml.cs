using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Windows;

using ClientServerSockets;

namespace TestClient
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private TcpClient _client;
    private readonly ObservableCollection<string> _messages = new ObservableCollection<string>();

    public MainWindow()
    {
      InitializeComponent();
      DataContext = this;

      btnConnect_Click(this, new RoutedEventArgs());
    }

    private void SendMessage(string message)
    {
      byte[] outStream = System.Text.Encoding.ASCII.GetBytes(message);

      var s = _client.GetStream();
      s.Write(outStream);
      s.Flush();
    }

    private void btnConnect_Click(object sender, RoutedEventArgs e)
    {
      if(_client != null)
      {
        btnDisconnect_Click(this, e);
      }

      _client = new TcpClient();
      _client.Connect("127.0.0.1", 8888);

      ClientMessageHandler
        .FromClient(_client)
        .ObserveOnDispatcher()
        .Subscribe(msg =>
        {
          var clMessage = msg as ClientMessages.ClientMessage;
          if (clMessage != null)
          {
            _messages.Add(clMessage.Message);
          }
        },
        err =>
        {
          _messages.Add("Disconnected");
          DisposeClient();
        },
        () => 
        {
          _messages.Add("RX Stream Terminated");
          DisposeClient();
        });
    }

    public IEnumerable<string> ServerMessages => _messages;

    private void btnMessage_Click(object sender, RoutedEventArgs e)
    {
      SendMessage($"Message From Client At: {System.DateTime.Now}");
    }

    private void btnDisconnect_Click(object sender, RoutedEventArgs e)
    {
      _messages.Add("LOCAL: Sending Logout Message");

      if (_client != null)
      {
        SendMessage("Logout");
      }

      _messages.Add("LOCAL: Sent Logout Message");
    }

    private void DisposeClient()
    {
      if (_client != null)
      {
        _messages.Add("LOCAL: Disposed Of Client");

        _client.Close();
        _client.Dispose();
        _client = null;
      }
    }
  }
}
