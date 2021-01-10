using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace iuF
{
    public class Streamer
    {
        private string _address;
        private int _port;
        private int _chunk_size;
        private UdpClient _client;
        public Streamer(string address, int port)
        {
            _address = address;
            _port = port;
            _client = new UdpClient();
            _chunk_size = 1500 - 20 - 8;
        }

        public bool TryConnect() {
            try {
                _client.Connect(_address, _port);
                Console.WriteLine("{0}:{1} Connected", _address, _port);
                return true;
            }
            catch (Exception e) {
                Console.WriteLine("Failed to connect to {0}:{1}", _address, _port);
                return false;
            }
        }

        public void SendSkeleton(byte[] joints, ulong timestamp) {
            byte[] buffer;
            int buffer_cursor = 0;
            int buffer_size = 0;

            while (joints.Length - buffer_cursor > 0) {
                buffer_size = _chunk_size;
                if (joints.Length - buffer_cursor < _chunk_size) { buffer_size = joints.Length - buffer_cursor; }
                buffer = new byte[buffer_size];

                Array.Copy(joints, buffer_cursor, buffer, 0, buffer_size);
                //Console.WriteLine("Skeleton [{0}/{1}] sended (buffer size: {2})", buffer_cursor, joints.Length, buffer_size);
                _client.Send(buffer, buffer_size);
                buffer_cursor += buffer_size;
            }

            Console.WriteLine("Skeleton Sended at {0}:{1} [{2}]", _address, _port, timestamp);
        }

        public void SendPixels(byte[] pixels, ulong timestamp) {
            byte[] buffer;
            int buffer_cursor = 0;
            int buffer_size = 0;

            while (pixels.Length - buffer_cursor > 0) {
                buffer_size = _chunk_size;
                if (pixels.Length - buffer_cursor < _chunk_size) { buffer_size = pixels.Length - buffer_cursor; }
                buffer = new byte[buffer_size];

                Array.Copy(pixels, buffer_cursor, buffer, 0, buffer_size);
                //Console.WriteLine("Pixels [{0}/{1}] sended (buffer size: {2})", buffer_cursor, pixels.Length, buffer_size); 
                _client.Send(buffer, buffer_size);
                buffer_cursor += buffer_size;
            }

            Console.WriteLine("Pixels Sended at {0}:{1} [{2}]", _address, _port, timestamp);
        }
    }
}
