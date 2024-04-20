// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;
using System.Security.Cryptography;
using System.Text;

namespace RhubarbGeekNz.Base64
{
    internal class PSCmdletByteStream : Stream
    {
        private readonly PSCmdlet cmdlet;
        private byte[] buffer;
        private int bufferLength;

        internal PSCmdletByteStream(PSCmdlet pscmdlet, int maxlen)
        {
            cmdlet = pscmdlet;
            buffer = new byte[maxlen];
            bufferLength = 0;
        }

        public override bool CanRead => throw new NotImplementedException();

        public override bool CanSeek => throw new NotImplementedException();

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            if (bufferLength > 0)
            {
                byte[] bytes = new byte[bufferLength];
                Buffer.BlockCopy(buffer, 0, bytes, 0, bufferLength);
                bufferLength = 0;
                cmdlet.WriteObject(bytes);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int len = this.buffer.Length - bufferLength;

                if (len > count)
                {
                    len = count;
                }

                Buffer.BlockCopy(buffer, offset, this.buffer, bufferLength, len);
                offset += len;
                count -= len;
                bufferLength += len;

                if (bufferLength == this.buffer.Length)
                {
                    byte[] bytes = this.buffer;
                    this.buffer = new byte[bytes.Length];
                    bufferLength = 0;
                    cmdlet.WriteObject(bytes);
                }
            }
        }
    }

    [Cmdlet(VerbsData.ConvertFrom, "Base64")]
    [OutputType(typeof(byte[]))]
    sealed public class ConvertFromBase64 : PSCmdlet, IDisposable
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, HelpMessage = "Base64 Data")]
        [AllowNull()]
        [AllowEmptyString()]
        public string InputString;

        [Parameter(Mandatory = false, HelpMessage = "Buffer Length")]
        public int Length = 4096;

        private Stream writer;
        private Encoding encoding = Encoding.ASCII;

        protected override void BeginProcessing()
        {
            if (Length > 0)
            {
                writer = new CryptoStream(new PSCmdletByteStream(this, Length), new FromBase64Transform(), CryptoStreamMode.Write);
            }
            else
            {
                WriteError(new ErrorRecord(new IndexOutOfRangeException("Length must be larger than zero"), "Length", ErrorCategory.InvalidArgument, null));
            }
        }

        protected override void ProcessRecord()
        {
            if (InputString != null && InputString.Length > 0)
            {
                try
                {
                    byte[] bytes = encoding.GetBytes(InputString);
                    writer.Write(bytes, 0, bytes.Length);
                }
                catch (FormatException ex)
                {
                    WriteError(new ErrorRecord(ex, ex.GetType().Name, ErrorCategory.InvalidData, null));
                }
            }
        }

        protected override void EndProcessing()
        {
            writer.Close();
        }

        public void Dispose()
        {
            IDisposable disposable = writer;
            writer = null;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
}
