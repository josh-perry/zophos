using System.Net;
using System.Net.Sockets;
using System.Text;
using FlatBuffers;
using Zophos.Data;

public class Program
{
    public static void Main()
    {
        var ip = new IPEndPoint(IPAddress.Any, 22122);
        var socket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram, ProtocolType.Udp);

        socket.Bind(ip);

        var sender = new IPEndPoint(IPAddress.Any, 0);
        var remote = (EndPoint)sender;

        while (true)
        {
            var receivedData = new byte[1024];
            var receivedDataLength = socket.ReceiveFrom(receivedData, ref remote);

            var byteBuffer = new ByteBuffer(receivedData);
            var testObject = TestObject.GetRootAsTestObject(byteBuffer);
            
            socket.SendTo(receivedData, receivedDataLength, SocketFlags.None, remote);
        }
    }
}