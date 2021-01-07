using System;

namespace iuF
{
    class Program
    {
        static void Main()
        {
            Streamer streamer = new Streamer("127.0.0.1");
            Reader sensor = new Reader(streamer);
            sensor.Run();
        }
    }
}