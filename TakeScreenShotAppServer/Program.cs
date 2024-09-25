using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;

public class Program
{
	public static void Main(string[] args)
	{
		Console.WindowHeight = 49;
		Console.WindowWidth = 99;

		Console.WriteLine("Taking screenshot.");
		
		TakeScreenShot();

		Console.WriteLine("Screenshot taken and sent.");
	}

	public static void TakeScreenShot()
	{
		try
		{
			Rectangle bounds = new Rectangle(0, 0, 1920, 1080);
			Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
			using (Graphics g = Graphics.FromImage(bitmap))
			{
				g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
			}

			var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			var clientEndPoint = new IPEndPoint(IPAddress.Loopback, 27001);

			using (MemoryStream ms = new MemoryStream())
			{
				bitmap.Save(ms, ImageFormat.Png);
				byte[] imageBytes = ms.ToArray();

				int size = 32768;  // 32 KB
				int total = (imageBytes.Length + size - 1) / size;

				serverSocket.SendTo(BitConverter.GetBytes(total), clientEndPoint);

				for (int num = 0; num < total; num++)
				{
					int currentSize = Math.Min(size, imageBytes.Length - (num * size));
					byte[] data = new byte[currentSize];

					Buffer.BlockCopy(imageBytes, num * size, data, 0, currentSize);

					bool isContinue = false;

					while (!isContinue)
					{
						byte[] message = BitConverter.GetBytes(num).Concat(data).ToArray();
						serverSocket.SendTo(message, clientEndPoint);

						var arr = new byte[4];
						EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
						serverSocket.ReceiveFrom(arr, ref endPoint);
						int currentNum = BitConverter.ToInt32(arr, 0);

						if (currentNum == num)
						{
							Console.WriteLine($"Chunk {num} acknowledged.");
							isContinue = true;
						}
					}
				}
			}

			Console.WriteLine("Data sent to client.");
		}
		catch (Exception ex)
		{
			Console.WriteLine("Error: " + ex.Message);
		}
	}
}
