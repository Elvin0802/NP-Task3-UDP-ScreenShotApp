using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.Xaml;

namespace TakeSSAppClient;

public partial class MainWindow : Window
{
	public Socket ClientSocket { get; set; }

	public MainWindow()
	{
		InitializeComponent();

		ServerPath = @$"..\..\..\..\TakeScreenShotAppServer\bin\Debug\net8.0\TakeScreenShotAppServer.exe";

		ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		ClientSocket.Bind(new IPEndPoint(IPAddress.Loopback, 27001));

		B = null;
	}

	public Process? Server { get; set; }
	public string ServerPath { get; init; }
	public Bitmap B { get; set; }

	private void StartBtn_Click(object sender, RoutedEventArgs e)
	{
		Server = Process.Start(ServerPath);

		EndPoint ServerEndPoint = new IPEndPoint(IPAddress.Any, 0);
		var totalBuffer = new byte[4];

		ClientSocket.ReceiveFrom(totalBuffer, ref ServerEndPoint);

		int total = BitConverter.ToInt32(totalBuffer, 0);

		List<byte[]> byteList = new List<byte[]>();

		for (int Num = 0; Num < total; Num++)
		{
			bool isRecieved = false;

			while (!isRecieved)
			{
				var arr = new byte[32772];
				int length = ClientSocket.ReceiveFrom(arr, ref ServerEndPoint);

				int receivedNum = BitConverter.ToInt32(arr, 0);

				byte[] data = new byte[length - 4];

				Buffer.BlockCopy(arr, 4, data, 0, length - 4);

				if (receivedNum == Num)
				{
					byteList.Add(data);

					isRecieved = true;

					ClientSocket.SendTo(BitConverter.GetBytes(receivedNum), ServerEndPoint);
				}
			}
		}

		using (MemoryStream ms = new MemoryStream())
		{
			foreach (var i in byteList)
				ms.Write(i, 0, i.Length);

			B = new Bitmap(ms);
			ImageDisplayer.Source = BitmapToBitmapImage(B);
		}
	}

	public BitmapImage BitmapToBitmapImage(Bitmap bitmap)
	{
		using (MemoryStream ms = new MemoryStream())
		{
			bitmap.Save(ms, ImageFormat.Png);
			ms.Seek(0, SeekOrigin.Begin);

			BitmapImage b = new();
			b.BeginInit();
			b.StreamSource = ms;
			b.CacheOption = BitmapCacheOption.OnLoad;
			b.EndInit();
			b.Freeze();

			return b;
		}
	}

	private void SaveBtn_Click(object sender, RoutedEventArgs e)
	{
		if (B is null) return;

		string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
		path += "\\UDP_Screenshots";
		
		if (!File.Exists(path))
			Directory.CreateDirectory(path);

		var d = DateTime.Now;
		var name = $"\\pic_{d.Year}{d.Month}{d.Day}_{d.Hour}{d.Minute}{d.Second}_{d.Millisecond}_ss.png";

		var s = File.Create(path + name);

		MemoryStream ms = new MemoryStream();

		B.Save(ms, ImageFormat.Png);

		s.Write(ms.ToArray());

		MessageBox.Show("Screenshot Saved to Desktop.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
