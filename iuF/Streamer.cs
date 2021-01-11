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
        private int _port_skeleton;
        private int _port_pixels;

        private int _chunk_size;
        private UdpClient _client_skeleton;
        private UdpClient _client_pixels;
        public Streamer(string address, int port_skeleton, int port_pixels)
        {
            _address = address;
            _port_skeleton = port_skeleton;
            _port_pixels = port_pixels;

            _client_skeleton = new UdpClient();
            _client_pixels = new UdpClient();

            _chunk_size = 1500 - 20 - 8;
        }

        public bool TryConnect() {
            try { _client_skeleton.Connect(_address, _port_skeleton); }
            catch (Exception e) { Console.WriteLine("Failed to connect to {0}:{1}", _address, _port_skeleton); return false; }

            try {_client_pixels.Connect(_address, _port_pixels); }
            catch (Exception e) { Console.WriteLine("Failed to connect to {0}:{1}", _address, _port_pixels); return false; }

            Console.WriteLine("{0} Connected\nStreaming:\n * Skeleton on port {1}\n * Pixels on port {2}", _address, _port_skeleton, _port_pixels);
            return true;
        }

        public void SendSkeleton(byte[] joints, ulong timestamp) {
            byte[] buffer;
            byte[] time = BitConverter.GetBytes((Int64)timestamp);

            int buffer_cursor = 0;
            int time_size = time.Length;

            while (joints.Length - buffer_cursor > 0) {
                int buffer_size = _chunk_size;
                if (joints.Length - buffer_cursor + time_size < _chunk_size) { buffer_size = joints.Length - buffer_cursor + time_size; }
                int data_size = buffer_size - time_size;
                buffer = new byte[buffer_size];

                Array.Copy(time, 0, buffer, 0, time_size);
                Array.Copy(joints, buffer_cursor, buffer, 0, data_size);
                _client_skeleton.Send(buffer, buffer_size);
                buffer_cursor += data_size;
            }

            //Console.WriteLine("Skeleton Sended at {0}:{1} [{2}]", _address, _port, timestamp);
        }

        public void SendPixels(byte[] pixels, ulong timestamp) {
            byte[] buffer;
            byte[] time = BitConverter.GetBytes((Int64)timestamp);

            int buffer_cursor = 0;
            int time_size = time.Length;

            while (pixels.Length - buffer_cursor > 0) {
                int buffer_size = _chunk_size;
                if (pixels.Length - buffer_cursor + time_size < _chunk_size) { buffer_size = pixels.Length - buffer_cursor + time_size; }
                int data_size = buffer_size - time_size;

                buffer = new byte[buffer_size];

                Array.Copy(time, 0, buffer, 0, time_size);
                Array.Copy(pixels, buffer_cursor, buffer, 0, data_size);
                //Console.WriteLine("Pixels [{0}/{1}] sended (buffer size: {2})", buffer_cursor, pixels.Length, buffer_size); 
                _client_pixels.Send(buffer, buffer_size);
                buffer_cursor += data_size;
            }

            //Console.WriteLine("Pixels Sended at {0}:{1} [{2}]", _address, _port, timestamp);
        }
    }
}
