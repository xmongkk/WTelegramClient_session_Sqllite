using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace WTelegramClient_session_Sqllite
{
  public class SqliteStore : Stream
    {
        private readonly SqliteConnection _sql;
        private readonly string _sessionName;
        private byte[] _data;
        private int _dataLen;
        private DateTime _lastWrite;
        private Task _delayedWrite;

        public SqliteStore(string databasePath, string sessionName = "DefaultSession")
        {
            _sessionName = sessionName;
            _sql = new SqliteConnection($"Data Source={databasePath};");
            _sql.Open();

            using (var create = new SqliteCommand("CREATE TABLE IF NOT EXISTS Sessions (name TEXT NOT NULL PRIMARY KEY, data BLOB)", _sql))
                create.ExecuteNonQuery();

            using var cmd = new SqliteCommand("SELECT data FROM Sessions WHERE name = @name", _sql);
            cmd.Parameters.AddWithValue("@name", _sessionName);
            using var rdr = cmd.ExecuteReader();
            if (rdr.Read())
                _dataLen = (_data = rdr["data"] as byte[])?.Length ?? 0;
        }

        protected override void Dispose(bool disposing)
        {
            _delayedWrite?.Wait();
            _sql.Dispose();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Array.Copy(_data, 0, buffer, offset, count);
            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _data = buffer;
            _dataLen = count;

            if (_delayedWrite != null) return;

            var left = 1000 - (int)(DateTime.UtcNow - _lastWrite).TotalMilliseconds;
            if (left < 0)
            {
                using var cmd = new SqliteCommand("INSERT OR REPLACE INTO Sessions (name, data) VALUES (@name, @data)", _sql);
                cmd.Parameters.AddWithValue("@name", _sessionName);
                cmd.Parameters.AddWithValue("@data", buffer.Length == count ? buffer : buffer[offset..(offset + count)]);
                cmd.ExecuteNonQuery();
                _lastWrite = DateTime.UtcNow;
            }
            else
            {
                _delayedWrite = Task.Delay(left).ContinueWith(t =>
                {
                    lock (this)
                    {
                        _delayedWrite = null;
                        Write(_data, 0, _dataLen);
                    }
                });
            }
        }

        public override long Length => _dataLen;
        public override long Position { get => 0; set { } }
        public override bool CanSeek => false;
        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override long Seek(long offset, SeekOrigin origin) => 0;
        public override void SetLength(long value) { }
        public override void Flush() { }
    }
}
