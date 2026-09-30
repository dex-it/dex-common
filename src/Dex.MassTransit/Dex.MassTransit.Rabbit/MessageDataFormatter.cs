using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Dex.MassTransit.Rabbit;

/// <summary>
/// Готовит тело сообщения для записи в лог.
/// </summary>
internal static class MessageDataFormatter
{
    /// <remarks>
    /// Без ослабленного экранирования нелатинский текст уезжает в лог как <c>\uXXXX</c>: читать
    /// такую запись нельзя, и на каждый символ уходит втрое больше отведённого объёма.
    /// Экранирование кавычек и управляющих символов сохраняется, JSON остаётся разбираемым.
    /// </remarks>
    private static readonly JsonSerializerOptions Options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// Отдаёт тело сообщения в JSON, усечённое до <paramref name="limit"/> байт.
    /// </summary>
    /// <remarks>
    /// Сериализация идёт в буфер фиксированного размера, а не в строку: размер тела ничем не
    /// ограничен сверху. Вызов приходит из обработчика ошибки, где своё исключение подменило бы
    /// исходное, поэтому ни сбой сериализации, ни отсутствие тела наружу не выходят.
    /// </remarks>
    public static string Format<TMessage>(TMessage? message, int limit)
    {
        using var buffer = new BoundedBuffer(Math.Max(0, limit));

        try
        {
            JsonSerializer.Serialize(buffer, message, Options);
        }
        catch (BoundedBuffer.LimitReachedException)
        {
            // тело добрано до лимита, остаток графа обходить незачем
        }
        catch (Exception e)
        {
            return $"<not serialized: {e.GetType().Name}>";
        }

        return buffer.GetText();
    }

    /// <summary>
    /// Поток, принимающий не больше заданного числа байт.
    /// </summary>
    /// <remarks>
    /// На переполнении бросает <see cref="LimitReachedException"/>, чтобы сериализатор прекратил
    /// обход: размер графа ничем не ограничен, а всё после лимита всё равно отбрасывается.
    /// </remarks>
    private sealed class BoundedBuffer(int limit) : Stream
    {
        private readonly byte[] _buffer = new byte[limit];
        private int _length;
        private bool _truncated;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _length;

        public override long Position
        {
            get => _length;
            set => throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count) => Write(new ReadOnlySpan<byte>(buffer, offset, count));

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            var free = _buffer.Length - _length;

            if (buffer.Length > free)
            {
                _truncated = true;
                buffer = buffer[..free];
            }

            buffer.CopyTo(_buffer.AsSpan(_length));
            _length += buffer.Length;

            if (_truncated)
                throw new LimitReachedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        /// <summary>
        /// Отдаёт принятые байты текстом, помечая усечение.
        /// </summary>
        /// <remarks>
        /// Обрез по байтам разрывает многобайтовую последовательность UTF-8, поэтому незакрытый
        /// хвост отбрасывается: иначе на его месте оказывается символ замены.
        /// </remarks>
        public string GetText()
        {
            var length = _length;

            if (_truncated)
            {
                while (length > 0 && (_buffer[length - 1] & 0xC0) == 0x80)
                    length--;

                if (length > 0 && _buffer[length - 1] >= 0xC0)
                    length--;
            }

            return Encoding.UTF8.GetString(_buffer, 0, length) + (_truncated ? "..." : string.Empty);
        }

        /// <summary>
        /// Признак того, что буфер полон и писать больше некуда.
        /// </summary>
        internal sealed class LimitReachedException : Exception;
    }
}